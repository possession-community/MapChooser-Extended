﻿using CounterStrikeSharp.API;
using MapChooserExtended.Core;

namespace MapChooserExtended.Core
{
    public class MapCooldown : IPluginDependency<Plugin, Config>
    {
        private readonly Dictionary<string, int> _mapsOnCoolDown = new();
        private readonly Dictionary<string, HashSet<string>> _taggedMaps = new();
        private readonly MapSettingsManager _mapSettingsManager;
        private Map[]? _allMaps = null;

        public event EventHandler<Map[]>? EventCooldownRefreshed;

        public MapCooldown(MapSettingsManager mapSettingsManager)
        {
            _mapSettingsManager = mapSettingsManager;
        }

        public void OnConfigParsed(Config config)
        {
        }

        public void OnLoad(Plugin plugin)
        {
            // Initialize tag-based map grouping
            InitializeTaggedMaps();
        }

        public void OnMapStart(string mapName)
        {
            // Update cooldowns when map changes
            UpdateCooldowns(mapName.Trim());
        }

        /// <summary>
        /// Initialize tag-based map grouping
        /// </summary>
        private void InitializeTaggedMaps()
        {
            _taggedMaps.Clear();

            // Group maps by tags - use AllMaps from MapLister to include all maps
            foreach (var map in _allMaps ?? Array.Empty<Map>())
            {
                var mapName = map.Name;
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

            Console.WriteLine($"[MCE] Initialized {_taggedMaps.Count} tag groups for cooldown");
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
            // Use AllMaps from MapLister to include all maps
            foreach (var map in _allMaps ?? Array.Empty<Map>())
            {
                var availableMap = map.Name;
                if (availableMap != mapName && !settings.Settings.Cooldown.Tags.Any(tag => _taggedMaps.ContainsKey(tag) && _taggedMaps[tag].Contains(availableMap)))
                {
                    var mapSettings = _mapSettingsManager.GetMapSettings(availableMap);
                    mapSettings.Settings.Cooldown.CurrentCount = Math.Max(0, mapSettings.Settings.Cooldown.CurrentCount - 1);
                    mapSettings.SaveToFile(Path.Combine(_mapSettingsManager.GetMapsDirectory(), $"{availableMap}.json"));
                }
            }

            Console.WriteLine($"[MCE] Updated cooldowns: {_mapsOnCoolDown.Count} maps in cooldown");
        }

        /// <summary>
        /// Check if a map is in cooldown
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns>Whether the map is in cooldown</returns>
        public bool IsMapInCooldown(string mapName)
        {
            return _mapsOnCoolDown.ContainsKey(mapName) && _mapsOnCoolDown[mapName] > 0;
        }

        /// <summary>
        /// Get the remaining cooldown for a map
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns>Remaining cooldown (0 if not in cooldown)</returns>
        public int GetRemainingCooldown(string mapName)
        {
            if (_mapsOnCoolDown.TryGetValue(mapName, out var cooldown))
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

        public void SetAllMaps(Map[] maps)
        {
            _allMaps = maps;

            InitializeTaggedMaps();
        }

        public void OnMapsLoaded(object? sender, Map[] maps)
        {
            _allMaps = maps;
            
            var map = Server.MapName;
            if (map is not null)
            {
                UpdateCooldowns(map.Trim());
                EventCooldownRefreshed?.Invoke(this, maps);
            }
        }
    }
}
