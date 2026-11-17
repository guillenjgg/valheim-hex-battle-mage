using HarmonyLib;
using HexBattleMage.Assets;
using HexBattleMage.Core;
using UnityEngine;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(VisEquipment), "AttachItem")]
    internal static class WandHolsterPatch
    {

        private static void Postfix(VisEquipment __instance, int itemHash, bool backAttach, GameObject __result)
        {
            if (!backAttach)
            {
                return;
            }

            if (__result == null)
            {
                return;
            }

            if (ZNetScene.instance == null)
            {
                return;
            }

            if (ZNetScene.instance.GetPrefab(itemHash) != AssetBundleHelper.HexBattleMageWand)
            {
                return;
            }

            Transform hips = null;

            foreach (var bone in __instance.GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == PrefabSettings.Wand.HipBoneName)
                {
                    hips = bone;
                    break;
                }
            }

            if (hips == null)
            {
                DebugLog.LogWarning("Cannot holster wand at the hip: player Hips bone was not found.");
                return;
            }

            var attachment = __result.transform;
            var offset = Vector3.Scale(PrefabSettings.Wand.HipOffset, __instance.transform.lossyScale);

            attachment.SetParent(hips, true);
            attachment.position = hips.position + hips.rotation * offset;
            attachment.localRotation = Quaternion.Euler(PrefabSettings.Wand.HolsterAngles);
        }
    }
}
