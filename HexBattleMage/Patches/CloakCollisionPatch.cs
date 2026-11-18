using System;
using System.Collections.Generic;
using HarmonyLib;
using HexBattleMage.Core.Managers;
using HexBattleMage.Core.Visuals;
using MagicaCloth2;
using UnityEngine;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(VisEquipment), "SetupCloth", new[] { typeof(GameObject) })]
    internal static class CloakCollisionPatch
    {
        private static void Prefix(VisEquipment __instance, GameObject __0)
        {
            var profile = __0.GetComponent<HexBattleMageCloakCollisionProfile>();

            if (profile == null)
            {
                return;
            }

            var originalLists = new Dictionary<MagicaCloth, List<ColliderComponent>>();
            CloakCollisionController controller = null;

            foreach (var cloth in __0.GetComponentsInChildren<MagicaCloth>(true))
            {
                originalLists.Add(cloth, cloth.SerializeData.colliderCollisionConstraint.colliderList);
            }

            try
            {
                if (profile.ProtectBody)
                {
                    profile.GetBodyCapsules();
                }

                controller = __0.GetComponent<CloakCollisionController>();

                if (controller == null)
                {
                    controller = __0.AddComponent<CloakCollisionController>();
                }

                controller.Configure(__instance, profile);
            }
            catch (Exception exception)
            {
                foreach (var original in originalLists)
                {
                    original.Key.SerializeData.colliderCollisionConstraint.colliderList = original.Value;
                }

                if (controller != null)
                {
                    controller.enabled = false;
                }

                Plugin.Log?.LogWarning("Cloak protection could not initialize; continuing native cape attachment. " + exception.Message);
            }
        }
    }
}
