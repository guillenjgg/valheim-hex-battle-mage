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
        Legs
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
        Legs
    }

    public sealed class MageEquipmentDefinition : MonoBehaviour
    {
        public MageEquipmentCategory Category;
        public MageEquipmentSlot Slot;
        public bool SupportsBlocking;
        public string ProgressionTier = "Apprentice";
        public string CatalogBiome = "Meadows";
        [Header("Dropped-item physics")]
        public CollisionDetectionMode DropCollisionDetection = CollisionDetectionMode.ContinuousDynamic;
    }
}
