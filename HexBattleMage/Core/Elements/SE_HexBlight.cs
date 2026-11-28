using System.Collections.Generic;
using HexBattleMage.Core.Tuning;
using UnityEngine;
using UnityEngine.Rendering;

namespace HexBattleMage.Core.Elements
{
    public sealed class SE_HexBlight : StatusEffect
    {
        private const float TickTolerance = 0.0001f;
        private static Material _auraMaterial;
        private GameObject _aura;
        private Character _attacker;
        private float _tickTimer;
        private float _damagePerSecond;
        private float _interval;
        private bool _startChain;

        public override void Setup(Character character)
        {
            base.Setup(character);

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return;
            }

            if (_auraMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");

                if (shader == null)
                {
                    return;
                }

                _auraMaterial = new Material(shader);
            }

            _aura = new GameObject("HexBattleMage_BlightMotes");
            _aura.transform.SetParent(character.transform, false);
            _aura.transform.position = character.GetCenterPoint();
            var profile = WandElementManager.GetSettings(WandElement.Blight).Profile;
            var particles = _aura.AddComponent<ParticleSystem>();

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;

            main.startLifetime = profile.AuraLifetime;
            main.startSpeed = profile.AuraSpeed;
            main.startSize = profile.AuraSize;
            main.startColor = WandElementManager.GetSettings(WandElement.Blight).Profile.ImpactColor;
            main.maxParticles = Mathf.Max(1, Mathf.CeilToInt(profile.AuraRate * profile.AuraLifetime * 2f));
            var emission = particles.emission;

            emission.rateOverTime = profile.AuraRate;
            var shape = particles.shape;

            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = profile.AuraRadius;

            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = _auraMaterial;
            particles.Play();
        }

        public override void Stop()
        {
            if (_aura != null)
            {
                Object.Destroy(_aura);
            }

            base.Stop();
        }

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            var settings = WandElementManager.GetSettings(WandElement.Blight);

            m_ttl = Mathf.Max(0.1f, settings.Duration.EffectiveValue());
            _interval = Mathf.Max(0.1f, settings.TickInterval.EffectiveValue());
            _damagePerSecond = Mathf.Max(0f, settings.TotalDamage.EffectiveValue()) / m_ttl;
            _tickTimer = 0f;
            _startChain = itemLevel >= 0;
        }

        public override void SetAttacker(Character attacker)
        {
            base.SetAttacker(attacker);
            _attacker = attacker;

            if (!_startChain)
            {
                return;
            }

            _startChain = false;

            if (attacker == null || !m_character.IsOwner())
            {
                return;
            }

            SpreadChain(m_character, attacker);
        }

        public override void UpdateStatusEffect(float dt)
        {
            float activeTime = Mathf.Clamp(m_ttl - m_time, 0f, dt);

            base.UpdateStatusEffect(dt);

            if (!m_character.IsOwner())
            {
                return;
            }

            if (m_character.IsDead())
            {
                return;
            }

            if (!WandElementManager.Enabled.EffectiveValue())
            {
                return;
            }

            _tickTimer += activeTime;

            while (_tickTimer + TickTolerance >= _interval && !m_character.IsDead())
            {
                _tickTimer -= _interval;
                Tick(_interval);
            }

            if (m_time >= m_ttl && _tickTimer > TickTolerance && !m_character.IsDead())
            {
                Tick(_tickTimer);
                _tickTimer = 0f;
            }
        }

        public override bool IsDone()
        {
            return m_time >= m_ttl || m_character.IsDead() || !WandElementManager.Enabled.EffectiveValue();
        }

        private void Tick(float seconds)
        {
            float multiplier = m_character.IsBoss() ? WandElementManager.GetSettings(WandElement.Blight).BossTickMultiplier.EffectiveValue() : 1f;
            var hit = new HitData { m_point = m_character.GetCenterPoint(), m_blockable = false, m_dodgeable = false };

            hit.m_damage.m_damage = _damagePerSecond * seconds * Mathf.Max(0f, multiplier);

            if (_attacker != null)
            {
                hit.SetAttacker(_attacker);
            }

            // Untyped damage has no Spirit/Poison resistance channel; only the owning peer ticks it.
            m_character.ApplyDamage(hit, true, false, HitData.DamageModifier.Normal);
        }

        internal static void SpreadChain(Character first, Character attacker)
        {
            var settings = WandElementManager.GetSettings(WandElement.Blight);
            var profile = settings.Profile;
            float radius = settings.SpreadRadius.EffectiveValue();

            if (!profile.Spreads || radius <= 0f)
            {
                return;
            }

            var visited = new HashSet<Character> { first };
            var pending = new Queue<Character>();
            var neighbours = new List<Character>();
            int maximumTargets = Mathf.Max(1, profile.MaximumChainTargets);
            int worldMask = LayerMask.GetMask("terrain", "piece", "static_solid", "Default_small", "Default");

            pending.Enqueue(first);

            while (pending.Count > 0 && visited.Count < maximumTargets)
            {
                var source = pending.Dequeue();

                neighbours.Clear();
                Character.GetCharactersInRange(source.transform.position, radius, neighbours);

                foreach (var candidate in neighbours)
                {
                    if (candidate == null || visited.Contains(candidate))
                    {
                        continue;
                    }

                    if (candidate.IsDead() || candidate.IsPlayer() || candidate.IsTamed())
                    {
                        continue;
                    }

                    if (!BaseAI.IsEnemy(attacker, candidate))
                    {
                        continue;
                    }

                    if (profile.RequireSpreadLineOfSight && Physics.Linecast(source.GetCenterPoint(), candidate.GetCenterPoint(), worldMask, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }

                    visited.Add(candidate);
                    pending.Enqueue(candidate);
                    var infection = new HitData { m_statusEffectHash = WandElementManager.GetHash(WandElement.Blight), m_itemLevel = -1, m_point = candidate.GetCenterPoint(), m_blockable = false, m_dodgeable = false };

                    infection.SetAttacker(attacker);
                    candidate.Damage(infection);

                    if (visited.Count >= maximumTargets)
                    {
                        break;
                    }
                }
            }
        }
    }
}
