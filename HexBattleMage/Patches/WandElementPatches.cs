using HexBattleMage.Core.Tuning;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using HexBattleMage.Core.Elements;

namespace HexBattleMage.Patches
{
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    internal static class WandNativeElementPatch
    {
        private const int NativeElementCount = 5;
        private static readonly Dictionary<WandElement, Action<Character, float, short>> NativeEffects = new Dictionary<WandElement, Action<Character, float, short>>
        {
            { WandElement.Fire, Native("AddFireDamage") },
            { WandElement.Frost, Native("AddFrostDamage") },
            { WandElement.Spirit, Native("AddSpiritDamage") },
            { WandElement.Lightning, Native("AddLightningDamage") },
            { WandElement.Poison, Native("AddPoisonDamage") }
        };

        private static Action<Character, float, short> Native(string name)
        {
            return AccessTools.MethodDelegate<Action<Character, float, short>>(AccessTools.Method(typeof(Character), name));
        }

        private static void Prefix(HitData hit)
        {
            if (!WandElementManager.IsWandElementHash(hit.m_statusEffectHash))
            {
                return;
            }

            if (hit.GetTotalDamage() > 0f)
            {
                hit.m_itemLevel = 1;
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int replaced = 0;

            foreach (var instruction in instructions)
            {
                bool matched = false;

                foreach (WandElement element in NativeEffects.Keys)
                {
                    if (!instruction.Calls(AccessTools.Method(typeof(Character), "Add" + element + "Damage")))
                    {
                        continue;
                    }

                    instruction.opcode = OpCodes.Ldarg_2;
                    instruction.operand = null;
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldc_I4, (int)element);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WandNativeElementPatch), nameof(ApplyElement)));
                    replaced++;
                    matched = true;
                    break;
                }

                if (!matched)
                {
                    yield return instruction;
                }
            }

            if (replaced != NativeElementCount)
            {
                throw new InvalidOperationException("Valheim elemental damage hooks changed; expected five native elemental calls, found " + replaced + ".");
            }
        }

        internal static void ApplyElement(Character character, float damage, short variant, HitData originalHit, WandElement element)
        {
            if (!WandElementManager.IsWandElementHash(originalHit.m_statusEffectHash))
            {
                NativeEffects[element](character, damage, variant);
                return;
            }

            if (element != WandElement.Fire && element != WandElement.Spirit && element != WandElement.Poison)
            {
                return;
            }

            if (damage <= 0f)
            {
                return;
            }

            var direct = new HitData { m_point = originalHit.m_point, m_attacker = originalHit.m_attacker };

            direct.m_damage.m_damage = damage;
            character.ApplyDamage(direct, true, true, HitData.DamageModifier.Normal);
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    internal static class WandProjectileColorPatch
    {
        private static void Postfix(Projectile __instance, HitData hitData, ItemDrop.ItemData item)
        {
            if (!HexBattleMage.Core.Managers.ItemRegistrationManager.IsWand(item))
            {
                return;
            }

            if (hitData == null)
            {
                return;
            }

            var view = __instance.GetComponent<ZNetView>();

            if (view == null || !view.IsValid())
            {
                return;
            }

            if (!view.IsOwner())
            {
                return;
            }

            view.GetZDO().Set(WandElementManager.ProjectileElementsKey, WandElementManager.GetElementMask(hitData.m_statusEffectHash));
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    internal static class BlightProjectileSetupPatch
    {
        private static void Postfix(Projectile __instance, HitData hitData, ItemDrop.ItemData item)
        {
            if (!HexBattleMage.Core.Managers.ItemRegistrationManager.IsWand(item))
            {
                return;
            }

            if (hitData == null)
            {
                return;
            }

            if (!WandElementManager.HasElement(hitData.m_statusEffectHash, WandElement.Blight))
            {
                return;
            }

            if (WandElementManager.HasElement(hitData.m_statusEffectHash, WandElement.Fire))
            {
                return;
            }

            if (WandElementManager.GetSettings(WandElement.Blight).Profile.SingleTargetProjectile)
            {
                __instance.m_aoe = 0f;
            }
        }
    }

    [HarmonyPatch(typeof(Character), "Awake")]
    internal static class WandElementCharacterPatch
    {
        private static void Postfix(Character __instance)
        {
            __instance.gameObject.AddComponent<WandElementAnimation>();
        }
    }

    [HarmonyPatch(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.Speed))]
    internal static class WandElementAnimationPatch
    {
        private static void Postfix(Character ___m_character, float speedScale)
        {
            if (___m_character == null)
            {
                return;
            }

            var controller = ___m_character.GetComponent<WandElementAnimation>();

            if (controller != null)
            {
                controller.RecordBaseSpeed(speedScale);
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    internal static class WandElementStunAttackPatch
    {
        private static bool Prefix(Humanoid __instance, ref bool __result)
        {
            if (WandElementManager.Enabled == null || !WandElementManager.Enabled.EffectiveValue())
            {
                return true;
            }

            bool stunned = false;

            foreach (WandElement element in Enum.GetValues(typeof(WandElement)))
            {
                if (!WandElementManager.GetSettings(element).Profile.Stuns)
                {
                    continue;
                }

                if (__instance.GetSEMan().HaveStatusEffect(WandElementManager.GetHash(element)))
                {
                    stunned = true;
                    break;
                }
            }

            if (!stunned)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
