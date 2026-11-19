using UnityEngine;

namespace HexBattleMage.Core.Trinkets
{
    public enum TrinketRewardTrigger
    {
        None,
        SuccessfulParry
    }

    public sealed class HexBattleMageTrinketProfile : MonoBehaviour
    {
        [Header("Identity and configuration")]
        public string ItemPrefabName = string.Empty;
        public string ConfigSection = string.Empty;
        public string RewardStatusName = string.Empty;
        public TrinketRewardTrigger RewardTrigger;
        public string BaseItemPrefab = "Amber";
        [Header("Player-facing text (localization tokens)")]
        public string DisplayName = "$hexbattlemage_trinket_default_name";
        public string Description = "$hexbattlemage_trinket_default_description";
        public string PassiveHeading = "$hexbattlemage_passive_effects";
        public string RewardHeading = "$hexbattlemage_combat_reward";
        public string RewardCondition = "$hexbattlemage_on_successful_parry";
        public string RewardDetails = "$hexbattlemage_parry_refresh";
        public string BossHeading = "$hexbattlemage_boss_effect";
        public string BossCondition = "$hexbattlemage_on_successful_boss_parry";
        public Sprite Icon;
        [Header("Item stats")]
        public float Weight = 0.5f;
        public int MaximumQuality = 1;
        public int MaximumStackSize = 1;
        public int Value;
        public bool UseDurability;
        public float Durability = 100f;
        public float DurabilityPerQuality;
        public float DurabilityDrain;
        public float MovementModifier;
        [Header("Successful-parry reward; ignored when RewardTrigger is None")]
        public float ParryDuration = 2f;
        public float WandDamageBonus;
        public float BossParryDamageBonus;
        [Header("Buff text (localization tokens)")]
        public string BuffDisplayName = "$hexbattlemage_trinket_reward";
        public string BuffDescription = "$se_hexbattlemage_parry_wand_damage_description";
        [Header("Worn attachment and world drop")]
        public string AttachmentBone = "Neck";
        [Tooltip("Offset from the attachment bone, measured along the character's right, up and forward axes.")]
        public Vector3 WornPosition = new Vector3(0f, -0.14f, 0.12f);
        public Vector3 WornRotation = new Vector3(0f, 180f, 0f);
        public Vector3 WornScale = Vector3.one;
        public Vector3 DropColliderSize = new Vector3(0.065f, 0.22f, 0.035f);
        public Vector3 DropColliderCenter = new Vector3(0f, 0.06f, 0f);
    }
}
