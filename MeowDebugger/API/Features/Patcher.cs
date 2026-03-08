using HarmonyLib;
using LabApi.Features.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using LabApi.Loader;

namespace MeowDebugger.API.Features;

internal class Patcher
{
    private static List<string> Blacklisted => MeowDebugger.Instance!.Config!.BlacklistAssemblies;
    private static List<string> Whitelist => MeowDebugger.Instance!.Config!.WhitelistNamespaces;
    public readonly Type[] types;

    private readonly Harmony _harmony;

    private readonly MethodInfo _prefixMethod;
    private readonly MethodInfo _finalizerMethod;

    public int PatchedMethods = 0;

    public Patcher(Harmony harmony)
    {
        _harmony = harmony ?? throw new ArgumentNullException(nameof(harmony));
        _prefixMethod = typeof(Patch.Patch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Patch.Prefix (static, non-public) not found.");
        _finalizerMethod = typeof(Patch.Patch).GetMethod("Finalizer", BindingFlags.Static | BindingFlags.NonPublic)
                           ?? throw new InvalidOperationException("Patch.Finalizer (static, non-public) not found.");
        
        HashSet<Assembly> assemblySet = new HashSet<Assembly>();
        Assembly[] assemblies = assemblySet.ToArray();

        Assembly gameAsm = typeof(ReferenceHub).Assembly;
        
        try
        {
            if (!gameAsm.IsDynamic && !IsBlacklisted(gameAsm))
                assemblySet.Add(gameAsm);
            
            foreach (Assembly asm in PluginLoader.Plugins.Values)
                if (!asm.IsDynamic && asm != global::MeowDebugger.MeowDebugger.Assembly && !IsBlacklisted(asm))
                    assemblySet.Add(asm);
                    
            foreach (Assembly asm in PluginLoader.Dependencies)
                if (!asm.IsDynamic && asm != global::MeowDebugger.MeowDebugger.Assembly && !IsBlacklisted(asm))
                    assemblySet.Add(asm);
        }
        catch (Exception e)
        {
            Logger.Warn($"Failed to query PluginLoader: {e.Message}");
        }

        var allTypes = new List<Type>();
        foreach (var asm in assemblies)
        {
            Type[] asmTypes;
            try
            {
                asmTypes = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException rtle)
            {
                asmTypes = rtle.Types.Where(t => t != null).ToArray()!;
                Logger.Warn($"ReflectionTypeLoadException while reading types from {asm.FullName}; using {asmTypes.Length} loadable types.");
            }
            
            var filtered = asmTypes
                .Where(t => t is { IsInterface: false })
                // allow HarmonyPatch types so everything is instrumented
                // skip compiler generated types (e.g. coroutine state machines)
                .Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                // skip types with static constructors to avoid premature dependency initialization
                .Where(t => t.TypeInitializer == null);

            allTypes.AddRange(filtered);
        }

        types = allTypes.ToArray();
    }

    public void PatchMethods()
    {
        if (types.Length == 0)
        {
            Logger.Warn("No types to patch.");
            return;
        }

        int tried = 0;

        foreach (Type type in types)
        {
            foreach (MethodInfo method in EnumeratePatchableMethods(type))
            {
                var fullKey = $"{type.FullName}::{method.Name}";
                /*var cfg = Plugin.Instance.Config;
                if ((cfg.IgnoreMethods?.Contains(method.Name) ?? false) ||
                    (cfg.IgnoreMethods?.Contains(fullKey) ?? false))
                    continue;*/

                tried++;
                PatchMethod(method, type);
            }
        }

        Logger.Info($"Tried patching {tried} methods across {types.Length} types; successfully patched {PatchedMethods}.");
    }

    private static bool IsBlacklisted(Assembly asm)
    {
        var name = asm.GetName().Name;
        bool yesDisplay = Blacklisted.Any(prefix => name.Contains(prefix));
        
        if(!yesDisplay) 
            Logger.Info($"Patched {asm.GetName().Name}");
            
        return yesDisplay;
    }

    private static bool IsNamespaceWhitelisted(string? @namespace)
    {
        if  (@namespace == null) return false;
        
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

            PatchedMethods++;
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
        if (t == null || !IsNamespaceWhitelisted(t.Namespace))
            yield break;

        const BindingFlags flags =
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Instance |
        BindingFlags.Static |
        BindingFlags.DeclaredOnly;

        foreach (var m in t.GetMethods(flags))
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
        catch  { return false; }
        

        if (m.DeclaringType != m.Module.GetTypes().FirstOrDefault(t => t == m.DeclaringType))
            return false;
        
        if (m.GetMethodBody() == null) return false;

        if (m.DeclaringType != m.Module.GetTypes().FirstOrDefault(t => t == m.DeclaringType))
            return false;
        
        if (m.DeclaringType != m.Module.GetTypes().FirstOrDefault(t => t == m.DeclaringType))
            return false;

        if (m.GetCustomAttribute<IteratorStateMachineAttribute>() != null) return false;
        if (typeof(IEnumerator).IsAssignableFrom(m.ReturnType)) return false;

        return true;
    }
}