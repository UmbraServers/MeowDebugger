using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using LabApi.Features.Console;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using Mono.CompilerServices.SymbolWriter;

namespace MeowDebugger.API.Features;

internal class Patcher
{
    private static readonly string[] Blacklisted = ["Discord", "Exiled.API", "System", "mscorlib", "netstandard", "Interop", "Microsoft", "CedModV3", "0Harmony", "NVorbis", "Mono.Posix", "SemanticVersioning", "System.Buffers", "System.ComponentModel.DataAnnotations", "System.Memory", "System.Numerics.Vectors", "System.Runtime.CompilerServices.Unsafe", "System.ValueTuple"];

    public readonly Type[] types;

    private readonly Harmony _harmony;

    private readonly MethodInfo _prefixMethod;
    private readonly MethodInfo _postfixMethod;

    public int PatchedMethods = 0;

    public Patcher(Harmony harmony)
    {
        _harmony = harmony ?? throw new ArgumentNullException(nameof(harmony));
        _prefixMethod = typeof(Patch.Patch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Patch.Prefix (static, non-public) not found.");
        _postfixMethod = typeof(Patch.Patch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Patch.Postfix (static, non-public) not found.");

        // Prefer assemblies known to the plugin loader so we don't accidentally
        // trigger static constructors in unrelated system assemblies.
        var assemblySet = new HashSet<Assembly>();
        var assemblies = assemblySet.ToArray();

        try
        {
            var gameAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

            if (gameAssembly == null)
            {
                return;
            }

            assemblies = new[] { gameAssembly };
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
                //.Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                .Where(t => t.FullName == null || !t.FullName.Contains("Discord"));
            // skip types with static constructors to avoid premature dependency initialization
            //.Where(t => t.TypeInitializer == null);

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
                // Optional per-method ignore: supports short and "FullName::Method"
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
                postfix: new HarmonyMethod(_postfixMethod)
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

    // --- Helpers -------------------------------------------------------------
    private static IEnumerable<MethodInfo> EnumeratePatchableMethods(Type t)
    {
        if (t == null)
            yield break;

        if (t.Namespace == null || !t.Namespace.Contains("PlayerRoles"))
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

        // Skip *all* special-name methods (covers get/set/add/remove/op_ and explicit iface impls like IFoo.Bar)
        // if (m.IsSpecialName) return false;

        // Explicit interface implementations often appear as Name.Contains(".")
        //if (m.Name.IndexOf('.') >= 0) return false;

        //if (m.DeclaringType != m.ReflectedType)
        //    return false;

        if (m.DeclaringType != m.Module.GetTypes().FirstOrDefault(t => t == m.DeclaringType))
            return false;

        if (m.IsAbstract) return false;
        if (m.ContainsGenericParameters) return false;
        if (m.GetMethodBody() == null) return false;

        // Coroutines / iterator state machines break when patched
        if (m.DeclaringType != m.Module.GetTypes().FirstOrDefault(t => t == m.DeclaringType))
            return false;

        if (m.DeclaringType.Name.Contains("<"))
            return false;

        // Skip iterator methods
        if (m.Name == "MoveNext")
            return false;

        if (m.Name == "System.Collections.IEnumerator.Reset")
            return false;

        if (m.GetCustomAttribute<IteratorStateMachineAttribute>() != null) return false;
        if (typeof(IEnumerator).IsAssignableFrom(m.ReturnType)) return false;

        return true;
    }
}