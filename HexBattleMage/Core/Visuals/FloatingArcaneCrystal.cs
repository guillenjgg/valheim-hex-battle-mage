using UnityEngine;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class FloatingArcaneCrystal : MonoBehaviour
    {
        private const float BobHeight = 0.035f;
        private const float BobSpeed = 1.8f;
        private const float RotationSpeed = 25f;
        private Vector3 _origin;

        private void Start()
        {
            _origin = transform.localPosition;
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * BobSpeed + _origin.x) * BobHeight;

            transform.localPosition = _origin + Vector3.up * bob;
            transform.localRotation = Quaternion.Euler(0f, Time.time * RotationSpeed, 0f);
        }
    }
}
