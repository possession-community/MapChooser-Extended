using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace MapChooserExtended
{
    public interface ICommandConfig
    {
        public bool EnabledInWarmup { get; set; }
        public int MinPlayers { get; set; }
        public int MinRounds { get; set; }
    }

    public interface IVoteConfig
    {
        public int VotePercentage { get; set; }
        public bool ChangeMapImmediately { get; set; }
        public bool IgnoreSpec { get; set; }
    }

    public interface IEndOfMapConfig
    {
        public int MapsToShow { get; set; }
        public bool ChangeMapImmediately { get; set; }
        public int VoteDuration { get; set; }
        public int VoteCountdownTime { get; set; }
    }

    public interface IExtendMapConfig
    {
        public bool Enabled { get; set; }
        public int VoteDuration { get; set; }
        public int VotePercentage { get; set; }
    }

    public class EndOfMapConfig : IEndOfMapConfig, IExtendMapConfig
    {
        public bool Enabled { get; set; } = true;
        public int MapsToShow { get; set; } = 6;
        public bool ChangeMapImmediately { get; set; } = false;
        public int VoteDuration { get; set; } = 30;
        public int TriggerSecondsBeforeEnd { get; set; } = 120;
        public int TriggerRoundsBeforeEnd { get; set; } = 2;
        public float DelayToChangeInTheEnd { get; set; } = 6F;
        public int VoteCountdownTime { get; set; } = 10;
        public int VotePercentage { get; set; } = 60;
    }

    public class RtvConfig : ICommandConfig, IVoteConfig, IEndOfMapConfig, IExtendMapConfig
    {
        public bool Enabled { get; set; } = true;
        public bool EnabledInWarmup { get; set; } = true;
        public bool NominationEnabled { get; set; } = true;
        public int MinPlayers { get; set; } = 0;
        public int MinRounds { get; set; } = 0;
        public bool ChangeMapImmediately { get; set; } = false;
        public int MapsToShow { get; set; } = 6;
        public int VoteDuration { get; set; } = 30;
        public int VotePercentage { get; set; } = 60;
        public bool DontChangeRtv { get; set; } = true;
        public bool IgnoreSpec { get; set; } = true;
        public int RtvCooldownTime { get; set; } = 300;
        public int InitialRtvDelay { get; set; } = 300;
        public int VoteCountdownTime { get; set; } = 10;
    }

    public class TimeleftConfig
    {
        public bool ShowToAll { get; set; } = false;
    }

    public class NextmapConfig
    {
        public bool ShowToAll { get; set; } = false;
    }

    public class WorkshopConfig {
        public string[] collection_ids { get; set; } = [];
    }

    public class Config : IBasePluginConfig
    {
        public int Version { get; set; } = 15;
        public RtvConfig Rtv { get; set; } = new();
        public EndOfMapConfig EndOfMapVote { get; set; } = new();
        public TimeleftConfig Timeleft { get; set; } = new();
        public NextmapConfig Nextmap { get; set; } = new();
        public WorkshopConfig Workshop { get; set; } = new();
    }
}
