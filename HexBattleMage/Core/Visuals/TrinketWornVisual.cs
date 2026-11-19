using HexBattleMage.Core.Trinkets;
using UnityEngine;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class TrinketWornVisual : MonoBehaviour
    {
        private void LateUpdate()
        {
            var owner = GetComponentInParent<Humanoid>(true);

            if (owner == null)
            {
                return;
            }

            var profile = GetComponent<HexBattleMageTrinketProfile>();

            if (profile == null)
            {
                return;
            }

            var attachment = transform.parent;
            var offset = profile.WornPosition;

            transform.position = attachment.position + owner.transform.right * offset.x + owner.transform.up * offset.y + owner.transform.forward * offset.z;
            transform.rotation = owner.transform.rotation * Quaternion.Euler(profile.WornRotation);
        }
    }
}
