using System;

namespace HexBattleMage.Core.Elements
{
    public sealed class WandElementGroupStatus : StatusEffect
    {
        public int ElementMask;

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            foreach (WandElement element in Enum.GetValues(typeof(WandElement)))
            {
                if ((ElementMask & (1 << (int)element)) != 0)
                {
                    m_character.GetSEMan().AddStatusEffect(WandElementManager.GetHash(element), true, itemLevel, skillLevel);
                }
            }
        }

        public override void SetAttacker(Character attacker)
        {
            foreach (WandElement element in Enum.GetValues(typeof(WandElement)))
            {
                if ((ElementMask & (1 << (int)element)) == 0)
                {
                    continue;
                }

                var effect = m_character.GetSEMan().GetStatusEffect(WandElementManager.GetHash(element));

                if (effect != null)
                {
                    effect.SetAttacker(attacker);
                }
            }
        }

        public override bool IsDone()
        {
            return true;
        }
    }
}
