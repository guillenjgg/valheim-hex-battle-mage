using System;
using System.Collections.Generic;

namespace HexBattleMage.Core
{
    internal static class GearTooltip
    {
        internal static string InsertPerks(ItemDrop.ItemData item, string tooltip, string perks)
        {
            var armor = new List<string>();
            var remaining = new List<string>();

            foreach (string line in tooltip.Split('\n'))
            {
                if (line.StartsWith("$item_armor:", StringComparison.Ordinal) || line.StartsWith("$inventory_dmgmod:", StringComparison.Ordinal))
                {
                    armor.Add(line);
                }
                else
                {
                    remaining.Add(line);
                }
            }

            string body = string.Join("\n", remaining);
            string description = item.m_shared.m_description;
            int descriptionEnd = !string.IsNullOrEmpty(description) && body.StartsWith(description, StringComparison.Ordinal) ? description.Length : 0;
            string stats = armor.Count > 0 ? string.Join("\n", armor) + "\n" : string.Empty;

            return body.Insert(descriptionEnd, "\n\n" + stats + perks.Trim() + "\n");
        }
    }
}
