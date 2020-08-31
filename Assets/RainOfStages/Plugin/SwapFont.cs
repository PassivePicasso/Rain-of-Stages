#if THUNDERKIT_CONFIGURED
using PassivePicasso.ThunderKit.Proxy.RoR2.UI;
using TMPro;
using UnityEngine;


namespace PassivePicasso.RainOfStages.UI
{
    [RequireComponent(typeof(HGTextMeshProUGUI))]
    public class SwapFont : MonoBehaviour
    {
        public TMP_Text TextComponent;
        public string FontLocation;

        private void Start()
        {
            var tMP_FontAsset = Resources.Load<TMP_FontAsset>(FontLocation);
            TextComponent.font = tMP_FontAsset;
        }
    }
}
#endif