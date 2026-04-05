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
    private static List<string> Blacklisted => ConfigDebugger.Instance!.BlacklistAssemblies;
    
    private static List<string> BlacklistedNamespaces => ConfigDebugger.Instance!.BlacklistedNamespaces;
    
    private static List<string> Whitelist => ConfigDebugger.Instance!.WhitelistNamespaces;
    
    private readonly List<Type> _types;
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
        _types = [];

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


            foreach (Type type in asmTypes)
            {
                if (type == null)
                    continue;

                if (type.Namespace == null || !IsNamespaceWhitelisted(type.Namespace))
                    continue;

                if (type.IsInterface)
                    continue;

                // https://stackoverflow.com/questions/14343498/how-does-the-c-sharp-compiler-work-with-generics
                if (type.FullName != null && (type.FullName.Contains('`') || type.FullName.Contains("<"))) 
                    continue;

                if (type.TypeInitializer != null)
                    continue;

                _types.Add(type);
            }
        }
    }

    public void PatchMethods()
    {
        if (_types.Count == 0)
        {
            Logger.Warn("No types to patch.");
            return;
        }

        int tried = 0;
        List<(MethodInfo method, Type type)> methodsToPatch = [];

        foreach (Type type in _types)
        {
            if (BlacklistedNamespaces.Any(prefix => type.Namespace?.Contains(prefix) == true))
                continue;   
            
            foreach (MethodInfo method in EnumeratePatchableMethods(type))
            {
                tried++;
                methodsToPatch.Add((method, type));
            }
        }

        foreach ((MethodInfo method, Type type) in methodsToPatch)
        {
            try
            {
                _harmony.Patch(
                    original: method,
                    prefix: new HarmonyMethod(_prefixMethod),
                    finalizer: new HarmonyMethod(_finalizerMethod)
                );

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


        Logger.Info($"Tried patching {tried} methods across {_types.Count} types; successfully patched {_patchedMethods}.");
    }

    private static bool IsBlacklisted(Assembly asm)
    {
        string? name = asm.GetName().Name;
        bool yesDisplay = Blacklisted.Any(prefix => name.Contains(prefix));

        if (!yesDisplay)
            Logger.Info($"Found assembly: {asm.GetName().Name}");

        return yesDisplay;
    }

    private static bool IsNamespaceWhitelisted(string? name)
    {
        if (name == null) 
            return false;

        for (int i = 0; i < Whitelist.Count; i++)
            if (name.Contains(Whitelist[i]))
                return true;
        
        return false;
    }

    private static IEnumerable<MethodInfo> EnumeratePatchableMethods(Type type)
    {
        const BindingFlags flags =
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Instance |
        BindingFlags.Static |
        BindingFlags.DeclaredOnly;

        MethodInfo[] methods;
        try
        {
            methods = type.GetMethods(flags);
        }
        catch
        {
            yield break;
        }

        foreach (MethodInfo method in methods)
            if (CanPatchRegular(method))
                yield return method;
    }

    private static bool CanPatchRegular(MethodInfo? method)
    {
        if (method == null) 
            return false;

        if (method.IsAbstract) 
            return false;

        // https://stackoverflow.com/questions/14343498/how-does-the-c-sharp-compiler-work-with-generics
        if (method.Name.Contains("<"))
            return false;

        if (method.Name == "MoveNext") 
            return false;

        if (method.Name == "System.Collections.IEnumerator.Reset") 
            return false;

        try
        {
            // TODO: Test if ` will work here
            if (method.IsGenericMethod || method.IsGenericMethodDefinition) 
                return false;

            if (method.ContainsGenericParameters) 
                return false;

            if (method.ReturnType.IsGenericParameter) 
                return false;

            if (method.ReturnType.IsGenericType && method.ReturnType.ContainsGenericParameters) 
                return false;

            if (typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
                return false;
        }
        catch 
        { 
            return false; 
        }

        if (method.DeclaringType != method.Module.GetTypes().FirstOrDefault(t => t == method.DeclaringType))
            return false;

        if (method.GetMethodBody() == null) 
            return false;

        if (method.GetCustomAttribute<IteratorStateMachineAttribute>() != null) 
            return false;

        if (method.GetCustomAttribute<AsyncStateMachineAttribute>() != null) 
            return false;

        return true;
    }
}