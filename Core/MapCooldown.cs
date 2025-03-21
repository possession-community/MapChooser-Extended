using CounterStrikeSharp.API;
using cs2_rockthevote.Core;

namespace cs2_rockthevote.Core
{
    public class MapCooldown : IPluginDependency<Plugin, Config>
    {
        private readonly Dictionary<string, int> _mapsOnCoolDown = new();
        private readonly Dictionary<string, HashSet<string>> _taggedMaps = new();
        private ushort _defaultCoolDown = 0;
        private readonly MapSettingsManager _mapSettingsManager;
        private readonly MapLister _mapLister;

        public event EventHandler<Map[]>? EventCooldownRefreshed;

        public MapCooldown(MapSettingsManager mapSettingsManager, MapLister mapLister)
        {
            _mapSettingsManager = mapSettingsManager;
            _mapLister = mapLister;

            // Subscribe to map loaded event to update cooldowns
            _mapLister.EventMapsLoaded += (e, maps) =>
            {
                var map = Server.MapName;
                if (map is not null)
                {
                    UpdateCooldowns(map.Trim().ToLower());
                    EventCooldownRefreshed?.Invoke(this, maps);
                }
            };
        }

        public void OnConfigParsed(Config config)
        {
            _defaultCoolDown = config.MapsInCoolDown;
        }

        public void OnLoad(Plugin plugin)
        {
            // Initialize tag-based map grouping
            InitializeTaggedMaps();
        }

        public void OnMapStart(string mapName)
        {
            // Update cooldowns when map changes
            UpdateCooldowns(mapName.Trim().ToLower());
        }

        /// <summary>
        /// Initialize tag-based map grouping
        /// </summary>
        private void InitializeTaggedMaps()
        {
            _taggedMaps.Clear();

            // Group maps by tags
            foreach (var mapName in _mapSettingsManager.GetAvailableMaps())
            {
                var settings = _mapSettingsManager.GetMapSettings(mapName);
                foreach (var tag in settings.Settings.Cooldown.Tags)
                {
                    if (!_taggedMaps.ContainsKey(tag))
                    {
                        _taggedMaps[tag] = new HashSet<string>();
                    }
                    _taggedMaps[tag].Add(mapName);
                }
            }

            Console.WriteLine($"[RockTheVote] Initialized {_taggedMaps.Count} tag groups for cooldown");
        }

        /// <summary>
        /// Update cooldowns when a map is played
        /// </summary>
        /// <param name="mapName">Map name</param>
        private void UpdateCooldowns(string mapName)
        {
            // Get map settings for the current map
            var settings = _mapSettingsManager.GetMapSettings(mapName);

            // Add the current map to cooldown in memory
            _mapsOnCoolDown[mapName] = settings.Settings.Cooldown.Count;
            
            // Reset CurrentCount for the current map to Count
            settings.Settings.Cooldown.CurrentCount = settings.Settings.Cooldown.Count;
            
            // Save changes to file
            string currentMapFilePath = Path.Combine(_mapSettingsManager.GetMapsDirectory(), $"{mapName}.json");
            settings.SaveToFile(currentMapFilePath);

            // Process maps with the same tags
            foreach (var tag in settings.Settings.Cooldown.Tags)
            {
                if (_taggedMaps.TryGetValue(tag, out var taggedMaps))
                {
                    foreach (var taggedMap in taggedMaps)
                    {
                        if (taggedMap != mapName) // Skip the current map
                        {
                            var taggedMapSettings = _mapSettingsManager.GetMapSettings(taggedMap);
                            _mapsOnCoolDown[taggedMap] = taggedMapSettings.Settings.Cooldown.Count;
                            
                            // Reset CurrentCount for tagged maps
                            taggedMapSettings.Settings.Cooldown.CurrentCount = taggedMapSettings.Settings.Cooldown.Count;
                            string taggedMapFilePath = Path.Combine(_mapSettingsManager.GetMapsDirectory(), $"{taggedMap}.json");
                            taggedMapSettings.SaveToFile(taggedMapFilePath);
                        }
                    }
                }
            }

            // Remove maps that have completed their cooldown
            var mapsToRemove = _mapsOnCoolDown.Where(x => x.Value <= 0).Select(x => x.Key).ToList();
            foreach (var map in mapsToRemove)
            {
                _mapsOnCoolDown.Remove(map);
            }

            // Decrement cooldown for all maps in memory
            foreach (var key in _mapsOnCoolDown.Keys.ToList())
            {
                _mapsOnCoolDown[key]--;
            }
            
            // Decrement CurrentCount for all maps except the current map and tagged maps
            foreach (var availableMap in _mapSettingsManager.GetAvailableMaps())
            {
                if (availableMap != mapName && !settings.Settings.Cooldown.Tags.Any(tag => _taggedMaps.ContainsKey(tag) && _taggedMaps[tag].Contains(availableMap)))
                {
                    var mapSettings = _mapSettingsManager.GetMapSettings(availableMap);
                    mapSettings.Settings.Cooldown.CurrentCount = Math.Max(0, mapSettings.Settings.Cooldown.CurrentCount - 1);
                    mapSettings.SaveToFile(Path.Combine(_mapSettingsManager.GetMapsDirectory(), $"{availableMap}.json"));
                }
            }

            Console.WriteLine($"[RockTheVote] Updated cooldowns: {_mapsOnCoolDown.Count} maps in cooldown");
        }

        /// <summary>
        /// Check if a map is in cooldown
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns>Whether the map is in cooldown</returns>
        public bool IsMapInCooldown(string mapName)
        {
            return _mapsOnCoolDown.ContainsKey(mapName.ToLower()) && _mapsOnCoolDown[mapName.ToLower()] > 0;
        }

        /// <summary>
        /// Get the remaining cooldown for a map
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns>Remaining cooldown (0 if not in cooldown)</returns>
        public int GetRemainingCooldown(string mapName)
        {
            if (_mapsOnCoolDown.TryGetValue(mapName.ToLower(), out var cooldown))
            {
                return cooldown;
            }
            return 0;
        }

        /// <summary>
        /// Get all maps currently in cooldown
        /// </summary>
        /// <returns>Dictionary of map names and their remaining cooldown</returns>
        public Dictionary<string, int> GetMapsInCooldown()
        {
            return new Dictionary<string, int>(_mapsOnCoolDown);
        }
    }
}
