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

            itemDrop.m_itemData.m_shared.m_attack.m_attackProjectile = AssetBundleHelper.HexMagicBolt;

            var wand = new CustomItem(wandPrefab, fixReference: true);

            ItemManager.Instance.AddItem(wand);
        }
    }
}