using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public enum MageEquipmentCategory
    {
        None,
        Bracer,
        Wand
    }

    public enum MageEquipmentSlot
    {
        None,
        Bracer,
        MainHand
    }

    public sealed class MageEquipmentDefinition : MonoBehaviour
    {
        public MageEquipmentCategory Category;
        public MageEquipmentSlot Slot;
        public bool SupportsBlocking;
        public string ProgressionTier = "Apprentice";
        public string CatalogBiome = "Meadows";
    }
}
