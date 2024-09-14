using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Plugin;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using cs2_rockthevote.Core;
using Microsoft.Extensions.Localization;

namespace cs2_rockthevote
{
    public partial class Plugin
    {
        [ConsoleCommand("css_voteextend", "Extends time for the current map")]
        [ConsoleCommand("css_ve", "Extends time for the current map")]
        //[ConsoleCommand("css_ext", "Extends time for the current map")] //move this to ExtendRoundTimeCommand. 60% player vote to trigger extension
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/vip")]
        public void OnVoteExtendRoundTimeCommandCommand(CCSPlayerController? player, CommandInfo commandInfo)
        {
            _voteExtendRoundTime.CommandHandler(player!, commandInfo);
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult EventPlayerDisconnectExtend(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
        {
            var player = @event.Userid;
            _rtvManager.PlayerDisconnected(player);
            return HookResult.Continue;
        }
    }

    public class VoteExtendRoundTimeCommand : IPluginDependency<Plugin, Config>
    {
        private TimeLimitManager _timeLimitManager;
        private ExtendRoundTimeManager _extendRoundTimeManager;
        private readonly GameRules _gameRules;
        private StringLocalizer _localizer;
        private PluginState _pluginState;
        private VipExtendMapConfig _config = new();

        public VoteExtendRoundTimeCommand(TimeLimitManager timeLimitManager, ExtendRoundTimeManager extendRoundTimeManager, GameRules gameRules, IStringLocalizer stringLocalizer, PluginState pluginState)
        {
            _gameRules = gameRules;
            _localizer = new StringLocalizer(stringLocalizer, "extendtime.prefix");
            _timeLimitManager = timeLimitManager;
            _extendRoundTimeManager = extendRoundTimeManager;
            _pluginState = pluginState;
        }

        public void CommandHandler(CCSPlayerController player, CommandInfo commandInfo)
        {
            if (_pluginState.VoteExtendsLeft == 0)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("extendtime.extend-limit-met"));
                return;
            }

            if (_pluginState.EofVoteHappening)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.disabled"));
                return;
            }

            if (_gameRules.WarmupRunning)
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("general.validation.warmup"));
                return;
            }

            if (!_timeLimitManager.UnlimitedTime)
            {
                // Initialize the extend map vote
                if (!_pluginState.ExtendTimeVoteHappening)
                {
                    _extendRoundTimeManager.StartVote(_config);
                }
                else
                {
                    player.PrintToChat(_localizer.LocalizeWithPrefix("extendtime.notapplicable"));
                }
            }
            else
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("extendtime.notapplicable"));
            }
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.VipExtendMapVote;
            _pluginState.VoteExtendsLeft = config.VipExtendMapVote.ExtendLimit;
        }
    }
}
