using HexBattleMage.Core.Tuning;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace HexBattleMage.Core.Elements
{
    public sealed class WandElementStatus : StatusEffect
    {
        private const float TickToleranceSeconds = 0.0001f;
        private static readonly AccessTools.FieldRef<Humanoid, Attack> CurrentAttack = AccessTools.FieldRefAccess<Humanoid, Attack>("m_currentAttack");
        public WandElement Element;
        private HashSet<Character> _spreadTargets;
        private Character _attacker;
        private float _tickTimer;
        private float _spreadTimer;
        private int _stacks;

        internal int Stacks => _stacks;

        public override bool CanAdd(Character character)
        {
            var modifier = character.GetDamageModifiers(null).GetModifier(WandElementManager.GetDamageType(Element));

            if (modifier == HitData.DamageModifier.Immune)
            {
                return false;
            }

            return WandElementManager.Enabled.EffectiveValue() && base.CanAdd(character);
        }

        public override void Setup(Character character)
        {
            var profile = WandElementManager.GetSettings(Element).Profile;

            if (!string.IsNullOrEmpty(profile.NativeVisualStatus) && ObjectDB.instance != null)
            {
                var burning = ObjectDB.instance.m_StatusEffects.Find(effect => effect.name == profile.NativeVisualStatus);

                if (burning != null)
                {
                    m_startEffects = burning.m_startEffects;
                    m_stopEffects = burning.m_stopEffects;
                    m_effectsOnlyOnPlayer = burning.m_effectsOnlyOnPlayer;
                }
            }

            base.Setup(character);
            _spreadTargets = new HashSet<Character>();
            _stacks = 0;
            _tickTimer = 0f;
            _spreadTimer = 0f;
        }

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            var settings = WandElementManager.GetSettings(Element);

            m_ttl = settings.Duration.EffectiveValue();

            if (m_character == null)
            {
                return;
            }

            _stacks = settings.Profile.StackDamage ? Mathf.Min(_stacks + 1, settings.MaximumStacks.EffectiveValue()) : 1;

            if (itemLevel > 0 && settings.Profile.ResetSpreadOnDirectHit)
            {
                _spreadTargets.Clear();
            }

            if (settings.Profile.Stuns && m_character is Humanoid humanoid)
            {
                var attack = CurrentAttack(humanoid);

                if (attack != null)
                {
                    attack.Stop();
                }
            }
        }

        public override void SetAttacker(Character attacker)
        {
            base.SetAttacker(attacker);
            _attacker = attacker;
        }

        public override void UpdateStatusEffect(float dt)
        {
            var settings = WandElementManager.GetSettings(Element);

            m_ttl = settings.Duration.EffectiveValue();
            _stacks = Mathf.Min(_stacks, settings.Profile.StackDamage ? settings.MaximumStacks.EffectiveValue() : 1);

            float activeTime = Mathf.Clamp(m_ttl - m_time, 0f, dt);

            base.UpdateStatusEffect(dt);

            if (!WandElementManager.Enabled.EffectiveValue())
            {
                return;
            }

            if (m_character.IsDead())
            {
                return;
            }

            if (WandElementManager.GetSettings(Element).Profile.Stuns)
            {
                m_character.StopMovement();
            }

            _tickTimer += activeTime;

            while (_tickTimer + TickToleranceSeconds >= settings.TickInterval.EffectiveValue())
            {
                _tickTimer -= settings.TickInterval.EffectiveValue();
                ApplyTick(settings);

                if (m_character.IsDead())
                {
                    return;
                }
            }

            _spreadTimer += activeTime;

            if (_spreadTimer >= settings.SpreadInterval.EffectiveValue())
            {
                _spreadTimer = 0f;
                Spread(settings);
            }
        }

        public override bool IsDone()
        {
            if (!WandElementManager.Enabled.EffectiveValue())
            {
                return true;
            }

            if (m_character.IsDead())
            {
                return true;
            }

            return m_time >= m_ttl || base.IsDone();
        }

        public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 direction)
        {
            if (!WandElementManager.Enabled.EffectiveValue())
            {
                return;
            }

            if (WandElementManager.GetSettings(Element).Profile.Stuns)
            {
                speed = 0f;
                return;
            }

            speed *= 1f - WandElementManager.GetSettings(Element).MovementSlow.EffectiveValue();
        }

        private void ApplyTick(WandElementSettings settings)
        {
            float damage = settings.TickDamage.EffectiveValue() * _stacks;

            if (m_character.IsBoss())
            {
                damage *= settings.BossTickMultiplier.EffectiveValue();
            }

            if (damage <= 0f)
            {
                return;
            }

            var hit = new HitData { m_point = m_character.GetCenterPoint(), m_blockable = false, m_dodgeable = false };

            WandElementManager.SetDamage(ref hit.m_damage, Element, damage);

            if (_attacker != null)
            {
                hit.SetAttacker(_attacker);
            }

            hit.ApplyResistance(m_character.GetDamageModifiers(null), out var modifier);
            m_character.ApplyDamage(hit, true, false, modifier);
        }

        private void Spread(WandElementSettings settings)
        {
            if (!settings.Profile.Spreads)
            {
                return;
            }

            if (settings.SpreadRadius.EffectiveValue() <= 0f)
            {
                return;
            }

            if (_attacker == null)
            {
                return;
            }

            float radiusSquared = settings.SpreadRadius.EffectiveValue() * settings.SpreadRadius.EffectiveValue();

            foreach (var candidate in Character.GetAllCharacters().ToArray())
            {
                if (candidate == null)
                {
                    continue;
                }

                if (candidate == m_character)
                {
                    continue;
                }

                if (candidate.IsDead())
                {
                    continue;
                }

                if (candidate.IsPlayer() && !settings.Profile.SpreadToPlayers)
                {
                    continue;
                }

                if (candidate.IsTamed() && !settings.Profile.SpreadToTamed)
                {
                    continue;
                }

                if (settings.Profile.RequireHostileTarget && !BaseAI.IsEnemy(_attacker, candidate))
                {
                    continue;
                }

                if ((candidate.transform.position - m_character.transform.position).sqrMagnitude > radiusSquared)
                {
                    continue;
                }

                if (!_spreadTargets.Add(candidate))
                {
                    continue;
                }

                var infection = new HitData
                {
                    m_statusEffectHash = NameHash(),
                    m_itemLevel = 0,
                    m_point = candidate.GetCenterPoint(),
                    m_blockable = false,
                    m_dodgeable = false
                };

                infection.SetAttacker(_attacker);
                candidate.Damage(infection);
            }
        }
    }
}
