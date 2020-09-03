﻿namespace PassivePicasso.ThunderKit.Editor
{
    public static class ConfigTemplate
    {
        public static string CreatBepInExConfig(bool consoleEnabled, string logLevel) =>
            string.Format(Content, consoleEnabled.ToString().ToLower(), logLevel.Replace("None, ", ""));

        public static readonly string Content = @"[Caching]

## Enable/disable assembly metadata cache
## Enabling this will speed up discovery of plugins and patchers by caching the metadata of all types BepInEx discovers.
# Setting type: Boolean
# Default value: true
EnableAssemblyCache = true

[Harmony.Logger]

## Specifies which Harmony log channels to listen to.
## NOTE: IL channel dumps the whole patch methods, use only when needed!
# Setting type: LogChannel
# Default value: Warn, Error
# Acceptable values: None, Info, IL, Warn, Error, All
# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)
LogChannels = Warn, Error

[Logging]

## Enables showing unity log messages in the BepInEx logging system.
# Setting type: Boolean
# Default value: true
UnityLogListening = true

## If enabled, writes Standard Output messages to Unity log
## NOTE: By default, Unity does so automatically. Only use this option if no console messages are visible in Unity log
## 
# Setting type: Boolean
# Default value: false
LogConsoleToUnityLog = false

PreloaderConsoleOutRedirection = true

[Logging.Console]

## Enables showing a console for log output.
# Setting type: Boolean
# Default value: false
Enabled = {0}

## If true, console is set to the Shift-JIS encoding, otherwise UTF-8 encoding.
# Setting type: Boolean
# Default value: false
ShiftJisEncoding = false

## Which log levels to show in the console output.
# Setting type: LogLevel
# Default value: Fatal, Error, Warning, Message, Info
# Acceptable values: None, Fatal, Error, Warning, Message, Info, Debug, All
# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)
LogLevels = {1}

DisplayedLogLevel = Debug

[Logging.Disk]

## Include unity log messages in log file output.
# Setting type: Boolean
# Default value: false
WriteUnityLog = true

## Appends to the log file instead of overwriting, on game startup.
# Setting type: Boolean
# Default value: false
AppendLog = false

## Enables writing log messages to disk.
# Setting type: Boolean
# Default value: true
Enabled = {0}

## Which log leves are saved to the disk log output.
# Setting type: LogLevel
# Default value: Fatal, Error, Warning, Message, Info
# Acceptable values: None, Fatal, Error, Warning, Message, Info, Debug, All
# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)
LogLevels = {1}

DisplayedLogLevel = Debug

[Preloader]

## Enables or disables runtime patches.
## This should always be true, unless you cannot start the game due to a Harmony related issue (such as running .NET Standard runtime) or you know what you're doing.
# Setting type: Boolean
# Default value: true
ApplyRuntimePatches = true

## Specifies which MonoMod backend to use for Harmony patches. Auto uses the best available backend.
## This setting should only be used for development purposes (e.g. debugging in dnSpy). Other code might override this setting.
# Setting type: MonoModBackend
# Default value: auto
# Acceptable values: auto, dynamicmethod, methodbuilder, cecil
HarmonyBackend = auto

## If enabled, BepInEx will save patched assemblies into BepInEx/DumpedAssemblies.
## This can be used by developers to inspect and debug preloader patchers.
# Setting type: Boolean
# Default value: false
DumpAssemblies = false

## If enabled, BepInEx will load patched assemblies from BepInEx/DumpedAssemblies instead of memory.
## This can be used to be able to load patched assemblies into debuggers like dnSpy.
## If set to true, will override DumpAssemblies.
# Setting type: Boolean
# Default value: false
LoadDumpedAssemblies = false

## If enabled, BepInEx will call Debugger.Break() once before loading patched assemblies.
## This can be used with debuggers like dnSpy to install breakpoints into patched assemblies before they are loaded.
# Setting type: Boolean
# Default value: false
BreakBeforeLoadAssemblies = false

ShimHarmonySupport = true

[Preloader.Entrypoint]

## The local filename of the assembly to target.
# Setting type: String
# Default value: UnityEngine.CoreModule.dll
Assembly = UnityEngine.CoreModule.dll

## The name of the type in the entrypoint assembly to search for the entrypoint method.
# Setting type: String
# Default value: Application
Type = Application

## The name of the method in the specified entrypoint assembly and type to hook and load Chainloader from.
# Setting type: String
# Default value: .cctor
Method = .cctor
";
    }
}