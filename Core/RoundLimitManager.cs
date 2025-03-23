using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MapChooserExtended.Core
{
    public class RoundLimitManager : IPluginDependency<Plugin, Config>
    {
        private GameRules _gameRules;

        private ConVar? _maxRounds; // move to RoundLimitManager.cs?
        public int RoundLimitValue => _maxRounds?.GetPrimitiveValue<int>() ?? 0;

        public bool UnlimitedRound => RoundLimitValue <= 0;

        public int RoundPlayed
        {
            get
            {
                if (_gameRules.WarmupRunning)
                    return 0;

                return _gameRules.TotalRoundsPlayed;
            }
        }

        public int RoundsRemaining
        {
            get
            {
                if (UnlimitedRound || RoundPlayed > RoundLimitValue)
                    return 0;

                return RoundLimitValue - RoundPlayed;
            }

            set
            {
                _maxRounds?.SetValue((int)value);
            }
        }

        public RoundLimitManager(GameRules gameRules)
        {
            _gameRules = gameRules;
        }

        void LoadCvar()
        {
            _maxRounds = ConVar.Find("mp_maxrounds"); 
        }

        public void OnMapStart(string map)
        {
            LoadCvar();
        }

        public void OnLoad(Plugin plugin)
        {
            LoadCvar();
        }

        public void ExtendRound(int rounds)
        {
            if (!UnlimitedRound)
            {
                RoundsRemaining += rounds;
            }
        }
    }
}