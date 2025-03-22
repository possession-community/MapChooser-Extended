using System.Net.Http;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using MapChooserExtended.Core;

namespace MapChooserExtended
{
    /// <summary>
    /// Class for synchronizing maps from Steam Workshop collections
    /// </summary>
    public class WorkshopSynchronizer : IPluginDependency<Plugin, Config>
    {
        private readonly MapSettingsManager _mapSettingsManager;
        private readonly MapLister _mapLister;
        private readonly HttpClient _httpClient;
        private Plugin? _plugin;
        private Config? _config;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mapSettingsManager">Map settings manager</param>
        public WorkshopSynchronizer(MapSettingsManager mapSettingsManager, MapLister mapLister)
        {
            _mapSettingsManager = mapSettingsManager;
            _mapLister = mapLister;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Called when the plugin is loaded
        /// </summary>
        /// <param name="plugin">Plugin instance</param>
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            
            // Sync workshop collections if config is available
            if (_config != null && _config.Workshop.collection_ids.Length > 0)
            {
                SyncWorkshopCollections(_config.Workshop.collection_ids);
            }
        }

        /// <summary>
        /// Called when the config is parsed
        /// </summary>
        /// <param name="config">Config</param>
        public void OnConfigParsed(Config config)
        {
            _config = config;
            
            // Sync workshop collections if plugin is loaded
            if (_plugin != null && _config.Workshop.collection_ids.Length > 0)
            {
                SyncWorkshopCollections(_config.Workshop.collection_ids);
            }
        }

        /// <summary>
        /// Called when a map starts
        /// </summary>
        /// <param name="mapName">Map name</param>
        public void OnMapStart(string mapName)
        {
            // Check if the current map is from a workshop
            UpdateMapNameIfWorkshop();
        }

        /// <summary>
        /// Sync workshop collections
        /// </summary>
        /// <param name="collectionIds">Collection IDs</param>
        private void SyncWorkshopCollections(string[] collectionIds)
        {
            if (collectionIds.Length == 0)
                return;

            Console.WriteLine($"[WorkshopSynchronizer] Syncing {collectionIds.Length} workshop collections");
            
            foreach (string collectionId in collectionIds)
            {
                Task.Run(async () => await SyncWorkshopCollectionAsync(collectionId));
            }
        }

