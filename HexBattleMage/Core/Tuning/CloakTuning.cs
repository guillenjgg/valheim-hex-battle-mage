using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core.Tuning
{
    internal static class CloakTuning
    {
        private const string MeadowsCloak = "HexBattleMage_Cloak_Meadows_01";

        internal static void Apply(GameObject prefab)
        {
            var cloak = prefab.GetComponent<HexBattleMageCloakProfile>();

            if (cloak == null)
            {
                return;
            }

            if (cloak.ItemPrefabName == MeadowsCloak)
            {
                Meadows01(prefab, cloak);
            }
        }

        private static void Meadows01(GameObject prefab, HexBattleMageCloakProfile cloak)
        {
            cloak.Armor = 2f;
            cloak.ArmorPerQuality = 1f;
            cloak.MaximumQuality = 3;
            cloak.FrostResistance = true;
            PrefabSettings.Require<HexBattleMageVitalityProfile>(prefab).MaximumStamina = 5f;
        }
    }
}
