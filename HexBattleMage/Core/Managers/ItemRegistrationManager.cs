using HexBattleMage.Assets;
using Jotunn.Entities;
using Jotunn.Managers;

namespace HexBattleMage.Core.Managers
{
    internal static class ItemRegistrationManager
    {
        internal static void RegisterItems()
        {
            var wandPrefab = AssetBundleHelper.HexBeechWand;
            var itemDrop = wandPrefab.GetComponent<ItemDrop>();
            var attack = itemDrop.m_itemData.m_shared.m_attack;

            attack.m_attackProjectile = AssetBundleHelper.HexMagicBolt;
            attack.m_projectileVel = 100f;
            attack.m_projectileAccuracy = 0f;
            attack.m_projectileAccuracyMin = 0f;
            attack.m_skillAccuracy = false;

            var wand = new CustomItem(wandPrefab, fixReference: true);

            ItemManager.Instance.AddItem(wand);
        }
    }
}