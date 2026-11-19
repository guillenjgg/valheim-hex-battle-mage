using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class MageArmorStyleVisual : MonoBehaviour, IEquipmentVisual
    {
        [SerializeField] private MageArmorStyleProfile _profile;
        [SerializeField] private bool _applyToAllRenderers;

        internal void Configure(MageArmorStyleProfile profile, bool applyToAllRenderers = false)
        {
            _profile = profile;
            _applyToAllRenderers = applyToAllRenderers;
        }

        public void Setup(int variant)
        {
            if (_profile == null)
            {
                return;
            }

            if (_profile.Styles.Length == 0)
            {
                return;
            }

            var style = _profile.Styles[Mathf.Clamp(variant, 0, _profile.Styles.Length - 1)];

            if (style.Material == null)
            {
                return;
            }

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (!_applyToAllRenderers && renderer.name != _profile.RendererName)
                {
                    continue;
                }

                style.Material.shader = renderer.sharedMaterial.shader;
                renderer.sharedMaterial = style.Material;
            }
        }
    }
}
