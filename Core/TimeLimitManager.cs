using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MapChooserExtended.Core
{
    public class TimeLimitManager : IPluginDependency<Plugin, Config>
    {
        private GameRules _gameRules;

        private ConVar? _timeLimit;

        public decimal TimeLimitValue {
            get => (decimal)(_timeLimit?.GetPrimitiveValue<float>() ?? 0F);
            set => _timeLimit?.SetValue((float)value);
        }

        public bool UnlimitedTime => TimeLimitValue <= 0;

        public decimal TimePlayed
        {
            get
            {
                if (_gameRules.WarmupRunning)
                    return 0;

                return (decimal)(Server.CurrentTime - _gameRules.GameStartTime) / 60M;
            }
        }

        public decimal TimeRemaining
        {
            get
            {
                if (UnlimitedTime || TimePlayed > TimeLimitValue)
                    return 0;

                return TimeLimitValue - TimePlayed;
            }

            set
            {
                _timeLimit?.SetValue((float)value);
            }
        }

        public TimeLimitManager(GameRules gameRules)
        {
            _gameRules = gameRules;
        }

        void LoadCvar()
        {
            _timeLimit = ConVar.Find("mp_timelimit");
        }

        public void OnMapStart(string map)
        {
            LoadCvar();
        }

        public void OnLoad(Plugin plugin)
        {
            LoadCvar();
        }

        public void ExtendTime(int minutes)
        {
            if (!UnlimitedTime)
            {
                TimeLimitValue += minutes;
            }
        }
    }
}
