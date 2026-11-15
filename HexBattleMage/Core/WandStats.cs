using HexBattleMage.Assets;
using HexBattleMage.Core.Managers;
using System.Text;

namespace HexBattleMage.Core
{
    internal static class WandStats
    {
        internal const string CompendiumTopic = "$hexbattlemage_wandstats_title";

        private static Player _player;
        private static string _prefabName;
        private static HitData.DamageTypes _damage;
        private static float _areaRadius;

        internal static void RecordImpact(Player player, string prefabName, HitData.DamageTypes damage, float areaRadius)
        {
            _player = player;
            _prefabName = prefabName;
            _damage = damage;
            _areaRadius = areaRadius;
        }

        internal static string GetText()
        {
            var item = GetCurrentWand();
            var text = new StringBuilder();

            text.AppendLine(ModLocalization.Format("$hexbattlemage_wandstats_current", AssetBundleHelper.HexBattleMageWand.name, item.m_quality));
            AppendDamage(text, item.GetDamage());
            text.AppendLine();
            text.AppendLine(ModLocalization.Format("$hexbattlemage_wandstats_base_note"));
            text.AppendLine();

            if (_player == Player.m_localPlayer && _prefabName != null)
            {
                text.Append(FormatImpact(_prefabName, _damage, _areaRadius));
            }
            else
            {
                text.Append(ModLocalization.Format("$hexbattlemage_wandstats_empty"));
            }

            return text.ToString();
        }

        private static ItemDrop.ItemData GetCurrentWand()
        {
            var player = Player.m_localPlayer;

            if (player != null)
            {
                var equipped = player.GetCurrentWeapon();

                if (ItemRegistrationManager.IsWand(equipped))
                {
                    return equipped;
                }

                foreach (var item in player.GetInventory().GetAllItems())
                {
                    if (ItemRegistrationManager.IsWand(item))
                    {
                        return item;
                    }
                }
            }

            return AssetBundleHelper.HexBattleMageWand.GetComponent<ItemDrop>().m_itemData;
        }

        internal static string FormatImpact(string prefabName, HitData.DamageTypes damage, float areaRadius)
        {
            var text = new StringBuilder();
            string token = areaRadius > 0f ? "$hexbattlemage_wandstats_area" : "$hexbattlemage_wandstats_direct";

            text.AppendLine(ModLocalization.Format(token, prefabName, damage.GetTotalDamage(), areaRadius));
            AppendDamage(text, damage);
            text.AppendLine();
            text.Append(ModLocalization.Format("$hexbattlemage_wandstats_impact_note"));
            return text.ToString();
        }

        private static void AppendDamage(StringBuilder text, HitData.DamageTypes damage)
        {
            AppendDamageLine(text, "total", damage.GetTotalDamage(), true);
            AppendDamageLine(text, "blunt", damage.m_blunt, true);
            AppendDamageLine(text, "pierce", damage.m_pierce, true);
            AppendDamageLine(text, "fire", damage.m_fire);
            AppendDamageLine(text, "frost", damage.m_frost);
            AppendDamageLine(text, "lightning", damage.m_lightning);
            AppendDamageLine(text, "poison", damage.m_poison);
            AppendDamageLine(text, "spirit", damage.m_spirit);
            AppendDamageLine(text, "slash", damage.m_slash);
            AppendDamageLine(text, "generic", damage.m_damage);
            AppendDamageLine(text, "chop", damage.m_chop);
            AppendDamageLine(text, "pickaxe", damage.m_pickaxe);
        }

        private static void AppendDamageLine(StringBuilder text, string type, float amount, bool alwaysShow = false)
        {
            if (!alwaysShow && amount <= 0f)
            {
                return;
            }

            string label = ModLocalization.Format("$hexbattlemage_damage_" + type);

            text.AppendLine(ModLocalization.Format("$hexbattlemage_damage_line", label, amount));
        }
    }
}
