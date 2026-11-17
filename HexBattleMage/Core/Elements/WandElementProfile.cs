using UnityEngine;

namespace HexBattleMage.Core.Elements
{
    public enum WandElement
    {
        Fire,
        Frost,
        Spirit,
        Lightning,
        Poison
    }

    public sealed class WandElementProfile : MonoBehaviour
    {
        public WandElement Element;
        public float DirectDamage;
        public float DamagePerQuality = 6f;
        public float TickDamage;
        public float Duration;
        public float TickInterval = 1f;
        public float MovementSlow;
        public float AttackSlow;
        public float SpreadRadius;
        public float SpreadInterval = 0.5f;
        public int MaximumStacks = 1;
        public float BossTickMultiplier = 1f;
        public Color TipColor = Color.white;
        public float TipEmissionStrength = 4.5f;
        public Color ImpactColor = Color.white;
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
