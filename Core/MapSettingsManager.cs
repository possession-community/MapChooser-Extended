using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;

namespace MapChooserExtended.Core
{
    /// <summary>
    /// Class for managing map settings
    /// </summary>
    public class MapSettingsManager : IPluginDependency<Plugin, Config>
    {
        private Plugin? _plugin;
        private readonly Dictionary<string, MapSettings> _mapSettingsCache = new();
        private MapSettings _defaultSettings = MapSettings.CreateDefault();
        private readonly static string _configDirectory = Path.Combine(Application.RootDirectory, "configs/plugins/MapChooserExtended");
        private string _mapsDirectory = Path.Combine(_configDirectory, "maps");
        private string _cfgsDirectory = Path.Combine(Server.GameDirectory, "csgo/cfg/MapChooserExtended/maps");
        private bool _isInitialized = false;
        private readonly string[] _ignoredMaps = ["default", "<empty>", "\u003Cempty\u003E"];

        /// <summary>
        /// Constructor
        /// </summary>
        public MapSettingsManager()
        {
        }

        /// <summary>
        /// Called when the plugin is loaded
        /// </summary>
        /// <param name="plugin">Plugin instance</param>
        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            //_mapsDirectory = Path.Combine(plugin.ModulePath, "maps");
            
            // Create maps directory if it doesn't exist
            if (!Directory.Exists(_mapsDirectory))
            {
                Directory.CreateDirectory(_mapsDirectory);
                Console.WriteLine($"[MCE] Created maps directory: {_mapsDirectory}");
            }

            // Create configs directory if it doesn't exist
            if (!Directory.Exists(_cfgsDirectory))
            {
                Directory.CreateDirectory(_cfgsDirectory);
                Console.WriteLine($"[MCE] Created configs directory: {_cfgsDirectory}");
            }

            // Create default settings file if it doesn't exist
            string defaultSettingsPath = Path.Combine(_mapsDirectory, "default.json");
            if (!File.Exists(defaultSettingsPath))
            {
                _defaultSettings.SaveToFile(defaultSettingsPath);
                Console.WriteLine($"[MCE] Created default settings file: {defaultSettingsPath}");
            }
            else
            {
                // Load default settings
                if (_defaultSettings.LoadFromFile(defaultSettingsPath))
                {
                    Console.WriteLine($"[MCE] Loaded default settings from: {defaultSettingsPath}");
                }
                else
                {
                    Console.WriteLine($"[MCE] Failed to load default settings from: {defaultSettingsPath}");
                }
            }

            // Load all map settings
            LoadAllMapSettings();
            _isInitialized = true;
        }

        /// <summary>
        /// Called when a map starts
        /// </summary>
        /// <param name="mapName">Map name</param>
        public void OnMapStart(string mapName)
        {
            if (!_isInitialized)
                return;

            // Delay applying map settings to ensure they are loaded
            _plugin!.AddTimer(0.1f, () =>
            {
                Server.NextFrame(() =>
                {
                    // Reload current map settings
                    ReloadMapSettings(mapName);

                    // Apply map settings
                    ApplyMapSettings(mapName);
                });
            });
        }

        /// <summary>
        /// Called when the config is parsed
        /// </summary>
        /// <param name="config">Config</param>
        public void OnConfigParsed(Config config)
        {
            // Apply config if needed
        }

        /// <summary>
        /// Load all map settings
        /// </summary>
        public void LoadAllMapSettings()
        {
            _mapSettingsCache.Clear();

            // Add default settings to cache
            _mapSettingsCache["default"] = _defaultSettings;

            // Exit if maps directory doesn't exist
            if (!Directory.Exists(_mapsDirectory))
                return;

            // Load all JSON files in the maps directory
            foreach (string filePath in Directory.GetFiles(_mapsDirectory, "*.json"))
            {
                string mapName = Path.GetFileNameWithoutExtension(filePath);
                if (_ignoredMaps.Contains(mapName))
                    continue; // Default settings already loaded

                MapSettings settings = new();
                if (settings.LoadFromFile(filePath))
                {
                    // Load base settings
                    if (!string.IsNullOrEmpty(settings.BasePath))
                    {
                        string basePath = Path.Combine(_mapsDirectory, settings.BasePath);
                        if (File.Exists(basePath))
                        {
                            MapSettings baseSettings = new();
                            if (baseSettings.LoadFromFile(basePath))
                            {
                                settings.MergeWithBase(baseSettings);
                            }
                        }
                    else if (settings.Settings == null)
                    {
                        // If base is not specified and Settings is null, use default.json
                        settings.MergeWithBase(_defaultSettings);
                        Console.WriteLine($"[MCE] Using default settings for {mapName} because base is not specified and Settings is null");
                    }
                    }

                    _mapSettingsCache[mapName] = settings;
                    // TODO: hide this to avoid spamming the console
                    // Console.WriteLine($"[MCE] Loaded map settings for: {mapName}");
                }
                else
                {
                    Console.WriteLine($"[MCE] Failed to load map settings for: {mapName}");
                }
            }

            Console.WriteLine($"[MCE] Loaded {_mapSettingsCache.Count} map settings");
        }

