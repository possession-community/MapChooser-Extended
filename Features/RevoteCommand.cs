using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using MapChooserExtended.Core;
using Microsoft.Extensions.Localization;

namespace MapChooserExtended
{
    public partial class Plugin
    {
        [ConsoleCommand("revote", "Revote the current vote")]
        [ConsoleCommand("css_revote", "Revote the current vote")]
        public void OnRevote(CCSPlayerController? player, CommandInfo? command)
        {
            _revoteCommand.CommandHandler(player!);
        }
    }

    public class RevoteCommand : IPluginDependency<Plugin, Config>
    {
        private readonly StringLocalizer _localizer;
        private readonly ExtendRoundTimeManager _extendRoundTimeManager;
        private readonly EndMapVoteManager _endMapVoteManager;

        public RevoteCommand(IStringLocalizer localizer, ExtendRoundTimeManager extendRoundTimeManager, EndMapVoteManager endMapVoteManager)
        {
            _localizer = new StringLocalizer(localizer, "revote.prefix");
            _extendRoundTimeManager = extendRoundTimeManager;
            _endMapVoteManager = endMapVoteManager;
        }

        public void CommandHandler(CCSPlayerController player)
        {
            if (player is null || !player.IsValid || player.IsBot)
                return;

            if (_extendRoundTimeManager != null && _extendRoundTimeManager.VoteInProgress)
            {
                _extendRoundTimeManager.RevokeVote(player);
                player.PrintToChat(_localizer.LocalizeWithPrefix("revote.success"));
            }
            else if (_endMapVoteManager != null && _endMapVoteManager.VoteInProgress)
            {
                _endMapVoteManager.RevokeVote(player);
                player.PrintToChat(_localizer.LocalizeWithPrefix("revote.success"));
            }
            else
            {
                player.PrintToChat(_localizer.LocalizeWithPrefix("revote.no-vote-in-progress"));
            }
        }
    }
}
