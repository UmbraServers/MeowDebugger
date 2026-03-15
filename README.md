[issues]: https://github.com/UmbraServers/MeowDebugger/issues

# MeowDebugger ![Version](https://img.shields.io/github/v/release/UmbraServers/MeowDebugger?style=plastic&label=Version&color=dc3e3e) ![Downloads](https://img.shields.io/github/downloads/UmbraServers/MeowDebugger/total?style=plastic&label=Downloads&color=50f63f)
SCP:SL Debugger/Profiller, that helps you debug the game and see which methods can take a toll on the server.
You can debug from Plugins to the Game Assembly.

## Features
The features the plugin currently has:
 - Evented and Sampled Profilling
 - Search by namespaces
 - FlameGraph Showcase
 - In-Game Reporter
 - Intelligent Danger System

 ## How to create a flamegraph
> [!NOTE]
> We recommend using this if you need accurate data.

 Use the command "reporter flame" and then go to this website [speedscope](https://www.speedscope.app/) to allow you to see them

 ## Config
| Setting Key | Value Type | Default Value | Description |
|---|---|---|---|
| blacklist_assemblies | list | ``CedModV3`` ``0Harmony`` ``NVorbis`` ``Mono.Posix`` ``SemanticVersioning`` ``System.Buffers`` ``System.ComponentModel.DataAnnotations`` ``System.Memory`` ``System.Numerics.Vectors`` ``System.Runtime.CompilerServices.Unsafe`` ``System.ValueTuple`` | Blacklists assemblies to not look for namespaces. |
| whitelist_namespaces | list | ``InventorySystem`` ``CommandSystem`` | Namespaces that will get patched. |
| nanoseconds_threshold | double | 200000 | Minimal nanoseconds difference between frames for the file.   ``1ms = 1000000ns``  |
| speedscope_output_path | string | '' | Exports the profile to a custom path.  |
| should_include_namespace_in_output | bool | true | If it the output file will contain the filtered namespace name.  |
| should_patch_on_waiting_for_players | bool | true | If it should patch on loading for players rather than on the boot of the plugin.  |

 # How to Install?
 Go to [Releases](https://github.com/UmbraServers/MeowDebugger/releases),
 Depending on what version you wanna use you need to do the following
 
## LabAPI
Place it in the Following folder depending on your OS:
  - Windows: `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global`
  - Linux: `~/.config/SCP Secret Laboratory/LabAPI/plugins/global`

## EXILED
Install the version with the -EXILED and depending on your OS you need to place the file in:
  - Windows: `%AppData%\EXILED\Plugins`
  - Linux: `~/.config/EXILED/Plugins`

## Contribute
If you would like to contribute towards MeowDebugger you are free to do it.
  
### Support
- [Issue Tracker][issues]
