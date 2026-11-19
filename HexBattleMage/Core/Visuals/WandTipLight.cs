using HexBattleMage.Core.Tuning;
using UnityEngine;
using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class WandTipLight : MonoBehaviour
    {
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

            light.color = PrefabSettings.Require<HexBattleMageWandProfile>(prefab).LightColor;
            light.enabled = false;
            tip.gameObject.AddComponent<WandTipLight>();
        }

        private void Awake()
        {
            _light = GetComponent<Light>();
            _tipRenderer = transform.parent.GetComponent<Renderer>();
            _tipProperties = new MaterialPropertyBlock();
        }
        private void Update()
        {
            var wielder = GetComponentInParent<Humanoid>();

            if (_tipRenderer != null)
            {
                var emission = PrefabSettings.Wand.UnattunedTipEmission;

                if (WandElementManager.Enabled != null && WandElementManager.Enabled.EffectiveValue())
                {
                    var element = WandElementManager.ActiveElement.Value;
                    var profile = WandElementManager.GetSettings(element).Profile;

                    emission = profile.TipColor * profile.TipEmissionStrength;
                }

                _tipRenderer.GetPropertyBlock(_tipProperties);
                _tipProperties.SetColor(EmissionProperty, emission);
                _tipRenderer.SetPropertyBlock(_tipProperties);
            }

            _light.color = PrefabSettings.Wand.LightColor;
            _light.range = DevConfig.WandLightRange.EffectiveValue();
            _light.intensity = DevConfig.WandLightIntensity.EffectiveValue();
            _light.enabled = wielder != null && ItemRegistrationManager.IsWand(wielder.GetCurrentWeapon());
        }
    }
}
