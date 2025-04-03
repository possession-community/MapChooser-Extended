using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using CS2MenuManager.API.Enum;
using CounterStrikeSharp.API.Modules.Timers;
using MapChooserExtended.Core;
using System.Text;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using static CounterStrikeSharp.API.Core.Listeners;

namespace MapChooserExtended
{
    public class EndMapVoteManager : IPluginDependency<Plugin, Config>
    {
        const int MAX_OPTIONS_HUD_MENU = 6;

        public EndMapVoteManager(
            MapLister mapLister, 
            ChangeMapManager changeMapManager, 
            NominationCommand nominationManager, 
            StringLocalizer localizer, 
            PluginState pluginState, 
            MapCooldown mapCooldown, 
            ExtendRoundTimeManager extendRoundTimeManager, 
            TimeLimitManager timeLimitManager, 
            RoundLimitManager roundLimitManager, 
            GameRules gameRules,
            MapSettingsManager mapSettingsManager)
        {
            _mapLister = mapLister;
            _changeMapManager = changeMapManager;
            _nominationManager = nominationManager;
            _localizer = localizer;
            _pluginState = pluginState;
            _mapCooldown = mapCooldown;
            _extendRoundTimeManager = extendRoundTimeManager;
            _timeLimitManager = timeLimitManager;
            _roundLimitManager = roundLimitManager;
            _gameRules = gameRules;
            _mapSettingsManager = mapSettingsManager;
        }

        private readonly MapLister _mapLister;
        private readonly ChangeMapManager _changeMapManager;
        private readonly NominationCommand _nominationManager;
        private readonly StringLocalizer _localizer;
        private readonly PluginState _pluginState;
        private readonly MapCooldown _mapCooldown;
        private readonly MapSettingsManager _mapSettingsManager;
        private Timer? Timer;
        private readonly ExtendRoundTimeManager _extendRoundTimeManager;
        private readonly TimeLimitManager _timeLimitManager;
        private readonly RoundLimitManager _roundLimitManager;
        private readonly GameRules _gameRules;

        // Track the number of extends used for the current map
        private int _extendsUsed = 0;

        Dictionary<string, int> Votes = new();
        Dictionary<CCSPlayerController, string> PlayerVotes = new();
        int timeLeft = -1;
        int countdownTimeLeft = -1;

        List<string> mapsEllected = new();

        private IEndOfMapConfig? _config = null;
        private IEndOfMapConfig? _configBackup = null;
        private int _canVote = 0;
        private Plugin? _plugin;

        HashSet<int> _voted = new();

        public bool VoteInProgress => timeLeft >= 0;
        public bool CountdownInProgress => countdownTimeLeft >= 0;

        public void OnLoad(Plugin plugin)
        {
            _plugin = plugin;
            plugin.RegisterListener<OnTick>(DisplayTick);
        }

        public void OnConfigParsed(Config config)
        {
            //_eomConfig = config.EndOfMapVote;
            // Use map settings instead of config
        }

        public void OnMapStart(string map)
        {
            Votes.Clear();
            PlayerVotes.Clear();
            timeLeft = -1;
            countdownTimeLeft = -1;
            mapsEllected.Clear();
            KillTimer();
            // Reset extends used counter on map start
            _extendsUsed = 0;

            // Restore the config if it was changed by the server command
            if (_configBackup is not null)
            {
                _config = _configBackup;
                _configBackup = null;
            }
        }

        public void MapVoted(CCSPlayerController player, string mapName)
        {
            _voted.Add(player.UserId!.Value);

            if (PlayerVotes.ContainsKey(player))
            {
                Votes[PlayerVotes[player]] -= 1;
            }

            Votes[mapName] += 1;
            PlayerVotes[player] = mapName;
            player.PrintToChat(_localizer.LocalizeWithPrefix("emv.you-voted", mapName));
            if (Votes.Select(x => x.Value).Sum() >= _canVote)
            {
                EndVote();
            }
        }

        private ScreenMenu CreateMapVoteMenu()
        {
            ScreenMenu menu = new ScreenMenu(_localizer.Localize("emv.hud.menu-title"), _plugin) {
                MenuType = MenuType.KeyPress,
            };

            // Add extend option if allowed
            // Get current map extend settings
            var extendSettings = GetCurrentMapExtendSettings();
            if (extendSettings.Enabled && (extendSettings.Times > _extendsUsed || extendSettings.Times == -1))
            {
                Votes[_localizer.Localize("general.extend-current-map")] = 0;
                menu.AddItem(_localizer.Localize("general.extend-current-map"), (player, option) =>
                {
                    MapVoted(player, option.Text);
                });
            }

            // Add ignore option if DontChangeRtv is enabled and this is an RTV vote
            //if (_config is RtvConfig rtvConfig && rtvConfig.DontChangeRtv)
            //{
            //    Votes[_localizer.Localize("general.ignore-rtv")] = 0;
            //    menu.AddItem(_localizer.Localize("general.ignore-rtv"), (player, option) =>
            //    {
            //        MapVoted(player, option.Text);
            //    });
            //}

            // Add map options
            foreach (var map in mapsEllected.Take(MAX_OPTIONS_HUD_MENU))
            {
                Votes[map] = 0;
                menu.AddItem(map, (player, option) =>
                {
                    MapVoted(player, option.Text);
                });
            }

            return menu;
        }

