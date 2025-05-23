﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using MapChooserExtended.Core;
using Microsoft.Extensions.Localization;

namespace MapChooserExtended
{
    public partial class Plugin
    {
        [ConsoleCommand("css_ext", "Extend current map")]
        [ConsoleCommand("css_extend", "Extend current map")]
        [ConsoleCommand("css_extendmap", "Extend current map")]
        [CommandHelper(minArgs: 1, usage: "<number of minutes to extend the map time ex. 30>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/changemap")]
        public void OnExtend(CCSPlayerController? player, CommandInfo? command)
        {
            var extendNum = command.GetArg(1);
            int parsed;

            if (int.TryParse(extendNum, out parsed))
            {
                _extendMapManager.CommandHandler(player!, parsed);
            }
            else
            {
                command.ReplyToCommand("You entered an incorrect integer for the map time limit.");
            }
        }
    }

    public class ExtendMapCommand : IPluginDependency<Plugin, Config>
    {
        private readonly StringLocalizer _localizer;
        private readonly GameRules _gameRules;
        private TimeLimitManager _timeLimitManager;
        private ExtendRoundTimeManager _extendRoundTimeManager;
        private MapSettingsManager _mapSettingsManager;

        public ExtendMapCommand(GameRules gameRules, IStringLocalizer localizer, TimeLimitManager timeLimitManager, ExtendRoundTimeManager extendRoundTimeManager, MapSettingsManager mapSettingsManager)
        {
            _localizer = new StringLocalizer(localizer, "extendmap.prefix");
            _gameRules = gameRules;
            _timeLimitManager = timeLimitManager;
            _extendRoundTimeManager = extendRoundTimeManager;
            _mapSettingsManager = mapSettingsManager;
        }

        public void OnMapStart(string map)
        {
        }

        public void CommandHandler(CCSPlayerController? player, int extendTime)
        {
            if (player is null || !player.IsValid || player.IsBot)
                return;

            string currentMap = Server.MapName;
            var mapSettings = _mapSettingsManager.GetMapSettings(currentMap);

            // do not check extend times left because this is admin command
            if (mapSettings.Settings.Match.Type == 0) // Time limit
                _extendRoundTimeManager.ExtendMapTimeLimit(extendTime);
            else if (mapSettings.Settings.Match.Type == 1) // Round limit
                _extendRoundTimeManager.ExtendMaxRoundLimit(extendTime);
            else if (mapSettings.Settings.Match.Type == 2) // Round limit
                _extendRoundTimeManager.ExtendRoundTime(extendTime, _gameRules);

            Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("extendmap.map-extended", extendTime)}");
        }

        public void OnConfigParsed(Config config)
        {
        }
    }
}
