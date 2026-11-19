using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace HexBattleMage.Core
{
    internal static class MageEquipmentCatalog
    {
        internal sealed class Entry
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

        private static readonly AccessTools.FieldRef<Player, HashSet<string>> KnownRecipes = AccessTools.FieldRefAccess<Player, HashSet<string>>("m_knownRecipes");

        internal static IEnumerable<Entry> GetEntries()
        {
            return Entries.OrderBy(entry => System.Array.IndexOf(BiomeProgression, entry.Biome)).ThenBy(entry => ModLocalization.Format(entry.Item.m_itemData.m_shared.m_name));
        }

        internal static bool IsUnlocked(Entry entry, Player player)
        {
            return KnownRecipes(player).Contains(entry.Item.m_itemData.m_shared.m_name);
        }

        internal static bool IsVisible(Entry entry, Player player)
        {
            var definition = entry.Item.GetComponent<Managers.MageEquipmentDefinition>();

            if (definition == null)
            {
                return false;
            }

            if (definition.CatalogRequiresBiomeDiscovery)
            {
                bool discovered = player.GetCurrentBiome() == entry.Biome || player.IsBiomeKnown(new BiomeSector(null, entry.Biome));

                if (!discovered)
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(definition.CatalogBossDefeatKey))
            {
                return true;
            }

            return ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(definition.CatalogBossDefeatKey);
        }

        internal static string BuildDetails(Entry entry, Player player)
        {
            var text = new StringBuilder();
            var resources = new HashSet<string>();

            text.AppendLine(ModLocalization.Format(entry.Recipe.m_requireOnlyOneIngredient ? "$hexbattlemage_unlocks_any_resource" : "$hexbattlemage_unlocks_resources"));

            foreach (var requirement in entry.Recipe.m_resources)
            {
                if (requirement.m_resItem == null)
                {
                    continue;
                }

                if (requirement.m_amount <= 0)
                {
                    continue;
                }

                string token = requirement.m_resItem.m_itemData.m_shared.m_name;

                if (!resources.Add(token))
                {
                    continue;
                }

                bool known = player.IsKnownMaterial(token);
                string color = known ? "#FFFFFF" : "#FFA500";
                string status = known ? "$hexbattlemage_unlocks_known" : "$hexbattlemage_unlocks_missing";

                text.AppendLine("<color=" + color + ">" + ModLocalization.Format(status, ModLocalization.Format(token)) + "</color>");
            }

            if (entry.Recipe.m_craftingStation != null)
            {
                text.AppendLine(ModLocalization.Format("$hexbattlemage_unlocks_station", ModLocalization.Format(entry.Recipe.m_craftingStation.m_name), entry.Recipe.m_minStationLevel));
            }

            return text.ToString().TrimEnd();
        }
    }
}
