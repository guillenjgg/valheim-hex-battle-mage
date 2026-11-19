using HarmonyLib;
using HexBattleMage.Core.Managers;
using UnityEngine;

namespace HexBattleMage.Core
{
    internal static class GearVitals
    {
        private delegate void FoodValues(Player player, ref float health, ref float stamina, ref float eitr);
        private static readonly FoodValues ReadFoodValues = AccessTools.MethodDelegate<FoodValues>(AccessTools.Method(typeof(Player), "GetTotalFoodValue"));

        internal static void CopyProfile(GameObject source, GameObject target)
        {
            var profile = source.GetComponent<HexBattleMageVitalityProfile>();

            if (profile == null)
            {
                return;
            }

            var copy = target.GetComponent<HexBattleMageVitalityProfile>();

            if (copy == null)
            {
                copy = target.AddComponent<HexBattleMageVitalityProfile>();
            }

            copy.MaximumHealth = profile.MaximumHealth;
            copy.MaximumStamina = profile.MaximumStamina;
        }

        internal static float Bonus(Humanoid humanoid, bool health)
        {
            float total = 0f;

            foreach (var item in humanoid.GetInventory().GetAllItems())
            {
                if (!item.m_equipped)
                {
                    continue;
                }

                if (item.m_dropPrefab == null)
                {
                    continue;
                }

                var profile = item.m_dropPrefab.GetComponent<HexBattleMageVitalityProfile>();

                if (profile != null)
                {
                    total += Mathf.Max(0f, health ? profile.MaximumHealth : profile.MaximumStamina);
                }
            }

            return total;
        }

        internal static void Refresh(Player player, float health, float stamina)
        {
            if (!player.IsOwner())
            {
                return;
            }

            float maximumHealth = 0f;
            float maximumStamina = 0f;
            float eitr = 0f;

            ReadFoodValues(player, ref maximumHealth, ref maximumStamina, ref eitr);
            player.SetMaxHealth(maximumHealth, false);
            player.SetMaxStamina(maximumStamina, false);
            player.SetHealth(Mathf.Min(health, player.GetMaxHealth()));

            float staminaDifference = Mathf.Min(stamina, player.GetMaxStamina()) - player.GetStamina();

            if (staminaDifference > 0f)
            {
                player.AddStamina(staminaDifference);
            }
        }
    }
}
