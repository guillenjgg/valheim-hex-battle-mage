using UnityEngine;
using UnityEngine.Rendering;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class MagicShieldVisual : MonoBehaviour
    {
        private const float OpenDuration = 0.14f;
        private const string ForearmBoneName = "LeftForeArm";
        private const string WristBoneName = "LeftHand";
        private const int FemaleModelIndex = 1;
        private const float BracerForearmPosition = 0.62f;
        private const float ReferenceForearmLength = 0.2600273f;
        private const float MinimumForearmLength = 0.001f;
        private const float MaleBracerRadialScale = 1.2f;
        private const float FemaleBracerRadialScale = 1.05f;
        private const float FieldScale = 1.5f;
        private const float FieldForwardOffset = 0.22f;
        private const float RuneDegreesPerSecond = 8f;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static Material _fieldMaterial;
        private Transform _bracer;
        private Transform _field;
        private Transform _runes;
        private Renderer[] _fieldRenderers;
        private MaterialPropertyBlock _properties;
        private float _openAmount;
        private float _runeAngle;
        private bool _canBlock = true;

        internal void SetCanBlock(bool canBlock)
        {
            _canBlock = canBlock;

            if (!canBlock && _field != null)
            {
                HideField();
            }
        }

        private void Awake()
        {
            _bracer = transform.Find("BracerModel");
            _field = transform.Find("MagicField");

            if (_field == null)
            {
                enabled = false;
                return;
            }

            _field.gameObject.SetActive(false);

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                enabled = false;
                return;
            }

            if (_fieldMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");

                if (shader == null)
                {
                    DebugLog.LogWarning("Magic shield field shader is unavailable.");
                    enabled = false;
                    return;
                }

                _fieldMaterial = new Material(shader) { name = "HexMagicShieldField", mainTexture = Texture2D.whiteTexture };
            }

            _runes = _field.Find("RuneRing");
            _fieldRenderers = _field.GetComponentsInChildren<Renderer>(true);
            _properties = new MaterialPropertyBlock();

            foreach (var renderer in _fieldRenderers)
            {
                renderer.sharedMaterial = _fieldMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void LateUpdate()
        {
            var humanoid = GetComponentInParent<Humanoid>();

            if (humanoid == null)
            {
                HideField();
                return;
            }

            var equipment = humanoid.GetComponent<VisEquipment>();

            if (equipment == null)
            {
                HideField();
                return;
            }

            var handAttachment = equipment.m_leftHand;

            if (handAttachment == null)
            {
                HideField();
                return;
            }

            if (transform.parent != handAttachment)
            {
                HideField();
                return;
            }

            var wrist = FindAncestor(handAttachment, WristBoneName);

            if (wrist == null)
            {
                HideField();
                return;
            }

            var forearm = FindAncestor(wrist, ForearmBoneName);

            if (forearm == null)
            {
                HideField();
                return;
            }

            if (_bracer != null)
            {
                FitBracer(equipment, forearm, wrist);
            }

            if (humanoid.IsDead())
            {
                HideField();
                return;
            }

            if (!_canBlock)
            {
                HideField();
                return;
            }

            if (!humanoid.IsBlocking())
            {
                HideField();
                return;
            }

            _openAmount = Mathf.MoveTowards(_openAmount, 1f, Time.deltaTime / OpenDuration);
            _runeAngle = Mathf.Repeat(_runeAngle + RuneDegreesPerSecond * Time.deltaTime, 360f);

            float opening = Mathf.SmoothStep(0f, 1f, _openAmount);
            float pulse = 1f + Mathf.Sin(Time.time * 2.5f) * 0.012f;

            _field.position = handAttachment.position + humanoid.transform.forward * FieldForwardOffset;
            _field.rotation = Quaternion.LookRotation(humanoid.transform.forward, humanoid.transform.up);
            _field.localScale = Vector3.one * FieldScale * Mathf.Lerp(0.08f, pulse, opening);
            _field.gameObject.SetActive(true);
            _runes.localRotation = Quaternion.Euler(0f, 0f, _runeAngle);
            _properties.SetColor(ColorProperty, new Color(1f, 1f, 1f, opening));

            foreach (var renderer in _fieldRenderers)
            {
                renderer.SetPropertyBlock(_properties);
            }
        }

        private void FitBracer(VisEquipment equipment, Transform forearm, Transform wrist)
        {
            var forearmDirection = wrist.position - forearm.position;
            float forearmLength = forearmDirection.magnitude;

            if (forearmLength < MinimumForearmLength)
            {
                return;
            }

            float bodyScale = forearmLength / ReferenceForearmLength;
            float radialScale = bodyScale * GetBracerRadialScale(equipment);
            var parentScale = _bracer.parent.lossyScale;

            _bracer.position = Vector3.Lerp(forearm.position, wrist.position, BracerForearmPosition);
            _bracer.rotation = Quaternion.LookRotation(forearm.forward, -forearmDirection);
            _bracer.localScale = new Vector3(radialScale / Mathf.Abs(parentScale.x), bodyScale / Mathf.Abs(parentScale.y), radialScale / Mathf.Abs(parentScale.z));
        }

        private static float GetBracerRadialScale(VisEquipment equipment)
        {
            if (equipment.m_bodyModel == null)
            {
                return MaleBracerRadialScale;
            }

            if (equipment.m_models == null)
            {
                return MaleBracerRadialScale;
            }

            if (equipment.m_models.Length <= FemaleModelIndex)
            {
                return MaleBracerRadialScale;
            }

            if (equipment.m_models[FemaleModelIndex] == null)
            {
                return MaleBracerRadialScale;
            }

            var femaleMesh = equipment.m_models[FemaleModelIndex].m_mesh;

            if (femaleMesh == null)
            {
                return MaleBracerRadialScale;
            }

            return equipment.m_bodyModel.sharedMesh == femaleMesh ? FemaleBracerRadialScale : MaleBracerRadialScale;
        }

        private static Transform FindAncestor(Transform child, string boneName)
        {
            var ancestor = child.parent;

            while (ancestor != null)
            {
                if (ancestor.name == boneName)
                {
                    return ancestor;
                }

                ancestor = ancestor.parent;
            }

            return null;
        }

        private void HideField()
        {
            _openAmount = 0f;
            _runeAngle = 0f;
            _field.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (_field != null)
            {
                HideField();
            }
        }
    }
}
