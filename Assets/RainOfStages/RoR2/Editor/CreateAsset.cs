#if THUNDERKIT_CONFIGURED
using PassivePicasso.RainOfStages.Proxy;
using PassivePicasso.RainOfStages.Utilities;
using PassivePicasso.ThunderKit.Proxy;
using PassivePicasso.ThunderKit.Proxy.RoR2;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BodySpawnCard = PassivePicasso.RainOfStages.Proxy.BodySpawnCard;
using CharacterSpawnCard = PassivePicasso.RainOfStages.Proxy.CharacterSpawnCard;
using InteractableSpawnCard = PassivePicasso.RainOfStages.Proxy.InteractableSpawnCard;

namespace PassivePicasso.RainOfStages.Editor
{
    public class CreateAsset : ScriptableObject
    {
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

        [MenuItem("Assets/Rain of Stages/New Stag&e", priority = 1)]
        public static void CreateStage()
        {
            var defaultGameObjects = ScriptableObject.CreateInstance<DefaultGameObjects>();


            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(defaultGameObjects.Director, scene);
            PrefabUtility.InstantiatePrefab(defaultGameObjects.GlobalEventManager, scene);
            PrefabUtility.InstantiatePrefab(defaultGameObjects.SceneInfo, scene);
            var worldObject = new GameObject("World");
            worldObject.layer = LayerMask.NameToLayer("World");

            var lightObject = new GameObject("Directional Light (SUN)", typeof(Light));
            lightObject.transform.forward = Vector3.forward + Vector3.down + Vector3.right;

            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            (float h, float s, float v) color = (0, 0, 0);
            Color.RGBToHSV(Color.white, out color.h, out color.s, out color.v);
            color.s = 0.5f;
            color.v = 0.8f;
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

        [MenuItem("Assets/Rain of Stages/Modding Assets/" + nameof(BakeSettings))]
        public static void CreateBakeSettings() => ScriptableHelper.CreateAsset<BakeSettings>();
    }
}
#endif