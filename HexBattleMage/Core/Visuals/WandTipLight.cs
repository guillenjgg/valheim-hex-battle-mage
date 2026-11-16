using UnityEngine;
using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class WandTipLight : MonoBehaviour
    {
        private const float TipEmissionStrength = 4.5f;
        private const float FireTipEmissionStrength = 2f;
        private static readonly int EmissionProperty = Shader.PropertyToID("_EmissionColor");
        private Renderer _tipRenderer;
        private MaterialPropertyBlock _tipProperties;
        private Light _light;

        internal static void Configure(GameObject prefab)
        {
            var model = prefab.transform.Find("attach/WandModel");

            if (model == null)
            {
                DebugLog.LogWarning("Wand tip light requires attach/WandModel.");
                return;
            }

            var tip = model.Find("WandTip");

            foreach (var existingLight in prefab.GetComponentsInChildren<Light>(true))
            {
                if (existingLight.transform == tip)
                {
                    continue;
                }

                existingLight.enabled = false;
                existingLight.gameObject.SetActive(false);
            }

            var meshFilter = model.GetComponent<MeshFilter>();

            if (meshFilter == null)
            {
                return;
            }

            if (meshFilter.sharedMesh == null)
            {
                return;
            }

            if (tip == null)
            {
                var bounds = meshFilter.sharedMesh.bounds;

                tip = new GameObject("WandTip").transform;
                tip.SetParent(model, false);
                tip.localPosition = new Vector3(bounds.center.x, bounds.center.y, bounds.max.z);
            }

            var light = tip.GetComponent<Light>();

            if (light == null)
            {
                light = tip.gameObject.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = new Color(0.7f, 0.8f, 1f);
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            light.enabled = false;
            tip.gameObject.AddComponent<WandTipLight>();
        }

        private void Awake()
        {
            _light = GetComponent<Light>();
            _tipRenderer = transform.parent.GetComponent<Renderer>();
            _tipProperties = new MaterialPropertyBlock();
        }
        private static Color GetTipColor(WandElement element)
        {
            switch (element)
            {
                case WandElement.Fire:
                    return new Color(1f, 0f, 0f);
                case WandElement.Frost:
                    return new Color(0.35f, 0.8f, 1f);
                case WandElement.Spirit:
                    return new Color(0.7f, 0.3f, 1f);
                case WandElement.Lightning:
                    return new Color(0.75f, 0.9f, 1f);
                case WandElement.Poison:
                    return new Color(0.3f, 1f, 0.15f);
                default:
                    return new Color(0.35f, 0.75f, 1f);
            }
        }

        private void Update()
        {
            var wielder = GetComponentInParent<Humanoid>();

            if (_tipRenderer != null)
            {
                var emission = new Color(1.8f, 3f, 4.5f);

                if (WandElementManager.Enabled != null && WandElementManager.Enabled.Value)
                {
                    var element = WandElementManager.ActiveElement.Value;
                    float emissionStrength = element == WandElement.Fire ? FireTipEmissionStrength : TipEmissionStrength;

                    emission = GetTipColor(element) * emissionStrength;
                }

                _tipRenderer.GetPropertyBlock(_tipProperties);
                _tipProperties.SetColor(EmissionProperty, emission);
                _tipRenderer.SetPropertyBlock(_tipProperties);
            }

            _light.range = DevConfig.WandLightRange.Value;
            _light.intensity = DevConfig.WandLightIntensity.Value;
            _light.enabled = wielder != null && ItemRegistrationManager.IsWand(wielder.GetCurrentWeapon());
        }
    }
}
