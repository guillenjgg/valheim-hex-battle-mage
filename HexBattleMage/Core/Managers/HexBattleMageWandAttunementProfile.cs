using HexBattleMage.Core.Elements;
using UnityEngine;

namespace HexBattleMage.Core.Managers
{
    public sealed class HexBattleMageWandAttunementProfile : MonoBehaviour
    {
        public WandElement Element = WandElement.Fire;
        public string DisplayName = "$hexbattlemage_attunement_fire";
        public string Description = "$hexbattlemage_attunement_fire_description";
    }
}