        void KillTimer()
        {
            timeLeft = -1;
            countdownTimeLeft = -1;
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

        void PrintCenterHtmlAll(string html)
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player.IsValid)
                {
                    player.PrintToCenterHtml(html);
                }
            }
        }

        public void DisplayTick()
        {
            if (countdownTimeLeft >= 0)
            {
                CountdownDisplayTick();
            }
            else if (timeLeft >= 0)
            {
                VoteDisplayTick();
            }
        }

        public void VoteDisplayTick()
        {
            if (timeLeft < 0)
                return;

            int index = 1;
            StringBuilder stringBuilder = new();
            stringBuilder.AppendFormat($"<b>{_localizer.Localize("emv.hud.hud-timer", timeLeft)}</b>");
            foreach (var kv in Votes)
            {
                stringBuilder.AppendFormat($"<br><font color='yellow'>!{index++}</font> {kv.Key} <font color='green'>({kv.Value})</font>");
            }

            PrintCenterHtmlAll(stringBuilder.ToString());
        }

        public void CountdownDisplayTick()
        {
            if (countdownTimeLeft < 0)
                return;

            StringBuilder stringBuilder = new();
            stringBuilder.AppendFormat($"<b>{_localizer.Localize("emv.hud.countdown", countdownTimeLeft)}</b>");
            PrintCenterHtmlAll(stringBuilder.ToString());
        }

        void EndVote()
        {
            bool mapEnd = _config is EndOfMapConfig;
            KillTimer();
            decimal maxVotes = Votes.Select(x => x.Value).Max();
            IEnumerable<KeyValuePair<string, int>> potentialWinners = Votes.Where(x => x.Value == maxVotes);
            Random rnd = new();
            KeyValuePair<string, int> winner = potentialWinners.ElementAt(rnd.Next(0, potentialWinners.Count()));

            decimal totalVotes = Votes.Select(x => x.Value).Sum();
            decimal percent = totalVotes > 0 ? winner.Value / totalVotes * 100M : 0;

            if (maxVotes > 0)
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("emv.vote-ended", winner.Key, percent, totalVotes));
            }
            else
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("emv.vote-ended-no-votes", winner.Key));
            }

            if (winner.Key == _localizer.Localize("general.extend-current-map"))
            {
                if (_config != null)
                {
                    // Get current map extend settings
                    var extendSettings = GetCurrentMapExtendSettings();
                    var currentMap = Server.MapName;
                    var mapSettings = _mapSettingsManager.GetMapSettings(currentMap);
                    
                    // Use map settings to determine extend behavior
                    // TODO: make round time extend?
                    if (mapSettings.Settings.Match.Type == 0 && !_timeLimitManager.UnlimitedTime)
                    {
                        _extendRoundTimeManager.ExtendMapTimeLimit(extendSettings.Number);
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendtime.vote-ended.passed",
                            extendSettings.Number, percent, totalVotes));
                    }
                    else if (mapSettings.Settings.Match.Type == 1 && !_roundLimitManager.UnlimitedRound)
                    {
                        _extendRoundTimeManager.ExtendMaxRoundLimit(extendSettings.Number);
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendtime.vote-ended.passed.rounds",
                            extendSettings.Number, percent, totalVotes));
                    }
                    else if (mapSettings.Settings.Match.Type == 2)
                    {
                        _extendRoundTimeManager.ExtendRoundTime(extendSettings.Number, _gameRules);
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendtime.vote-ended.passed.rounds",
                            extendSettings.Number, percent, totalVotes));
                    }

                    // Increment extends used counter
                    _extendsUsed++;
                    
                    // Show extends left message if there's a limit
                    if (extendSettings.Times != -1)
                    {
                        int extendsLeft = extendSettings.Times - _extendsUsed;
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendtime.extendsleft", extendsLeft, extendSettings.Times));
                    }

                    _pluginState.MapChangeScheduled = false;
                    _pluginState.EofVoteHappening = false;
                    _pluginState.CommandsDisabled = false;
                    _pluginState.ExtendTimeVoteHappening = false;

                    _nominationManager.ResetNominations();
                    _nominationManager.Nomlist.Clear();
                }
            }
            else if (winner.Key == _localizer.Localize("general.ignore-rtv"))
            {
                // Do nothing if Ignore is selected
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.ignored"));
                
                _pluginState.MapChangeScheduled = false;
                _pluginState.EofVoteHappening = false;
                _pluginState.CommandsDisabled = false;
                _pluginState.ExtendTimeVoteHappening = false;
            }
            else
            {
                _changeMapManager.ScheduleMapChange(winner.Key, mapEnd: mapEnd);
                if (_config != null && _config.ChangeMapImmediately)
                {
                    _changeMapManager.ChangeNextMap(mapEnd);
                }
                else
                {
                    if (!mapEnd)
                    {
                        // Use a more appropriate message for map change notification
                        Server.PrintToChatAll(_localizer.LocalizeWithPrefix("general.nextmap-will-be", winner.Key));
                        _pluginState.CommandsDisabled = true;
                    }
                }
            }
            _pluginState.EofVoteHappening = false;
        }

        IList<T> Shuffle<T>(Random rng, IList<T> array)
        {
            int n = array.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
            return array;
        }

        public void StartVoteWithCountdown(IEndOfMapConfig config)
        {
            // Backup the current config as if this is called via the server command, the config will be changed
            _configBackup = _config;
            _config = config;

            // Start countdown
            countdownTimeLeft = config.VoteCountdownTime;
            
            // Announce countdown
            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("emv.countdown-started", countdownTimeLeft));
            
            // Start countdown timer
            Timer = _plugin!.AddTimer(1.0F, () =>
            {
                if (countdownTimeLeft <= 0)
                {
                    // Countdown finished, start the vote
                    if (Timer != null)
                    {
                        Timer.Kill();
                        Timer = null;
                    }
                    countdownTimeLeft = -1;
                    StartVote(config);
                }
                else
                {
                    countdownTimeLeft--;
                }
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        public void StartVote(IEndOfMapConfig config)
        {
            Votes.Clear();
            PlayerVotes.Clear();
            _voted.Clear();

            // Backup the current config as if this is called via the server command, the config will be changed
            if (_configBackup == null)
            {
                _configBackup = _config;
            }

            _pluginState.EofVoteHappening = true;
            _config = config;
            int mapsToShow = _config!.MapsToShow == 0 || _config!.MapsToShow > MAX_OPTIONS_HUD_MENU ? MAX_OPTIONS_HUD_MENU : _config!.MapsToShow;

            // Get maps from MapLister which are already filtered by availability
            var availableMaps = _mapLister.Maps!
                .Select(m => m.Name)
                .Where(m => m != Server.MapName)
                .ToList();

            // If no maps meet cycle conditions, use all maps except current and cooldown
            if (availableMaps.Count == 0)
            {
                Console.WriteLine("[MCE] No maps meet cycle conditions, using all available maps");
                availableMaps = _mapLister.Maps!
                    .Select(x => x.Name)
                    .Where(x => x != Server.MapName && !_mapCooldown.IsMapInCooldown(x))
                    .ToList();
            }

            // Shuffle available maps
            var mapsScrambled = Shuffle(new Random(), availableMaps);
            
            // Get nominated maps from the nomination winners
            // No need to check if maps are available for cycle as MapLister already does this
            var nominatedMaps = _nominationManager.NominationWinners().ToList();

            // If we have enough nominated maps, use only those
            if (nominatedMaps.Count >= mapsToShow)
            {
                mapsEllected = nominatedMaps.Take(mapsToShow).ToList();
            }
            // Otherwise, use all nominated maps and fill the rest with random maps
            else
            {
                // Add all nominated maps first
                mapsEllected = new List<string>(nominatedMaps);
                
                // Fill the remaining slots with random maps that aren't already nominated
                int remainingSlots = mapsToShow - nominatedMaps.Count;
                if (remainingSlots > 0)
                {
                    var additionalMaps = mapsScrambled
                        .Where(m => !nominatedMaps.Contains(m))
                        .Take(remainingSlots)
                        .ToList();
                    
                    mapsEllected.AddRange(additionalMaps);
                }
            }

            _canVote = ServerManager.ValidPlayerCount();
            var menu = CreateMapVoteMenu();

            foreach (var player in ServerManager.ValidPlayers())
                menu.Display(player, _config.VoteDuration);

            timeLeft = _config.VoteDuration;
            Timer = _plugin!.AddTimer(1.0F, () =>
            {
                if (timeLeft <= 0)
                {
                    EndVote();
                }
                else
                    timeLeft--;
            }, TimerFlags.STOP_ON_MAPCHANGE);
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
