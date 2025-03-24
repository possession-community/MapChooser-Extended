using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using MapChooserExtended.Core;

namespace MapChooserExtended
{
    public partial class Plugin
    {
        [GameEventHandler(HookMode.Post)]
        public HookResult OnRoundEndMapChanger(EventRoundEnd @event, GameEventInfo info)
        {
            _changeMapManager.ChangeNextMap();
            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnRoundStartMapChanger(EventRoundStart @event, GameEventInfo info)
        {
            _changeMapManager.ChangeNextMap();
            return HookResult.Continue;
        }
    }

    public class ChangeMapManager : IPluginDependency<Plugin, Config>
    {
        private Plugin? _plugin;
        private readonly StringLocalizer _localizer;
        private readonly PluginState _pluginState;
        private readonly MapLister _mapLister;
        private readonly MapSettingsManager _mapSettingsManager;
        private readonly MapCooldown _mapCooldown;

        public string? NextMap { get; private set; } = null;
        private string _prefix = DEFAULT_PREFIX;
        private const string DEFAULT_PREFIX = "rtv.prefix";
        private bool _mapEnd = false;

        private Map[] _maps = Array.Empty<Map>();
        private Config? _config;

        public ChangeMapManager(
            StringLocalizer localizer, 
            PluginState pluginState, 
            MapLister mapLister,
            MapSettingsManager mapSettingsManager,
            MapCooldown mapCooldown)
        {
            _localizer = localizer;
            _pluginState = pluginState;
            _mapLister = mapLister;
            _mapSettingsManager = mapSettingsManager;
            _mapCooldown = mapCooldown;
            _mapLister.EventMapsLoaded += OnMapsLoaded;
        }

        public void OnMapsLoaded(object? sender, Map[] maps)
        {
            _maps = maps;
        }

        public void ScheduleMapChange(string map, bool mapEnd = false, string prefix = DEFAULT_PREFIX)
        {
            // Admin commands should ignore cycle conditions
            NextMap = map;

            _prefix = prefix;
            _pluginState.MapChangeScheduled = true;
            _mapEnd = mapEnd;
        }

        public void OnMapStart(string mapName)
        {
            NextMap = null;
            _prefix = DEFAULT_PREFIX;
        }

        public bool ChangeNextMap(bool mapEnd = false)
        {
            if (mapEnd != _mapEnd)
                return false;

            if (!_pluginState.MapChangeScheduled || NextMap == null)
                return false;

            _pluginState.MapChangeScheduled = false;
            Server.PrintToChatAll(_localizer.LocalizeWithPrefixInternal(_prefix, "general.changing-map", NextMap));
            _plugin!.AddTimer(3.0F, () =>
            {
                Map? map = _maps.FirstOrDefault(x => x.Name == NextMap);
                if (map == null)
                {
                    Console.WriteLine($"[MCE] Error: Map {NextMap} not found in map list");
                    return;
                }
                
                if (Server.IsMapValid(map.Name))
                {
                    Server.ExecuteCommand($"changelevel {map.Name}");
                }
                else if (map.Id is not null)
                {
                    Server.ExecuteCommand($"host_workshop_map {map.Id}");
                }
                else
                    Server.ExecuteCommand($"ds_workshop_changelevel {map.Name}");
            });
            return true;
        }

        public void OnConfigParsed(Config config)
        {
            _config = config;
        }

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            plugin.RegisterEventHandler<EventCsWinPanelMatch>((ev, info) =>
            {
                if (_pluginState.MapChangeScheduled)
                {
                    var delay = _config!.EndOfMapVote.DelayToChangeInTheEnd - 3.0F; //subtracting the delay that is going to be applied by ChangeNextMap function anyway
                    if (delay < 0)
                        delay = 0;

                    _plugin.AddTimer(delay, () =>
                    {
                        ChangeNextMap(true);
                    });
                }
                return HookResult.Continue;
            });
        }

        /// <summary>
        /// Check if a map is available for the cycle
        /// </summary>
        /// <param name="mapName">Map name</param>
        /// <returns>Whether the map is available</returns>
        private bool IsMapAvailableForCycle(string mapName)
        {
            // Check if the map meets cycle conditions
            return _mapSettingsManager.IsMapAvailableForCycle(mapName) && 
                   !_mapCooldown.IsMapInCooldown(mapName);
        }

        /// <summary>
        /// Get a list of maps available for the cycle
        /// </summary>
        /// <returns>List of available maps</returns>
        public List<string> GetAvailableMapsForCycle()
        {
            return _mapSettingsManager.GetAvailableMaps()
                .Where(m => !_mapCooldown.IsMapInCooldown(m))
                .ToList();
        }
    }
}
