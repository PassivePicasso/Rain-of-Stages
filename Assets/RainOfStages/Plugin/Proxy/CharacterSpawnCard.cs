#if THUNDERKIT_CONFIGURED
using System.Reflection;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Proxy
{
    public class CharacterSpawnCard : global::RoR2.CharacterSpawnCard, IProxyReference<global::RoR2.SpawnCard>
    {
        static FieldInfo runtimeLoadoutField = typeof(global::RoR2.CharacterSpawnCard).GetField("runtimeLoadout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        new void Awake()
        {
            if (Application.isEditor) return;
            var card = (global::RoR2.CharacterSpawnCard)ResolveProxy();

            prefab = card.prefab;
            loadout = card.loadout;
            noElites = card.noElites;
            hullSize = card.hullSize;
            nodeGraphType = card.nodeGraphType;
            requiredFlags = card.requiredFlags;
            forbiddenFlags = card.forbiddenFlags;
            occupyPosition = card.occupyPosition;

            runtimeLoadoutField.SetValue(this, runtimeLoadoutField.GetValue(card));

            forbiddenAsBoss = card.forbiddenAsBoss;
            sendOverNetwork = card.sendOverNetwork;
            directorCreditCost = card.directorCreditCost;

        }
        public global::RoR2.SpawnCard ResolveProxy() => LoadCard<global::RoR2.CharacterSpawnCard>();

        private T LoadCard<T>() where T : global::RoR2.SpawnCard
        {
            var card = Resources.Load<T>($"spawncards/{typeof(T).Name.ToLower()}s/{name}");
            if (card == null)
                card = Resources.Load<T>($"SpawnCards/{typeof(T).Name}s/{name}");
            if (card == null)
                card = Resources.Load<T>($"spawncards/{typeof(T).Name.ToLower()}s/titan/{name}");
            if (card == null)
                card = Resources.Load<T>($"SpawnCards/{typeof(T).Name}s/Titan/{name}");
            return card;
        }
    }
}
#endif
