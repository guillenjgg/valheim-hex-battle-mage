using HexBattleMage.Core.Tuning;
using UnityEngine;

namespace HexBattleMage.Core.Elements
{
    internal sealed class WandElementAnimation : MonoBehaviour
    {
        private const string SpeedKey = "HexBattleMage.ElementAnimationSpeed";
        private float _baseSpeed = 1f;
        private float _lastApplied = float.NaN;

        internal void RecordBaseSpeed(float speed)
        {
            _baseSpeed = speed;
            _lastApplied = speed;
        }

        private void LateUpdate()
        {
            var character = GetComponent<Character>();
            var view = GetComponent<ZNetView>();
            float multiplier = 1f;

            if (view != null && view.IsValid())
            {
                var data = view.GetZDO();

                if (view.IsOwner())
                {
                    multiplier = GetMultiplier(character);

                    if (!Mathf.Approximately(data.GetFloat(SpeedKey, 1f), multiplier))
                    {
                        data.Set(SpeedKey, multiplier);
                    }
                }
                else
                {
                    multiplier = data.GetFloat(SpeedKey, 1f);
                }
            }
            else
            {
                multiplier = GetMultiplier(character);
            }

            var animation = character.GetZAnim();

            if (animation == null)
            {
                return;
            }

            var animator = animation.GetComponentInChildren<Animator>();

            if (animator == null)
            {
                return;
            }

            if (!Mathf.Approximately(animator.speed, _lastApplied))
            {
                _baseSpeed = animator.speed;
            }

            _lastApplied = _baseSpeed * multiplier;
            animator.speed = _lastApplied;
        }

        internal static float GetMultiplier(Character character)
        {
            if (WandElementManager.Enabled == null || !WandElementManager.Enabled.EffectiveValue())
            {
                return 1f;
            }

            var effects = character.GetSEMan();

            float multiplier = 1f;

            foreach (WandElement element in System.Enum.GetValues(typeof(WandElement)))
            {
                if (effects.HaveStatusEffect(WandElementManager.GetHash(element)))
                {
                    if (WandElementManager.GetSettings(element).Profile.Stuns)
                    {
                        return 0f;
                    }

                    multiplier = Mathf.Min(multiplier, 1f - WandElementManager.GetSettings(element).AttackSlow.EffectiveValue());
                }
            }

            return multiplier;
        }
    }
}
