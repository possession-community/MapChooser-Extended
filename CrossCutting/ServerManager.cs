using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace cs2_rockthevote
{
    public static class ServerManager
    {
        private static bool _ignoreSpectators = true;

        public static void SetIgnoreSpectators(bool ignore)
        {
            _ignoreSpectators = ignore;
        }

        public static bool GetIgnoreSpectators()
        {
            return _ignoreSpectators;
        }

        public static CCSPlayerController[] ValidPlayers(bool considerBots = false)
        {
            return Utilities.GetPlayers()
                .Where(x => x.ReallyValid(considerBots, _ignoreSpectators))
                .ToArray();
        }

        public static int ValidPlayerCount(bool considerBots = false)
        {
            return ValidPlayers(considerBots).Length;
        }

        //public static CCSPlayerController[] ValidPlayers(bool considerBots = false, bool ignoreSpecs = false)
        //{
        //    //considerBots = true;
        //    return Utilities.GetPlayers()
        //        .Where(x => x.ReallyValid(considerBots, ignoreSpecs))
        //        .Where(x => !x.IsHLTV)
        //        .Where(x => considerBots || !x.IsBot)
        //        .ToArray();
        //}

        //public static int ValidPlayerCount(bool considerBots = false, bool ignoreSpecs = false)
        //{
        //    return ValidPlayers(considerBots, ignoreSpecs).Length;
        //}
    }
}
