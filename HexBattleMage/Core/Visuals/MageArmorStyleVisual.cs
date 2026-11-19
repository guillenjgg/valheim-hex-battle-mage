using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class MageArmorStyleVisual : MonoBehaviour, IEquipmentVisual
    {
        [SerializeField] private MageArmorStyleProfile _profile;
        [SerializeField] private bool _applyToRootRenderer;

        internal void Configure(MageArmorStyleProfile profile, bool applyToRootRenderer = false)
        {
            _profile = profile;
            _applyToRootRenderer = applyToRootRenderer;
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

            var rootRenderer = _applyToRootRenderer ? GetComponent<Renderer>() : null;

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (_applyToRootRenderer && renderer != rootRenderer)
                {
                    continue;
                }

                if (!_applyToRootRenderer && renderer.name != _profile.RendererName)
                {
                    continue;
                }

                style.Material.shader = renderer.sharedMaterial.shader;
                renderer.sharedMaterial = style.Material;
            }
        }
    }
}
