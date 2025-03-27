using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CS2MenuManager.API.Menu;
using CS2MenuManager.API.Enum;
using CounterStrikeSharp.API.Modules.Timers;
using MapChooserExtended.Core;
using Microsoft.Extensions.Localization;
using System.Data;
using System.Text;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Core.Listeners;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace MapChooserExtended
{
    public class ExtendRoundTimeManager : IPluginDependency<Plugin, Config>
    {
        public ExtendRoundTimeManager(PluginState pluginState, TimeLimitManager timeLimitManager, RoundLimitManager roundLimitManager, GameRules gameRules, MapSettingsManager mapSettingsManager)
        {
            _pluginState = pluginState;
            _timeLimitManager = timeLimitManager;
            _roundLimitManager = roundLimitManager;
            _gameRules = gameRules;
            _mapSettingsManager = mapSettingsManager;
        }

        private PluginState _pluginState;
        private TimeLimitManager _timeLimitManager;
        private RoundLimitManager _roundLimitManager;
        private readonly MapSettingsManager _mapSettingsManager;
        private GameRules _gameRules;

        public void OnLoad(Plugin plugin)
        {
        }

        /*
         *  Need a better way to handle this, as the plugin reload approach breaks map cooldown.
         *
         */
        public void OnMapStart(string map)
        {
        }

        // ExtendRoundTime: Extend the current round time for non-round-based gamemodes (bhop/surf/kz/deathmatch etc)
        public bool ExtendRoundTime(int minutesToExtendBy, GameRules gameRules)
        {
            try
            {
                /* IDK why, razpberry decided to sync the mp_timelimit by current mp_roundtime here.
                 Maybe suits the use case for bhup/surf/kz, but bugged the use case that depends on rounds.

                 Case 1: When !timeleft is around 10 minutes and the current round time is 55 minutes remaining, after !rtv extends by 20 minutes, the current map round time is 75 minutes left, and !timeleft also sets to 75 minutes. 

                 Case 2: When !timeleft is around 55 minutes and the current round time is 2 minutes left, after rtv extends it by 20 minutes, the current map round is 22 minutes left, and !timeleft also follows 22 minutes.
                */
                // RoundTime is in seconds, so multiply by 60 to convert to minutes
                gameRules.RoundTime += (minutesToExtendBy * 60);

                var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
                Utilities.SetStateChanged(gameRulesProxy, "CCSGameRulesProxy", "m_pGameRules");

                // Update TimeRemaining in timeLimitManager
                // TimeRemaining is in minutes, divide round time by 60
                _timeLimitManager.TimeRemaining = _gameRules.RoundTime / 60;

                _pluginState.MapChangeScheduled = false;
                _pluginState.EofVoteHappening = false;
                _pluginState.CommandsDisabled = false;

                return true;
            }
            catch (Exception) //(Exception ex)
            {
                //Logger.LogWarning("Something went wrong when updating the round time {message}", ex.Message);

                return false;
            }

        }

        public bool ExtendMapTimeLimit(int minutesToExtendBy)
        {
            try
            {
                // Use the ExtendTime method to properly update the time limit
                _timeLimitManager.ExtendTime(minutesToExtendBy);

                _pluginState.MapChangeScheduled = false;
                _pluginState.EofVoteHappening = false;
                _pluginState.CommandsDisabled = false;

                return true;
            }
            catch (Exception) //(Exception ex)
            {
                //Logger.LogWarning("Something went wrong when updating the round time {message}", ex.Message);

                return false;
            }
        }

        public bool ExtendMaxRoundLimit(int roundsToExtendBy)
        {
            try
            {
                _roundLimitManager.ExtendRound(roundsToExtendBy);

                _pluginState.MapChangeScheduled = false;
                _pluginState.EofVoteHappening = false;
                _pluginState.CommandsDisabled = false;

                return true;
            }
            catch (Exception) //(Exception ex)
            {
                //Logger.LogWarning("Something went wrong when updating the round time {message}", ex.Message);

                return false;
            }
        }
        
        /// <summary>
        /// Get the extend settings for the current map
        /// </summary>
        /// <returns>Extend settings</returns>
        public ExtendSettings GetCurrentMapExtendSettings()
        {
            string currentMap = Server.MapName;
            var settings = _mapSettingsManager.GetMapSettings(currentMap);
            return settings.Settings.Extend;
        }
    }
}
