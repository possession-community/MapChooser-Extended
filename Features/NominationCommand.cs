
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
using MapChooserExtended.Core;

namespace MapChooserExtended
{
    public partial class Plugin
    {
        [ConsoleCommand("css_nominate", "nominate a map to rtv")]
        [ConsoleCommand("nominate", "nominate a map to rtv")]
        [ConsoleCommand("css_nom", "nominate a map to rtv")]
        [ConsoleCommand("nom", "nominate a map to rtv")]
        [ConsoleCommand("css_yd", "nominate a map to rtv")]
        [ConsoleCommand("yd", "nominate a map to rtv")]
        public void OnNominate(CCSPlayerController player, CommandInfo command)
        {
            string map = command.GetArg(1).Trim().ToLower();
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
        [ConsoleCommand("enable_nominate", "Enable nominate command (Admin only)")]
        [RequiresPermissions("@css/generic")]
        public void OnEnableNominate(CCSPlayerController player, CommandInfo command)
        {
            _nominationManager.EnableNominateCommandHandler(player);
        }

        [ConsoleCommand("css_disable_nominate", "Disable nominate command (Admin only)")]
        [ConsoleCommand("disable_nominate", "Disable nominate command (Admin only)")]
        [RequiresPermissions("@css/generic")]
        public void OnDisableNominate(CCSPlayerController player, CommandInfo command)
        {
            _nominationManager.DisableNominateCommandHandler(player);
        }

        [ConsoleCommand("css_nominate_addmap", "Add a map to nomination list (Admin only)")]
        [ConsoleCommand("nominate_addmap", "Add a map to nomination list (Admin only)")]
        [RequiresPermissions("@css/generic")]
        public void OnNominateAddMap(CCSPlayerController player, CommandInfo command)
        {
            _nominationManager.NominateAddMapCommandHandler(player, command);
        }

        [ConsoleCommand("css_nominate_removemap", "Remove a map from nomination list (Admin only)")]
        [ConsoleCommand("nominate_removemap", "Remove a map from nomination list (Admin only)")]
        [RequiresPermissions("@css/generic")]
        public void OnNominateRemoveMap(CCSPlayerController player, CommandInfo command)
        {
            _nominationManager.NominateRemoveMapCommandHandler(player, command);
        }
    }

    public class NominationCommand : IPluginDependency<Plugin, Config>
    {
        Dictionary<int, (string PlayerName, List<string> Maps)> Nominations = new();
        ChatMenu? nominationMenu = null;
        private RtvConfig _config = new();
        private GameRules _gamerules;
        private StringLocalizer _localizer;
        private PluginState _pluginState;
        private MapCooldown _mapCooldown;
        private MapLister _mapLister;
        private MapSettingsManager _mapSettingsManager;

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

        public void OnMapStart(string map)
        {
            Nominations.Clear();
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.Rtv;
        }

        public void OnMapsLoaded(object? sender, Map[] maps)
        {
            nominationMenu = new("Nomination");
            foreach (var map in _mapLister.Maps!.Where(x => x.Name != Server.MapName))
            {
                nominationMenu.AddMenuOption(map.Name, (CCSPlayerController player, ChatMenuOption option) =>
                {
                    Nominate(player, option.Text);
                }, _mapCooldown.IsMapInCooldown(map.Name));
            }

            nominationMenu.AddMenuOption("Exit", (CCSPlayerController player, ChatMenuOption option) =>
            {
                MenuManager.CloseActiveMenu(player);
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
            string mapName = command.GetArg(1).Trim().ToLower();
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
            string mapName = command.GetArg(1).Trim().ToLower();
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

            map = map.ToLower().Trim();
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
            // TODO: Remove
            /*
            else if (_config.MinRounds > 0 && _config.MinRounds > _gamerules.TotalRoundsPlayed)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-rounds", _config.MinRounds));
                return;
            }

            if (ServerManager.ValidPlayerCount() < _config!.MinPlayers)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.MinPlayers));
                return;
            }
            */

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
            MenuManager.OpenChatMenu(player!, nominationMenu!);
        }

        void Nominate(CCSPlayerController player, string map)
        {
            if (map == Server.MapName)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.current-map"));
                return;
            }

            // TODO: Remove
            /*
            if (_mapCooldown.IsMapInCooldown(map))
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.map-played-recently"));
                return;
            }
            */

            string matchingMap = _mapLister.GetSingleMatchingMapName(map, player, _localizer);

            if (matchingMap == "")
                return;

            // TODO: Change message to admin only message
            if (!_mapSettingsManager.IsMapAvailableForNomination(player, matchingMap)) {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.map-not-available"));
                return;
            }

            if (!_mapSettingsManager.IsMapAvailableForCycle(matchingMap)) {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.map-not-available"));
                return;
            }

            var userId = player.UserId!.Value;
            if (!Nominations.ContainsKey(userId))
                Nominations[userId] = (player.PlayerName, new List<string>());

            bool alreadyVoted = Nominations[userId].Maps.IndexOf(matchingMap) != -1;
            if (!alreadyVoted)
                Nominations[userId].Maps.Add(matchingMap);

            var totalVotes = Nominations.Select(x => x.Value.Maps.Where(y => y == matchingMap).Count())
                .Sum();

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
            if (player != null)
                MenuManager.CloseActiveMenu(player);
        }

        public List<string> NominationWinners()
        {
            if (Nominations.Count == 0)
                return new List<string>();

            var rawNominations = Nominations
                .Select(x => x.Value.Maps)
                .Aggregate((acc, x) => acc.Concat(x).ToList());

            return rawNominations
                .Distinct()
                .Select(map => new KeyValuePair<string, int>(map, rawNominations.Count(x => x == map)))
                .OrderByDescending(x => x.Value)
                .Select(x => x.Key)
                .ToList();
        }

        // Force nominate a map (admin only)
        void ForceNominate(CCSPlayerController player, string map)
        {
            // Check if the map is available for nomination
            if (!_mapSettingsManager.IsMapAvailableForNomination(player, map))
            {
                if (player != null)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.map-not-available"));
                else
                    Console.WriteLine(_localizer.LocalizeWithPrefix("general.map-not-available"));
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
