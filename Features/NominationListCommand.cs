﻿using CounterStrikeSharp.API;

namespace MapChooserExtended;

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Plugin;
using CounterStrikeSharp.API.Modules.Commands;
using MapChooserExtended.Core;

public partial class Plugin
{

    public char NewLine = '\u2029';
    [ConsoleCommand("css_nomlist", "Show nominate list")]
    public void OnNomlist(CCSPlayerController? player, CommandInfo command)
    {

        var Nomlist = _nominationManager.Nomlist
            .SelectMany(kvp => kvp.Value.Maps.Select(map => new { PlayerName = kvp.Key, Map = map }))
            .Distinct()
            .Select((entry, index) =>
            {
                var playerName = ServerManager.ValidPlayers()
                    .FirstOrDefault(p => p.UserId == entry.PlayerName)?.PlayerName ?? "Unknown";
                return $"{index + 1}. {entry.Map} - {playerName}";
            });

        string Maplist = string.Join(NewLine.ToString(), Nomlist);

        player?.PrintToChat(Localize("","nominate.nominate-list"));
        player?.PrintToChat(Maplist);
        //player.PrintToChat("********************************");
    }


}