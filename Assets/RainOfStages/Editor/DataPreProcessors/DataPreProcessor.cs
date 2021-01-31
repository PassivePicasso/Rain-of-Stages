#if THUNDERKIT_CONFIGURED
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Editor.DataPreProcessors
{
    public abstract class DataPreProcessor<T, DPP> where T : Object where DPP : DataPreProcessor<T, DPP>, new()
    {
        static int waitTime = 10;
        static float elapsed = 0;

        public static DPP Instance { get; protected set; }

        private Dictionary<string, IEnumerable<T>> dataSets;

        public virtual string Group(T instance) => "Default";

        public IEnumerable<T> this[string group] => dataSets?.ContainsKey(group) ?? false ? dataSets[group] : Enumerable.Empty<T>();

        public DataPreProcessor() { }

        public void UpdateSelectionsCache()
        {
            elapsed += Time.deltaTime;
            if (elapsed < waitTime) return;
            elapsed = 0;

            string[] dataSet = AssetDatabase.FindAssets($"t:{typeof(T).Name}");


            var selections = dataSet.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                                .Select(path => AssetDatabase.LoadAssetAtPath<T>(path))
                                .ToArray();

            dataSets = selections.GroupBy(Group).ToDictionary(group => group.Key, group => (IEnumerable<T>)group.ToArray());
        }
    }
}
#endif