        /// <summary>
        /// Map is loaded or not via map name
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns>Map settings</returns>
        public bool IsLoadedMapName(string mapName)
        {
            // Return true if loaded
            if (_mapSettingsCache.ContainsKey(mapName))
                return true;

            return false;
        }

        /// <summary>
        /// Map is loaded or not via workshop id
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns>Map settings</returns>
        public bool IsLoadedMapId(string workshopId)
        {
            // Return true if loaded
            if (_mapSettingsCache.Where(x => x.Value.Meta.WorkshopId == workshopId).Any())
                return true;

            return false;
        }

        /// <summary>
        /// Get map settings
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns>Map settings</returns>
        public MapSettings GetMapSettings(string mapName)
        {
            // Return cached settings if available
            if (_mapSettingsCache.TryGetValue(mapName, out MapSettings? settings))
                return settings;

            // Load settings file if it exists
            string filePath = Path.Combine(_mapsDirectory, $"{mapName}.json");
            if (File.Exists(filePath))
            {
                MapSettings newSettings = new();
                if (newSettings.LoadFromFile(filePath))
                {
                    // Load base settings
                    if (!string.IsNullOrEmpty(newSettings.BasePath))
                    {
                        string basePath = Path.Combine(_mapsDirectory, newSettings.BasePath);
                        if (File.Exists(basePath))
                        {
                            MapSettings baseSettings = new();
                            if (baseSettings.LoadFromFile(basePath))
                            {
                                newSettings.MergeWithBase(baseSettings);
                            }
                        }
                    else if (newSettings.Settings == null)
                    {
                        // If base is not specified and Settings is null, use default.json
                        newSettings.MergeWithBase(_defaultSettings);
                        Console.WriteLine($"[MCE] Using default settings for {mapName} because base is not specified and Settings is null");
                    }
                    }

                    _mapSettingsCache[mapName] = newSettings;
                    return newSettings;
                }
            }

            // If map settings don't exist, create them
            if (!_ignoredMaps.Contains(mapName))
            {
                if (CreateMapSettingsFile(mapName))
                {
                    Console.WriteLine($"[MCE] Auto-created map settings for: {mapName}");
                    return GetMapSettings(mapName); // Recursive call to load the newly created settings
                }
            }
            
            return _defaultSettings;
        }

        /// <summary>
        /// Check if a map is available for the cycle
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns>Whether the map is available</returns>
        public bool IsMapAvailableForCycle(string mapName)
        {
            MapSettings settings = GetMapSettings(mapName);
            // Check if the map is available based on cooldown
            if (settings.Settings.Cooldown.CurrentCount > 0)
                return false;
                
            return settings.Settings.Enabled && 
                   IsMapAvailableByTime(settings) && 
                   IsMapAvailableByPlayerCount(settings);
        }

        /// <summary>
        /// Check if a map is available based on time
        /// </summary>
        /// <param name="settings">Map settings</param>
        /// <returns>Whether the map is available based on time</returns>
        private bool IsMapAvailableByTime(MapSettings settings)
        {
            int currentHour = DateTime.Now.Hour;
            return (settings.Settings.Times.Count() == 0) || settings.Settings.Times.Contains(currentHour);
        }

        /// <summary>
        /// Check if a map is available based on player count
        /// </summary>
        /// <param name="settings">Map settings</param>
        /// <returns>Whether the map is available based on player count</returns>
        private bool IsMapAvailableByPlayerCount(MapSettings settings)
        {
            int playerCount = ServerManager.ValidPlayerCount();
            return playerCount >= settings.Settings.Players.Min && 
                   playerCount <= settings.Settings.Players.Max;
        }

        /// <summary>
        /// Check if a map is available for the nomination
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns>Whether the map is available</returns>
        public bool IsMapAvailableForNomination(CCSPlayerController player, string mapName)
        {
            MapSettings settings = GetMapSettings(mapName);
            bool playerIsAdmin = AdminManager.PlayerHasPermissions(player, ["@css/changemap"]);
            if (playerIsAdmin) return true;

            return !settings.Settings.Nomination.Admin && settings.Settings.Enabled && settings.Settings.Nomination.Enabled;
        }

