using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using cs2_rockthevote.Core;
using Microsoft.Extensions.Localization;

namespace cs2_rockthevote
{
    public partial class Plugin
    {
        [ConsoleCommand("css_extend", "Extends time for the current map")]
        [CommandHelper(minArgs: 1, usage: "<number of minutes to extend the map time ex. 30>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/headadmins")]
        public void OnExtendRoundTimeCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            var newRoundTime = commandInfo.GetArg(1);
            int newRoundTimeInt = 0;

            var intParseSuccess = int.TryParse(newRoundTime, out newRoundTimeInt);

            if (intParseSuccess)
            {
                var secondsToIncrease = newRoundTimeInt;

                _extendRoundTime.CommandHandler(player!, commandInfo, newRoundTimeInt);
            }
            else
            {
                commandInfo.ReplyToCommand("You entered an incorrect integer for the roundtime. Try a number between 1 and 60.");
            }
        }
    }

    public class ExtendRoundTimeCommand : IPluginDependency<Plugin, Config>
    {
        private TimeLimitManager _timeLimitManager;
        private readonly GameRules _gameRules;
        private StringLocalizer _localizer;

        public ExtendRoundTimeCommand(TimeLimitManager timeLimitManager, GameRules gameRules, IStringLocalizer stringLocalizer)
        {
            _gameRules = gameRules;
            _localizer = new StringLocalizer(stringLocalizer, "extendtime.prefix");
            _timeLimitManager = timeLimitManager;

        }

        public bool CommandHandler(CCSPlayerController player, CommandInfo commandInfo, int timeToExtend)
        {
            if (_gameRules.WarmupRunning)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                return false;
            }

            if (!_timeLimitManager.UnlimitedTime)
            {
                if (_timeLimitManager.TimeRemaining > 1)
                {
                    // Update the round time
                    ExtendRoundTime(timeToExtend);

                    // Update TimeRemaining in timeLimitManager
                    // TimeRemaining is in minutes, divide round time by 60
                    _timeLimitManager.TimeRemaining = timeToExtend;

                    commandInfo.ReplyToCommand($"Increased round time by {timeToExtend * 60} minute(s)");

                    return true;
                }
                else
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("extendtime.notapplicable"));
                    return false;
                }
            }
            else
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("extendtime.notapplicable"));
                return false;
            }
        }

        public bool ExtendRoundTime(int minutesToExtendBy)
        {
            try
            {
                // RoundTime is in seconds, so multiply by 60 to convert to minutes
                _gameRules.RoundTime += (minutesToExtendBy * 60);

                var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First();
                Utilities.SetStateChanged(gameRulesProxy, "CCSGameRulesProxy", "m_pGameRules");

                return true;
            }
            catch (Exception ex)
            {
                //Logger.LogWarning("Something went wrong when updating the round time {message}", ex.Message);

                return false;
            }
        }
    }
}
