#if THUNDERKIT_CONFIGURED
using BepInEx.Logging;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace PassivePicasso.RainOfStages.Monomod
{
    internal class DetourMap
    {
        public MethodInfo SourceMethod;
        public DetourAttribute Detour;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DetourAttribute : Attribute
    {
        private static readonly ParameterModifier[] EmptyPMs = new ParameterModifier[0];
        public static ManualLogSource Logger = null;
        private static List<(Type, Detour)> detours = new List<(Type, Detour)>();

        public Type Type { get; private set; }
        public BindingFlags BindingFlags { get; private set; }
        public string MethodName { get; }
        public bool IsStatic { get; }
        public int Priority { get; }

        public DetourAttribute(string type, string methodName = null, bool isStatic = false, int priority = 0)
        {
            Type = Type.GetType(type);
            MethodName = methodName;
            IsStatic = isStatic;
            Priority = priority;
        }

        public DetourAttribute(Type type, string methodName = null, bool isStatic = false, int priority = 0)
        {
            Type = type;
            MethodName = methodName;
            IsStatic = isStatic;
            Priority = priority;
        }

        internal static IEnumerable<DetourMap> GetRetargets(Type type, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
            => type.GetMethods(bindingFlags)
                   .Select(mi => new DetourMap { SourceMethod = mi, Detour = mi.GetCustomAttributes<DetourAttribute>().FirstOrDefault() })
                   .Where(dm => dm.Detour != null);
        public static void ApplyDetours(Type type)
        {
            var retargets = GetRetargets(type, BindingFlags.Public | BindingFlags.Static)
                     .Union(GetRetargets(type, BindingFlags.Public | BindingFlags.Instance))
                     .Union(GetRetargets(type, BindingFlags.NonPublic | BindingFlags.Static))
                     .Union(GetRetargets(type, BindingFlags.NonPublic | BindingFlags.Instance))
                     .ToArray();

            if (retargets.Any())
            {
                Logger?.LogDebug($"Configuring {retargets.Length} Detours");
                foreach (var map in retargets)
                {
                    try
                    {
                        var bindingFlags = (map.Detour.IsStatic ? BindingFlags.Static : BindingFlags.Instance)
                                         | (map.SourceMethod.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic);

                        Type[] parameterTypes = map.SourceMethod.GetParameters().Select(pi => pi.ParameterType).ToArray();
                        string[] parameterNames = map.SourceMethod.GetParameters().Select(pi => pi.Name).ToArray();
                        string paramString = String.Empty;

                        for (int i = 0; i < parameterNames.Length; i++)
                        {
                            if (i > 0) paramString += ",";
                            paramString += $"{parameterTypes[i].Name} {parameterNames[i]}";
                        }
                        Logger?.LogDebug($"Source Method: {map.SourceMethod.DeclaringType}.{map.SourceMethod.Name}({paramString})");

                        string targetMethodName = map.Detour.MethodName ?? map.SourceMethod.Name;
                        Logger?.LogDebug($"Target Method: {map.Detour.Type}.{targetMethodName}({paramString})");

                        var targetMethod = map.Detour.Type.GetMethod(targetMethodName, bindingFlags, null, parameterTypes, EmptyPMs);
                        Logger?.LogDebug($"Found Method: ({targetMethod?.Name}) on {map.Detour.Type.FullName}");

                        detours.Add((type, new Detour(map.SourceMethod, targetMethod)));
                    }
                    catch (Exception e)
                    {
                        Logger?.LogError(e);
                    }
                }
            }
        }

        public static void DisableDetours(Type type)
        {
            var appliedDetours = detours.Where((detourType, _) => detourType.Equals(type));
            foreach (var (_, detour) in appliedDetours)
                detour.Undo();
        }
    }
}
#endif
