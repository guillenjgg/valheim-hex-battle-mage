using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HexBattleMage.Core
{
    internal static class MageEquipmentCatalog
    {
        private sealed class Entry
        {
            internal ItemDrop Item;
            internal Recipe Recipe;
            internal Heightmap.Biome Biome;
        }

        private static readonly List<Entry> Entries = new List<Entry>();
        private static readonly Heightmap.Biome[] BiomeProgression =
        {
            Heightmap.Biome.Meadows, Heightmap.Biome.BlackForest, Heightmap.Biome.Swamp,
            Heightmap.Biome.Mountain, Heightmap.Biome.Plains, Heightmap.Biome.Mistlands,
            Heightmap.Biome.AshLands, Heightmap.Biome.DeepNorth, Heightmap.Biome.Ocean
        };

        internal static void Register(ItemDrop item, Recipe recipe, Heightmap.Biome biome)
        {
            Entries.RemoveAll(entry => entry.Item.name == item.name);
            Entries.Add(new Entry { Item = item, Recipe = recipe, Biome = biome });
        }

        internal static string BuildText(Player player)
        {
            var text = new StringBuilder();

            text.AppendLine(ModLocalization.Format("$hexbattlemage_unlocks_intro"));
            text.AppendLine();

            foreach (var group in Entries.GroupBy(entry => entry.Biome).OrderBy(group => System.Array.IndexOf(BiomeProgression, group.Key)))
            {
                string biome = ModLocalization.Format("$biome_" + group.Key.ToString().ToLowerInvariant());

                text.AppendLine($"<color=#FFD479><b>{ModLocalization.Format("$hexbattlemage_unlocks_biome", biome, group.Count())}</b></color>");

                foreach (var entry in group)
                {
                    text.AppendLine($"<b>{ModLocalization.Format(entry.Item.m_itemData.m_shared.m_name)}</b>");

                    var resourceNames = new HashSet<string>();

                    foreach (var requirement in entry.Recipe.m_resources)
                    {
                        if (requirement.m_resItem == null)
                        {
                            continue;
                        }

                        string resourceToken = requirement.m_resItem.m_itemData.m_shared.m_name;

                        if (!resourceNames.Add(resourceToken))
                        {
                            continue;
                        }

                        bool known = player.IsKnownMaterial(resourceToken);
                        string statusToken = known ? "$hexbattlemage_unlocks_known" : "$hexbattlemage_unlocks_missing";
                        string color = known ? "#A4D28A" : "#D6A58A";

                        text.AppendLine($"  <color={color}>{ModLocalization.Format(statusToken, ModLocalization.Format(resourceToken))}</color>");
                    }

                    text.AppendLine();
                }
            }

            return text.ToString();
        }
    }
}
