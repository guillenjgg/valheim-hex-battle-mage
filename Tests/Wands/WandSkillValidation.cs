using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using HexBattleMage.Core.Managers;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WandSkillValidation
{
    private static int _awards;
    private static Skills.SkillType _skill;
    private static float _amount;
    private static readonly BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static void Run()
    {
        var harmony = new Harmony("HexBattleMage.WandSkillValidation");
        var root = new GameObject("Skill fixture");
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable("HEXBATTLEMAGE_TEST_BUNDLE"));

        root.SetActive(false);

        try
        {
            foreach (var type in new[] { typeof(Projectile), typeof(ZNetView), typeof(ItemDrop) })
            {
                foreach (string name in new[] { "Awake", "OnDestroy", "OnValidate" })
                {
                    var method = AccessTools.DeclaredMethod(type, name);

                    if (method != null)
                    {
                        harmony.Patch(method, prefix: new HarmonyMethod(typeof(WandSkillValidation), nameof(Skip)));
                    }
                }
            }

            harmony.Patch(AccessTools.Method(typeof(Projectile), "IsValidTarget"), prefix: new HarmonyMethod(typeof(WandSkillValidation), nameof(ValidTarget)));
            harmony.Patch(AccessTools.Method(typeof(Character), "Damage"), prefix: new HarmonyMethod(typeof(WandSkillValidation), nameof(Skip)));
            harmony.Patch(AccessTools.Method(typeof(Player), "AddAdrenaline"), prefix: new HarmonyMethod(typeof(WandSkillValidation), nameof(Skip)));
            harmony.Patch(AccessTools.Method(typeof(Character), "GetZDOID"), prefix: new HarmonyMethod(typeof(WandSkillValidation), nameof(Identity)));
            harmony.Patch(AccessTools.Method(typeof(TrinketValidationPlayer), "RaiseSkill"), prefix: new HarmonyMethod(typeof(WandSkillValidation), nameof(Award)));
            var wand = bundle.LoadAsset<GameObject>("HexBattleMage_Wand_Meadows_01");
            var profile = wand.GetComponent<HexBattleMageWandProfile>();
            var shared = wand.GetComponent<ItemDrop>().m_itemData.m_shared;

            Check(profile.WeaponSkill == Skills.SkillType.Clubs && shared.m_skillType == Skills.SkillType.Clubs && shared.m_attack.m_raiseSkillAmount == 1f, "Clubs and its native XP amount are baked on the wand.");
            var owner = root.AddComponent<TrinketValidationPlayer>();
            Check(profile.AttackStamina == 5f && shared.m_attack.m_attackStamina == 5f, "Five base stamina per shot is baked in both inspectable settings and native attack.");
            var effects = new SEMan(owner, root.AddComponent<ZNetView>());

            AccessTools.Field(typeof(Character), "m_seman").SetValue(owner, effects);
            var attack = shared.m_attack.Clone();

            AccessTools.Field(typeof(Attack), "m_character").SetValue(attack, owner);
            AccessTools.Field(typeof(Attack), "m_weapon").SetValue(attack, wand.GetComponent<ItemDrop>().m_itemData);
            float nativeCost = (float)AccessTools.Method(typeof(Attack), "GetAttackStamina").Invoke(attack, null);

            Check(Mathf.Abs(nativeCost - 5f) < 0.001f, "Native attack stamina calculation returns five at zero skill with no modifiers.");
            var enemyObject = new GameObject("Valid creature");

            enemyObject.transform.SetParent(root.transform, false);
            enemyObject.AddComponent<TrinketValidationEnemy>();
            var collider = enemyObject.AddComponent<BoxCollider>();
            var projectile = root.AddComponent<Projectile>();

            Set(projectile, "m_owner", owner);
            Set(projectile, "m_skill", shared.m_skillType);
            Set(projectile, "m_raiseSkillAmount", shared.m_attack.m_raiseSkillAmount);
            projectile.m_onlyStopOnTerrain = true;
            Set(projectile, "m_stayAfterHitStatic", true);
            Set(projectile, "m_hitNoise", 0f);
            Set(projectile, "m_noDamageFriendly", false);
            projectile.m_aoe = 0f;
            projectile.m_hitEffects = new EffectList();
            projectile.m_hitWaterEffects = new EffectList();
            _awards = 0;
            projectile.OnHit(collider, Vector3.zero, false, Vector3.zero);
            Check(_awards == 1 && _skill == Skills.SkillType.Clubs && _amount == 1f, "Actual native projectile impact awards Clubs exactly once for a qualifying creature hit.");
            projectile.OnHit(null, Vector3.one, false, Vector3.zero);
            Check(_awards == 1, "A missed projectile awards no skill XP.");
            var wall = new GameObject("Wall");

            wall.transform.SetParent(root.transform, false);
            projectile.OnHit(wall.AddComponent<BoxCollider>(), Vector3.one, false, Vector3.zero);
            Check(_awards == 1, "A plain world collider awards no skill XP.");
            var damage = new HitData.DamageTypes { m_blunt = 5f, m_pierce = 5f, m_fire = 15f };

            damage.Modify(0.5f);
            Check(damage.m_blunt == 2.5f && damage.m_pierce == 2.5f && damage.m_fire == 7.5f, "Native damage multiplication affects every wand damage channel.");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "wand-skill-report.txt"), "PASS: baked 5 base stamina and native stamina calculation; baked Clubs skill and 1 XP factor; actual native Projectile.OnHit awards one Clubs call on a valid creature hit and none for misses/plain world colliders; native damage scaling affects blunt, pierce and fire together. Target validity, damage transport, adrenaline and skill-storage bootstrap are isolated fixtures. No custom XP hook is introduced.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            bundle.Unload(true);
            harmony.UnpatchSelf();
        }
    }

    private static void Set(Projectile projectile, string field, object value)
    {
        AccessTools.Field(typeof(Projectile), field).SetValue(projectile, value);
    }

    private static bool Skip() => false;
    private static bool ValidTarget(ref bool __result) { __result = true; return false; }
    private static bool Identity(ref ZDOID __result) { __result = default(ZDOID); return false; }
    private static bool Award(Skills.SkillType type, float amount) { _awards++; _skill = type; _amount = amount; return false; }
    private static void Check(bool value, string message) { if (!value) { throw new InvalidOperationException(message); } }
}

