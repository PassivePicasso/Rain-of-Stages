using PassivePicasso.RainOfStages.Proxy;
using PassivePicasso.RainOfStages.Utilities;
using RoR2;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using BodySpawnCard = PassivePicasso.RainOfStages.Proxy.BodySpawnCard;
using CharacterSpawnCard = PassivePicasso.RainOfStages.Proxy.CharacterSpawnCard;
using InteractableSpawnCard = PassivePicasso.RainOfStages.Proxy.InteractableSpawnCard;

namespace PassivePicasso.RainOfStages.Editor
{
    using Path = System.IO.Path;
    public class CreateAsset : ScriptableObject
    {
        static int updateWait = 10;

        [InitializeOnLoadMethod]
        public static void InitializeProject()
        {
            updateWait = 10;
            EditorApplication.update += InstallEditorPack;
        }

        private static void InstallEditorPack()
        {
            if (--updateWait > 0) return;
            Debug.Log("Configuring project for Rain of Stages");
            EditorApplication.update -= InstallEditorPack;

            var rosConfigured = AssetDatabase.IsValidFolder("Assets/RainOfStages");
            if (rosConfigured) return;

            var editorPack = AssetDatabase.FindAssets("RainOfStagesEditorPack", new[] { "Packages" }).Select(x => AssetDatabase.GUIDToAssetPath(x)).ToArray();
            if (editorPack.Length > 0)
            {
                var assetPath = editorPack[0];
                var pwd = Directory.GetCurrentDirectory();
                var finalPath = Path.Combine(pwd, assetPath);
                var fullPath = Path.GetFullPath(finalPath);
                Debug.Log(fullPath);
                System.Diagnostics.Process.Start(fullPath);
            }
        }

        [MenuItem("Assets/Rain of Stages/" + nameof(SurfaceDef))]
        public static void CreateSurfaceDef() => ScriptableHelper.CreateAsset<SurfaceDef>();

        [MenuItem("Assets/Rain of Stages/" + nameof(DirectorCardCategorySelection))]
        public static void CreateDirectorCardCategorySelection() => ScriptableHelper.CreateAsset<DirectorCardCategorySelection>();

        [MenuItem("Assets/Rain of Stages/" + nameof(MusicTrackDefRef))]
        public static void CreateMusicTrackDefRef() => ScriptableHelper.CreateAsset<MusicTrackDefRef>();

        [MenuItem("Assets/Rain of Stages/SpawnCards/" + nameof(SpawnCard))]
        public static void CreateSpawnCard() => ScriptableHelper.CreateAsset<SpawnCard>();
        [MenuItem("Assets/Rain of Stages/SpawnCards/" + nameof(InteractableSpawnCard))]
        public static void CreateInteractableSpawnCard() => ScriptableHelper.CreateAsset<InteractableSpawnCard>();
        [MenuItem("Assets/Rain of Stages/SpawnCards/" + nameof(CharacterSpawnCard))]
        public static void CreateCharacterSpawnCard() => ScriptableHelper.CreateAsset<CharacterSpawnCard>();
        [MenuItem("Assets/Rain of Stages/SpawnCards/" + nameof(BodySpawnCard))]
        public static void CreateBodySpawnCard() => ScriptableHelper.CreateAsset<BodySpawnCard>();


        [MenuItem("Assets/Rain of Stages/Stages/" + nameof(SceneDefReference), priority = 2)]
        public static void CreateSceneDefReference() => ScriptableHelper.CreateAsset<SceneDefReference>();

        [MenuItem("Assets/Rain of Stages/Stages/" + nameof(SceneDefinition), priority = 2)]
        public static void CreateCustomSceneProxy() => ScriptableHelper.CreateAsset<SceneDefinition>();

        [MenuItem("Tools/Rain of Stages/New Stag&e", priority = 1)]
        public static void CreateStage()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var director = new GameObject("Director", typeof(NetworkIdentity), typeof(DirectorCore), typeof(SceneDirector), typeof(CombatDirector), typeof(CombatDirector));
            var globalEventManager = new GameObject("GlobalEventManager", typeof(Proxy.GlobalEventManager));

