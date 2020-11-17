using Facepunch.Steamworks.Callbacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PassivePicasso.ThunderKit.Config.StubGeneration
{
    [Serializable]
    public class ClassDefinition : ScriptableObject
    {
        private static readonly string NL = Environment.NewLine;
        public string Name => Type.GetFriendlyName();

        public string Indent;
        public string Declaration
        {
            get
            {
                var abstractStr = Type.IsAbstract ? " abstract " : " ";
                var serializable = Type.GetCustomAttribute<SerializableAttribute>() != null ? $"{Indent}[System.Serializable]" : "";
                string def;

                var currentTypeFriendlyName = Type.GetFriendlyName();
                currentTypeFriendlyName = currentTypeFriendlyName.Substring(currentTypeFriendlyName.LastIndexOf('.') + 1);

                def = Type.BaseType == null || Type.BaseType == typeof(object) ? $"{Indent}public{abstractStr}class {currentTypeFriendlyName}"
                         : typeof(ValueType).IsAssignableFrom(Type.BaseType) ? $"{Indent}public struct {currentTypeFriendlyName}"
                                                                                    : $"{Indent}public{abstractStr}class {currentTypeFriendlyName} : global::{Type.BaseType.GetFriendlyName()}";
                if (!string.IsNullOrEmpty(serializable))
                    def = $"{serializable}\r\n{def}";

                return def;
            }
        }

        public IEnumerable<FieldDefinition> Fields;
        public IEnumerable<PropertyDefinition> Properties;
        public IEnumerable<ConstructorDefinition> Constructors;
        public IEnumerable<MethodDefinition> Methods;
        public IEnumerable<ClassDefinition> Classes;
        public ClassDefinition Parent;
        public ClassDefinition BaseType;

        public Type Type { get; set; }

        public override string ToString()
        {
            var parts = new List<string> { Declaration, $"{Indent}{{" };

            if (Fields.Any()) parts.AddRange(Fields.Select(f => f.ToString()));
            if (Properties.Any()) parts.AddRange(Properties.Select(p => p.ToString()));
            if (Methods.Any()) parts.AddRange(Methods.Select(m => m.ToString()));
            if (Methods.Any()) parts.AddRange(Methods.Select(m => m.ToString()));
            //if (!string.IsNullOrEmpty(methods)) parts.Add(methods);
            //if (!string.IsNullOrEmpty(nestedClassDefs)) parts.Add(nestedClassDefs);

            //parts.Add($"{Indent}}}");

            //definition = string.Join(NL, parts.ToArray());

            return base.ToString();
        }
    }
}