
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using MapChooserExtended.Core;
using CS2MenuManager.API.Menu;
using CS2MenuManager.API.Enum;

namespace MapChooserExtended
{
    public partial class Plugin
    {
        [ConsoleCommand("css_nominate", "nominate a map to rtv")]
        [ConsoleCommand("css_nom", "nominate a map to rtv")]
        public void OnNominate(CCSPlayerController player, CommandInfo command)
        {
            string map = command.GetArg(1).Trim();
            _nominationManager.CommandHandler(player!, map);
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult EventPlayerDisconnectNominate(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
        {
            var player = @event.Userid;
            if (player != null)
            {
                _nominationManager.PlayerDisconnected(player);
            }

            return HookResult.Continue;
        }

        [ConsoleCommand("css_enable_nominate", "Enable nominate command (Admin only)")]
        [RequiresPermissions("@css/changemap")]
        public void OnEnableNominate(CCSPlayerController player, CommandInfo command)
        {
            _nominationManager.EnableNominateCommandHandler(player);
        }

        [ConsoleCommand("css_disable_nominate", "Disable nominate command (Admin only)")]
        [RequiresPermissions("@css/changemap")]
        public void OnDisableNominate(CCSPlayerController player, CommandInfo command)
        {
            _nominationManager.DisableNominateCommandHandler(player);
        }

        [ConsoleCommand("css_nominate_addmap", "Add a map to nomination list (Admin only)")]
        [RequiresPermissions("@css/changemap")]
        public void OnNominateAddMap(CCSPlayerController player, CommandInfo command)
        {
            _nominationManager.NominateAddMapCommandHandler(player, command);
        }

        [ConsoleCommand("css_nominate_removemap", "Remove a map from nomination list (Admin only)")]
        [RequiresPermissions("@css/changemap")]
        public void OnNominateRemoveMap(CCSPlayerController player, CommandInfo command)
        {
            _nominationManager.NominateRemoveMapCommandHandler(player, command);
        }
    }

    public class NominationCommand : IPluginDependency<Plugin, Config>
    {
        Dictionary<int, (string PlayerName, List<string> Maps)> Nominations = new();
        CenterHtmlMenu? nominationMenu = null;
        private Plugin? _plugin;
        private RtvConfig _config = new();
        private EndOfMapConfig _eomConfig = new();
        private GameRules _gamerules;
        private StringLocalizer _localizer;
        private PluginState _pluginState;
        private MapCooldown _mapCooldown;
        private MapLister _mapLister;
        private MapSettingsManager _mapSettingsManager;
        private int _maxNominations = 0;

        public Dictionary<int, (string PlayerName, List<string> Maps)> Nomlist => Nominations;

        public NominationCommand(MapLister mapLister, GameRules gamerules, StringLocalizer localizer, PluginState pluginState, MapSettingsManager mapSettingsManager, MapCooldown mapCooldown)
        {
            _mapLister = mapLister;
            _gamerules = gamerules;
            _localizer = localizer;
            _pluginState = pluginState;
            _mapSettingsManager = mapSettingsManager;
            _mapCooldown = mapCooldown;
            _mapCooldown.EventCooldownRefreshed += OnMapsLoaded;
        }

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        public void OnMapStart(string map)
        {
            Nominations.Clear();
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.Rtv;
            _eomConfig = config.EndOfMapVote;
            // Set max nominations based on maps to show in vote
            _maxNominations = _eomConfig.MapsToShow == 0 ? 6 : _eomConfig.MapsToShow;
        }

        public void OnMapsLoaded(object? sender, Map[] maps)
        {
            nominationMenu = new CenterHtmlMenu("Nomination", _plugin);
            foreach (var map in _mapLister.Maps!.Where(x => x.Name != Server.MapName))
            {
                // TODO: Use allMaps + check if the map is available for nomination
                //       And, add the text to each options, the reason why the map is not available
                if (_mapCooldown.IsMapInCooldown(map.Name)) {
                    nominationMenu.AddItem(map.Name, DisableOption.DisableHideNumber);
                } else {
                    nominationMenu.AddItem(map.Name, (player, option) =>
                    {
                        Nominate(player, option.Text);
                    });
                }
            }

            nominationMenu.AddItem("Exit", (player, option) =>
            {
                // in default, Menu will be closed automatically
            });
        }

        public void EnableNominateCommandHandler(CCSPlayerController player)
        {
            if (!_pluginState.NominateDisabled)
            {
                if (player != null)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.already-enabled"));
                else
                    Console.WriteLine(_localizer.LocalizeWithPrefix("nominate.already-enabled"));
                return;
            }

            _pluginState.NominateDisabled = false;
            
            if (player != null)
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.enabled-by-admin", player.PlayerName));
            else
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.enabled-by-server"));
        }

        public void DisableNominateCommandHandler(CCSPlayerController player)
        {
            if (_pluginState.NominateDisabled)
            {
                if (player != null)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.already-disabled"));
                else
                    Console.WriteLine(_localizer.LocalizeWithPrefix("nominate.already-disabled"));
                return;
            }

            _pluginState.NominateDisabled = true;
            
            if (player != null)
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.disabled-by-admin", player.PlayerName));
            else
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.disabled-by-server"));
        }

        public void NominateAddMapCommandHandler(CCSPlayerController player, CommandInfo command)
        {
            string mapName = command.GetArg(1).Trim();
            if (string.IsNullOrEmpty(mapName))
            {
                if (player != null)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.specify-map"));
                else
                    Console.WriteLine(_localizer.LocalizeWithPrefix("nominate.specify-map"));
                return;
            }

            // Check if the map exists in the map list
            string matchingMap = _mapLister.GetSingleMatchingMapName(mapName, player, _localizer, true);
            if (matchingMap == "")
                return;

            // Check if the map is the current map
            if (matchingMap == Server.MapName)
            {
                if (player != null)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current-map"));
                else
                    Console.WriteLine(_localizer.LocalizeWithPrefix("general.validation.current-map"));
                return;
            }

            // Force nominate the map
            ForceNominate(player, matchingMap);
            
            if (player != null)
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.admin-added", player.PlayerName, matchingMap));
            else
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.admin-added", "Server", matchingMap));
        }

        public void NominateRemoveMapCommandHandler(CCSPlayerController player, CommandInfo command)
        {
            string mapName = command.GetArg(1).Trim();
            if (string.IsNullOrEmpty(mapName))
            {
                if (player != null)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.specify-map"));
                else
                    Console.WriteLine(_localizer.LocalizeWithPrefix("nominate.specify-map"));
                return;
            }

            // Check if the map exists in the map list
            string matchingMap = _mapLister.GetSingleMatchingMapName(mapName, player, _localizer);
            if (matchingMap == "")
                return;

            // Check if the map is nominated
            bool mapFound = false;
            foreach (var nomination in Nominations)
            {
                if (nomination.Value.Maps.Contains(matchingMap))
                {
                    mapFound = true;
                    nomination.Value.Maps.Remove(matchingMap);
                    
                    // If the player has no more nominations, remove them from the list
                    if (nomination.Value.Maps.Count == 0)
                        Nominations.Remove(nomination.Key);
                    
                    break;
                }
            }

            if (mapFound)
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.admin-removed", player?.PlayerName ?? "Server", matchingMap));
            else
            {
                if (player != null)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.not-nominated", matchingMap));
                else
                    Console.WriteLine(_localizer.LocalizeWithPrefix("nominate.not-nominated", matchingMap));
            }
        }

        public void CommandHandler(CCSPlayerController player, string map)
        {
            if (player is null)
                return;

            if (_pluginState.DisableCommands || !_config.NominationEnabled || _pluginState.NominateDisabled)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                return;
            }

            if (_gamerules.WarmupRunning)
            {
                if (!_config.EnabledInWarmup)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                    return;
                }
            }

            if (string.IsNullOrEmpty(map))
            {
                OpenNominationMenu(player!);
            }
            else
            {
                Nominate(player, map);
            }
        }