        /// <summary>
        /// Sync a workshop collection asynchronously
        /// </summary>
        /// <param name="collectionId">Collection ID</param>
        /// <returns>Task</returns>
        private async Task<int> SyncWorkshopCollectionAsync(string collectionId)
        {
            try
            {
                string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={collectionId}";
                Console.WriteLine($"[WorkshopSynchronizer] Fetching Workshop collection: {url}");
                
                string pageSource;
                using (var response = await _httpClient.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();
                    pageSource = await response.Content.ReadAsStringAsync();
                }
                
                // Regular expression to extract workshop IDs and map names
                var pattern = new Regex(@"<a href=""https://steamcommunity.com/sharedfiles/filedetails/\?id=(\d+)"">.*?<div class=""workshopItemTitle"">(.*?)</div>", RegexOptions.Singleline);
                
                // Find all matches
                var matches = pattern.Matches(pageSource);
                
                if (matches.Count == 0)
                {
                    Console.WriteLine($"[WorkshopSynchronizer] Warning: No maps found in Workshop collection {collectionId}");
                    return 0;
                }
                
                Console.WriteLine($"[WorkshopSynchronizer] Found {matches.Count} maps in Workshop collection {collectionId}");
                
                int newMapsAdded = 0;
                
                // Process matches in the main thread
                Server.NextFrame(() => {
                    foreach (Match match in matches)
                    {
                        string workshopId = match.Groups[1].Value;
                        string mapName = match.Groups[2].Value.Trim();
                        
                        // Create a valid map name (lowercase, no spaces, etc.)
                        string validMapName = CreateValidMapName(mapName);

                        // Skip if the map already exists in map settings
                        if (_mapSettingsManager.IsLoadedMapName(validMapName) || _mapSettingsManager.IsLoadedMapId(workshopId)) {
                            //Console.WriteLine($"[WorkshopSynchronizer] Map {mapName} (ID: {workshopId}) already exists in map settings");
                            continue;
                        };

                        // Create map settings
                        MapSettings settings = new()
                        {
                            BasePath = "default.json",
                            Meta = new MapMeta
                            {
                                Name = validMapName,
                                DisplayName = mapName,
                                WorkshopId = workshopId
                            },
                            Settings = new MapCycleSettings()
                        };
                        
                        // Save map settings
                        if (settings.SaveToFile(Path.Combine(_mapSettingsManager.GetMapsDirectory(), $"{validMapName}.json")))
                        {
                            Console.WriteLine($"[WorkshopSynchronizer] Created map settings for workshop map: {mapName} (ID: {workshopId})");
                            newMapsAdded++;
                        }
                        else
                        {
                            Console.WriteLine($"[WorkshopSynchronizer] Error: Failed to create map settings for workshop map: {mapName} (ID: {workshopId})");
                        }
                    }
                    
                    // Reload map settings
                    _mapSettingsManager.LoadAllMapSettings();
                    _mapLister.LoadMaps();
                    
                    Console.WriteLine($"[WorkshopSynchronizer] Added {newMapsAdded} new maps from Workshop collection {collectionId}");
                });
                
                return newMapsAdded;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorkshopSynchronizer] Error syncing Workshop collection {collectionId}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Create a valid map name from a workshop title
        /// </summary>
        /// <param name="workshopTitle">Workshop title</param>
        /// <returns>Valid map name</returns>
        private string CreateValidMapName(string workshopTitle)
        {
            // Remove invalid characters and convert to lowercase
            string validName = Regex.Replace(workshopTitle, @"[^a-zA-Z0-9_]", "_").ToLower();
            
            // Ensure the name starts with a letter
            if (!char.IsLetter(validName[0]))
                validName = "map_" + validName;
            
            return validName;
        }

        /// <summary>
        /// Update map name if it's a workshop map
        /// </summary>
        /// <param name="mapName">Map name</param>
        private void UpdateMapNameIfWorkshop()
        {
            try
            {
                // Get current map name
                string currentMapName = Server.MapName;
                
                // Get workshop ID for the current map
                var forceFullUpdate = new ForceFullUpdate();
                string? workshopId = forceFullUpdate.GetAddonID();
                
                if (string.IsNullOrEmpty(workshopId))
                    return; // Not a workshop map
                
                Console.WriteLine($"[WorkshopSynchronizer] Current map is from workshop. Map: {currentMapName}, Workshop ID: {workshopId}");
                
                // Find map settings with this workshop ID
                string mapsDirectory = _mapSettingsManager.GetMapsDirectory();
                foreach (string filePath in Directory.GetFiles(mapsDirectory, "*.json"))
                {
                    string settingsMapName = Path.GetFileNameWithoutExtension(filePath);
                    
                    // Skip default settings
                    if (settingsMapName == "default")
                        continue;
                    
                    // Load map settings
                    MapSettings settings = new();
                    if (settings.LoadFromFile(filePath) && 
                        settings.Meta.WorkshopId == workshopId && 
                        settings.Meta.Name != currentMapName)
                    {
                        // Update map name in settings
                        string oldName = settings.Meta.Name;
                        settings.Meta.Name = currentMapName;
                        
                        // Save updated settings
                        if (settings.SaveToFile(filePath))
                        {
                            Console.WriteLine($"[WorkshopSynchronizer] Updated map name in settings from {oldName} to {currentMapName}");
                            
                            // Rename the file
                            string newFilePath = Path.Combine(mapsDirectory, $"{currentMapName}.json");
                            if (filePath != newFilePath)
                            {
                                File.Move(filePath, newFilePath, true);
                                Console.WriteLine($"[WorkshopSynchronizer] Renamed settings file from {filePath} to {newFilePath}");
                            }
                            
                            // Reload map settings
                            _mapSettingsManager.LoadAllMapSettings();
                            _mapLister.LoadMaps();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WorkshopSynchronizer] Error updating workshop map name: {ex.Message}");
            }
        }
    }
}