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
                _extManager.CommandServerHandler(player, command!);
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
        private readonly StringLocalizer? _localizer;
        private readonly GameRules? _gameRules;
        //private ExtManager _extManager;
        private PluginState? _pluginState;
        private RtvConfig _config = new();
        private AsyncVoteManager? _voteManager;

        public ExtendCommand(StringLocalizer localizer)
        {
            _localizer = localizer;
        }

        public ExtendCommand(GameRules gameRules)
        {
            _gameRules = gameRules;
        }
        
        public ExtendCommand(PluginState pluginState)
        {
            _pluginState = pluginState;
        }

        public void CommandServerHandler(CCSPlayerController? player, CommandInfo command)
        {
            // Only handle command from server
            if (player is not null)
                return;

            // more todo later
        }

        public void CommandHandler(CCSPlayerController? player)
        {
            if (player is null)
                return;

            // more todo later
        }

        public void PlayerDisconnected(CCSPlayerController? player)
        {
            if (player?.UserId != null)
                _voteManager!.RemoveVote(player.UserId.Value);
        }

        public void OnConfigParsed(Config config)
        {
           // _config = config.Ext;
            _voteManager = new AsyncVoteManager(_config);
        }
    }
}