using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Proxy
{
    [RequireComponent(typeof(MeshRenderer))]
    public class AddWater : MonoBehaviour
    {
        public Texture2D normalMap;
        public TextAsset assetData;
        public bool useAssetData;
        private Material material;
        Regex floatRegex = new Regex("    - (.*?):\\s(.*)");
        Regex colorRegex = new Regex("    - (.*?):\\s\\{r:(.*?), g:(.*?), b:(.*?), a:(.*?)\\}");

        public Vector2 tile1Size = Vector2.one * 20, tile2Size = Vector2.one * 10;
        public Vector2 offsetSpeed = Vector2.one * .01f;
        Vector2 offset1 = Vector2.zero, offset2 = Vector2.zero;
        public WindZone windZone;
        // Start is called before the first frame update
        void Start()
        {
            material = new Material(Shader.Find("Hopoo Games/Environment/Distant Water"));
            material.SetTexture("_Normal1Tex", normalMap);
            material.SetTexture("_Normal2Tex", normalMap);
            if(useAssetData)
                SetData(material);
            var renderer = gameObject.GetComponent<MeshRenderer>();
            renderer.material = material;
        }

        private void Update()
        {
            if (!material) return;

            offset1 += offsetSpeed * Time.deltaTime;
            var windDir = new Vector2(windZone.transform.forward.x, windZone.transform.forward.z);
            offset2 += (windZone ? (windDir * windZone.windMain) : offsetSpeed) * Time.deltaTime;

            material.SetTextureOffset("_Normal1Tex", offset1);
            material.SetTextureOffset("_Normal2Tex", offset2);
            material.SetTextureScale("_Normal1Tex", tile1Size);
            material.SetTextureScale("_Normal2Tex", tile2Size);
        }

        void SetData(Material material)
        {
            var file = assetData.text;
            var lines = file.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var section = string.Empty;
            foreach (var line in lines)
            {
                switch (line)
                {
                    case string floatLine when floatLine.Contains("m_Floats"):
                        section = "m_Floats";
                        continue;
                    case string colorsLine when colorsLine.Contains("m_Colors"):
                        section = "m_Colors";
                        continue;
                }
                if (!line.StartsWith("    -")) continue;
                switch (section)
                {
                    case "m_Floats":
                        {
                            var match = floatRegex.Match(line);
                            var s1 = match.Groups[1].Value;
                            var s2 = match.Groups[2].Value;
                            material.SetFloat(s1, float.Parse(s2));
                        }
                        continue;
                    case "m_Colors":
                        {
                            var match = colorRegex.Match(line);
                            var s1 = match.Groups[1].Value;
                            var s2 = match.Groups[2].Value;
                            var s3 = match.Groups[3].Value;
                            var s4 = match.Groups[4].Value;
                            var s5 = match.Groups[5].Value;
                            material.SetColor(s1, new Color(float.Parse(s2), float.Parse(s3), float.Parse(s4), float.Parse(s5)));
                        }
                        continue;
                }
            }
        }

    }
}
