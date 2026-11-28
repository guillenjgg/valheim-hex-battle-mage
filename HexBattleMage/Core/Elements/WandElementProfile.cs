using UnityEngine;

namespace HexBattleMage.Core.Elements
{
    public enum WandElement
    {
        Fire,
        Frost,
        Spirit,
        Lightning,
        Poison,
        Blight
    }

    public sealed class WandElementProfile : MonoBehaviour
    {
        public WandElement Element;
        public float DirectDamage;
        public float DamagePerQuality = 6f;
        public float TickDamage;
        [Tooltip("Blight damage budget for a completed application; independent of duration and tick interval.")]
        public float TotalDamage = 60f;
        [Tooltip("Blight-only shots do not inherit the wand impact explosion. Mixed Fire/Blight keeps Fire area damage.")]
        public bool SingleTargetProjectile;
        public float Duration;
        public float TickInterval = 1f;
        public float MovementSlow;
        public float AttackSlow;
        public float SpreadRadius;
        public float SpreadInterval = 0.5f;
        public int MaximumStacks = 1;
        public float BossTickMultiplier = 1f;
        [Header("Projectile flight colours")]
        [HideInInspector] public int ProjectileVisualVersion;
        public Color ProjectileCoreColor = Color.white;
        public Color ProjectileHaloColor = Color.white;
        public Color TipColor = Color.white;
        public float TipEmissionStrength = 4.5f;
        public Color ImpactColor = Color.white;
        [Header("Blight contagion; resolved once per direct hit")]
        public bool RequireSpreadLineOfSight = true;
        [Header("Blight aura")]
        public float AuraLifetime = 0.8f;
        public float AuraSpeed = 0.15f;
        public float AuraSize = 0.035f;
        public float AuraRate = 12f;
        public float AuraRadius = 0.35f;
        [Min(1)] public int MaximumChainTargets = 128;
        public bool Spreads;
        public bool StackDamage;
        public bool Stuns;
        public bool SpreadToPlayers;
        public bool SpreadToTamed;
        public bool RequireHostileTarget = true;
        public bool ResetSpreadOnDirectHit = true;
        public string StatusDisplayName;
        public string NativeVisualStatus;
        [HideInInspector] public int AuthoredVersion;
    }
}
