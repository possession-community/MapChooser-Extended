using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace MapChooserExtended
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
            try
            {
                return ValidPlayers(considerBots).Length;
            }
            catch (CounterStrikeSharp.API.Core.NativeException ex) when (ex.Message.Contains("Global Variables not initialized"))
            {
                // サーバー初期化前はデフォルト値を返す
                Console.WriteLine("[MCE] Server not fully initialized yet, returning default player count");
                return 0;
            }
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
