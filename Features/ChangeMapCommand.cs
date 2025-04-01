using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CS2MenuManager.API.Menu;
using CS2MenuManager.API.Enum;

namespace MapChooserExtended.Features
{
    public class ChangeMapCommand : IPluginDependency<Plugin, Config>
    {
        private readonly ChangeMapManager _changeMapManager;
        private readonly StringLocalizer _localizer;
        private readonly MapLister _mapLister;
        private Plugin? _plugin;

        [ConsoleCommand("css_movemap", "Change the map immediately (Admin only)")]
        [RequiresPermissions("@css/changemap")]
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

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
        }

        public void OnMapsLoaded(object? sender, Map[] maps)
        {
        }

        public void CommandHandler(CCSPlayerController player, string map)
        {
            // Check if player is admin
            if (!player.IsValid || !AdminManager.PlayerHasPermissions(player, ["@css/changemap"]))
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.no-access"));
                return;
            }

            if (string.IsNullOrEmpty(map))
            {
                // Open menu with all maps
                ScreenMenu menu = new ScreenMenu("Move Map To:", _plugin) {
                    MenuType = MenuType.KeyPress,
                };
                foreach (var _map in _mapLister.AllMaps!) // Use AllMaps to ignore cycle conditions for admin commands
                {
                    menu.AddItem(_map.Name, (player, option) =>
                    {
                        ChangeMap(player, option.Text);
                    });
                }

                menu.Display(player!, 0);
            }
            else
            {
                // Search for maps containing the specified string
                var matchingMaps = _mapLister.AllMaps!
                    .Select(x => x.Name)
                    .Where(x => x.Contains(map))
                    .ToList();

                if (matchingMaps.Count == 0)
                {
                    player!.PrintToChat(_localizer.LocalizeWithPrefix("general.invalid-map"));
                    return;
                }
                else if (matchingMaps.Count == 1)
                {
                    // Even if there's only one match, still show the menu to prevent accidental map changes
                    ScreenMenu singleMapMenu = new ScreenMenu("Move Map To:", _plugin) {
                        MenuType = MenuType.KeyPress,
                    };
                    singleMapMenu.AddItem(matchingMaps[0], (p, option) =>
                    {
                        ChangeMap(p, option.Text);
                    });
                    
                    singleMapMenu.Display(player!, 0);
                }
                else
                {
                    // Create a menu with matching maps
                    ScreenMenu matchingMapMenu = new ScreenMenu("Move Map To:", _plugin) {
                        MenuType = MenuType.KeyPress,
                    };
                    foreach (var matchingMap in matchingMaps)
                    {
                        matchingMapMenu.AddItem(matchingMap, (p, option) =>
                        {
                            ChangeMap(p, option.Text);
                        });
                    }
                    
                    matchingMapMenu.Display(player!, 0);
                }
            }
        }

        private void ChangeMap(CCSPlayerController player, string option)
        {
            string map = option;
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
            
            Console.WriteLine($"[MCE] Admin {player.PlayerName} changed map to {map}");
        }

        public void OnConfigParsed(Config config)
        {
            // No specific config needed for this command
        }
    }
}