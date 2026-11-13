using HexBattleMage.Assets;
using Jotunn.Entities;
using Jotunn.Managers;

namespace HexBattleMage.Core.Managers
{
    internal static class ItemRegistrationManager
    {
        private const float ProjectileVelocity = 100f;

        internal static bool RegisterItems()
        {
            var wandPrefab = AssetBundleHelper.HexBeechWand;
            var itemDrop = wandPrefab.GetComponent<ItemDrop>();

            if (itemDrop == null)
            {
                Plugin.Log.LogError($"Prefab '{wandPrefab.name}' is missing its ItemDrop component.");
                return false;
            }

            var attack = itemDrop.m_itemData.m_shared.m_attack;

            attack.m_attackProjectile = AssetBundleHelper.HexMagicBolt;
            attack.m_projectileVel = ProjectileVelocity;
            attack.m_projectileAccuracy = 0f;
            attack.m_projectileAccuracyMin = 0f;
            attack.m_skillAccuracy = false;

            var wand = new CustomItem(wandPrefab, fixReference: true);

            if (!ItemManager.Instance.AddItem(wand))
            {
                Plugin.Log.LogError($"Failed to register item '{wandPrefab.name}'.");
                return false;
            }

            return true;
        }
    }
}
