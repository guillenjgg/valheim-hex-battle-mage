using UnityEngine;
using HarmonyLib;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class WandStyle : MonoBehaviour, IEquipmentVisual
    {
        internal static int StyleCount => PrefabSettings.Wand.StyleMaterials.Length;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionProperty = Shader.PropertyToID("_EmissionColor");
        private Renderer[] _runeRenderers;
        private MaterialPropertyBlock _runeProperties;
        [SerializeField] private int _variant;
        [SerializeField] private Material[] _materials;

        internal void ConfigureMaterials(Material[] materials)
        {
            var renderer = transform.Find("WandModel").GetComponent<MeshRenderer>();

            foreach (var material in materials)
            {
                material.shader = renderer.sharedMaterial.shader;
            }

            _materials = materials;
        }

        internal static void EnableEquipmentVariants()
        {
            var equipmentType = typeof(Jotunn.Managers.ItemManager).Assembly.GetType("Jotunn.Utils.ExtEquipment", true);

            AccessTools.Method(equipmentType, "Enable").Invoke(null, null);
        }

        private void Awake()
        {
            var progression = transform.Find("WandModel/ProgressionRunes");

            if (progression != null)
            {
                _runeRenderers = progression.GetComponentsInChildren<Renderer>(true);
                _runeProperties = new MaterialPropertyBlock();
            }

            Setup(_variant);
        }

        private void Update()
        {
            if (_runeRenderers == null)
            {
                return;
            }

            var profile = PrefabSettings.Wand;

            if (profile == null)
            {
                return;
            }

            var color = profile.RuneColor;

            _runeProperties.SetColor(ColorProperty, color);
            _runeProperties.SetColor(EmissionProperty, color * profile.RuneEmissionStrength);

            foreach (var renderer in _runeRenderers)
            {
                if (renderer.name != "GlowingInlay")
                {
                    continue;
                }

                renderer.SetPropertyBlock(_runeProperties);
            }
        }

        public void Setup(int variant)
        {
            _variant = variant;

            if (_materials == null)
            {
                return;
            }

            if (_materials.Length != StyleCount)
            {
                return;
            }

            var model = transform.Find("WandModel");

            if (model == null)
            {
                return;
            }

            var renderer = model.GetComponent<MeshRenderer>();

            if (renderer == null)
            {
                return;
            }

            renderer.SetPropertyBlock(null);
            renderer.sharedMaterial = _materials[Mathf.Clamp(variant, 0, StyleCount - 1)];
        }
    }
}
