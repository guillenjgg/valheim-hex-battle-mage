using UnityEngine;
using HarmonyLib;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class WandStyle : MonoBehaviour, IEquipmentVisual
    {
        internal const int StyleCount = 3;
        [SerializeField] private int _variant;
        [SerializeField] private Material[] _materials;

        internal void ConfigureMaterials(Material darkMaterial, Material birchMaterial)
        {
            var renderer = transform.Find("WandModel").GetComponent<MeshRenderer>();

            darkMaterial.shader = renderer.sharedMaterial.shader;
            birchMaterial.shader = renderer.sharedMaterial.shader;
            _materials = new[] { renderer.sharedMaterial, darkMaterial, birchMaterial };
        }

        internal static void EnableEquipmentVariants()
        {
            var equipmentType = typeof(Jotunn.Managers.ItemManager).Assembly.GetType("Jotunn.Utils.ExtEquipment", true);

            AccessTools.Method(equipmentType, "Enable").Invoke(null, null);
        }

        private void Awake()
        {
            Setup(_variant);
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
