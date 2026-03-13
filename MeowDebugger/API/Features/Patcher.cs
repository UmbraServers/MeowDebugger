using HarmonyLib;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using MeowDebugger.API.Features.Speedscope.File.Structs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MeowDebugger.API.Features;

internal class Patcher
{
    public static List<Frame> Frames { get; } = new List<Frame>();
    public static Dictionary<MethodBase, int> MethodIndexes { get; } = new Dictionary<MethodBase, int>();
    private static List<string> Blacklisted => ConfigDebugger.Instance!.BlacklistAssemblies;
    private static List<string> Whitelist => ConfigDebugger.Instance!.WhitelistNamespaces;
    
    private readonly Type[] _types;
    private readonly Harmony _harmony;
    private readonly MethodInfo _prefixMethod;
    private readonly MethodInfo _finalizerMethod;

    private int _patchedMethods;

    public Patcher(Harmony harmony)
    {

        _harmony = harmony ?? throw new ArgumentNullException(nameof(harmony));
        _prefixMethod = typeof(Patch.Patch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Patch.Prefix (static, non-public) not found.");
        _finalizerMethod = typeof(Patch.Patch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.NonPublic)
                           ?? throw new InvalidOperationException("Patch.Finalizer (static, non-public) not found.");
        
        Assembly[] assemblies = [];
        Assembly gameAsm = typeof(ReferenceHub).Assembly;

        try
        {
            HashSet<Assembly> assemblySet = [];
            
            bool shouldPatchLabAPI = true;

            if (!gameAsm.IsDynamic && !IsBlacklisted(gameAsm))
                assemblySet.Add(gameAsm);

#if EXILED_RELEASE

            foreach (var plugin in Exiled.Loader.Loader.Plugins)
                if (!plugin.Assembly.IsDynamic && plugin.Assembly != GeneralUtils.Assembly && !IsBlacklisted(plugin.Assembly))
                    assemblySet.Add(plugin.Assembly);

            foreach (Assembly asm in Exiled.Loader.Loader.Dependencies)
                if (!asm.IsDynamic && asm != GeneralUtils.Assembly && !IsBlacklisted(asm))
                    assemblySet.Add(asm);

            shouldPatchLabAPI = ConfigDebugger.Instance!.ShouldPatchLabApiPlugins;
#endif

            foreach (Assembly asm in PluginLoader.Plugins.Values)
                if (!asm.IsDynamic && asm != GeneralUtils.Assembly && !IsBlacklisted(asm) && shouldPatchLabAPI)
                    assemblySet.Add(asm);

            foreach (Assembly asm in PluginLoader.Dependencies)
                if (!asm.IsDynamic && asm != GeneralUtils.Assembly && !IsBlacklisted(asm) && shouldPatchLabAPI)
                    assemblySet.Add(asm);
            
            assemblies = assemblySet.ToArray();
        }
        catch (Exception e)
        {
            Logger.Warn($"Failed to query PluginLoader: {e.Message}");
        }

        if (assemblies.Length == 0)
        {
            Logger.Warn("No assemblies were found to patch.");
            return;
        }

        List<Type> allTypes = [];
        
        foreach (Assembly asm in assemblies)
        {
            Type[] asmTypes;
            try
            {
                asmTypes = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException rtle)
            {
                asmTypes = rtle.Types.Where(t => t != null).ToArray();
                Logger.Warn($"ReflectionTypeLoadException while reading types from {asm.FullName}; using {asmTypes.Length} loadable types.");
            }

            IEnumerable<Type> filtered = asmTypes
                .Where(t => t is { IsInterface: false })
                // allow HarmonyPatch types so everything is instrumented
                // skip compiler generated types (e.g. coroutine state machines)
                .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                // skip types with static constructors to avoid premature dependency initialization
                .Where(t => t.TypeInitializer == null);

            allTypes.AddRange(filtered);
        }

        _types = allTypes.ToArray();
    }

    public void PatchMethods()
    {
        if (_types.Length == 0)
        {
            Logger.Warn("No types to patch.");
            return;
        }

        int tried = 0;

        foreach (Type type in _types)
        {
            foreach (MethodInfo method in EnumeratePatchableMethods(type))
            {
                tried++;
                PatchMethod(method, type);
            }
        }

        Logger.Info($"Tried patching {tried} methods across {_types.Length} types; successfully patched {_patchedMethods}.");
    }

    public static int StoreIndex(MethodBase method)
    {
        if (MethodIndexes.TryGetValue(method, out int id))
            return id;

        id = Frames.Count;

        string methodName = method.DeclaringType != null ? $"{method.DeclaringType.FullName}.{method.Name}" : method.Name;

        Frame frame = new Frame(methodName, method.Module.FullyQualifiedName, id);

        Frames.Add(frame);

        MethodIndexes[method] = id;
        return id;
    }

    public static int GetMethodIndex(MethodBase method) => MethodIndexes.TryGetValue(method, out int id) ? id : -1;

    private static bool IsBlacklisted(Assembly asm)
    {
        string? name = asm.GetName().Name;
        bool yesDisplay = Blacklisted.Any(prefix => name.Contains(prefix));

        if (!yesDisplay)
            Logger.Info($"Patched {asm.GetName().Name}");

        return yesDisplay;
    }

    private static bool IsNamespaceWhitelisted(string? @namespace)
    {
        if (@namespace == null) return false;

        for (int i = 0; i < Whitelist.Count; i++)
        {
            if (@namespace.Contains(Whitelist[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void PatchMethod(MethodInfo method, Type type)
    {
        try
        {
            var info = Harmony.GetPatchInfo(method);
            if (info?.Owners?.Contains(_harmony.Id) == true)
            {
                Logger.Debug($"Already patched: {type.FullName}::{method.Name}()");
                return;
            }

            _harmony.Patch(
                original: method,
                prefix: new HarmonyMethod(_prefixMethod),
                finalizer: new HarmonyMethod(_finalizerMethod)
            );

            StoreIndex(method);

            _patchedMethods++;
        }
        catch (NotSupportedException)
        {
            Logger.Warn($"Won't patch (not supported): {type.FullName}::{method.Name}()");
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to patch {type.FullName}::{method.Name}(): {e}");
        }
    }

    private static IEnumerable<MethodInfo> EnumeratePatchableMethods(Type t)
    {
        if (!IsNamespaceWhitelisted(t.Namespace))
            yield break;

        const BindingFlags flags =
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Instance |
        BindingFlags.Static |
        BindingFlags.DeclaredOnly;

        foreach (MethodInfo m in t.GetMethods(flags))
            if (CanPatchRegular(m))
                yield return m;
    }

    private static bool CanPatchRegular(MethodInfo? m)
    {
        if (m == null) return false;
        if (m.IsAbstract) return false;
        if (m.Name.Contains("<")) return false;
        if (m.Name == "MoveNext") return false;
        if (m.Name == "System.Collections.IEnumerator.Reset") return false;

        try
        {
            if (m.ContainsGenericParameters) return false;
            if (m.IsGenericMethod || m.IsGenericMethodDefinition) return false;
        }
        catch { return false; }


        if (m.DeclaringType != m.Module.GetTypes().FirstOrDefault(t => t == m.DeclaringType))
            return false;

        if (m.GetMethodBody() == null) return false;

        if (m.GetCustomAttribute<IteratorStateMachineAttribute>() != null) return false;
        
        return !typeof(IEnumerator).IsAssignableFrom(m.ReturnType);

    }
}