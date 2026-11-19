using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class HexBattleMageCloakProfile : MonoBehaviour
    {
        [Header("Identity and native equipment behavior")]
        public string ItemPrefabName = "HexBattleMage_Cloak_Meadows_01";
        public string BaseItemPrefab = "CapeDeerHide";
        public string DisplayName = "$item_hexbattlemage_cloak_meadows_01";
        public string Description = "$item_hexbattlemage_cloak_meadows_01_description";
        public Sprite Icon;
        [Header("Body fits and world drop")]
        public GameObject MaleVisual;
        public GameObject FemaleVisual;
        public GameObject WorldModel;
        public Vector3 DropColliderCenter;
        public Vector3 DropColliderSize = new Vector3(0.25f, 0.4f, 0.18f);
        [Header("Item stats")]
        public float Weight = 4f;
        public int MaximumQuality = 3;
        public float Armor = 2f;
        public float ArmorPerQuality = 1f;
        public bool UseDurability = true;
        public float Durability = 400f;
        public float DurabilityPerQuality;
        public float MovementModifier;
        public bool FrostResistance;
    }
}
