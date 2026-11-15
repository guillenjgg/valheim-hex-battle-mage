using UnityEngine;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class WandProjectileImpact : MonoBehaviour
    {
        private const string ImpactRpcName = "HexBattleMage_WandImpact";
        private Projectile _projectile;
        private ZNetView _view;
        private bool _registered;

        private void Awake()
        {
            _projectile = GetComponent<Projectile>();
            _view = GetComponent<ZNetView>();

            if (_projectile != null)
            {
                _projectile.m_onHit += OnImpact;
            }

            RegisterRpc();
        }

        private void Start()
        {
            RegisterRpc();
        }

        private void RegisterRpc()
        {
            if (_registered)
            {
                return;
            }

            if (_view == null)
            {
                return;
            }

            if (!_view.IsValid())
            {
                return;
            }

            _view.Register<Vector3, float, int>(ImpactRpcName, ReceiveImpact);
            _registered = true;
        }

        private void OnImpact(Collider collider, Vector3 hitPoint, bool water)
        {
            if (_projectile == null)
            {
                return;
            }

            var velocity = _projectile.GetVelocity();

            if (velocity.sqrMagnitude > 0f)
            {
                const float surfaceProbeDistance = 2f;
                const float surfaceOffset = 0.06f;
                var direction = velocity.normalized;
                var ray = new Ray(hitPoint - direction * surfaceProbeDistance, direction);

                if (collider != null)
                {
                    if (collider.Raycast(ray, out RaycastHit surface, surfaceProbeDistance * 2f))
                    {
                        hitPoint = surface.point + surface.normal * surfaceOffset;
                    }
                }
            }

            var elements = GetElements(_projectile.m_damage);

            RegisterRpc();

            if (_registered)
            {
                if (!_view.IsOwner())
                {
                    return;
                }

                _view.InvokeRPC(ZNetView.Everybody, ImpactRpcName, new object[] { hitPoint, _projectile.m_aoe, (int)elements });
                return;
            }

            WandImpactVisual.Show(hitPoint, _projectile.m_aoe, elements);
        }

        private void ReceiveImpact(long sender, Vector3 hitPoint, float radius, int elements)
        {
            if (!_view.IsValid())
            {
                return;
            }

            if (sender != _view.GetZDO().GetOwner())
            {
                return;
            }

            WandImpactVisual.Show(hitPoint, radius, (WandImpactElement)elements);
        }

        internal static WandImpactElement GetElements(HitData.DamageTypes damage)
        {
            var elements = WandImpactElement.None;

            if (damage.m_fire > 0f)
            {
                elements |= WandImpactElement.Fire;
            }

            if (damage.m_frost > 0f)
            {
                elements |= WandImpactElement.Frost;
            }

            if (damage.m_spirit > 0f)
            {
                elements |= WandImpactElement.Spirit;
            }

            if (damage.m_lightning > 0f)
            {
                elements |= WandImpactElement.Lightning;
            }

            if (damage.m_poison > 0f)
            {
                elements |= WandImpactElement.Poison;
            }

            return elements;
        }

        private void OnDestroy()
        {
            if (_projectile != null)
            {
                _projectile.m_onHit -= OnImpact;
            }
        }
    }
}
