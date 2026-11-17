using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class StoneTrinketProfile : MonoBehaviour
    {
        public string ItemPrefabName = "HexStoneParryTrinket";
        public string BaseItemPrefab = "Amber";
        public string DisplayName = "Stone of Reprisal";
        public string Description = "A rune-bound river stone. Grants Eitr while worn. A successful parry empowers wand shots, even against enemies that cannot be staggered.";
        public Sprite Icon;
        public float Weight = 0.5f;
        public int MaximumQuality = 1;
        public int MaximumStackSize = 1;
        public int Value;
        public bool UseDurability;
        public float Durability = 100f;
        public float DurabilityPerQuality;
        public float DurabilityDrain;
        public float MovementModifier;
        public float ParryDuration = 5f;
        public float WandDamageBonus = 0.25f;
        public string BuffDisplayName = "Rune Reprisal";
        public string BuffDescription = "Wand damage increased after a successful parry.";
        public string AttachmentBone = "Neck";
        [Tooltip("Offset from the attachment bone, measured along the character's right, up and forward axes.")]
        public Vector3 WornPosition = new Vector3(0f, -0.14f, 0.12f);
        public Vector3 WornRotation = new Vector3(0f, 180f, 0f);
        public Vector3 WornScale = Vector3.one;
        public Vector3 DropColliderSize = new Vector3(0.065f, 0.22f, 0.035f);
        public Vector3 DropColliderCenter = new Vector3(0f, 0.06f, 0f);
    }
}
