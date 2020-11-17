using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PassivePicasso.ThunderKit.Config.StubGeneration
{
    public class AssemblyTarget : ScriptableObject
    {
        private List<ClassDefinition> classDefinitions;
        public string AssemblyPath;
        public bool Loaded { get; private set; } = false;
        public IEnumerable<ClassDefinition> ClassDefinitions => classDefinitions.AsEnumerable();

        private void Awake()
        {
            if (AssemblyPath == null) return;

            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(asm => asm.CodeBase.Contains(AssemblyPath));
            if (assembly == null) return;

            var processableTypes = assembly.GetTypes().Where(IsProcessableType);

            classDefinitions = processableTypes.Select(pt =>
            {
                var instance = ScriptableObject.CreateInstance<ClassDefinition>();
                instance.Type = pt;
                return instance;
            }).ToList();
        }

        static bool IsProcessableType(Type t) => t.MemberType != MemberTypes.NestedType
                                              && !t.GetFriendlyName().StartsWith("<PrivateImplementationDetails>")
                                              && !t.Name.Contains("<")
                                              && !t.Name.Contains(">")
                                              && !t.Name.Contains("DisplayClass");

    }
}
