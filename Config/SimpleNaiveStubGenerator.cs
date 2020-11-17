#if UNITY_EDITOR

using PassivePicasso.ThunderKit.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PassivePicasso.ThunderKit.Config
{
    public static class SimpleNaiveStubGenerator
    {
        private static readonly string[] CommonUsings = new[] { "UnityEngine" };

        private static readonly string NL = Environment.NewLine;

        [MenuItem("ThunderKit/Generate Proxies")]
        public static void GenerateProxies()
        {
            var settings = ThunderKitSettings.GetOrCreateSettings();
            var currentDir = Directory.GetCurrentDirectory();
            //var proxyPath = Path.Combine(currentDir, "Assets", Path.GetFileNameWithoutExtension(settings.GameExecutable), "GeneratedProxies");
            var assembliesPath = Path.Combine(currentDir, "Assets", "Assemblies");
            var gameAssembly = EditorUtility.OpenFilePanel("Open Game Assembly", assembliesPath, "dll");
            var proxyPath = EditorUtility.OpenFolderPanel("Output Location", Path.Combine(currentDir, "Assets"), Path.Combine(Path.GetFileNameWithoutExtension(settings.GameExecutable), "GeneratedProxies"));
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(asm => asm.CodeBase.Contains(gameAssembly));

            var overwrite = EditorUtility.DisplayDialog("Overwrite", "Overwrite existing proxy files?", "Yes", "No");
            if (assembly == null)
            {
                Debug.LogError("Assembly not loaded cannot generate proxies. Only load assemblies from under the Assets directory.");
                return;
            }

            var assemblyTypes = assembly.GetTypes();

            var uniObjects = assemblyTypes.Where(IsProcessableType).Where(t => t.MemberType != MemberTypes.NestedType);//.Where(t => typeof(UnityEngine.Object).IsAssignableFrom(t)).Where(t => !t.IsAbstract);

            if (!Directory.Exists(proxyPath)) Directory.CreateDirectory(proxyPath);
            foreach (var type in uniObjects)
            {
                try
                {
                    var isGlobal = string.IsNullOrWhiteSpace(type.Namespace);
                    var classDefinitions = GenerateDefinition(type, isGlobal ? "" : "    ");

                    var definition = string.Join(NL,
                 isGlobal ? "" : $"namespace {type.Namespace}",
                 isGlobal ? "" : $"{{",
                            classDefinitions,
                 isGlobal ? "" : $"}}"
                        );

                    var filePath = proxyPath;
                    if (!isGlobal) filePath = Path.Combine(proxyPath, Path.Combine(type.Namespace.Split('.')));

                    string fileName = $"{type.Name}.cs";
                    if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);

                    filePath = Path.Combine(filePath, fileName);
                    if (overwrite && File.Exists(filePath)) File.Delete(filePath);

                    File.WriteAllText(filePath, definition);
                }
                catch (Exception e) { Debug.Log(e); }
            }

            AssetDatabase.Refresh();
        }

        private static string GenerateDefinition(Type type, string indent = "")
        {
            var isGlobal = string.IsNullOrWhiteSpace(type.Namespace);
            var nestedClassDefs = string.Empty;
            var definition = string.Empty;
            var methods = string.Empty;

            if (!typeof(Enum).IsAssignableFrom(type) && !typeof(MulticastDelegate).IsAssignableFrom(type))
            {
                var classDef = string.Empty;
                var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                     .Where(pf => pf.IsPublic || (pf.GetCustomAttribute<SerializeField>() != null));

                var fieldLines = fieldInfos.Any() ? string.Join(NL, fieldInfos.SelectMany(RenderField).Select(s => $"{indent}{s}").ToArray())
                                                  : "";

                var propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).AsEnumerable();
                if (type.BaseType != null && type.BaseType != typeof(object))
                    propertyInfos = propertyInfos.Union(type.BaseType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                                                     .Where(pi =>
                                                                     {
                                                                         var getter = pi.GetGetMethod();
                                                                         var setter = pi.GetSetMethod();
                                                                         bool result = false;
                                                                         if (getter != null) result |= getter.IsAbstract;
                                                                         if (setter != null) result |= setter.IsAbstract;
                                                                         return result;
                                                                     }));

                var propertyLines = propertyInfos.Any() ? string.Join(NL, propertyInfos.SelectMany(RenderProperty).Select(s => $"{indent}{s}").ToArray())
                                                        : "";


                var nestedDefinitions = new List<string>();
                nestedDefinitions.Clear();
                var types = type.GetNestedTypes().Union(type.GetNestedTypes(BindingFlags.NonPublic)).Where(IsProcessableType);
                foreach (var t in types)
                    nestedDefinitions.Add($"\r\n{GenerateDefinition(t, indent + "    ")}");

                if (nestedDefinitions.Any())
                    nestedClassDefs = string.Join(NL, nestedDefinitions.ToArray());

                var abstractStr = type.IsAbstract ? " abstract " : " ";
                var serializable = type.GetCustomAttribute<SerializableAttribute>() != null ? $"{indent}[System.Serializable]" : "";

                var currentTypeFriendlyName = type.GetFriendlyName();
                currentTypeFriendlyName = currentTypeFriendlyName.Substring(currentTypeFriendlyName.LastIndexOf('.') + 1);

                classDef = type.BaseType == null || type.BaseType == typeof(object) ? $"{indent}public{abstractStr}class {currentTypeFriendlyName}"
                         : typeof(ValueType).IsAssignableFrom(type.BaseType) ? $"{indent}public struct {currentTypeFriendlyName}"
                                                                                    : $"{indent}public{abstractStr}class {currentTypeFriendlyName} : global::{type.BaseType.GetFriendlyName()}";
                if (!string.IsNullOrEmpty(serializable))
                    classDef = $"{serializable}\r\n{classDef}";

                if (type.BaseType != null)
                {
                    var ctors = type.BaseType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
                    if (ctors.Any())
                    {
                        var ctor = ctors.FirstOrDefault(ctorA => ctorA.GetParameters().Length > 0);
                        if (ctor != null)
                        {
                            var ctorParams = string.Join(",", ctor.GetParameters().Select(p =>
                            {
                                var paramOptions = p.IsOut ? "out " :
                                   p.ParameterType.IsByRef ? "ref " : "";
                                return $"{paramOptions}{p.ParameterType.GetParameterFriendlyName().Trim('&')} {p.Name}";
                            }).ToArray());

                            var baseCtorParams = string.Join(",", ctor.GetParameters().Select(p => p.Name).ToArray());

                            var visibility = ctor.IsPublic ? "public" : ctor.IsAssembly ? "internal" : ctor.IsFamily ? "protected" : ctor.IsFamilyAndAssembly ? "protected internal" : "private";
                            methods += $"{indent}    {visibility} {type.Name}({ctorParams}) : base({baseCtorParams}){{}}{NL}";
                        }
                    }
                }

                if (type.IsAbstract)
                {
                    var abstractMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                              .Where(mi => mi.IsAbstract);
                    if (abstractMethods.Any())
                    {
                        foreach (var method in abstractMethods)
                        {
                            if (method.Name.StartsWith("get_")) continue;
                            var visibility = method.IsPublic ? "public" : method.IsAssembly ? "internal" : method.IsFamily ? "protected" : method.IsFamilyAndAssembly ? "protected internal" : "private";
                            string returnType, parameters;
                            GetMethodData(method, out returnType, out parameters);
                            methods += $"{indent}    {visibility} abstract {returnType} {method.Name}({parameters});{NL}";
                        }
                    }
                }
                else
                {
                    var abstractImplementations = type.BaseType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                              .Where(mi => mi.IsAbstract);
                    if (abstractImplementations.Any())
                    {
                        foreach (var method in abstractImplementations)
                        {
                            if (method.Name.StartsWith("get_")) continue;
                            var visibility = method.IsPublic ? "public" : method.IsAssembly ? "internal" : method.IsFamily ? "protected" : method.IsFamilyAndAssembly ? "protected internal" : "private";
                            string returnType, parameters;
                            GetMethodData(method, out returnType, out parameters);
                            methods += $"{indent}    {visibility} override {returnType} {method.Name}({parameters}) {{ throw new System.NotImplementedException(); }}{NL}";
                        }
                    }
                }

                methods = methods.TrimEnd();
                fieldLines = fieldLines.TrimEnd();
                nestedClassDefs = nestedClassDefs.TrimEnd();

                var parts = new List<string> { classDef, $"{indent}{{" };

                if (!string.IsNullOrEmpty(fieldLines)) parts.Add(fieldLines);
                if (!string.IsNullOrEmpty(propertyLines)) parts.Add(propertyLines);
                if (!string.IsNullOrEmpty(methods)) parts.Add(methods);
                if (!string.IsNullOrEmpty(nestedClassDefs)) parts.Add(nestedClassDefs);

                parts.Add($"{indent}}}");

                definition = string.Join(NL, parts.ToArray());
            }
            else if (typeof(MulticastDelegate).IsAssignableFrom(type))
            {
                var visibility = type.IsNested ? (type.IsPublic || type.IsNestedPublic ? "public"
                                                                                       : "private")
                                               : string.Empty;

                var method = type.GetMethods()[0];
                string returnType, parameters;
                GetMethodData(method, out returnType, out parameters);

                definition = $"{indent}{visibility} delegate {returnType} {type.Name}({parameters});";
            }
            else if (typeof(Enum).IsAssignableFrom(type.BaseType))
            {
                var enumValues = string.Join(NL, Enum.GetNames(type).Select(v => $"{indent}    {v},").ToArray()).Trim(',');
                definition = string.Join(NL,
                        $"{indent}public enum {type.Name}",
                        $"{indent}{{",
                        enumValues,
                        $"{indent}}}"
                    );
            }

            return definition;
        }
        static bool IsProcessableType(Type t) => !t.GetFriendlyName().StartsWith("<PrivateImplementationDetails>") && !t.Name.Contains("<") && !t.Name.Contains(">") && !t.Name.Contains("DisplayClass");

        private static void GetMethodData(MethodInfo method, out string returnType, out string parameters)
        {
            returnType = string.Empty;
            if (typeof(void).IsAssignableFrom(method.ReturnType)) returnType = "void";
            else returnType = $"global::{method.ReturnType.GetFriendlyName()}";

            parameters = string.Join(",", method.GetParameters().Select(p =>
            {
                var paramOptions = p.IsOut ? "out " :
                   p.ParameterType.IsByRef ? "ref " : "";
                return $"{paramOptions}{p.ParameterType.GetParameterFriendlyName().Trim('&')} {p.Name}";
            }).ToArray());
        }

        private static IEnumerable<string> RenderField(FieldInfo info)
        {
            var hasSerializeField = info.GetCustomAttribute<SerializeField>() != null;
            var visibility = info.IsPublic ? "public" : info.IsAssembly ? "internal" : info.IsFamily ? "protected" : info.IsFamilyAndAssembly ? "protected internal" : "private";
            if (info.FieldType.IsNestedPrivate)
                visibility = "private";
            var fieldType = info.FieldType.GetFriendlyName();

            if (hasSerializeField) yield return $"    [UnityEngine.SerializeField]";
            yield return $"    {visibility} global::{fieldType} {info.Name};";
        }
        private static IEnumerable<string> RenderProperty(PropertyInfo info)
        {
            var hasSerializeField = info.GetCustomAttribute<SerializeField>() != null;

            if (hasSerializeField) yield return $"    [UnityEngine.SerializeField]";

            var propertyType = info.PropertyType.GetFriendlyName();
            var getter = info.GetGetMethod();
            var setter = info.GetSetMethod();
            var result = "    ";
            if (getter?.IsPublic ?? false) result += "public ";
            if (getter?.IsAbstract ?? false) result += "override ";

            result += $"global::{propertyType} {info.Name} {{ ";
            if (propertyType.EndsWith("&"))
            {
                propertyType = propertyType.Trim('&');
                result += $"get; ";
                result += "private set; ";
            }
            else
            {
                if (getter != null) result += $"get; ";

                if (setter != null)
                    if (setter.IsPublic) result += "set; ";
                    else
                        result += "private set; ";
            }

            result += "}";

            yield return result;
        }
    }
}

#endif