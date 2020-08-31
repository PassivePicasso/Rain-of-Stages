#if THUNDERKIT_CONFIGURED
using BepInEx.Logging;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace PassivePicasso.RainOfStages.Monomod
{
    internal class HookMap
    {
        public MethodInfo SourceMethod;
        public HookAttribute Hook;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class HookAttribute : Attribute
    {

        private static readonly ParameterModifier[] EmptyPMs = new ParameterModifier[0];
        public static ManualLogSource Logger;

        public string MethodName { get; }
        public bool IsStatic { get; }
        public int Priority { get; }

        private static List<(Type, Hook)> hooks = new List<(Type, Hook)>();

        private Type type;

        public HookAttribute(string type, string methodName = null, bool isStatic = false, int priority = 0) 
        : this(Type.GetType(type), methodName, isStatic, priority)
        {
        }

        public HookAttribute(Type type, string methodName = null, bool isStatic = false, int priority = 0)
        {
            this.type = type;
            MethodName = methodName;
            IsStatic = isStatic;
            Priority = priority;
        }

        internal static IEnumerable<HookMap> GetRetargets(Type type, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
            => type.GetMethods(bindingFlags)
                   .Select(mi => new HookMap { SourceMethod = mi, Hook = mi.GetCustomAttributes<HookAttribute>().FirstOrDefault() })
                   .Where(dm => dm.Hook != null);
        public static void ApplyHooks<T>() => ApplyHooks(typeof(T));
        public static void ApplyHooks(Type type)
        {
            var retargets = GetRetargets(type, BindingFlags.Public | BindingFlags.Static)
                     .Union(GetRetargets(type, BindingFlags.Public | BindingFlags.Instance))
                     .Union(GetRetargets(type, BindingFlags.NonPublic | BindingFlags.Static))
                     .Union(GetRetargets(type, BindingFlags.NonPublic | BindingFlags.Instance))
                     .ToArray();

            if (retargets.Any())
            {
                Logger?.LogInfo($"Configuring {retargets.Length} Hooks");
                foreach (var map in retargets)
                {
                    try
                    {
                        var bindingFlags = (map.Hook.IsStatic ? BindingFlags.Static : BindingFlags.Instance)
                                         | (map.SourceMethod.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic);

                        Logger?.LogDebug($"Source Method Binding Flags: {(map.SourceMethod.IsStatic ? $"{BindingFlags.Static}" : $"{BindingFlags.Instance}")} " +
                                                                  $" | {(map.SourceMethod.IsPublic ? $"{BindingFlags.Public}" : $"{BindingFlags.NonPublic}")}");

                        Type[] parameterTypes = map.SourceMethod.GetParameters()
                                                   .Select(pi => pi.ParameterType)
                                                   .Skip(map.Hook.IsStatic ? 1 : 2)
                                                   .ToArray();

                        string[] parameterNames = map.SourceMethod.GetParameters()
                                                     .Select(pi => pi.Name)
                                                     .Skip(map.Hook.IsStatic ? 1 : 2)
                                                     .ToArray();

                        string paramString = String.Empty;
                        for (int i = 0; i < parameterNames.Length; i++)
                        {
                            if (i > 0) paramString += ",";
                            paramString += $"{parameterTypes[i].Name} {parameterNames[i]}";
                        }
                        Logger?.LogDebug($"Source Method: {map.SourceMethod.DeclaringType}.{map.SourceMethod.Name}({paramString})");

                        string targetMethodName = map.Hook.MethodName ?? map.SourceMethod.Name;
                        Logger?.LogDebug($"Target Method: {map.Hook.type}.{targetMethodName}({paramString})");

                        var targetMethod = map.Hook.type.GetMethod(targetMethodName, bindingFlags, null, parameterTypes, EmptyPMs);
                        Logger?.LogDebug($"Found Method: ({targetMethod?.Name}) on {map.Hook.type.FullName}");
                        
                        
                        Logger?.LogDebug($"Target Method: {map.Hook.type}.{targetMethodName}({paramString})");
                        Logger?.LogInfo($"Hooking: {map.Hook.type}.{targetMethodName} hooked by ({type.FullName}.{map.SourceMethod.Name})");

                        hooks.Add((type, new Hook(targetMethod, map.SourceMethod)));
                    }
                    catch (Exception e)
                    {
                        Logger?.LogError(e);
                    }
                }
            }
        }

        public static void DisableHooks(Type type)
        {
            var appliedHooks = hooks.Where((hookType, _) => hookType.Equals(type));
            foreach (var (_, hook) in appliedHooks)
                hook.Undo();
        }
    }
}
#endif
