using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class HexBattleMageStaffProfile : MonoBehaviour
    {
        public string ItemPrefabName = "HexBattleMage_Staff_BlackForest_01";
        public string BaseItemPrefab = "StaffFireball";
        public string DisplayName = "$item_hexbattlemage_staff_blackforest_01";
        public string Description = "$item_hexbattlemage_staff_blackforest_01_description";
        public Sprite Icon;
        public GameObject WornModel;
        public GameObject ProjectileModel;
        public float BluntDamage = 30f;
        public float PickaxeDamage = 20f;
        public float BluntPerQuality = 6f;
        public float PickaxePerQuality = 4f;
        public float StaminaCost = 5f;
        public float EitrCost = 8f;
        public float ProjectileSpeed = 25f;
        public float ProjectileGravity = 2f;
        public float ProjectileVisualScale = 2.75f;
        public float ProjectileCollisionRadius = 0.22f;
        public bool SuppressNativeCinders = true;
        [HideInInspector] public int BoulderVersion;
        public int ToolTier = 1;
        public int MaximumQuality = 3;
        public float Weight = 2f;
        public float Durability = 200f;
        public float DurabilityPerQuality = 50f;
        public Skills.SkillType Skill = Skills.SkillType.ElementalMagic;
        public Vector3 DropColliderCenter = new Vector3(0f, 0f, 0.4f);
        public Vector3 DropColliderSize = new Vector3(0.2f, 0.2f, 1.8f);
    }
}
