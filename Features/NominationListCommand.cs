using CounterStrikeSharp.API;

namespace cs2_rockthevote;

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Plugin;
using CounterStrikeSharp.API.Modules.Commands;
using cs2_rockthevote.Core;


// public class NominationListCommand : IPluginDependency<Plugin, Config>
public partial class Plugin
{

    public char NewLine = '\u2029';
    [ConsoleCommand("css_nomlist", "Show nominate list")]
    [ConsoleCommand("nomlist", "Show nominate list")]
    public void OnNomlist(CCSPlayerController? player, CommandInfo command)
    {

        var Nomlist = _nominationManager.Nomlist
            .Values
            .SelectMany(list => list)
            .Distinct()
            .Select((map, index) => $"{index + 1}. {map}");

        string Maplist = string.Join(NewLine.ToString(), Nomlist);

        player.PrintToChat(Localize("","nominate.nominate-list"));
        player.PrintToChat(Maplist);
        //player.PrintToChat("********************************");
    }


}