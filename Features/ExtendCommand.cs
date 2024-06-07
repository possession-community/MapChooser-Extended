
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace cs2_rockthevote
{
    public partial class Plugin
    {
        [ConsoleCommand("ext", "Votes to extend current map")]
        public void OnEXT(CCSPlayerController? player, CommandInfo? command)
        {
            if (player is null)
            {
                // Handle server command
                _extManager.CommandServerHandler(player, command);
            }
            else
            {
                // Handle player command
                _extManager.CommandHandler(player!);
            }
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult EventPlayerDisconnectEXT(EventPlayerDisconnect @event, GameEventInfo @eventInfo)
        {
            var player = @event.Userid;
            _extManager.PlayerDisconnected(player);
            return HookResult.Continue;
        }
    }
    public class ExtendCommand : IPluginDependency<Plugin, Config>
    {
        public void PlayerDisconnected(CCSPlayerController? player)
        {
            if (player?.UserId != null)
                _voteManager!.RemoveVote(player.UserId.Value);
        }

        public void OnConfigParsed(Config config)
        {
            _config = config.Ext;
            _voteManager = new AsyncVoteManager(_config);
        }
    }
}