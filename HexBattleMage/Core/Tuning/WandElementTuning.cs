using HexBattleMage.Core.Elements;

namespace HexBattleMage.Core.Tuning
{
    internal static class WandElementTuning
    {
        internal static void Apply(WandElementProfile element)
        {
            switch (element.Element)
            {
                case WandElement.Fire:
                    Fire(element);
                    break;
                case WandElement.Frost:
                    Frost(element);
                    break;
                case WandElement.Spirit:
                    Spirit(element);
                    break;
                case WandElement.Lightning:
                    Lightning(element);
                    break;
                case WandElement.Poison:
                    Poison(element);
                    break;
            }
        }

        private static void Fire(WandElementProfile element)
        {
            element.ProjectileCoreColor = new UnityEngine.Color(1f, 0.01f, 0.005f);
            element.ProjectileHaloColor = new UnityEngine.Color(0.7f, 0.025f, 0.005f);
            element.ImpactColor = new UnityEngine.Color(1f, 0.06f, 0.01f);
            element.DirectDamage = 15f;
            element.DamagePerQuality = 2f;
            element.TickDamage = 6f;
            element.Duration = 5f;
            element.TickInterval = 1f;
            element.MovementSlow = 0f;
            element.AttackSlow = 0f;
            element.Spreads = false;
            element.StackDamage = false;
            element.Stuns = false;
        }

        private static void Frost(WandElementProfile element)
        {
            element.DirectDamage = 20f;
            element.DamagePerQuality = 6f;
            element.TickDamage = 0f;
            element.Duration = 4f;
            element.MovementSlow = 0.3f;
            element.AttackSlow = 0.3f;
            element.Spreads = false;
            element.StackDamage = false;
            element.Stuns = false;
        }

        private static void Spirit(WandElementProfile element)
        {
            element.DirectDamage = 10f;
            element.DamagePerQuality = 6f;
            element.TickDamage = 8f;
            element.Duration = 8f;
            element.TickInterval = 1f;
            element.Spreads = true;
            element.SpreadRadius = 8f;
            element.SpreadInterval = 0.5f;
            element.MaximumStacks = 1;
            element.StackDamage = false;
            element.Stuns = false;
            element.BossTickMultiplier = 1f;
        }

        private static void Lightning(WandElementProfile element)
        {
            element.DirectDamage = 25f;
            element.DamagePerQuality = 6f;
            element.TickDamage = 0f;
            element.Duration = 1f;
            element.Spreads = false;
            element.StackDamage = false;
            element.Stuns = true;
        }

        private static void Poison(WandElementProfile element)
        {
            element.DirectDamage = 5f;
            element.DamagePerQuality = 6f;
            element.TickDamage = 3f;
            element.Duration = 10f;
            element.TickInterval = 1f;
            element.Spreads = true;
            element.SpreadRadius = 8f;
            element.SpreadInterval = 0.5f;
            element.MaximumStacks = 4;
            element.StackDamage = true;
            element.Stuns = false;
            element.BossTickMultiplier = 0.25f;
        }
    }
}