        public void OpenNominationMenu(CCSPlayerController player)
        {
            nominationMenu!.Display(player!);
        }

        void Nominate(CCSPlayerController player, string option)
        {
            string map = option;
            if (map == Server.MapName)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current-map"));
                return;
            }


            string matchingMap = _mapLister.GetSingleMatchingMapName(map, player, _localizer);

            if (matchingMap == "")
                return;

            // Check if map is available for nomination and cycle
            if (!_mapSettingsManager.IsMapAvailableForNomination(player, matchingMap) ||
                !_mapSettingsManager.IsMapAvailableForCycle(matchingMap) || 
                !_mapCooldown.IsMapInCooldown(matchingMap)) {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.map-not-available"));
                return;
            }

            // Check if the map is already nominated by someone else
            foreach (var nomination in Nominations)
            {
                if (nomination.Key != player.UserId!.Value && nomination.Value.Maps.Contains(matchingMap))
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.already-nominated-by-someone-else", matchingMap));
                    return;
                }
            }

            // Check if nomination list is full
            if (NominationWinners().Count >= _maxNominations)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.nomination-list-full", _maxNominations));
                return;
            }

            var userId = player.UserId!.Value;
            if (!Nominations.ContainsKey(userId))
                Nominations[userId] = (player.PlayerName, new List<string>());

            bool alreadyVoted = Nominations[userId].Maps.IndexOf(matchingMap) != -1;
            if (!alreadyVoted)
                Nominations[userId].Maps.Add(matchingMap);

            // Count total votes for this map more efficiently
            var totalVotes = Nominations.Sum(x => x.Value.Maps.Count(y => y == matchingMap));

            if (!alreadyVoted)
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("nominate.nominated", player.PlayerName,
                    matchingMap, totalVotes));
            }
            else
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.already-nominated", matchingMap,
                    totalVotes));
            }
        }

        public List<string> NominationWinners()
        {
            if (Nominations.Count == 0)
                return new List<string>();

            // Flatten all nominations into a single list
            var allMaps = Nominations.SelectMany(x => x.Value.Maps);
            
            // Group by map name, count occurrences, order by count descending, and return map names
            return allMaps
                .GroupBy(x => x)
                .Select(g => new { Map = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Select(x => x.Map)
                .ToList();
        }

        // Force nominate a map (admin only)
        void ForceNominate(CCSPlayerController player, string map)
        {
            // Check if map is available for nomination and cycle
            if (!_mapSettingsManager.IsMapAvailableForNomination(player, map))
            {
                if (player != null)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.map-not-available"));
                else
                    Console.WriteLine(_localizer.LocalizeWithPrefix("general.map-not-available"));
                return;
            }

            // Check if the map is already nominated by someone else
            foreach (var nomination in Nominations)
            {
                if (nomination.Key != player.UserId!.Value && nomination.Value.Maps.Contains(map))
                {
                    if (player != null)
                        player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.already-nominated-by-someone-else", map));
                    else
                        Console.WriteLine(_localizer.LocalizeWithPrefix("nominate.already-nominated-by-someone-else", map));
                    return;
                }
            }

            // Check if nomination list is full
            if (NominationWinners().Count >= _maxNominations)
            {
                if (player != null)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("nominate.nomination-list-full", _maxNominations));
                else
                    Console.WriteLine(_localizer.LocalizeWithPrefix("nominate.nomination-list-full", _maxNominations));
                return;
            }

            // Add the map to the nomination list
            var userId = player.UserId!.Value;
            if (!Nominations.ContainsKey(userId))
                Nominations[userId] = (player?.PlayerName ?? "Server", new List<string>());

            // Check if the map is already nominated
            if (Nominations[userId].Maps.IndexOf(map) == -1)
                Nominations[userId].Maps.Add(map);

            Console.WriteLine($"[MCE] Map {map} force nominated by {player?.PlayerName ?? "Server"}");
        }

        public void ResetNominations()
        {
            Nominations.Clear();
        }

        public void PlayerDisconnected(CCSPlayerController player)
        {
            int userId = player.UserId!.Value;
            if (Nominations.ContainsKey(userId))
                Nominations.Remove(userId);
        }
    }
}
