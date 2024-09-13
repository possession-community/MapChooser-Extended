namespace cs2_rockthevote;

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;


public partial class Plugin
{
    public char NewLine = '\u2029';
    [ConsoleCommand("nomlist", "Show nominate list")]
    public void OnNomlist(CCSPlayerController? player, CommandInfo command)
    {

        var Nomlist = _nominationManager.Nomlist
            .Values
            .SelectMany(list => list)
            .Distinct()
            .Select((map, index) => $"{index + 1}. {map}");

        string Maplist = string.Join(NewLine.ToString(), Nomlist);

        player.PrintToChat("*** Nominated Maps: ***");
        player.PrintToChat(Maplist);
        player.PrintToChat("********************************");
    }


}