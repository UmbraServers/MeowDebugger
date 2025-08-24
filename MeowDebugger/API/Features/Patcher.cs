using System;
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
    public readonly Assembly targetAssembly;
    public readonly Type[] types;

    private readonly Harmony _harmony;

    private readonly MethodInfo _prefixMethod;
    private readonly MethodInfo _postfixMethod;

    public int PatchedMethods = 0;

    public Patcher(Harmony harmony)
    {
        _harmony = harmony ?? throw new ArgumentNullException(nameof(harmony));

        // Validate that Patch.Prefix/Postfix exist and have the right visibility
        _prefixMethod = typeof(Patch.Patch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Patch.Prefix (static, non-public) not found.");
        _postfixMethod = typeof(Patch.Patch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Patch.Postfix (static, non-public) not found.");

        // Locate the plugin & target assembly
        (Plugin? plugin, Assembly? assembly) = PluginLoader.Plugins.FirstOrDefault(x => x.Key.Name == "ATOH");

        if (plugin is null)
        {
            Logger.Warn($"ERROR: Plugin not found!");
            Logger.Warn($"Available plugins: {string.Join(",", PluginLoader.Plugins.Select(p => p.Key.Name))}");
            types = [];
            return;
        }

        targetAssembly = assembly;

        // GetTypes can throw; recover whatever we can
        Type[] asmTypes;
        try
        {
            asmTypes = targetAssembly.GetTypes();
        }
        catch (ReflectionTypeLoadException rtle)
        {
            asmTypes = rtle.Types.Where(t => t != null).ToArray()!;
            Logger.Warn($"ReflectionTypeLoadException while reading types; using {asmTypes.Length} loadable types.");
        }

        // Build ignore sets (support both short names and full names)
        /*var cfg = Plugin.Instance.Config;
        var ignoreTypeNames = new HashSet<string>(cfg.IgnoreTypes ?? Array.Empty<string>());
        var ignoreMethodNames = new HashSet<string>(cfg.IgnoreMethods ?? Array.Empty<string>());*/

        var filtered = asmTypes
            .Where(t => t is { IsInterface: false })
            /*// ignore by short or full name
            .Where(t => !ignoreTypeNames.Contains(t.Name) && !ignoreTypeNames.Contains(t.FullName ?? t.Name))*/
            // skip types explicitly marked with HarmonyPatch (usually tooling/framework)
            .Where(t => !t.IsDefined(typeof(HarmonyPatch), inherit: false))
            .ToList();

        types = filtered.ToArray();
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
            // Avoid double-patching if this ran before (domain reload, etc.)
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
            Logger.Debug($"Patched {type.FullName}::{method.Name}()");
        }
        catch (NotSupportedException)
        {
            Logger.Warn($"Won't patch (not supported): {type.FullName}::{method.Name}()");
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to patch {type.FullName}::{method.Name}()");
        }
    }

    // --- Helpers -------------------------------------------------------------

   private static IEnumerable<MethodInfo> EnumeratePatchableMethods(Type t)
    {
        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.DeclaredOnly;

        // 1) Regular declared methods (filter below)
        foreach (var m in t.GetMethods(flags))
            if (CanPatchRegular(m))
                yield return m;

        // 2) Compiler-generated state machines (async / coroutines)
        foreach (var nt in t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!LooksLikeStateMachine(nt)) continue;

            // Only patch MoveNext; skip get_Current / Reset and other explicit iface members
            var moveNext = nt.GetMethod("MoveNext",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (CanPatchMoveNext(moveNext))
                yield return moveNext;
        }
    }

    private static bool CanPatchRegular(MethodInfo? m)
    {
        if (m == null) return false;

        // Skip *all* special-name methods (covers get/set/add/remove/op_ and explicit iface impls like IFoo.Bar)
        if (m.IsSpecialName) return false;

        // Explicit interface implementations often appear as Name.Contains(".")
        if (m.Name.IndexOf('.') >= 0) return false;

        if (m.IsAbstract) return false;
        if (m.ContainsGenericParameters) return false;
        if (m.GetMethodBody() == null) return false;

        return true;
    }

    private static bool CanPatchMoveNext(MethodInfo? m)
    {
        if (m == null) return false;
        if (m.IsAbstract) return false;
        if (m.ContainsGenericParameters) return false;
        if (m.GetMethodBody() == null) return false;
        // MoveNext is not special-name; safe to try
        return true;
    }

    private static bool LooksLikeStateMachine(Type nt)
    {
        if (typeof(IAsyncStateMachine).IsAssignableFrom(nt)) return true;

        if (Attribute.IsDefined(nt, typeof(CompilerGeneratedAttribute), inherit: false)) return true;

        var n = nt.Name;
        if (!string.IsNullOrEmpty(n) && (n.Contains("<>") || (n.Contains("<") && n.Contains(">"))))
            return true;

        return false;
    }
    
}