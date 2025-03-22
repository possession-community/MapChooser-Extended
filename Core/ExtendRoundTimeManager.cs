using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
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
        const int MAX_OPTIONS_HUD_MENU = 6;
        public ExtendRoundTimeManager(IStringLocalizer stringLocalizer, PluginState pluginState, TimeLimitManager timeLimitManager, GameRules gameRules, MapSettingsManager mapSettingsManager)
        {
            _localizer = new StringLocalizer(stringLocalizer, "extendtime.prefix");
            _pluginState = pluginState;
            _timeLimitManager = timeLimitManager;
            _gameRules = gameRules;
            _mapSettingsManager = mapSettingsManager;
        }

        private readonly StringLocalizer _localizer;
        private PluginState _pluginState;
        private TimeLimitManager _timeLimitManager;
        private readonly MapSettingsManager _mapSettingsManager;
        private Timer? Timer;
        private GameRules _gameRules;

        Dictionary<string, int> Votes = new();
        Dictionary<CCSPlayerController, string> PlayerVotes = new();
        int timeLeft = -1;

        private IExtendMapConfig? _config = null;

        private int _totalExtendLimit;
        private int _canVote = 0;
        private Plugin? _plugin;
        private VipExtendMapConfig _veConfig = new();
        
        // Track the number of extends used for the current map
        private int _extendsUsed = 0;

        public bool VoteInProgress => timeLeft >= 0;

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            plugin.RegisterListener<OnTick>(VoteDisplayTick);
        }

        /*
         *  Need a better way to handle this, as the plugin reload approach breaks map cooldown.
         *
         */
        public void OnMapStart(string map)
        {
            try
            {
                // Reset extends used counter on map start
                _extendsUsed = 0;
                
                // For backward compatibility, still set the plugin state
                _pluginState.ExtendsLeft = GetCurrentMapExtendSettings().Times;
                _totalExtendLimit = GetCurrentMapExtendSettings().Times;
            }
            catch (Exception)
            {
                // Screw it, Oz-Lin is fixing the null reference exception issue for razpberry.
                //Server.PrintToConsole(_localizer.LocalizeWithPrefix("") + "Null Reference Exception happened, default to 3 extends.");

                Server.ExecuteCommand("css_plugins reload MCE"); // screw it!
            }

            Votes.Clear();
            PlayerVotes.Clear();
            timeLeft = 0;
            KillTimer();
        }

        // VIP Extend Start
        public void ExtendTimeVoted(CCSPlayerController player, string voteResponse)
        {
            if (PlayerVotes.ContainsKey(player))
            {
                Votes[PlayerVotes[player]] -= 1;
            }

            Votes[voteResponse] += 1;
            PlayerVotes[player] = voteResponse;
            player.PrintToCenter(_localizer.LocalizeWithPrefix("extendtime.you-voted", voteResponse));
            if (Votes.Select(x => x.Value).Sum() >= _canVote)
            {
                ExtendTimeVote();
            }
        }

        public void RevokeVote(CCSPlayerController player)
        {
            if (PlayerVotes.ContainsKey(player))
            {
                Votes[PlayerVotes[player]] -= 1;
                PlayerVotes.Remove(player);
                player.PrintToCenter(_localizer.LocalizeWithPrefix("general.vote-revoked-choose-again"));
                ShowVoteMenu(player); // Bring back the vote menu
            }
            else
            {
                player.PrintToCenter(_localizer.LocalizeWithPrefix("general.no-vote-to-revoke"));
            }
        }

        private void ShowVoteMenu(CCSPlayerController player)
        {
            var menu = CreateVoteMenu();
            MenuManager.OpenChatMenu(player, menu);
        }

        private ChatMenu CreateVoteMenu()
        {
            ChatMenu menu = new(_localizer.Localize("extendtime.hud.menu-title"));

            var answers = new List<string>() { "Yes", "No" };

            foreach (var answer in answers)
            {
                Votes[answer] = 0;
                menu.AddMenuOption(answer, (player, option) =>
                {
                    ExtendTimeVoted(player, answer);
                    MenuManager.CloseActiveMenu(player);
                });
            }

            return menu;
        }

        void KillTimer()
        {
            timeLeft = -1;
            if (Timer is not null)
            {
                Timer!.Kill();
                Timer = null;
            }
        }

        void PrintCenterTextAll(string text)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player.IsValid)
                {
                    player.PrintToCenter(text);
                }
            }
        }

        public void VoteDisplayTick()
        {
            if (timeLeft < 0)
                return;

            int index = 1;
            StringBuilder stringBuilder = new();
            stringBuilder.AppendFormat($"<b>{_localizer.Localize("extendtime.hud.hud-timer", timeLeft)}</b>");
            if (!_config!.HudMenu)
                foreach (var kv in Votes.OrderByDescending(x => x.Value).Take(MAX_OPTIONS_HUD_MENU).Where(x => x.Value > 0))
                {
                    stringBuilder.AppendFormat($"<br>{kv.Key} <font color='green'>({kv.Value})</font>");
                }
            else
                foreach (var kv in Votes.Take(MAX_OPTIONS_HUD_MENU))
                {
                    stringBuilder.AppendFormat($"<br><font color='yellow'>!{index++}</font> {kv.Key} <font color='green'>({kv.Value})</font>");
                }

            foreach (CCSPlayerController player in ServerManager.ValidPlayers())
            {
                player.PrintToCenterHtml(stringBuilder.ToString());
            }
        }

        void ExtendTimeVote()
        {
            KillTimer();

            // Get current map extend settings
            var extendSettings = GetCurrentMapExtendSettings();
            var currentMap = Server.MapName;
            var mapSettings = _mapSettingsManager.GetMapSettings(currentMap);
            
            var minutesToExtend = extendSettings.Number; // use map extend settings

            decimal maxVotes = Votes.Select(x => x.Value).Max();
            IEnumerable<KeyValuePair<string, int>> potentialWinners = Votes.Where(x => x.Value == maxVotes);
            Random rnd = new();
            KeyValuePair<string, int> winner = potentialWinners.ElementAt(rnd.Next(0, potentialWinners.Count()));

            decimal totalVotes = Votes.Select(x => x.Value).Sum();
            decimal percent = totalVotes > 0 ? winner.Value / totalVotes * 100M : 0;

            if (maxVotes > 0)
            {
                if (winner.Key == "No")
                {
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendtime.vote-ended.failed", percent, totalVotes));
                }
                else
                {
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendtime.vote-ended.passed", minutesToExtend, percent, totalVotes));
                }
            }
            else
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendtime.vote-ended-no-votes"));
            }

            if (winner.Key == "No")
            {
                // Do nothing, vote did not pass
                PrintCenterTextAll(_localizer.Localize("extendtime.hud.finished", "not be extended."));
            }
            else
            {
                // Extend mp_timelimit
                // Use ExtendMapTimeLimit for round-based gamemodes (ze/zm/normal gunfights etc), and ExtendRoundTime for non-round-based gamemodes (bhop/surf/kz/deathmatch etc)
                if (mapSettings.Settings.Match.Type == 0) // Time limit
                {
                    ExtendMapTimeLimit(minutesToExtend, _timeLimitManager, _gameRules);
                }
                else
 // Round limit
                {
                    ExtendRoundTime(minutesToExtend, _timeLimitManager, _gameRules);
                }

                PrintCenterTextAll(_localizer.Localize("extendtime.hud.finished", "be extended."));
                
                // Increment extends used counter
                _extendsUsed++;
                
                // For backward compatibility, still update the plugin state
                if (_pluginState.ExtendsLeft > 0)
                {
                    _pluginState.ExtendsLeft -= 1;
                }
                
                // Show extends left message if there's a limit
                if (extendSettings.Times != -1)
                {
                    int extendsLeft = extendSettings.Times - _extendsUsed;
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendtime.extendsleft", extendsLeft, extendSettings.Times));
                }
                
                _pluginState.CommandsDisabled = false;
            }

            _pluginState.ExtendTimeVoteHappening = false;
        }

        public void StartVote(IExtendMapConfig config)
        {
            Votes.Clear();
            PlayerVotes.Clear();
            _pluginState.ExtendTimeVoteHappening = true;
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _canVote = ServerManager.ValidPlayerCount();

            var menu = CreateVoteMenu();

            foreach (var player in ServerManager.ValidPlayers())
                MenuManager.OpenChatMenu(player, menu);

            timeLeft = _config.VoteDuration;
            Timer = _plugin!.AddTimer(1.0F, () =>
            {
                if (timeLeft <= 0)
                {
                    ExtendTimeVote();
                }
                else
                    timeLeft--;
            }, TimerFlags.REPEAT);
        }
        // VIP Extend End

        // ExtendRoundTime: Extend the current round time for non-round-based gamemodes (bhop/surf/kz/deathmatch etc)
        public bool ExtendRoundTime(int minutesToExtendBy, TimeLimitManager timeLimitManager, GameRules gameRules)
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

                var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First();
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


        public bool ExtendMapTimeLimit(int minutesToExtendBy, TimeLimitManager timeLimitManager, GameRules gameRules)
        {
            try
            {
                _timeLimitManager.TimeLimitValue += (minutesToExtendBy * 60); //convert to seconds

                // Update TimeRemaining
                //_timeLimitManager.TimeRemaining = (_timeLimitManager.TimeLimitValue/60) - (_timeLimitManager.TimePlayed/60);

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
