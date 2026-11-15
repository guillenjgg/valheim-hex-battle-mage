using UnityEngine;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class WandTipLight : MonoBehaviour
    {
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
        }

        private void Update()
        {
            var wielder = GetComponentInParent<Humanoid>();

            _light.range = DevConfig.WandLightRange.Value;
            _light.intensity = DevConfig.WandLightIntensity.Value;
            _light.enabled = wielder != null && ItemRegistrationManager.IsWand(wielder.GetCurrentWeapon());
        }
    }
}
