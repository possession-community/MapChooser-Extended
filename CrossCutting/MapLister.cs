using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using MapChooserExtended.Core;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace MapChooserExtended
{
    public class MapLister : IPluginDependency<Plugin, Config>
    {
        public Map[]? Maps { get; private set; } = null;
        public bool MapsLoaded { get; private set; } = false;
        public Map[]? AllMaps { get; private set; } = null; // All maps ignoring cycle conditions, for admin commands
        private Timer? _updateTimer = null; // Timer for updating maps

        public event EventHandler<Map[]>? EventMapsLoaded;

        private Plugin? _plugin;
        private readonly MapSettingsManager _mapSettingsManager;
        private readonly MapCooldown _mapCooldown;
        private const float UPDATE_INTERVAL = 1.0f; // Update interval in seconds

        public MapLister(MapSettingsManager mapSettingsManager, MapCooldown mapCooldown)
        {
            _mapSettingsManager = mapSettingsManager;
            _mapCooldown = mapCooldown;

            EventMapsLoaded += _mapCooldown.OnMapsLoaded;
        }

        public void Clear()
        {
            MapsLoaded = false;
            Maps = null;
        }

        public void LoadMaps()
        {
            Clear();

            // Get available maps from MapSettingsManager
            var availableMaps = _mapSettingsManager.GetAvailableMaps(); // Maps that meet cycle conditions
            
            // Get all maps ignoring cycle conditions for admin commands
            var allMapsNames = _mapSettingsManager.GetAllMaps();
            
            // Convert to Map objects for admin commands
            AllMaps = allMapsNames
                .Select(mapName => {
                    var meta = _mapSettingsManager.GetMapMeta(mapName);
                    return new Map(
                        meta.Name, 
                        !string.IsNullOrEmpty(meta.WorkshopId) ? meta.WorkshopId : null
                    );
                }).ToArray();

            // Convert to Map objects
            Maps = availableMaps
                .Select(mapName => {
                    var meta = _mapSettingsManager.GetMapMeta(mapName);
                    return new Map(
                        meta.Name, 
                        !string.IsNullOrEmpty(meta.WorkshopId) ? meta.WorkshopId : null
                    );
                })
                .ToArray();

            MapsLoaded = true;

            _mapCooldown.SetAllMaps(AllMaps!);
            
            if (EventMapsLoaded is not null)
                EventMapsLoaded.Invoke(this, Maps!);
            
            Console.WriteLine($"[MCE] Loaded {Maps.Length} maps from settings");
        }

        public void OnMapStart(string mapName)
        {
            if (_plugin is not null)
            {
                // Initial load of maps
                LoadMaps();
                
                // Start timer to update maps periodically
                StartUpdateTimer();
            }
        }
        
        // Start timer to update maps periodically
        private void StartUpdateTimer()
        {
            // Clear existing timer if it exists
            if (_updateTimer != null)
            {
                _updateTimer.Kill();
                _updateTimer = null;
            }

            // Create new timer to update maps every UPDATE_INTERVAL seconds
            _updateTimer = _plugin!.AddTimer(UPDATE_INTERVAL, () =>
            {
                UpdateMaps();
            }, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
            
            Console.WriteLine($"[MCE] Started map update timer with interval {UPDATE_INTERVAL} seconds");
        }
        
        // Update maps without clearing the existing maps
        private void UpdateMaps()
        {
            if (_plugin is null)
                return;
                
            // Get available maps from MapSettingsManager
            var availableMaps = _mapSettingsManager.GetAvailableMaps(); // Maps that meet cycle conditions
            
            // Get all maps ignoring cycle conditions for admin commands
            var allMapsNames = _mapSettingsManager.GetAllMaps();
            
            // Convert to Map objects for admin commands
            AllMaps = allMapsNames
                .Select(mapName => {
                    var meta = _mapSettingsManager.GetMapMeta(mapName);
                    return new Map(
                        meta.Name, 
                        !string.IsNullOrEmpty(meta.WorkshopId) ? meta.WorkshopId : null
                    );
                }).ToArray();

            // Convert to Map objects
            // Filter out maps that are in cooldown
            Maps = availableMaps
                .Where(mapName => !_mapCooldown.IsMapInCooldown(mapName))
                .Select(mapName => {
                    var meta = _mapSettingsManager.GetMapMeta(mapName);
                    return new Map(
                        meta.Name,
                        !string.IsNullOrEmpty(meta.WorkshopId) ? meta.WorkshopId : null
                    );
                })
                .ToArray();
                
            // Trigger event to notify other components that maps have been updated
            //if (Maps.Length > 0 && EventMapsLoaded is not null)
            //    EventMapsLoaded.Invoke(this, Maps!);
        }

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            LoadMaps();
        }

        public void OnConfigParsed(Config config)
        {
            // Nothing to do here
        }

        // Returns "" if there's no matching
        // If there's more than one matching name, list all the matching names for players to choose
        // Otherwise, returns the matching name
        public string GetSingleMatchingMapName(string map, CCSPlayerController player, StringLocalizer _localizer)
        {
            Map[]? mapsToSearch = AllMaps;

            if (mapsToSearch == null)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.invalid-map"));
                return "";
            }

            // Original implementation
            if (mapsToSearch.Select(x => x.Name).FirstOrDefault(x => x == map) is not null)
                return map;

            var matchingMaps = mapsToSearch
                .Select(x => x.Name)
                .Where(x => x.Contains(map))
                .ToList();

            if (matchingMaps.Count == 0)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.invalid-map"));
                return "";
            }
            else if (matchingMaps.Count > 1)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("nominate.multiple-maps-containing-name"));
                player!.PrintToChat(string.Join(", ", matchingMaps));
                return "";
            }

            return matchingMaps[0];
        }

        public IEnumerable<Map> GetMaps()
        {
            return Maps ?? Enumerable.Empty<Map>();
        }
        
        // Get all maps ignoring cycle conditions, for admin commands
        public IEnumerable<Map> GetAllMaps()
        {
            return AllMaps ?? Enumerable.Empty<Map>();
        }
    }
}
