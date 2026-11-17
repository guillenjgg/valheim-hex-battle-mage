using UnityEngine;
using HexBattleMage.Core.Managers;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class FloatingArcaneCrystal : MonoBehaviour
    {
        private Vector3 _origin;

        private void Start()
        {
            _origin = transform.localPosition;
        }

        private void Update()
        {
            var profile = GetComponentInParent<MageTableProfile>(true);

            float bob = Mathf.Sin(Time.time * profile.CrystalBobSpeed + _origin.x) * profile.CrystalBobHeight;

            transform.localPosition = _origin + Vector3.up * bob;
            transform.localRotation = Quaternion.Euler(0f, Time.time * profile.CrystalRotationSpeed, 0f);
        }
    }
}
