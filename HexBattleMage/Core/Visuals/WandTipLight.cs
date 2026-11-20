using HexBattleMage.Core.Tuning;
using UnityEngine;
using HexBattleMage.Core.Elements;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class WandTipLight : MonoBehaviour
    {
        private const string NetworkElementKey = "HexBattleMage.WandElementMask";
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

            var item = wielder != null ? wielder.GetCurrentWeapon() : GetComponentInParent<ItemDrop>()?.m_itemData;
            var active = WandAttunements.GetCombatElements(item);
            int mask = 0;

            foreach (var element in active)
            {
                mask |= 1 << (int)element;
            }
            var view = wielder != null ? wielder.GetComponent<ZNetView>() : null;
            var data = view != null ? view.GetZDO() : null;

            if (data != null)
            {
                if (view.IsOwner())
                {
                    int encoded = mask;

                    if (data.GetInt(NetworkElementKey) != encoded)
                    {
                        data.Set(NetworkElementKey, encoded);
                    }
                }
                else
                {
                    int encoded = data.GetInt(NetworkElementKey);

                    mask = encoded;
                }
            }

            if (_tipRenderer != null)
            {
                var emission = PrefabSettings.Wand.UnattunedTipEmission;

                if (mask != 0 && WandElementManager.Enabled != null && WandElementManager.Enabled.EffectiveValue())
                {
                    emission = Color.black;
                    int count = 0;

                    foreach (WandElement element in System.Enum.GetValues(typeof(WandElement)))
                    {
                        if ((mask & (1 << (int)element)) != 0)
                        {
                            var profile = WandElementManager.GetSettings(element).Profile;

                            emission += profile.TipColor * profile.TipEmissionStrength;
                            count++;
                        }
                    }

                    emission /= Mathf.Max(1, count);
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
