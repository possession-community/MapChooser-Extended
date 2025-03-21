﻿﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using cs2_rockthevote.Core;
using cs2_rockthevote.Features;
using Microsoft.Extensions.DependencyInjection;
using static CounterStrikeSharp.API.Core.Listeners;

namespace cs2_rockthevote
{
    public class PluginDependencyInjection : IPluginServiceCollection<Plugin>
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var di = new DependencyManager<Plugin, Config>();
            di.LoadDependencies(typeof(Plugin).Assembly);
            di.AddIt(serviceCollection);
            serviceCollection.AddScoped<StringLocalizer>();
        }
    }

    public partial class Plugin : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "RockTheVote";
        public override string ModuleVersion => "1.9.4";
        public override string ModuleAuthor => "abnerfs, Oz-Lin";
        public override string ModuleDescription => "https://github.com/oz-lin/cs2-rockthevote";

        private readonly DependencyManager<Plugin, Config> _dependencyManager;
        private readonly NominationCommand _nominationManager;
        private readonly MapSettingsManager _mapSettingsManager;
        private readonly MapLister _mapLister;
        private readonly MapCooldown _mapCooldown;
        private readonly ChangeMapManager _changeMapManager;
        private readonly EndMapVoteManager _endMapVoteManager;
        private readonly VotemapCommand _votemapManager;
        private readonly RockTheVoteCommand _rtvManager;
        //private readonly ExtendCommand _extManager;
        private readonly TimeLeftCommand _timeLeft;
        private readonly ExtendRoundTimeCommand _extendRoundTime;
        private readonly VoteExtendRoundTimeCommand _voteExtendRoundTime;
        private readonly NextMapCommand _nextMap;
        private readonly DisplayMapListCommandHandler _displayMapListCommandHandler;
        private readonly ExtendMapCommand _extendMapManager;
        private readonly RevoteCommand _revoteCommand;
        private readonly WorkshopSynchronizer _workshopSynchronizer;

        public Plugin(DependencyManager<Plugin, Config> dependencyManager,
            NominationCommand nominationManager,
            ChangeMapManager changeMapManager,
            VotemapCommand voteMapManager,
            RockTheVoteCommand rtvManager,
            //ExtendCommand extManager,
            TimeLeftCommand timeLeft,
            ExtendRoundTimeCommand extendRoundTime,
            VoteExtendRoundTimeCommand voteExtendRoundTime,
            NextMapCommand nextMap,
            EndMapVoteManager endMapVoteManager,
            DisplayMapListCommandHandler displayMapListCommandHandler,
            MapLister mapLister,
            ExtendMapCommand extendMapManager,
            RevoteCommand revoteCommand,
            WorkshopSynchronizer workshopSynchronizer)
        {
            _dependencyManager = dependencyManager;
            _nominationManager = nominationManager;
            _changeMapManager = changeMapManager;
            _votemapManager = voteMapManager;
            _rtvManager = rtvManager;
            //_extManager = extManager;
            _timeLeft = timeLeft;
            _extendRoundTime = extendRoundTime;
            _voteExtendRoundTime = voteExtendRoundTime;
            _nextMap = nextMap;
            _endMapVoteManager = endMapVoteManager;
            _displayMapListCommandHandler = displayMapListCommandHandler;
            _mapLister = mapLister;
            _extendMapManager = extendMapManager;
            _revoteCommand = revoteCommand;
            _workshopSynchronizer = workshopSynchronizer;
        }

        public Config Config { get; set; } = null!;

        public string Localize(string prefix, string key, params object[] values)
        {
            return $"{Localizer[prefix]} {Localizer[key, values]}";
        }

        public override void Load(bool hotReload)
        {
            _dependencyManager.OnPluginLoad(this);
            _mapSettingsManager.OnLoad(this); // Initialize map settings
            _mapLister.OnLoad(this);       // Load maps from settings
            RegisterListener<OnMapStart>(_dependencyManager.OnMapStart);
        }

        [GameEventHandler(HookMode.Post)]
        public HookResult OnChat(EventPlayerChat @event, GameEventInfo info)
        {
            var player = Utilities.GetPlayerFromUserid(@event.Userid);
            if (player is not null)
            {
                var text = @event.Text.Trim().ToLower();
                if (text == "rtv")
                {
                    _rtvManager.CommandHandler(player);
                }
                else if (text == "ext")
                {
                    //_extManager.CommandHandler(player);
                    _extendMapManager.CommandHandler(player);
                }
                else if (text.StartsWith("nominate"))
                {
                    var split = text.Split("nominate");
                    var map = split.Length > 1 ? split[1].Trim() : "";
                    _nominationManager.CommandHandler(player, map);
                }
                else if (text.StartsWith("nom"))
                {
                    var split = text.Split("nom");
                    var map = split.Length > 1 ? split[1].Trim() : "";
                    _nominationManager.CommandHandler(player, map);
                }
                else if (text.StartsWith("yd"))
                {
                    var split = text.Split("yd");
                    var map = split.Length > 1 ? split[1].Trim() : "";
                    _nominationManager.CommandHandler(player, map);
                }
                else if (text.StartsWith("votemap"))
                {
                    var split = text.Split("votemap");
                    var map = split.Length > 1 ? split[1].Trim() : "";
                    _votemapManager.CommandHandler(player, map);
                }
                else if (text.StartsWith("vm"))
                {
                    var split = text.Split("vm");
                    var map = split.Length > 1 ? split[1].Trim() : "";
                    _votemapManager.CommandHandler(player, map);
                }
                else if (text.StartsWith("timeleft"))
                {
                    _timeLeft.CommandHandler(player);
                }
                else if (text.StartsWith("nextmap"))
                {
                    _nextMap.CommandHandler(player);
                }
                else if (text.StartsWith("extend"))
                {
                    _extendMapManager.CommandHandler(player);
                }
                // TODO: Implement this later
                //else if (text == "revote")
                //{
                //    _endMapVoteManager.HandleRevoteCommand(player);
                //}
            }
            return HookResult.Continue;
        }

        public void OnConfigParsed(Config config)
        {
            Config = config;

            if (Config.Version < 15)
                Console.WriteLine("[RockTheVote] please delete it from addons/counterstrikesharp/configs/plugins/RockTheVote and let the plugin recreate it on load");

            if (Config.Version < 13)
                throw new Exception("Your config file is too old, please delete it from addons/counterstrikesharp/configs/plugins/RockTheVote and let the plugin recreate it on load");

            _dependencyManager.OnConfigParsed(config);
        }
    }
}
