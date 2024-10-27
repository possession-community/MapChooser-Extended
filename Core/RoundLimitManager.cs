using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;

namespace cs2_rockthevote.Core
{
    public class RoundLimitManager : IPluginDependency<Plugin, Config>
    {
        private GameRules _gameRules;

        private ConVar? _maxRounds; // move to RoundLimitManager.cs?

        //private decimal TimeLimitValue => (decimal)(_timeLimit?.GetPrimitiveValue<float>() ?? 0F) * 60M;
        public int RoundLimitValue => _maxRounds?.GetPrimitiveValue<int>() ?? 0;

        public bool UnlimitedRound => RoundLimitValue <= 0;

        public int RoundPlayed
        {
            get
            {
                if (_gameRules.WarmupRunning)
                    return 0;

                //return (decimal)(Server.CurrentTime - _gameRules.GameStartTime);
                return -1; // todo
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
            // TODO: implement extending rounds
        }
    }
}