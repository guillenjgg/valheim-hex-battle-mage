using HexBattleMage.Assets;
using Jotunn.Entities;
using Jotunn.Managers;

namespace HexBattleMage.Core.Managers
{
    internal static class ItemRegistrationManager
    {
        internal static bool RegisterItems()
        {
            var wandPrefab = AssetBundleHelper.HexBeechWand;
            var itemDrop = wandPrefab.GetComponent<ItemDrop>();

            if (itemDrop == null)
            {
                DebugLog.LogError($"Prefab '{wandPrefab.name}' is missing its ItemDrop component.");
                return false;
            }

            ApplySettings(itemDrop.m_itemData.m_shared.m_attack);

            var wand = new CustomItem(wandPrefab, fixReference: true);

            if (!ItemManager.Instance.AddItem(wand))
            {
                DebugLog.LogError($"Failed to register item '{wandPrefab.name}'.");
                return false;
            }

            return true;
        }

        internal static bool IsWand(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.m_dropPrefab == null)
            {
                return false;
            }

            return item.m_dropPrefab.name == AssetBundleHelper.HexBeechWand.name;
        }

        internal static void ApplySettings(Attack attack)
        {
            attack.m_attackAnimation = DevConfig.AttackAnimation.Value;
            attack.m_attackChainLevels = DevConfig.AttackChainLevels.Value;
            attack.m_attackRandomAnimations = DevConfig.RandomAttackAnimations.Value;
            attack.m_attackProjectile = AssetBundleHelper.HexMagicBolt;
            attack.m_projectileVel = DevConfig.ProjectileVelocity.Value;
            attack.m_projectileAccuracy = DevConfig.ProjectileAccuracy.Value;
            attack.m_projectileAccuracyMin = DevConfig.ProjectileAccuracyMin.Value;
            attack.m_skillAccuracy = DevConfig.SkillAccuracy.Value;
        }
    }
}
