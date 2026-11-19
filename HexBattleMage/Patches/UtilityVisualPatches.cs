using System.Collections.Generic;
using HarmonyLib;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Patches
{
    internal sealed class UtilityLayerVisual : MonoBehaviour
    {
        private const float MinimumMeshScale = 0.0001f;
        private SkinnedMeshRenderer _renderer;
        private Mesh _source;
        private Mesh _expanded;
        private float _padding = -1f;

        internal void Configure(SkinnedMeshRenderer renderer, float padding, Vector3 worldUp)
        {
            if (_renderer == renderer && Mathf.Approximately(_padding, padding))
            {
                return;
            }

            _renderer = renderer;

            if (_source == null)
            {
                _source = renderer.sharedMesh;
            }

            if (_source == null)
            {
                return;
            }

            if (!_source.isReadable)
            {
                _padding = padding;

                return;
            }

            renderer.sharedMesh = _source;
            ReleaseMesh();
            _padding = padding;

            if (padding <= 0f)
            {
                return;
            }

            _expanded = Instantiate(_source);
            _expanded.name = _source.name + "_UtilityLayer";

            Vector3 localUp = renderer.transform.InverseTransformDirection(worldUp).normalized;
            Vector3 scale = renderer.transform.lossyScale;
            Vector3 center = _source.bounds.center;
            var vertices = _source.vertices;

            for (int index = 0; index < vertices.Length; index++)
            {
                Vector3 direction = Vector3.ProjectOnPlane(vertices[index] - center, localUp).normalized;
                var worldOffset = direction * padding;
                var localOffset = new Vector3(worldOffset.x / Mathf.Max(Mathf.Abs(scale.x), MinimumMeshScale), worldOffset.y / Mathf.Max(Mathf.Abs(scale.y), MinimumMeshScale), worldOffset.z / Mathf.Max(Mathf.Abs(scale.z), MinimumMeshScale));

                vertices[index] += localOffset;
            }

            _expanded.vertices = vertices;
            _expanded.RecalculateBounds();
            renderer.sharedMesh = _expanded;
        }

        private void OnDestroy()
        {
            if (_renderer != null && _renderer.sharedMesh == _expanded)
            {
                _renderer.sharedMesh = _source;
            }

            ReleaseMesh();
        }

        private void ReleaseMesh()
        {
            if (_expanded == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_expanded);
            }
            else
            {
                DestroyImmediate(_expanded);
            }

            _expanded = null;
        }
    }

    [HarmonyPatch(typeof(VisEquipment), "UpdateEquipmentVisuals")]
    internal static class UtilityVisualPatches
    {
        private static readonly AccessTools.FieldRef<VisEquipment, int> ChestHash = AccessTools.FieldRefAccess<VisEquipment, int>("m_currentChestItemHash");
        private static readonly AccessTools.FieldRef<VisEquipment, List<GameObject>> UtilityInstances = AccessTools.FieldRefAccess<VisEquipment, List<GameObject>>("m_utilityItemInstances");

        private static void Postfix(VisEquipment __instance)
        {
            if (ObjectDB.instance == null)
            {
                return;
            }

            var instances = UtilityInstances(__instance);

            if (instances == null)
            {
                return;
            }

            var chest = ObjectDB.instance.GetItemPrefab(ChestHash(__instance));
            var profile = chest != null ? chest.GetComponent<HexBattleMageArmorProfile>() : null;
            float padding = profile != null ? Mathf.Max(0f, profile.UtilityVisualPadding) : 0f;

            foreach (var instance in instances)
            {
                if (instance == null)
                {
                    continue;
                }

                foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var layer = renderer.GetComponent<UtilityLayerVisual>();

                    if (layer == null && padding <= 0f)
                    {
                        continue;
                    }

                    if (layer == null)
                    {
                        layer = renderer.gameObject.AddComponent<UtilityLayerVisual>();
                    }

                    layer.Configure(renderer, padding, __instance.transform.up);
                }
            }
        }
    }
}