            var sceneInfo = new GameObject("SceneInfo", typeof(Proxy.SceneInfo), typeof(ClassicStageInfo), typeof(PostProcessVolume));
            sceneInfo.layer = LayerIndex.postProcess.intVal;
            sceneInfo.SetActive(false);

            var combatDirectors = director.GetComponents<CombatDirector>();

            var sceneDirector = director.GetComponent<SceneDirector>();
            var teleporterPaths = AssetDatabase.FindAssets("iscTeleporter").Select(x => AssetDatabase.GUIDToAssetPath(x));
            var teleporterAssets = teleporterPaths.Select(x => AssetDatabase.LoadAssetAtPath<SpawnCard>(x)).ToArray();
            var teleporterSpawnCard = teleporterAssets.First(tp => tp.name.Equals("iscTeleporter"));
            sceneDirector.teleporterSpawnCard = teleporterSpawnCard;

            var stageInfo = sceneInfo.GetComponent<ClassicStageInfo>();
            var dccsMonsters = AssetDatabase.FindAssets("BlackBeachMonsters").Select(x => AssetDatabase.GUIDToAssetPath(x)).Select(x => AssetDatabase.LoadAssetAtPath<DirectorCardCategorySelection>(x)).FirstOrDefault();
            var dccsInteractables = AssetDatabase.FindAssets("BlackBeachInteractables").Select(x => AssetDatabase.GUIDToAssetPath(x)).Select(x => AssetDatabase.LoadAssetAtPath<DirectorCardCategorySelection>(x)).FirstOrDefault();
            if (dccsInteractables) stageInfo.interactableCategories = dccsInteractables;
            if (dccsMonsters)
            {
                var field = typeof(ClassicStageInfo).GetField("monsterCategories", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field.SetValue(stageInfo, dccsMonsters);
            }
            combatDirectors[0].moneyWaveIntervals = new RangeFloat[] { new RangeFloat { min = 1, max = 1 } };
            combatDirectors[1].moneyWaveIntervals = new RangeFloat[] { new RangeFloat { min = 1, max = 1 } };

            var worldObject = new GameObject("World");
            worldObject.layer = LayerMask.NameToLayer("World");

            var lightObject = new GameObject("Directional Light (SUN)", typeof(Light));
            lightObject.transform.forward = Vector3.forward + Vector3.down + Vector3.right;

            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.5f;
            (float h, float s, float v) color = (0, 0, 0);
            Color.RGBToHSV(Color.white, out color.h, out color.s, out color.v);
            color.v = 0.5f;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.color = Color.HSVToRGB(color.h, color.s, color.v);
            RenderSettings.sun = light;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientSkyColor = light.color;
            RenderSettings.ambientGroundColor = light.color;
            RenderSettings.ambientEquatorColor = light.color;
            RenderSettings.ambientIntensity = 0.8f;
            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
            Lightmapping.realtimeGI = false;
            Lightmapping.bakedGI = false;

            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        public static string GetUniquePath(string name, string rootPath = "Assets")
        {
            int i = -1;
            string localPath = GetPath(name, rootPath, i);

            //Check if the Prefab and/or name already exists at the path
            while (AssetDatabase.LoadAssetAtPath(localPath, typeof(GameObject)))
                localPath = GetPath(name, rootPath, ++i);
            return localPath;
        }

        //Set the path as within the Assets folder, and name it as the GameObject's name with the .prefab format
        public static string GetPath(string name, string rootPath = "Assets", int postfix = -1) => $"{rootPath}/{name}{(postfix > -1 ? $"_{postfix}" : "")}.prefab";


        public static void CreateNew(GameObject obj, string localPath)
        {
            //Create a new Prefab at the path given
            Object prefab = PrefabUtility.SaveAsPrefabAsset(obj, localPath, out _);
            //PrefabUtility.ReplacePrefab(obj, prefab, ReplacePrefabOptions.ConnectToPrefab);
        }

    }
}