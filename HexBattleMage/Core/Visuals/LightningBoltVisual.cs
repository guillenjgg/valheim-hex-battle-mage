using HexBattleMage.Core.Tuning;
using HexBattleMage.Core.Elements;
using HarmonyLib;
using UnityEngine;
using HexBattleMage.Core.Managers;
using UnityEngine.Rendering;

namespace HexBattleMage.Core.Visuals
{
    internal sealed class LightningBoltVisual : MonoBehaviour
    {
        private static readonly AccessTools.FieldRef<Projectile, int> StatusHash = AccessTools.FieldRefAccess<Projectile, int>("m_statusEffectHash");
        private ZNetView _view;
        private const string GlowShaderName = "Sprites/Default";
        private const int TextureHeight = 32;
        private static Material _material;
        private static bool _shaderWarningLogged;
        private Vector3[] _points;
        private Projectile _projectile;
        private LineRenderer _core;
        private LineRenderer _halo;
        private Vector3 _spawnPosition;
        private Vector3 _previousPosition;
        private bool _renderLogged;

        internal static void PrepareMaterial()
        {
            if (_material != null)
            {
                return;
            }

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return;
            }

            var shader = Shader.Find(GlowShaderName);

            if (shader == null)
            {
                if (!_shaderWarningLogged)
                {
                    DebugLog.LogWarning($"Lightning bolt shader '{GlowShaderName}' is unavailable.");
                    _shaderWarningLogged = true;
                }

                return;
            }

            var texture = new Texture2D(1, TextureHeight, TextureFormat.RGBA32, false);

            texture.name = "HexLightningSoftEdge";
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < TextureHeight; y++)
            {
                float distance = Mathf.Abs(y / (float)(TextureHeight - 1) * 2f - 1f);
                float opacity = Mathf.Pow(1f - distance, 2f);

                texture.SetPixel(0, y, new Color(1f, 1f, 1f, opacity));
            }

            texture.Apply();
            _material = new Material(shader);
            _material.name = "HexLightningBoltGlow";
            _material.mainTexture = texture;
            _material.renderQueue = (int)RenderQueue.Transparent;
        }

        private void Awake()
        {
            _points = new Vector3[PrefabSettings.Projectile.BoltPointCount];
            _projectile = GetComponentInParent<Projectile>();
            _view = _projectile != null ? _projectile.GetComponent<ZNetView>() : null;
            _spawnPosition = transform.position;
            _previousPosition = _spawnPosition;

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (_projectile == null)
            {
                _projectile = GetComponentInParent<Projectile>();

                if (_projectile == null)
                {
                    return;
                }
            }

            if (_view == null)
            {
                _view = _projectile.GetComponent<ZNetView>();
            }

            var data = _view != null ? _view.GetZDO() : null;
            int mask = data != null ? data.GetInt(WandElementManager.ProjectileElementsKey, -1) : WandElementManager.GetElementMask(StatusHash(_projectile));

            if (mask < 0)
            {
                return;
            }

            PrepareMaterial();

            if (_material == null)
            {
                return;
            }

            if (_core == null)
            {
                _halo = CreateLine("Halo");
                _core = CreateLine("Core");
            }

            var head = transform.position;
            var movement = head - _previousPosition;

            _previousPosition = head;

            var velocity = _projectile.GetVelocity();
            var direction = transform.forward;

            if (movement.sqrMagnitude > Mathf.Epsilon)
            {
                direction = movement.normalized;
            }
            else if (velocity.sqrMagnitude > Mathf.Epsilon)
            {
                direction = velocity.normalized;
            }

            var sideways = Vector3.Cross(direction, Vector3.up);

            if (sideways.sqrMagnitude < 0.01f)
            {
                sideways = Vector3.Cross(direction, Vector3.right);
            }

            sideways.Normalize();

            var upwards = Vector3.Cross(sideways, direction).normalized;
            float distanceTravelled = Vector3.Distance(_spawnPosition, head);
            float length = Mathf.Clamp(distanceTravelled, PrefabSettings.Projectile.MinimumVisibleLength, DevConfig.BoltLength.EffectiveValue());
            float flicker = Mathf.Floor(Time.time * PrefabSettings.Projectile.FlickerFrequency);
            float seed = Mathf.Abs(GetInstanceID() % 1000);

            _core.enabled = length >= PrefabSettings.Projectile.MinimumVisibleLength;
            _halo.enabled = _core.enabled;

            for (int index = 0; index < _points.Length; index++)
            {
                float progress = index / (float)(_points.Length - 1);
                float amplitude = Mathf.Sin(progress * Mathf.PI) * DevConfig.BoltJaggedness.EffectiveValue();
                float horizontal = Mathf.PerlinNoise(seed + index * 3.7f, flicker) * 2f - 1f;
                float vertical = Mathf.PerlinNoise(seed + index * 5.3f, flicker + 17f) * 2f - 1f;

                _points[index] = head - direction * (length * progress) + sideways * (horizontal * amplitude) + upwards * (vertical * amplitude);
            }

            float brightness = DevConfig.BoltBrightness.EffectiveValue();

            if (!WandElementManager.TryGetFlightColors(mask, out var coreColor, out var haloColor))
            {
                coreColor = Color.white;
                haloColor = DevConfig.BoltColor.EffectiveValue();
            }

            coreColor = new Color(coreColor.r * brightness, coreColor.g * brightness, coreColor.b * brightness, coreColor.a);
            haloColor = new Color(haloColor.r * brightness, haloColor.g * brightness, haloColor.b * brightness, haloColor.a * PrefabSettings.Projectile.HaloOpacity);
            _core.widthMultiplier = DevConfig.BoltWidth.EffectiveValue();
            _halo.widthMultiplier = DevConfig.BoltWidth.EffectiveValue() * PrefabSettings.Projectile.GlowWidthMultiplier;
            _core.startColor = coreColor;
            _core.endColor = coreColor;
            _halo.startColor = haloColor;
            _halo.endColor = haloColor;
            _core.SetPositions(_points);
            _halo.SetPositions(_points);

            if (!_renderLogged)
            {
                DebugLog.LogInfo($"Lightning visual drawing: shader={_material.shader.name}, supported={_material.shader.isSupported}, width={DevConfig.BoltWidth.EffectiveValue()}, length={length}, movement={movement.magnitude}, reported speed={velocity.magnitude}.");
                _renderLogged = true;
            }
        }

        private LineRenderer CreateLine(string name)
        {
            var child = new GameObject(name);

            child.transform.SetParent(transform, false);

            var line = child.AddComponent<LineRenderer>();

            line.sharedMaterial = _material;
            line.useWorldSpace = true;
            line.positionCount = _points.Length;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.widthCurve = PrefabSettings.Projectile.BoltWidthCurve;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 2;
            return line;
        }
    }
}
