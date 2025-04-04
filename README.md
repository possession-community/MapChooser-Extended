# MapChooser-Extended

General purpose map voting plugin, rtv, nominate, extend, and more + Bug fix.

**Current Version: 2.0.0**

Based on [Oz-Lin/cs2-rockthevote](https://github.com/Oz-Lin/cs2-rockthevote)

## Requirements
[Latest release of Counter Strike Sharp](https://github.com/roflmuffin/CounterStrikeSharp)

# Installation
1. Download the latest release from the [Releases](https://github.com/possession-community/MapChooser-Extended/releases) page
2. Extract the contents to your CS2 server's `game/csgo/` directory
3. Make sure the plugin is placed in the correct directory: `game/csgo/addons/counterstrikesharp/plugins/MapChooserExtended`
4. Restart your server or load the plugin using the `css_plugins load MapChooserExtended` command

# Work in Progress
- [ ] Vote sounds settings
- [ ] Menu optimization

## Future Plans
- Optimize codebase for better performance
  - Reduce duplicate code
  - Improve performance of vote validation
  - Simplify complex code sections
- Improve Nominate implementation
  - Hide already nominated maps in the nomination menu
  - Limit nominations settings, to one per player
- Improve vote result notifications
  - Improve messages when non-map options are selected (Extend, Ignore)

# Features

MapChooser Extended provides a comprehensive set of features for map management and voting in CS2 servers:

- **Rock The Vote (RTV)**: Allows players to vote for changing the current map
- **Map Nominations**: Players can nominate maps to be included in votes
- **End of Map Voting**: Automatic map vote before the current map ends
- **Map Extension**: Option to extend the current map through voting
- **Per-Map Settings**: Detailed configuration for each map individually
- **Workshop Integration**: Automatic synchronization with Steam Workshop collections
- **Admin Controls**: Comprehensive commands for server administrators
- **Multi-language Support**: Translations for multiple languages
- **CS2 Screen Menus**: Modern HTML-based menus instead of chat menus
- **Map Cooldown System**: Prevents the same maps from appearing too frequently
- **Time-based Map Cycling**: Configure maps to be available during specific hours
- **Player Count Conditions**: Set maps to be available based on server population
- **Map-specific Configuration**: Individual settings for each map including extend options
- **Custom Config Execution**: Execute specific configs when maps start

# Available Commands

## General Commands (Everyone)
| Command | Aliases | Description |
| ------- | ------- | ----------- |
| `css_rtv` | `rtv` | Start a Rock The Vote map vote |
| `css_unrtv` | `unrtv` | Remove your vote to rock the vote |
| `css_nominate <map>` | `css_nom <map>` | Nominate a map for voting |
| `css_nomlist` |  | Display a list of nominated maps and who nominated them |
| `css_timeleft` | `timeleft` | Display the remaining time or roundtime or rounds on the current map |
| `css_nextmap` | `nextmap` | Display the next map in rotation |

## Admin Commands

### Map Control Commands
| Command | Aliases | Permission | Description |
| ------- | ------- | ---------- | ----------- |
| `css_extend <number>` | `css_ext`, `css_extendmap` | `@css/changemap` | Extend the current map time limit or maxrounds or round time |
| `css_mcemaps <map>` | | `@css/changemap` | Change the map immediately |
| `css_setnextmap <map>` | | `@css/changemap` | Set the next map in rotation |

### RTV Control Commands
| Command | Aliases | Permission | Description |
| ------- | ------- | ---------- | ----------- |
| `css_enable_rtv` | | `@css/changemap` | Enable RTV command |
| `css_disable_rtv` | | `@css/changemap` | Disable RTV command |
| `css_force_rtv` | | `@css/changemap` | Force RTV vote |

### Nomination Control Commands
| Command | Aliases | Permission | Description |
| ------- | ------- | ---------- | ----------- |
| `css_enable_nominate` | | `@css/changemap` | Enable nomination command |
| `css_disable_nominate` | | `@css/changemap` | Disable nomination command |
| `css_nominate_addmap <map>` | | `@css/changemap` | Add a map to nomination list |
| `css_nominate_removemap <map>` | | `@css/changemap` | Remove a map from nomination list |

# Translations
| Language             | Contributor                    |
| -------------------- | --------------------           |
| Brazilian Portuguese | abnerfs + ChatGPT              |
| English              | abnerfs + Oz-Lin               |
| Ukrainian            | panikajo + ChatGPT             |
| Turkish              | brkvlr + ChatGPT               |
| Russian              | Auttend + ChatGPT              |
| Latvian              | rcon420 + ChatGPT              |
| Hungarian            | Chickender, lovasatt + ChatGPT |
| Polish               | D3X + ChatGPT                  |
| French               | o3LL + ChatGPT                 |
| Chinese (zh-Hans)    | himenekocn + Oz-Lin            |
| Chinese (zh-Hant)    | himenekocn + Oz-Lin            |
| Korean               | wjdrkfka3                      |
| Japanese             | uru                            |

# Configs
- A config file will be created in `addons/counterstrikesharp/configs/plugins/MapChooserExtended` the first time you load the plugin.
- Changes in the config file will require you to reload the plugin or restart the server (change the map won't work).
- Now, Per-map settings are stored in `addons/counterstrikesharp/configs/plugins/MapChooserExtended/maps/` directory as JSON files.
  - MCE don't use `maplist.txt`, see **Map Settings System** section.

The main configuration file is located at `addons/counterstrikesharp/configs/plugins/MapChooserExtended/MapChooserExtended.json`. Here are the key settings:

| Section | Description |
| ------- | ----------- |
| Version | Configuration version (current: 15) |
| Rtv | Rock The Vote settings |
| EndOfMapVote | End of map vote settings |
| Timeleft | Timeleft command settings |
| Nextmap | Nextmap command settings |
| Workshop | Workshop synchronization settings |

## RockTheVote
Players can type rtv to request the map to be changed, once a number of votes is reached (by default 60% of players in the server) a vote will start for the next map, this vote lasts up to 30 seconds (hardcoded for now), in the end server changes to the winner map.

| Config               | Description                                                         | Default Value | Min   | Max   |
| -------------------  | --------------------------------------------------------------------| ------------- | ----- | ----- |
| Enabled              | Enable/Disable RTV functionality                                    | true          | false | true  |
| EnabledInWarmup      | Enable/Disable RTV during warmup                                    | false         | false | true  |
| NominationEnabled    | Enable/Disable nomination                                           | true          | false | true  |
| MinPlayers           | Minimum amount of players to enable RTV/Nominate                    | 0             | 0     |       |
| MinRounds            | Minimum rounds to enable RTV/Nominate                               | 0             | 0     |       |
| ChangeMapImmediately | Whether to change the map immediatly when vote ends or not          | false         | false | true  |
| MapsToShow           | Amount of maps to show in vote,                                     | 6             | 1     | 5     |
| VoteDuration         | Seconds the RTV should last                                         | 30            | 1     |       |
| VotePercentage       | Percentage of players that should type RTV in order to start a vote | 60            | 0     | 100   |
| DontChangeRtv        | Enable/Disable option not to change the current map                 | true          | false | true  |
| IgnoreSpec           | Ignore spectators from vote count                                   | true          | false | true  |
| InitialRtvDelay      | Cooldown timer to start the "first" RTV                             | 300           | 0     | -     |
| RtvCooldownTime      | Cooldown timer to start the next RTV                                | 300           | 0     | -     |

## End of map vote
Based on `mp_timelimit` and `mp_maxrounds` cvar before the map ends a RTV like vote will start to define the next map, it can be configured to change immediatly or only when the map actually ends

| Config                  | Description                                                                              | Default Value | Min   | Max  |
| ----------------------- | -----------------------------------------------------------------------------------------| ------------- | ----- | -----|
| Enabled                 | Enable/Disable end of map vote functionality                                             | true          | false | true |
| ChangeMapImmediately    | Whether to change the map immediatly when vote ends or not                               | false         | false | true |
| MapsToShow              | Amount of maps to show in vote,                                                          | 6             | 1     | 5    |
| VoteDuration            | Seconds the RTV should last                                                              | 30            | 1     | -    |
| TriggerSecondsBeforeEnd | Amount of seconds before end of the map that should trigger the vote, for `mp_timelimit` | 120           | 1     | -    |
| TriggerRoundsBeforeEnd  | Amount of rounds before end of map that should trigger the vote,      for `mp_maxrounds` | 2             | 1     | -    |
| DelayToChangeInTheEnd   | Delay in seconds to change the map after the win panel is shown to the players           | 6             | 3     | -    |

## Timeleft
Players can type `timeleft` to see how much time is left in the current map 

| Config    | Description                                                                      | Default Value | Min   | Max  |
| --------- | -------------------------------------------------------------------------------- | ------------- | ----- | ---- |
| ShowToAll | Whether to show command response to everyone or just the player that executed it | false         | false | true |

## Nextmap
Players can type `nextmap` to see which map is going to be played next

| Config    | Description                                                                      | Default Value | Min   | Max  |
| --------- | -------------------------------------------------------------------------------- | ------------- | ----- | ---- |
| ShowToAll | Whether to show command response to everyone or just the player that executed it | false         | false | true |

# Map Settings System

Version 2.0.0 includes an improved map settings system that allows detailed configuration for each map individually.

This system enables server administrators to apply different settings to each map, including:
- Extend settings (times, number) that were previously configured globally
- Time-based availability (specify hours when a map should be available)
- Player count conditions (min/max players for a map to be available)
- Cooldown settings to prevent maps from appearing too frequently
- Map-specific match settings (time limit vs round limit)
- Nomination controls (who can nominate the map)
- Custom config execution when maps start

Editing maplist.txt is no longer necessary as the plugin now uses individual map configuration files.

While this system requires creating a file for each map, the base inheritance system allows you to define common settings once and have specific maps inherit and override only what's needed, reducing duplication and maintenance effort.

## Map Settings Files

Map settings are stored in `addons/counterstrikesharp/configs/plugins/MapChooserExtended/maps/` directory as JSON files.

Each map has its own settings file (e.g., `de_dust2.json`) that inherits from the default settings (`maps/default.json`).

If a specific map is loaded without a specific map configuration, the `maps/default.json` will be used to automatically generate the map configuration.

And, each map settings can be set multiple cfgs for exec some cfgs when OnMapStart. See `Settings.cfgs` comment on `example.json`

`default.json` is here:

<details> <summary>Click to view</summary>

```jsonc
{
  // The json specified in base is loaded as the base `Settings`. You can overwrite the loaded settings by setting `Settings` as usual.
  "base": "base/default.json",
  "meta": {
    "name": "default",
    "display": "",
    "workshop_id": ""
  },
  "settings": {
    "enabled": true,
    "times": [],
    "players": {
      "min": 0,
      "max": 64
    },
    "cooldown": {
      "count": 0,
      "current_count": 0,
      "tags": []
    },
    "nomination": {
      "admin": false,
      "enabled": true
    },
    "match": {
      "type": 0,
      "limit": "30"
    },
    "extend": {
      "enabled": true,
      "times": 2,
      "number": 15
    },
    "cfgs": []
  }
}
```

</details>

### Example (maps/map.json)

example map settings here:
<details> <summary>Click to view</summary>

```jsonc
{
  // The json specified in base is loaded as the base `Settings`. You can overwrite the loaded settings by setting `Settings` as usual.
  "base": "base/default.json",
  "meta": {
    // actuall vpk map name
    "name": "de_dust2",
    // name that displayed while map vote
    // if null or empty, plugin use meta.name
    // currently, not used this for now
    "display": "Dust II",
    // workshop map id
    // if null or empty, its official map or host collection
    "workshop_id": ""
  },
  "settings": {
    // enabled map cycle
    "enabled": true,
    // the time period included in the map cycle, only hours can be specified
    "times": [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23],
    // if a player on the server is within this player count range, then the map will cycle
    "players": {
      "min": 0,
      "max": 64
    },
    // the number of cooldowns until the next map cycle is reduced by 1 for each map
    // using tags will share the cooldown reset timing
    // ex.)
    // ze_ffx   = { count = 50, tags = ["ff"] }
    // ze_ffxii = { count = 80, tags = ["ff"] }
    // if ze_ffx map end and even if ze_ffxii cooldown count is 0,
    // both count will be reset by tags
    "cooldown": {
      "count": 2,
      // current count will be updated by plugins automatically
      "current_count": 0,
      "tags": ["official"]
    },
    "nomination": {
      // if admin is true, only admin can nominate it
      // so "enabled" will be ignore 'enabled' in that case
      "admin": false,
      // allow nominate this map
      // default "enabled" is true
      "enabled": true
    },
    "match": {
      // should be "0" or "1", other is fallback to default
      // 0 = time, 1 = round
      // default is 0 = time
      "type": 1,
      // type = "0" -> mp_maxround  30
      // type = "1" -> mp_timelimit 30
      // type = "2" -> mp_roundtime 30
      "limit": "30"
    },
    "extend": {
      // if enabled true, this map can be extend by vote
      "enabled": true,
      // times is how many times extend
      // 0 is disabled extend for this map
      // -1 is extend times = infinity
      "times": 2,
      // match.type = "0" -> mp_maxround  + 15
      // match.type = "1" -> mp_timelimit + 15
      // match.type = "2" -> mp_roundtime + 15
      "number": 15
    },
    // Each value can be named as a cfg file, allowing additional configuration to be performed at map start.
    // cfg file should be put on csgo/cfg/MapChooserExtended/maps
    // default is empty
    "cfgs": ["official", "deMap"]
  }
}
```

</details>

# Workshop Auto-Sync

Version 2.0.0 includes the ability to automatically synchronize maps from Steam Workshop collections. This feature makes it easy to keep your server's map pool up-to-date with workshop content.

## How to Configure Workshop Auto-Sync

1. Add the collection IDs to your config file under the `Workshop` section:
   ```json
   "Workshop": {
     "collection_ids": ["123456789", "987654321"]
   }
   ```

2. The plugin will automatically:
   - Fetch maps from these collections when the plugin loads
   - Create map settings files for each workshop map
   - Make the maps available for voting
   - Apply default settings to new workshop maps

3. Map name correction:
   - The map name generated from the workshop may not match the official VPK name (since workshop authors can freely name their submissions)
   - The plugin includes a process to automatically correct the map name when the map starts
   - This ensures proper tracking and configuration of workshop maps

4. Workshop map settings:
   - Workshop maps use the same settings format as regular maps
   - The `workshop_id` field in the map settings is automatically populated
   - You can customize settings for workshop maps by editing their generated config files

## Benefits of Workshop Auto-Sync

- Eliminates the need to manually download and install workshop maps
- Keeps your server's map pool synchronized with workshop collections
- Automatically creates appropriate configuration for each map
- Handles map name discrepancies between workshop titles and actual map files
- Simplifies management of large workshop map collections
- Allows for easy addition of new maps by simply adding them to a collection

## Workshop Map Management Tips

- Organize your maps into logical collections (e.g., by map type, theme, or difficulty)
- Use collection descriptions to document important information about the maps
- Consider creating a test collection for evaluating new maps before adding them to your main rotation
- Regularly check your collections for updates and new maps

# Troubleshooting

## Common Issues and Solutions

### Plugin Not Loading
- Make sure the plugin is placed in the correct directory: `game/csgo/addons/counterstrikesharp/plugins/MapChooserExtended`
- Check that you have the latest version of CounterStrikeSharp installed
- Verify that the plugin files have the correct permissions

### Configuration Issues
- If you encounter errors with the configuration file, delete it from `addons/counterstrikesharp/configs/plugins/MapChooserExtended` and let the plugin recreate it on load
- For older configurations (Version < 13), you must delete the config file and let the plugin recreate it
- If you're updating from a version prior to 2.0.0, you'll need to review your map settings as the format has changed

### Map Settings Issues
- If a map is not appearing in votes, check its settings file in the `maps` directory
- Verify that the map's `enabled` setting is set to `true`
- Check if the map is in cooldown (`current_count` > 0)
- Make sure the map meets the time and player count conditions
- Ensure the map's `nomination.enabled` is set to `true` if you want it to be nominatable

### Vote Timing Issues
- If votes appear at unexpected times, check your `TriggerSecondsBeforeEnd` and `TriggerRoundsBeforeEnd` settings
- Verify that your map settings have the correct `match.type` and `match.limit` values
- If votes appear immediately after map start, check for conflicts with other plugins that might be triggering votes

### Workshop Maps Not Syncing
- Verify that the collection IDs are correctly entered in the config file
- Check that the Steam Workshop is accessible from your server
- Look for any error messages in the server console during plugin load
- Make sure your server has sufficient disk space for workshop downloads

## Performance Optimization
If you experience performance issues:
- Consider reducing the frequency of votes and nominations
- Optimize your map pool size to a reasonable number
- Check server logs for any errors or warnings related to the plugin

## Getting Help
If you encounter issues not covered here, you can:
- Check the [GitHub Issues](https://github.com/possession-community/MapChooser-Extended/issues) page
- Create a new issue with detailed information about your problem
- Join the community Discord server for support
