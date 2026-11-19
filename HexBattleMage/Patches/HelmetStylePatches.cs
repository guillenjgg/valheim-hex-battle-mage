using HarmonyLib;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Patches
{
    internal sealed class HelmetStyleState : MonoBehaviour
    {
        internal int RequestedVariant;
        internal int DisplayedHash;
        internal int DisplayedVariant = -1;
    }

    internal static class HelmetStyleSynchronization
    {
        private const string VariantKey = "HexBattleMage_HelmetVariant";
        private static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> HelmetItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_helmetItem");
        private static readonly AccessTools.FieldRef<VisEquipment, ZNetView> NetworkView = AccessTools.FieldRefAccess<VisEquipment, ZNetView>("m_nview");
        internal static readonly AccessTools.FieldRef<VisEquipment, int> CurrentHelmetHash = AccessTools.FieldRefAccess<VisEquipment, int>("m_currentHelmetItemHash");

        internal static HelmetStyleState GetState(VisEquipment equipment)
        {
            var state = equipment.GetComponent<HelmetStyleState>();

            return state != null ? state : equipment.gameObject.AddComponent<HelmetStyleState>();
        }

        internal static void Publish(Humanoid humanoid, VisEquipment equipment, bool isRagdoll)
        {
            var view = NetworkView(equipment);
            var data = view != null ? view.GetZDO() : null;

            if (data != null && !view.IsOwner() && !isRagdoll)
            {
                return;
            }

            var item = HelmetItem(humanoid);

            if (item?.m_dropPrefab == null)
            {
                return;
            }

            if (item.m_dropPrefab.GetComponent<HexBattleMageArmorProfile>() == null)
            {
                return;
            }

            var styles = item.m_dropPrefab.GetComponent<MageArmorStyleProfile>();

            if (styles == null)
            {
                return;
            }

            int variant = Mathf.Clamp(item.m_variant, 0, styles.Styles.Length - 1);

            GetState(equipment).RequestedVariant = variant;

            if (data != null && view.IsOwner())
            {
                data.Set(VariantKey, variant);
            }
        }

        internal static int ReadVariant(VisEquipment equipment, int hash)
        {
            var styles = ArmorManager.GetHelmetStyles(hash);

            if (styles == null)
            {
                return 0;
            }

            var view = NetworkView(equipment);
            var data = view != null ? view.GetZDO() : null;
            int local = GetState(equipment).RequestedVariant;
            int variant = data != null ? data.GetInt(VariantKey, local) : local;

            return Mathf.Clamp(variant, 0, styles.Styles.Length - 1);
        }
    }

    [HarmonyPatch(typeof(Humanoid), "SetupVisEquipment", new[] { typeof(VisEquipment), typeof(bool) })]
    internal static class HelmetStylePublishPatch
    {
        private static void Prefix(Humanoid __instance, VisEquipment visEq, bool isRagdoll)
        {
            HelmetStyleSynchronization.Publish(__instance, visEq, isRagdoll);
        }
    }

    [HarmonyPatch(typeof(VisEquipment), "SetHelmetEquipped", new[] { typeof(int), typeof(int) })]
    internal static class HelmetStyleRefreshPatch
    {
        private static void Prefix(VisEquipment __instance, int hash)
        {
            if (ArmorManager.GetHelmetStyles(hash) == null)
            {
                return;
            }

            var state = HelmetStyleSynchronization.GetState(__instance);
            int variant = HelmetStyleSynchronization.ReadVariant(__instance, hash);

            if (state.DisplayedHash == hash && state.DisplayedVariant == variant)
            {
                return;
            }

            state.DisplayedHash = hash;
            state.DisplayedVariant = variant;
            HelmetStyleSynchronization.CurrentHelmetHash(__instance) = 0;
        }
    }

    [HarmonyPatch(typeof(VisEquipment), "AttachItem", new[] { typeof(int), typeof(int), typeof(Transform), typeof(bool), typeof(bool), typeof(int) })]
    internal static class HelmetStyleAttachPatch
    {
        private static void Prefix(VisEquipment __instance, int itemHash, ref int variant)
        {
            if (ArmorManager.GetHelmetStyles(itemHash) == null)
            {
                return;
            }

            variant = HelmetStyleSynchronization.ReadVariant(__instance, itemHash);
        }
    }
}
