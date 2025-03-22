﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;

namespace MapChooserExtended.Core
{
    public class TimeLimitManager : IPluginDependency<Plugin, Config>
    {
        private GameRules _gameRules;

        private ConVar? _timeLimit;

        public decimal TimeLimitValue {
            get => (decimal)(_timeLimit?.GetPrimitiveValue<float>() ?? 0F) * 60M;
            set => _timeLimit?.SetValue((float)(value / 60M));
        }

        public bool UnlimitedTime => TimeLimitValue <= 0;

        public decimal TimePlayed
        {
            get
            {
                if (_gameRules.WarmupRunning)
                    return 0;

                return (decimal)(Server.CurrentTime - _gameRules.GameStartTime);
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
            // TODO: implement extending time
        }
    }
}
