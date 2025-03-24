using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace MapChooserExtended
{
    public partial class Plugin
    {
        [ConsoleCommand("css_rtv", "Votes to rock the vote")]
        [ConsoleCommand("rtv", "Votes to rock the vote")]
        public void OnRTV(CCSPlayerController? player, CommandInfo? command)
        {
            if (player is null)
            {
                // Handle server command
                _rtvManager.CommandServerHandler(player, command!);
            }
            else
            {
                // Handle player command
                _rtvManager.CommandHandler(player!);
            }
        }

        [ConsoleCommand("css_unrtv", "Removes a vote to rock the vote")]
        [ConsoleCommand("unrtv", "Removes a vote to rock the vote")]
        public void OnUnRTV(CCSPlayerController? player, CommandInfo? command)
        {
            if (player is null)
            {
                // Handle server command
                _rtvManager.CommandServerHandler(player, command!);
            }
            else
            {
                // Handle player command
                _rtvManager.UnRTVCommandHandler(player!);
            }
        }

        [ConsoleCommand("css_enable_rtv", "Enable RTV command (Admin only)")]
        [ConsoleCommand("enable_rtv", "Enable RTV command (Admin only)")]
        [RequiresPermissions("@css/generic")]
        public void OnEnableRtv(CCSPlayerController? player, CommandInfo command)
        {
            _rtvManager.EnableRtvCommandHandler(player);
        }

        [ConsoleCommand("css_disable_rtv", "Disable RTV command (Admin only)")]
        [ConsoleCommand("disable_rtv", "Disable RTV command (Admin only)")]
        [RequiresPermissions("@css/generic")]
        public void OnDisableRtv(CCSPlayerController? player, CommandInfo command)
        {
            _rtvManager.DisableRtvCommandHandler(player);
        }

        [ConsoleCommand("css_force_rtv", "Force RTV vote (Admin only)")]
        [ConsoleCommand("force_rtv", "Force RTV vote (Admin only)")]
        [RequiresPermissions("@css/generic")]
        public void OnForceRtv(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null && player.IsValid != false)
                _rtvManager.ForceRtvCommandHandler(player, command);
            else
                return;
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult EventPlayerDisconnectRTV(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
        {
            var player = @event.Userid;
            _rtvManager.PlayerDisconnected(player);
            return HookResult.Continue;
        }
    }

    public class RockTheVoteCommand : IPluginDependency<Plugin, Config>
    {
        private readonly StringLocalizer _localizer;
        private readonly GameRules _gameRules;
        private EndMapVoteManager _endMapVoteManager;//, _extendConfig;
        private PluginState _pluginState;
        private RtvConfig _config = new();
        private AsyncVoteManager? _voteManager;
        // Track the last time RTV was triggered
        private DateTime _lastRtvTime = DateTime.MinValue;
        private bool _firstRtvOfMap = true;
        // Track when the map started
        // Timer for map start operations
        private Timer? _mapStartTimer = null;
        private DateTime _mapStartTime = DateTime.Now;
        public bool VotesAlreadyReached => _voteManager!.VotesAlreadyReached;

        public RockTheVoteCommand(GameRules gameRules, EndMapVoteManager endmapVoteManager, StringLocalizer localizer, PluginState pluginState)
        {
            _localizer = localizer;
            _gameRules = gameRules;
            _endMapVoteManager = endmapVoteManager;
            _pluginState = pluginState;
            //_extendConfig = new EndMapVoteManager(); // Initialize _extendConfig (added from copilot, need to check later)
        }

        public void OnMapStart(string map)
        {
            // Kill previous timer if it exists
            if (_mapStartTimer != null)
            {
                _mapStartTimer.Kill();
                _mapStartTimer = null;
            }
            
            // Create new timer and store reference
            _mapStartTimer = new Timer(20.0f, () =>
            {
                _voteManager!.OnMapStart(map);
                _mapStartTimer = null; // Clear reference after execution
            });
            // Reset RTV cooldown tracking on map start
            _firstRtvOfMap = true;
            // Record map start time
            _mapStartTime = DateTime.Now;
        }

        public void CommandServerHandler(CCSPlayerController? player, CommandInfo command)
        {
            // Only handle command from server
            if (player is not null)
                return;

            if (_pluginState.DisableCommands || !_config.Enabled)
            {
                Console.WriteLine(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                return;
            }

            int VoteDuration = _config.VoteDuration;
            string args = command.ArgString.Trim();
            if (!string.IsNullOrEmpty(args))
            {
                if (int.TryParse(args, out int duration))
                {
                    VoteDuration = duration;
                }
            }

            Console.WriteLine($"[MCE] Starting vote with {VoteDuration} seconds duration");

            RtvConfig config = new RtvConfig
            {
                Enabled = true,
                EnabledInWarmup = true,
                MinPlayers = 0,
                MinRounds = 0,
                ChangeMapImmediately = true,
                VoteDuration = VoteDuration,
                VotePercentage = 1
            };
            _endMapVoteManager.StartVote(config);
        }

        public void EnableRtvCommandHandler(CCSPlayerController? player)
        {
            if (player != null && player.IsValid != false)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.admin-only"));
                return;
            }

            if (!_pluginState.RtvDisabled)
            {
                if (player != null)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.already-enabled"));
                else
                    Console.WriteLine(_localizer.LocalizeWithPrefix("rtv.already-enabled"));
                return;
            }

            _pluginState.RtvDisabled = false;
            
            if (player != null)
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.enabled-by-admin", player.PlayerName));
            else
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.enabled-by-server"));
        }

        public void DisableRtvCommandHandler(CCSPlayerController? player)
        {
            if (player != null && player.IsValid != false)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.admin-only"));
                return;
            }

            if (_pluginState.RtvDisabled)
            {
                if (player != null)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.already-disabled"));
                else
                    Console.WriteLine(_localizer.LocalizeWithPrefix("rtv.already-disabled"));
                return;
            }

            _pluginState.RtvDisabled = true;
            
            if (player != null)
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.disabled-by-admin", player.PlayerName));
            else
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.disabled-by-server"));
        }

        public void ForceRtvCommandHandler(CCSPlayerController? player, CommandInfo command)
        {
            if (player != null && player.IsValid != false)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.admin-only"));
                return;
            }

            // Check if we're in warmup
            if (_gameRules.WarmupRunning)
            {
                if (player != null)
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                else
                    Console.WriteLine(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                return;
            }

            // Force RTV vote
            int voteDuration = _config.VoteDuration;
            string args = command.ArgString.Trim();
            if (!string.IsNullOrEmpty(args) && int.TryParse(args, out int duration))
            {
                voteDuration = duration;
            }

            Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.force-rtv", player?.PlayerName ?? "Server"));
            _endMapVoteManager.StartVote(_config);
            _voteManager!.ResetVotes();
        }

        public void CommandHandler(CCSPlayerController? player)
        {
            if (player is null)
                return;

            if (_pluginState.DisableCommands || !_config.Enabled || _pluginState.RtvDisabled)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                return;
            }

            if (_gameRules.WarmupRunning)
            {
                if (!_config.EnabledInWarmup)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                    return;
                }
            }
            else if (_config.MinRounds > 0 && _config.MinRounds > _gameRules.TotalRoundsPlayed)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-rounds", _config.MinRounds));
                return;
            }

            if (ServerManager.ValidPlayerCount() < _config!.MinPlayers)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.MinPlayers));
                return;
            }

            // ignore spectators
            if (player.Team == CsTeam.Spectator)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.spectator-blocked"));
                return;
            }

            // Check if enough time has passed since map start for the first RTV
            if (_firstRtvOfMap && _config.InitialRtvDelay > 0)
            {
                TimeSpan timeSinceMapStart = DateTime.Now - _mapStartTime;
                if (timeSinceMapStart.TotalSeconds < _config.InitialRtvDelay)
                {
                    int remainingSeconds = _config.InitialRtvDelay - (int)timeSinceMapStart.TotalSeconds;
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.wait-seconds", remainingSeconds));
                    return;
                }
            }
            
            // Check for cooldown if this is not the first RTV of the map and cooldown is enabled
            if (!_firstRtvOfMap && _config.VoteCooldownTime > 0)
            {
                TimeSpan timeSinceLastRtv = DateTime.Now - _lastRtvTime;
                if (timeSinceLastRtv.TotalSeconds < _config.VoteCooldownTime)
                {
                    int remainingSeconds = _config.VoteCooldownTime - (int)timeSinceLastRtv.TotalSeconds;
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.wait-seconds", remainingSeconds));
                    return;
                }
            }

            // If we get here on the first RTV, mark it as no longer the first
            _firstRtvOfMap = false;

            VoteResult result = _voteManager!.AddVote(player.UserId!.Value);
            switch (result.Result)
            {
                case VoteResultEnum.Added:
                    Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.rocked-the-vote", player.PlayerName)} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                    break;
                case VoteResultEnum.AlreadyAddedBefore:
                    player.PrintToChat($"{_localizer.LocalizeWithPrefix("rtv.already-rocked-the-vote")} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                    break;
                case VoteResultEnum.VotesAlreadyReached:
                    player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.disabled"));
                    break;
                case VoteResultEnum.VotesReached:
                    Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("rtv.rocked-the-vote", player.PlayerName)} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("rtv.votes-reached"));
                    
                    // Update the last RTV time
                    _lastRtvTime = DateTime.Now;
                    _endMapVoteManager.StartVoteWithCountdown(_config);

                    // reset vote status
                    _voteManager.ResetVotes();
                    break;
            }
        }

        public void UnRTVCommandHandler(CCSPlayerController? player)
        {
            if (player is null)
                return;

            if (_pluginState.DisableCommands || !_config.Enabled || _pluginState.RtvDisabled)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                return;
            }

            if (_gameRules.WarmupRunning)
            {
                if (!_config.EnabledInWarmup)
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                    return;
                }
            }
            else if (_config.MinRounds > 0 && _config.MinRounds > _gameRules.TotalRoundsPlayed)
            {
                player!.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-rounds", _config.MinRounds));
                return;
            }

            if (ServerManager.ValidPlayerCount() < _config!.MinPlayers)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.minimum-players", _config!.MinPlayers));
                return;
            }

            // ignore spectators
            if (player.Team == CsTeam.Spectator)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.spectator-blocked"));
                return;
            }

            _voteManager!.RemoveVote(player.UserId!.Value);
            player.PrintToChat(_localizer.LocalizeWithPrefix("rtv.vote-removed"));
        }

        public void PlayerDisconnected(CCSPlayerController? player)
        {
            if (player?.UserId != null)
                _voteManager!.RemoveVote(player.UserId.Value);
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.Rtv;
            _voteManager = new AsyncVoteManager(_config);

            // set if ignore spectators
            ServerManager.SetIgnoreSpectators(_config.IgnoreSpec);
        }
    }
}
