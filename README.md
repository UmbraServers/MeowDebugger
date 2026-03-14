[issues]: https://github.com/UmbraServers/MeowDebugger/issues

# MeowDebugger
SCP:SL Debugger, that helps you debug the game and see which methods can take a toll on the server.
You can debug from Plugins to the Game Assembly.

## Features
The features the plugin currently has:
 - Search by namespaces
 - FlameGraph Showcase
 - In-Game Reporter
 - Intelligent Danger System

 ## How to see flamegraph
> [!NOTE]
> We recommend using this if you need accurate data.

 Use the command "reporter flame" and then go to this website [speedscope](https://www.speedscope.app/) to allow you to see them

 ## Config
| Setting Key | Value Type | Default Value | Description |
|---|---|---|---|
| blacklist_assemblies | list | ``CedModV3`` ``0Harmony`` ``NVorbis`` ``Mono.Posix`` ``SemanticVersioning`` ``System.Buffers`` ``System.ComponentModel.DataAnnotations`` ``System.Memory`` ``System.Numerics.Vectors`` ``System.Runtime.CompilerServices.Unsafe`` ``System.ValueTuple`` | Blacklists assemblies to not look for namespaces. |
| whitelist_namespaces | list | ``InventorySystem`` ``CommandSystem`` | Namespaces that will get patched. |
| nanoseconds_threshold | double | 200000 | Minimal nanoseconds difference between frames for the file.   ``1ms = 1000000ns``  |
| speedscope_output_path | string | "" | Exports the profile to a custom path.  |

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
