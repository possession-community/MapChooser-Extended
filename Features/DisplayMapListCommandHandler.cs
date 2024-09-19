namespace cs2_rockthevote;

using cs2_rockthevote.Core;

public class DisplayMapListCommandHandler : IPluginDependency<Plugin, Config>
{
    private readonly MapLister _mapLister;
    private readonly int _mapsPerPage = 25;

    public DisplayMapListCommandHandler(MapLister mapLister)
    {
        _mapLister = mapLister;
    }

    public void OnLoad(Plugin plugin)
    {
        plugin.AddCommand("maplist", "Displays maplist into console", (player, info) =>
        {
            var part = info.GetArg(1); // current part to display
            if (!int.TryParse(part, out var partNumber))
            {
                player?.PrintToChat("You provided wrong argument, was executed with 0 by default.");
                partNumber = 0;
            }
            if (partNumber < 0)
            {
                player?.PrintToChat("Invalid part number. Please provide number greater than 0.");
                return;
            }
            _mapLister.GetMaps().Skip(_mapsPerPage * partNumber).Take(_mapsPerPage).ToList().ForEach(map =>
            {
                player?.PrintToConsole(map.Name);
            });
        });
    }
}
