using UnityEngine;
using HexBattleMage.Core.Managers;
using UnityEngine.Rendering;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class MagicShieldVisual : MonoBehaviour
    {
        private const float MinimumForearmLength = 0.001f;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionProperty = Shader.PropertyToID("_EmissionColor");
        private static Material _fieldMaterial;
        private MagicBracerProfile _profile;
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
            _profile = PrefabSettings.Require<MagicBracerProfile>(gameObject);
            _bracer = transform.Find("BracerModel");
            _field = transform.Find("MagicField");

            var rune = transform.Find("BracerModel/BracerRune");

            if (rune != null)
            {
                var runeRenderer = rune.GetComponent<Renderer>();
                var runeProperties = new MaterialPropertyBlock();
                var runeEmission = runeRenderer.sharedMaterial.GetColor(EmissionProperty);

                runeProperties.SetColor(EmissionProperty, runeEmission * _profile.RuneBrightness);
                runeRenderer.SetPropertyBlock(runeProperties);
            }

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

            var wrist = FindAncestor(handAttachment, _profile.WristBoneName);

            if (wrist == null)
            {
                HideField();
                return;
            }

            var forearm = FindAncestor(wrist, _profile.ForearmBoneName);

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

            _openAmount = Mathf.MoveTowards(_openAmount, 1f, Time.deltaTime / _profile.OpenDuration);
            _runeAngle = Mathf.Repeat(_runeAngle + _profile.RuneDegreesPerSecond * Time.deltaTime, 360f);

            float opening = Mathf.SmoothStep(0f, 1f, _openAmount);
            float pulse = 1f + Mathf.Sin(Time.time * _profile.PulseFrequency) * _profile.PulseAmplitude;

            _field.position = handAttachment.position + humanoid.transform.forward * _profile.FieldForwardOffset;
            _field.rotation = Quaternion.LookRotation(humanoid.transform.forward, humanoid.transform.up);
            _field.localScale = Vector3.one * _profile.FieldScale * Mathf.Lerp(_profile.InitialFieldScale, pulse, opening);
            _field.gameObject.SetActive(true);
            _runes.localRotation = Quaternion.Euler(0f, 0f, _runeAngle);
            var fieldColor = _profile.FieldTint;

            fieldColor.a *= opening;
            _properties.SetColor(ColorProperty, fieldColor);

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

            float bodyScale = forearmLength / _profile.ReferenceForearmLength;
            float radialScale = bodyScale * GetBracerRadialScale(equipment);
            var parentScale = _bracer.parent.lossyScale;

            _bracer.position = Vector3.Lerp(forearm.position, wrist.position, _profile.BracerForearmPosition);
            _bracer.rotation = Quaternion.LookRotation(forearm.forward, -forearmDirection);
            _bracer.localScale = new Vector3(radialScale / Mathf.Abs(parentScale.x), bodyScale / Mathf.Abs(parentScale.y), radialScale / Mathf.Abs(parentScale.z));
        }

        private float GetBracerRadialScale(VisEquipment equipment)
        {
            if (equipment.m_bodyModel == null)
            {
                return _profile.MaleBracerRadialScale;
            }

            if (equipment.m_models == null)
            {
                return _profile.MaleBracerRadialScale;
            }

            if (equipment.m_models.Length <= _profile.FemaleModelIndex)
            {
                return _profile.MaleBracerRadialScale;
            }

            if (equipment.m_models[_profile.FemaleModelIndex] == null)
            {
                return _profile.MaleBracerRadialScale;
            }

            var femaleMesh = equipment.m_models[_profile.FemaleModelIndex].m_mesh;

            if (femaleMesh == null)
            {
                return _profile.MaleBracerRadialScale;
            }

            return equipment.m_bodyModel.sharedMesh == femaleMesh ? _profile.FemaleBracerRadialScale : _profile.MaleBracerRadialScale;
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
