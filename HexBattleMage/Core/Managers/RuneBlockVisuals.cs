using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    internal sealed class RuneBlockVisuals : MonoBehaviour
    {
        private float _nextRefresh;

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            var profile = GetComponent<HexBattleMageRuneBlockProfile>();

            _nextRefresh = Time.unscaledTime + profile.RefreshInterval;
            ApplyLevel(profile, RuneBlockProgression.GetLevel(profile));
        }

        internal void ApplyLevel(HexBattleMageRuneBlockProfile profile, int level)
        {
            for (int index = 0; index < profile.StageVisualPaths.Length; index++)
            {
                var stage = transform.Find(profile.StageVisualPaths[index]);

                if (stage != null)
                {
                    stage.gameObject.SetActive(level > profile.BaseLevel + index);
                }
            }
        }
    }
}
