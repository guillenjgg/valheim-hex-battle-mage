using UnityEngine;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class RotatingArcaneRing : MonoBehaviour
    {

        private void Update()
        {
            var profile = GetComponentInParent<MageTableProfile>(true);

            transform.Rotate(Vector3.up, profile.RingRotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
