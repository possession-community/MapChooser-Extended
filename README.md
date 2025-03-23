# MapChooser-Extended

General purpose map voting plugin, rtv, nominate, and more.

Based on [Oz-Lin/cs2-rockthevote](https://github.com/Oz-Lin/cs2-rockthevote)

## Requirements
[Latest release of Counter Strike Sharp](https://github.com/roflmuffin/CounterStrikeSharp)

# Installation
- WIP

# Work in Progress
- ~~make Extend command for admin only~~
- vote sounds settings
- introducing CS2ScreenMenu, instead of Chat menu
  - for nomination menu and changemap menu
- remove codes related vip
  - move voteextend to admin only
- remove votemap
- investigate about timelimit and maxrounds, + roundtime

# Available Commands

## General Commands (Everyone)
| Command | Aliases | Description |
| ------- | ------- | ----------- |
| `rtv` | `css_rtv` | Start a Rock The Vote map vote |
| `unrtv` | `css_unrtv` | Remove your vote to rock the vote |
| `nominate <map>` | `css_nominate <map>`, `nom <map>`, `css_nom <map>`, `yd <map>`, `css_yd <map>` | Nominate a map for voting |
| `nomlist` | `css_nomlist`, `ydb`, `css_ydb` | Display a list of nominated maps and who nominated them |
| `votemap <map>` | `vm <map>` | Vote for a specific map |
| `timeleft` | | Display the remaining time on the current map |
| `nextmap` | | Display the next map in rotation |
| `ext` | `css_ext`, `extend`, `css_extend`, `extendmap`, `css_extendmap` | Vote to extend the current map |
| `revote` | `css_revote` | Change your vote during an active vote |

## VIP Commands
Requires `@css/vip` permission
| Command | Aliases | Description |
| ------- | ------- | ----------- |
| `css_voteextend` | `css_ve` | Start a vote to extend the current map |

## Admin Commands

### Map Control Commands
| Command | Aliases | Permission | Description |
| ------- | ------- | ---------- | ----------- |
| `css_extend <minutes>` | | `@css/changemap` | Extend the current map time limit |
| `css_changemap <map>` | `changemap <map>` | `@css/generic` | Change the map immediately |
| `css_setnextmap <map>` | | `@css/generic` | Set the next map in rotation |

### RTV Control Commands
| Command | Aliases | Permission | Description |
| ------- | ------- | ---------- | ----------- |
| `css_enable_rtv` | `enable_rtv` | `@css/generic` | Enable RTV command |
| `css_disable_rtv` | `disable_rtv` | `@css/generic` | Disable RTV command |
| `css_force_rtv` | `force_rtv` | `@css/generic` | Force RTV vote |

### Nomination Control Commands
| Command | Aliases | Permission | Description |
| ------- | ------- | ---------- | ----------- |
| `css_enable_nominate` | `enable_nominate` | `@css/generic` | Enable nomination command |
| `css_disable_nominate` | `disable_nominate` | `@css/generic` | Disable nomination command |
| `css_nominate_addmap <map>` | `nominate_addmap <map>` | `@css/generic` | Add a map to nomination list |
| `css_nominate_removemap <map>` | `nominate_removemap <map>` | `@css/generic` | Remove a map from nomination list |

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
- Now, Per-map settings are stored in `addons/counterstrikesharp/configs/plugins/MapChooserExtended/maps/` directory as JSON files, see under section.

## General config
| Config         | Description                                                                      | Default Value | Min | Max |
| -------------- | -------------------------------------------------------------------------------- | ------------- | --- | --- |
| MapsInCoolDown | Number of maps that can't be used in vote because they have been played recently | 3             | 0   |     |

## RockTheVote
Players can type rtv to request the map to be changed, once a number of votes is reached (by default 60% of players in the server) a vote will start for the next map, this vote lasts up to 30 seconds (hardcoded for now), in the end server changes to the winner map.

| Config              | Description                                                                                                            | Default Value | Min   | Max                                  |
| ------------------- | ---------------------------------------------------------------------------------------------------------------------- | ------------- | ----- | ------------------------------------ |
| Enabled             | Enable/Disable RTV functionality                                                                                       | true          | false | true                                 |
| EnabledInWarmup     | Enable/Disable RTV during warmup                                                                                       | false         | false | true                                 |
| NominationEnabled   | Enable/Disable nomination                                                                                              | true          | false | true                                 |
| MinPlayers          | Minimum amount of players to enable RTV/Nominate                                                                       | 0             | 0     |                                      |
| MinRounds           | Minimum rounds to enable RTV/Nominate                                                                                  | 0             | 0     |                                      |
| ChangeMapImmediately | Whether to change the map immediatly when vote ends or not                                                             | true          | false | true                                 |
| HudMenu             | Whether to use HudMenu or just the chat one, when false the hud only shows which map is winning instead of actual menu | true          | false | true                                 |
| HideHudAfterVote    | Whether to hide vote status hud after vote or not, only matters when HudMenu is true                                   | false         | false | true                                 |
| MapsToShow          | Amount of maps to show in vote,                                                                                        | 6             | 1     | 6 with HudMenu, unlimited without it |
| VoteDuration        | Seconds the RTV should last                                                                                            | 30            | 1     |                                      |
| VotePercentage      | Percentage of players that should type RTV in order to start a vote                                                    | 60            | 0     | 100                                  |
| DontChangeRtv       | Enable/Disable option not to change the current map                                                                    | true          | false | true                                 |
| IgnoreSpec          | Ignore spectators from vote count                                                                                      | true          | false | true                                 |
| InitialRtvDelay     | Cooldown timer to start the "first" RTV                                                                                | 60             | 0 |  -                                 |
| VoteCooldownTime    | Cooldown timer to start the next RTV                                                                                   | 300           | 0     | -                                 |
| ExtendTimeStep        | How long (in minutes) should the mp_timelimit to be extended                                                             | 15         | 0       | -                                |
| ExtendRoundStep         | How many rounds should the mp_maxrounds to be extended                                                                 | 5             | 0     |                                      |
| ExtendLimit             | How many times the current map can be extended (-1 for unlimited extensions)                                          | 3             | -1     |                                      |

## End of map vote
Based on `mp_timelimit` and `mp_maxrounds` cvar before the map ends a RTV like vote will start to define the next map, it can be configured to change immediatly or only when the map actually ends

| Config                  | Description                                                                                                            | Default Value | Min   | Max                                  |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------- | ------------- | ----- | ------------------------------------ |
| Enabled                 | Enable/Disable end of map vote functionality                                                                           | true          | false | true                                 |
| ChangeMapImmediately     | Whether to change the map immediatly when vote ends or not                                                             | true          | false | true                                 |
| HideHudAfterVote        | Whether to hide vote status hud after vote or not, only matters when HudMenu is true                                   | false         | false | true                                 |
| MapsToShow              | Amount of maps to show in vote,                                                                                        | 6             | 1     | 6 with HudMenu, unlimited without it |
| VoteDuration            | Seconds the RTV should last                                                                                            | 30            | 1     |                                      |
| HudMenu                 | Whether to use HudMenu or just the chat one, when false the hud only shows which map is winning instead of actual menu | true          | false | true                                 |
| TriggerSecondsBeforeEnd | Amount of seconds before end of the map that should trigger the vote, only used when mp_timelimit is greater than 0    | 120           | 1     |                                      |
| TriggerRoundsBeforeEnd   | Amount of rounds before end of map that should trigger the vote, only used when mp_maxrounds is set                    | 2             | 1     |                                      |
| DelayToChangeInTheEnd   | Delay in seconds that plugin will take to change the map after the win panel is shown to the players                   | 6             | 3     |                                      |
| AllowExtend             | Option to extend the current map (Also needs to configure ExtendLimit)                                                 | true          | false | true                                 |
| RoundBased              | Whether to extend `mp_timelimit` or extend current round `mp_roundtime`                                                | true          | false | true                                 |
| ExtendTimeStep          | How long (in minutes) should the mp_timelimit to be extended                                                           | 15            | 0     |                                      |
| ExtendRoundStep         | How many rounds should the mp_maxrounds to be extended                                                                 | 5             | 0     |                                      |
| ExtendLimit             | How many times the current map can be extended (-1 for unlimited extensions)                                           | 3             | -1     |                                      |

## Extend map vote
Players can extend the current map by using the `!ext` command. Extends the `mp_timelimit` and `mp_maxrounds` cvar

| Config                  | Description                                                                                                            | Default Value | Min   | Max                                  |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------- | ------------- | ----- | ------------------------------------ |
| Enabled                 | Enable/Disable extend map vote functionality                                                                           | true          | false | true                                 |
| EnabledInWarmup         | Enable/Disable EXT during warmup                                                                                       | true          | false | true                                 |
| MinRounds               | Minimum rounds to enable ext                                         | 0             |       |      |
| MinPlayers              | Minimum amount of players to enable ext                              |               |       |      |
| VotePercentage		  | Percentage of players that should vote in a map in order to extend it												   | 60            | 1     | 100								  |
| ChangeMapImmediately     | Placeholder field. Keep it as false to prevent breaking the plugin function                                            | false         | false | true                                 |
| ExtendTimeStep          | How long (in minutes) should the mp_timelimit to be extended                                                           | 15            | 0     |                                      |
| ExtendRoundStep         | How many rounds should the mp_maxrounds to be extended                                                                 | 5             | 0     |                                      |
| ExtendLimit             | How many times the current map can be extended (-1 for unlimited extensions)                                          | 3             | -1     |                                      |
| RoundBased              | Whether to extend `mp_timelimit` or extend current round `mp_roundtime`                                                | true          | false | true                                 |
| IgnoreSpec              | Ignore spectators from vote count																					   | true          | false | true								  |


## VIP Extend map vote
Players can extend the current map by using the `!ve` or `!voteextend` command. Requires `@css/vip` flag permission. Extends the `mp_timelimit` and `mp_maxrounds` cvar

| Config                  | Description                                                                                                            | Default Value | Min   | Max                                  |
| ----------------------- | ---------------------------------------------------------------------------------------------------------------------- | ------------- | ----- | ------------------------------------ |
| Enabled                 | Enable/Disable extend map vote functionality                                                                           | true          | false | true                                 |
| VoteDuration            | Seconds the VIP extend vote should last                                                                                | 30            | 1     |                                      |
| VotePercentage		  | Percentage of players that should vote in a map in order to extend it												   | 60            | 1     | 100								  |
| ExtendTimeStep          | How long (in minutes) should the mp_timelimit to be extended                                                           | 15            | 0     |                                      |
| ExtendRoundStep         | How many rounds should the mp_maxrounds to be extended                                                                 | 5             | 0     |                                      |
| ExtendLimit             | How many times the current map can be extended (-1 for unlimited extensions)                                           | 3             | -1     |                                      |
| RoundBased              | Whether to extend `mp_timelimit` or extend current round `mp_roundtime`                                                | true          | false | true                                 |
| HudMenu                 | Whether to use HudMenu or just the chat one                                                                            | true          | false | true                                 |


## Votemap
Players can vote to change to an specific map by using the votemap <mapname> command

| Config              | Description                                                              | Default Value | Min   | Max  |
| ------------------- | ------------------------------------------------------------------------ | ------------- | ----- | ---- |
| Enabled             | Enable/disable votemap funtionality                                      | true          | false | tru  |
| VotePercentage      | Percentage of players that should vote in a map in order to change to it | 60            | 1     | 100  |
| ChangeMapImmediately | Whether to change the map immediatly when vote ends or not               | true          | false | true |
| EnabledInWarmup     | Enable/Disable votemap during warmup                                     | true          | false | true |
| MinRounds           | Minimum rounds to enable votemap                                         | 0             |       |      |
| MinPlayers          | Minimum amount of players to enable votemap                              |               |       |      |
| HudMenu             | Whether to use HudMenu or just the chat one                              | true          | false | true |
| IgnoreSpec          | Ignore spectators from vote count                                        | true          | false | true |


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

Version 1.9.4 introduces a new map settings system that allows detailed configuration for each map individually.

This system enables server administrators to apply different settings to each map.

Editing maplist.txt is no longer necessary.

This system is a bit more tedious than before, since you have to create a file with the name of each map, and each map has its own settings, but the `base system` of map settings allows you to inherit the same settings, so you don't have to write the same settings multiple times.

## Map Settings Files

Map settings are stored in `addons/counterstrikesharp/configs/plugins/MapChooserExtended/maps/` directory as JSON files.

Each map has its own settings file (e.g., `de_dust2.json`) that inherits from the default settings (`maps/default.json`).

If a specific map is loaded without a specific map configuration, the `maps/default.json` will be used to automatically generate the map configuration.

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
    }
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
      // type = "0" -> mp_maxround  20
      // type = "1" -> mp_timelimit 20
      "limit": "30"
    },
    "extend": {
      // if enabled true, this map can be extend by vote
      "enabled": true,
      // times is how many times extend
      // 0 is disabled extend for this map
      // -1 is extend times = infinity
      "times": 2,
      // match.type = "0" -> mp_maxround  +
      // match.type = "1" -> mp_timelimit +
      "number": 15
    }
  }
}
```

</details>

# Workshop Auto-Sync

Version 1.9.4 adds the ability to automatically synchronize maps from Steam Workshop collections. Add the collection IDs to your config file under the `Workshop` section: `"Workshop": { "collection_ids": ["123456789"] }`.

The plugin will automatically fetch maps from these collections, create settings files for them, and make them available for voting.

In addition, the map name (including the file name) automatically generated from the workshop may not be the official vpk name(cuz sers can freely write the title of the workshop), so it also includes a process to automatically correct it when the map starts.
