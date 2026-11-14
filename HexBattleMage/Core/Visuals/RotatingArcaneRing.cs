using UnityEngine;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class RotatingArcaneRing : MonoBehaviour
    {
        private const float RotationSpeed = 12f;

        private void Update()
        {
            transform.Rotate(Vector3.up, RotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
