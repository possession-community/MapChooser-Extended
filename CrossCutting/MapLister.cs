using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using cs2_rockthevote.Core;

namespace cs2_rockthevote
{
    public class MapLister : IPluginDependency<Plugin, Config>
    {
        public Map[]? Maps { get; private set; } = null;
        public bool MapsLoaded { get; private set; } = false;

        public event EventHandler<Map[]>? EventMapsLoaded;

        private Plugin? _plugin;
        private readonly MapSettingsManager _mapSettingsManager;

        public MapLister(MapSettingsManager mapSettingsManager)
        {
            _mapSettingsManager = mapSettingsManager;
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
            var availableMaps = _mapSettingsManager.GetAvailableMaps();
            
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
            if (EventMapsLoaded is not null)
                EventMapsLoaded.Invoke(this, Maps!);
            
            Console.WriteLine($"[RockTheVote] Loaded {Maps.Length} maps from settings");
        }

        public void OnMapStart(string mapName)
        {
            if (_plugin is not null)
                LoadMaps();
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
            if (this.Maps!.Select(x => x.Name).FirstOrDefault(x => x.ToLower() == map) is not null)
                return map;

            var matchingMaps = this.Maps!
                .Select(x => x.Name)
                .Where(x => x.ToLower().Contains(map.ToLower()))
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
    }
}
