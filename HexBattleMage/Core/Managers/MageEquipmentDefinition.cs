using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public enum MageEquipmentCategory
    {
        None,
        Bracer,
        Wand,
        Trinket,
        Cloak,
        Helmet,
        Chest,
        Legs,
        Staff
    }

    public enum MageEquipmentSlot
    {
        None,
        Bracer,
        MainHand,
        Trinket,
        Cloak,
        Helmet,
        Chest,
        Legs,
        Staff
    }

    public sealed class MageEquipmentDefinition : MonoBehaviour
    {
        public MageEquipmentCategory Category;
        public MageEquipmentSlot Slot;
        public bool SupportsBlocking;
        public string ProgressionTier = "Apprentice";
        public string CatalogBiome = "Meadows";
        [Header("Unlock catalog visibility")]
        public bool CatalogRequiresBiomeDiscovery = true;
        [Tooltip("Native world boss-defeat key required to reveal this item in the unlock catalog.")]
        public string CatalogBossDefeatKey;
        [Header("Dropped-item physics")]
        public CollisionDetectionMode DropCollisionDetection = CollisionDetectionMode.ContinuousDynamic;
    }
}
