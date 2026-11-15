using HexBattleMage.Core.Managers;
using UnityEngine;
using UnityEngine.Rendering;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class MagicBracerVisualController : MonoBehaviour
    {
        private const string WornKey = "HexBattleMage.BracerWorn";
        private const string BlockingKey = "HexBattleMage.BracerUsable";
        private GameObject _instance;
        private bool _wasUsable;

        private void LateUpdate()
        {
            var humanoid = GetComponent<Humanoid>();
            var networkView = GetComponent<ZNetView>();
            bool worn;
            bool usable;

            if (networkView != null && networkView.IsValid())
            {
                var data = networkView.GetZDO();

                if (networkView.IsOwner())
                {
                    worn = HasWornBracer(humanoid);
                    usable = worn && MagicShieldManager.GetBlocker(humanoid) != null;

                    if (data.GetBool(WornKey, false) != worn)
                    {
                        data.Set(WornKey, worn);
                    }

                    if (data.GetBool(BlockingKey, false) != usable)
                    {
                        data.Set(BlockingKey, usable);
                    }
                }
                else
                {
                    worn = data.GetBool(WornKey, false);
                    usable = data.GetBool(BlockingKey, false);
                }
            }
            else
            {
                worn = HasWornBracer(humanoid);
                usable = worn && MagicShieldManager.GetBlocker(humanoid) != null;
            }

            if (_wasUsable != usable)
            {
                if (networkView == null || networkView.IsOwner())
                {
                    MagicShieldManager.RefreshAnimationState(humanoid);
                }
            }

            _wasUsable = usable;

            if (!worn)
            {
                if (_instance != null)
                {
                    _instance.SetActive(false);
                }

                return;
            }

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return;
            }

            var equipment = humanoid.GetComponent<VisEquipment>();

            if (equipment == null)
            {
                return;
            }

            if (equipment.m_leftHand == null)
            {
                return;
            }

            if (_instance == null)
            {
                _instance = MagicShieldManager.CreateWornVisual(equipment.m_leftHand);
            }

            _instance.transform.SetParent(equipment.m_leftHand, false);
            _instance.GetComponent<MagicShieldVisual>().SetCanBlock(usable);
            _instance.SetActive(true);
        }

        internal static bool HasWornBracer(Humanoid humanoid)
        {
            if (humanoid.IsDead())
            {
                return false;
            }

            return MagicShieldManager.GetEquippedBracer(humanoid) != null;
        }

        private void OnDestroy()
        {
            if (_instance != null)
            {
                Destroy(_instance);
            }
        }
    }
}
