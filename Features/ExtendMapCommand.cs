
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Localization;

namespace cs2_rockthevote
{
    public partial class Plugin
    {
        [ConsoleCommand("ext", "Vote to extend current map")]
        [ConsoleCommand("css_ext", "Vote to extend current map")]
        [ConsoleCommand("extend", "Vote to extend current map")]
        [ConsoleCommand("css_extend", "Vote to extend current map")]
        [ConsoleCommand("extendmap", "Vote to extend current map")]
        [ConsoleCommand("css_extendmap", "Vote to extend current map")]
        public void OnExtend(CCSPlayerController? player, CommandInfo? command)
        {
            _extendMapManager.CommandHandler(player!);
        }
    }

    public class ExtendMapCommand : IPluginDependency<Plugin, Config>
    {
        private readonly StringLocalizer _localizer;
        private readonly GameRules _gameRules;
        private PluginState _pluginState;
        private ExtendMapConfig _config = new();
        private AsyncVoteManager? _voteManager;
        private Plugin _ = null;
        public bool VotesAlreadyReached => _voteManager!.VotesAlreadyReached;

        public ExtendMapCommand(GameRules gameRules, EndMapVoteManager endmapVoteManager, StringLocalizer localizer, PluginState pluginState, IStringLocalizer stringLocalizer)
        {
            _localizer = new StringLocalizer(stringLocalizer, "extendmap.prefix");
            _gameRules = gameRules;
            _pluginState = pluginState;
        }

        public void Onload(Plugin plugin)
        {
            _ = plugin;
        }

        public void OnMapStart(string map)
        {
            _voteManager!.OnMapStart(map);
        }

        public void CommandHandler(CCSPlayerController? player)
        {
            if (player is null || !player.IsValid || player.IsBot)
                return;

            /*if (_pluginState.DisableCommands || !_config.Enabled)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                return;
            }*/

            if (!_config.Enabled)
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

            VoteResult result = _voteManager!.AddVote(player.UserId!.Value);
            switch (result.Result)
            {
                case VoteResultEnum.Added:
                    Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("extendmap.player-voted", player.PlayerName)} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                    break;
                case VoteResultEnum.AlreadyAddedBefore:
                    player.PrintToChat($"{_localizer.LocalizeWithPrefix("extendmap.already-voted")} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                    break;
                case VoteResultEnum.VotesAlreadyReached:
                    player.PrintToChat($"{_localizer.LocalizeWithPrefix("extendmap.already-extended", _config.ExtendLength)}");
                    break;
                case VoteResultEnum.VotesReached:
                    Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("extendmap.player-voted", player.PlayerName)} {_localizer.Localize("general.votes-needed", result.VoteCount, result.RequiredVotes)}");
                    Server.PrintToChatAll($"{_localizer.LocalizeWithPrefix("extendmap.map-extended", _config.ExtendLength)}");
                    ApplyExtend();
                    break;
            }
        }

        void ApplyExtend()
        {
            var mp_timelimit = ConVar.Find("mp_timelimit");
            if (mp_timelimit != null)
            {
                var timelimit = mp_timelimit.GetPrimitiveValue<float>();
                if (timelimit > 0)
                {
                    float newTimeLimit = timelimit + _config.ExtendTimeStep;
                    mp_timelimit.SetValue(newTimeLimit);

                    if (_ != null && _voteManager != null)
                    {
                        _.AddTimer(_config.ExtendTimeStep / 4, () =>
                        {
                            _voteManager.OnMapStart("");
                        });
                    }
                }
                else
                {
                    Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendmap.cannot-extend-timelimit-zero"));
                }
            }
            else
            {
                Server.PrintToChatAll(_localizer.LocalizeWithPrefix("extendmap.cannot-extend-no-cvar"));
            }
        }

        public void PlayerDisconnected(CCSPlayerController? player)
        {
            if (player?.UserId != null)
                _voteManager!.RemoveVote(player.UserId.Value);
        }
        public void OnConfigParsed(Config config)
        {
            _config = config.ExtendMapVote;
            _voteManager = new AsyncVoteManager(_config);
        }
    }
}
