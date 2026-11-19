using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class HexBattleMageArmorProfile : MonoBehaviour
    {
        [Header("Identity and native baseline")]
        public string ItemPrefabName;
        public string BaseItemPrefab;
        public string DisplayName;
        public string Description;
        public Sprite Icon;

        [Header("Item stats")]
        public float Weight = 2f;
        public int MaximumQuality = 1;
        public float Armor = 2f;
        public float ArmorPerQuality = 2f;
        public bool UseDurability = true;
        public float Durability = 400f;
        public float DurabilityPerQuality = 50f;
        public float MovementModifier;

        [Header("Appearance")]
        [Tooltip("Optional replacement for the native body armor texture. Its alpha preserves exposed skin.")]
        public Texture2D BodyTexture;
        [Tooltip("Empty preserves the native material. Chest armor uses _ChestTex; pants use _LegsTex.")]
        public string BodyTextureProperty;
        public Color MeshTint = Color.white;
        public bool ReplaceMeshTexture;
        [Header("Custom worn visuals")]
        public GameObject MaleVisual;
        public GameObject FemaleVisual;
        public GameObject WorldModel;
        public bool ReplaceNativeVisuals;
        public bool ClearBodyArmorMaterial;
        [Tooltip("Extra world-space clearance for skinned utility visuals worn over this armor.")]
        public float UtilityVisualPadding;
        [Header("Custom world-drop collider; native visuals retain their native collider")]
        public Vector3 DropColliderCenter;
        public Vector3 DropColliderSize = new Vector3(0.15f, 0.15f, 0.15f);
    }
}