        /// <summary>
        /// Reload map settings
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns>Whether the reload was successful</returns>
        public bool ReloadMapSettings(string mapName)
        {
            // Load settings file if it exists
            string filePath = Path.Combine(_mapsDirectory, $"{mapName}.json");
            if (File.Exists(filePath))
            {
                MapSettings settings = new();
                if (settings.LoadFromFile(filePath))
                {
                    // Load base settings
                    if (!string.IsNullOrEmpty(settings.BasePath))
                    {
                        string basePath = Path.Combine(_mapsDirectory, settings.BasePath);
                        if (File.Exists(basePath))
                        {
                            MapSettings baseSettings = new();
                            if (baseSettings.LoadFromFile(basePath))
                            {
                                settings.MergeWithBase(baseSettings);
                            }
                        }
                    else if (settings.Settings == null)
                    {
                        // If base is not specified and Settings is null, use default.json
                        settings.MergeWithBase(_defaultSettings);
                        Console.WriteLine($"[MCE] Using default settings for {mapName} because base is not specified and Settings is null");
                    }
                    }

                    _mapSettingsCache[mapName] = settings;
                    Console.WriteLine($"[MCE] Reloaded map settings for: {mapName}");
                    return true;
                }
            }

            // Remove from cache if settings don't exist
            if (_mapSettingsCache.ContainsKey(mapName))
            {
                _mapSettingsCache.Remove(mapName);
                Console.WriteLine($"[MCE] Removed map settings for: {mapName}");
            }

            return false;
        }

        /// <summary>
        /// Apply map settings
        /// </summary>
        /// <param name="mapName">Map name</param>
        private void ApplyMapSettings(string mapName)
        {
            if (_plugin == null)
                return;

            MapSettings settings = GetMapSettings(mapName);

            // Execute additional cfg files if specified
            if (settings.Settings.Cfgs != null && settings.Settings.Cfgs.Length > 0)
            {
                foreach (var cfg in settings.Settings.Cfgs)
                {
                    string cfgPath = Path.Combine("MapChooserExtended/maps", $"{cfg}.cfg");

                    // TODO: File check? or logging?
                    Server.ExecuteCommand($"exec {cfgPath}");
                }
            }

            // Apply match settings after cfgs
            if (settings.Settings.Match.Type == 0)
            {
                // Time limit
                Server.ExecuteCommand($"mp_timelimit {settings.Settings.Match.Limit}");
                Server.ExecuteCommand($"mp_maxrounds 0");
                Console.WriteLine($"[MCE] Set mp_timelimit to {settings.Settings.Match.Limit}");
            }
            else if (settings.Settings.Match.Type == 1)
            {
                // Round limit
                Server.ExecuteCommand($"mp_maxrounds {settings.Settings.Match.Limit}");
                Server.ExecuteCommand($"mp_timelimit 0");
                Console.WriteLine($"[MCE] Set mp_maxrounds to {settings.Settings.Match.Limit}");
            }
        }

        /// <summary>
        /// Create a map settings file
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns>Whether the creation was successful</returns>
        public bool CreateMapSettingsFile(string mapName)
        {
            // Don't create if settings file already exists
            string filePath = Path.Combine(_mapsDirectory, $"{mapName}.json");
            if (File.Exists(filePath))
                return false;

            // Create map settings based on default settings
            MapSettings settings = new()
            {
                BasePath = "default.json",
                Meta = new MapMeta
                {
                    Name = mapName,
                    DisplayName = "",
                    WorkshopId = ""
                },
                Settings = new MapCycleSettings()
            };

            // Save map settings file
            if (settings.SaveToFile(filePath))
            {
                Console.WriteLine($"[MCE] Created map settings file for: {mapName}");
                _mapSettingsCache[mapName] = settings;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get a list of available maps
        /// </summary>
        /// <returns>List of available maps</returns>
        public List<string> GetAvailableMaps()
        {
            List<string> availableMaps = new();

            foreach (var kvp in _mapSettingsCache)
            {
                if (_ignoredMaps.Contains(kvp.Key))
                    continue;

                if (IsMapAvailableForCycle(kvp.Key))
                    availableMaps.Add(kvp.Key);
            }

            return availableMaps;
        }

        /// <summary>
        /// Get a list of all maps, ignoring cycle conditions
        /// </summary>
        /// <returns>List of all maps</returns>
        public List<string> GetAllMaps()
        {
            List<string> allMaps = new();

            foreach (var kvp in _mapSettingsCache)
            {
                if (_ignoredMaps.Contains(kvp.Key))
                    continue;

                allMaps.Add(kvp.Key);
            }

            return allMaps;
        }

        /// <summary>
        /// Get map meta information
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns>Map meta information</returns>
        public MapMeta GetMapMeta(string mapName)
        {
            MapSettings settings = GetMapSettings(mapName);
            return settings.Meta;
        }
        
        /// <summary>
        /// Get the maps directory path
        /// </summary>
        /// <returns>Maps directory path</returns>
        public string GetMapsDirectory()
        {
            return _mapsDirectory;
        }
    }
}