using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

namespace MapChooserExtended.Features
{
    public class NextMapCommand : IPluginDependency<Plugin, Config>
    {
        private ChangeMapManager _changeMapManager;
        private StringLocalizer _stringLocalizer;
        private NextmapConfig _config = new();
        private MapLister _mapLister;

        public NextMapCommand(ChangeMapManager changeMapManager, StringLocalizer stringLocalizer, MapLister mapLister)
        {
            _changeMapManager = changeMapManager;
            _stringLocalizer = stringLocalizer;
            _mapLister = mapLister;
        }

        public void CommandHandler(CCSPlayerController? player)
        {
            string text;
            if (_changeMapManager.NextMap is not null)
            {
                text = _stringLocalizer.LocalizeWithPrefix("nextmap", _changeMapManager.NextMap);
            }
            else
                text = _stringLocalizer.LocalizeWithPrefix("nextmap.decided-by-vote");

            if (_config.ShowToAll)
                Server.PrintToChatAll(text);
            else if (player is not null)
                player.PrintToChat(text);
            else
                Server.PrintToConsole(text);
        }

        public void SetNextMapCommandHandler(CCSPlayerController player, CommandInfo command)
        {
            string mapName = command.GetArg(1).Trim();
            if (string.IsNullOrEmpty(mapName))
            {
                if (player != null)
                    player.PrintToChat(_stringLocalizer.LocalizeWithPrefix("nextmap.specify-map"));
                else
                    Console.WriteLine(_stringLocalizer.LocalizeWithPrefix("nextmap.specify-map"));
                return;
            }

            // Check if the map exists in the map list
            string matchingMap = _mapLister.GetSingleMatchingMapName(mapName, player, _stringLocalizer, true); // Use isAdmin=true to ignore cycle conditions
            if (matchingMap == "") {
                player.PrintToChat(_stringLocalizer.LocalizeWithPrefix("nextmap.cannot-set-current-map"));
                return;
            }

            // Check if the map is the current map
            if (matchingMap == Server.MapName)
            {
                if (player != null)
                    player.PrintToChat(_stringLocalizer.LocalizeWithPrefix("nextmap.cannot-set-current-map"));
                else
                    Console.WriteLine(_stringLocalizer.LocalizeWithPrefix("nextmap.cannot-set-current-map"));
                return;
            }

            // Force set the next map
            _changeMapManager.ScheduleMapChange(matchingMap, false, "nextmap.prefix");

            // Notify everyone
            if (player != null)
                Server.PrintToChatAll(_stringLocalizer.LocalizeWithPrefix("nextmap.admin-set", player.PlayerName, matchingMap));
            else
                Server.PrintToChatAll(_stringLocalizer.LocalizeWithPrefix("nextmap.admin-set", "Server", matchingMap));

            Console.WriteLine($"[MCE] Next map set to {matchingMap} by {player?.PlayerName ?? "Server"}");
        }

        public void OnLoad(Plugin plugin)
        {

            plugin.AddCommand("nextmap", "Shows nextmap when defined", (player, info) =>
            {
                CommandHandler(player);
            });

            plugin.AddCommand("css_setnextmap", "Set the next map (Admin only)", (player, info) =>
            {
                if (player != null && player.IsValid && AdminManager.PlayerHasPermissions(player, "@css/generic"))
                    SetNextMapCommandHandler(player, info);
                else
                    return;
            });
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.Nextmap ?? new();
        }
    }
}
