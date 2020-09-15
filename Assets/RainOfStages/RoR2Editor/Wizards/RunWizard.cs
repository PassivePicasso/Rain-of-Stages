#if THUNDERKIT_CONFIGURED
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using PassivePicasso.ThunderKit.Proxy.RoR2;

namespace PassivePicasso.RainOfStages.Editor
{
    using Path = System.IO.Path;

    [InitializeOnLoad]
    public class RunWizard : ScriptableWizard
    {

        private static List<Type> RunMap = new List<Type>();

        private int selectedIndex = 0;

        public string RunName;


        [MenuItem("Assets/Rain of Stages/Create Ru&n", priority = 1)]
        static void CreateWizard()
        {
            var ror2ModKitAsm = typeof(Proxy.BodySpawnCard).Assembly;
            var ror2Asm = typeof(RoR2.RoR2Application).Assembly;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(CouldContainRuns(ror2ModKitAsm, ror2Asm))
                .Where(asm => !asm.FullName.Contains("MMHOOK"))
                .Where(asm => !asm.FullName.Contains("R2API"));

            var types = assemblies.SelectMany(asm => asm.GetTypes());
            var assignableImpls = types.Where(type => typeof(Run).IsAssignableFrom(type) && !type.IsAbstract);

            var distinct = assignableImpls.Distinct().ToList();

            RunMap = distinct;

            var wizard = ScriptableWizard.DisplayWizard<RunWizard>("Create Run", "Create");
            wizard.minSize = new Vector2(300, 200);
            wizard.maxSize = new Vector2(300, 200);
        }

        private static Func<Assembly, bool> CouldContainRuns(params Assembly[] assemblies)
        {
            return asm => assemblies.Any(otherAsm => asm.GetReferencedAssemblies().Select(an => an.FullName).Contains(otherAsm.FullName));
        }

        void OnWizardCreate()
        {
            var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (selectedPath.EndsWith(".prefab"))
                selectedPath = Path.GetDirectoryName(selectedPath);

            string name = RunName;
            string localPath = CreateAsset.GetUniquePath(name, selectedPath);
            name = Path.GetFileNameWithoutExtension(localPath);
            GameObject runObject = new GameObject(name, RunMap[selectedIndex],
                                                    typeof(TeamManager),
                                                    typeof(RunCameraManager),
                                                    typeof(NetworkRuleBook));
            CreateAsset.CreateNew(runObject, localPath);
            DestroyImmediate(runObject);
        }

        void OnWizardUpdate()
        {
            titleContent = new GUIContent("New Run");
        }

        protected override bool DrawWizardGUI()
        {
            EditorGUI.BeginChangeCheck();
            var rect = EditorGUILayout.BeginVertical();

            var runName = EditorGUILayout.TextField("Run Name", RunName);

            int index = EditorGUILayout.Popup("Run Type", selectedIndex, RunMap.Select(rm => rm.Name).ToArray());

            EditorGUILayout.EndVertical();
            var changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                selectedIndex = index;
                RunName = runName;
            }

            return changed;
        }
    }
}
#endif