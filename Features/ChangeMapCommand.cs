using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;

namespace cs2_rockthevote.Features
{
    public class ChangeMapCommand : IPluginDependency<Plugin, Config>
    {
        private readonly ChangeMapManager _changeMapManager;
        private readonly StringLocalizer _localizer;
        private readonly MapLister _mapLister;
        private ChatMenu? _changeMapMenu = null;

        [ConsoleCommand("css_changemap", "Change the map immediately (Admin only)")]
        [ConsoleCommand("changemap", "Change the map immediately (Admin only)")]
        [RequiresPermissions("@css/generic")]
        public void ChangeMapCommandHandler(CCSPlayerController player, CommandInfo command)
        {
            CommandHandler(player, command.GetArg(1));
        }

        public ChangeMapCommand(ChangeMapManager changeMapManager, StringLocalizer localizer, MapLister mapLister)
        {
            _changeMapManager = changeMapManager;
            _localizer = localizer;
            _mapLister = mapLister;
            _mapLister.EventMapsLoaded += OnMapsLoaded;
        }

        public void OnMapsLoaded(object? sender, Map[] maps)
        {
            // Create a menu with all maps, including the current map
            _changeMapMenu = new("Change Map");
            foreach (var map in _mapLister.Maps!)
            {
                _changeMapMenu.AddMenuOption(map.Name, (CCSPlayerController player, ChatMenuOption option) =>
                {
                    ChangeMap(player, option.Text);
                });
            }

            _changeMapMenu.AddMenuOption("Exit", (CCSPlayerController player, ChatMenuOption option) =>
            {
                MenuManager.CloseActiveMenu(player);
            });
        }

        public void CommandHandler(CCSPlayerController player, string map)
        {
            // Check if player is admin
            if (!player.IsValid || !AdminManager.PlayerHasPermissions(player, ["@css/changemap"]))
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.no-access"));
                return;
            }

            map = map.ToLower().Trim();

            if (string.IsNullOrEmpty(map))
            {
                // Open menu with all maps
                OpenChangeMapMenu(player!);
            }
            else
            {
                // Search for maps containing the specified string
                var matchingMaps = _mapLister.Maps!
                    .Select(x => x.Name)
                    .Where(x => x.ToLower().Contains(map.ToLower()))
                    .ToList();

                if (matchingMaps.Count == 0)
                {
                    player!.PrintToChat(_localizer.LocalizeWithPrefix("general.invalid-map"));
                    return;
                }
                else if (matchingMaps.Count == 1)
                {
                    // Even if there's only one match, still show the menu to prevent accidental map changes
                    ChatMenu singleMapMenu = new("Confirm Map Change");
                    singleMapMenu.AddMenuOption(matchingMaps[0], (CCSPlayerController p, ChatMenuOption option) =>
                    {
                        ChangeMap(p, option.Text);
                    });
                    singleMapMenu.AddMenuOption("Exit", (CCSPlayerController p, ChatMenuOption option) =>
                    {
                        MenuManager.CloseActiveMenu(p);
                    });
                    
                    MenuManager.OpenChatMenu(player!, singleMapMenu);
                }
                else
                {
                    // Create a menu with matching maps
                    ChatMenu matchingMapMenu = new("Matching Maps");
                    foreach (var matchingMap in matchingMaps)
                    {
                        matchingMapMenu.AddMenuOption(matchingMap, (CCSPlayerController p, ChatMenuOption option) =>
                        {
                            ChangeMap(p, option.Text);
                        });
                    }
                    matchingMapMenu.AddMenuOption("Exit", (CCSPlayerController p, ChatMenuOption option) =>
                    {
                        MenuManager.CloseActiveMenu(p);
                    });
                    
                    MenuManager.OpenChatMenu(player!, matchingMapMenu);
                }
            }
        }

        private void OpenChangeMapMenu(CCSPlayerController player)
        {
            MenuManager.OpenChatMenu(player!, _changeMapMenu!);
        }

        private void ChangeMap(CCSPlayerController player, string map)
        {
            // Check if player is admin
            if (!player.IsValid || !AdminManager.PlayerHasPermissions(player, ["@css/changemap"]))
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.no-access"));
                return;
            }

            // Notify everyone about the map change
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("general.admin-changing-map", player.PlayerName, map));
            
            // Schedule immediate map change
            _changeMapManager.ScheduleMapChange(map, false, "changemap.prefix");
            _changeMapManager.ChangeNextMap();
            
            // Close the menu
            MenuManager.CloseActiveMenu(player);
            
            Console.WriteLine($"[RockTheVote] Admin {player.PlayerName} changed map to {map}");
        }

        public void OnLoad(Plugin plugin)
        {
            // TODO: Something
        }

        public void OnConfigParsed(Config config)
        {
            // No specific config needed for this command
        }
    }
}