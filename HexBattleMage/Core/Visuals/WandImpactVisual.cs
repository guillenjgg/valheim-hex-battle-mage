using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Rendering;

namespace HexBattleMage.Core.Visuals
{
    [Flags]
    internal enum WandImpactElement
    {
        None = 0,
        Fire = 1,
        Frost = 2,
        Spirit = 4,
        Lightning = 8,
        Poison = 16
    }

    internal sealed class WandImpactVisual : MonoBehaviour
    {
        private const string ShaderName = "Sprites/Default";
        private const int SparkCount = 18;
        private const float MaximumBurstRadius = 0.5f;
        private const float DirectHitBurstRadius = 0.12f;
        private static Material _material;
        private readonly LineRenderer[] _sparks = new LineRenderer[SparkCount];
        private readonly Vector3[] _directions = new Vector3[SparkCount];
        private readonly float[] _lengths = new float[SparkCount];
        private float _radius;
        private float _started;
        private float _duration;
        private Color[] _elementColors;
        private float _opacity;

        internal static void Show(Vector3 position, float radius, WandImpactElement elements)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return;
            }

            PlayImpactSound(position);

            if (_material == null)
            {
                var shader = Shader.Find(ShaderName);

                if (shader == null)
                {
                    DebugLog.LogWarning("Cannot render wand impact: Sprites/Default shader is unavailable.");
                    return;
                }

                _material = new Material(shader);
                _material.name = "HexWandImpactBurst";
                _material.mainTexture = Texture2D.whiteTexture;
                _material.renderQueue = (int)RenderQueue.Transparent;
            }

            var root = new GameObject("HexWandImpactBurst");

            root.transform.position = position;
            root.AddComponent<WandImpactVisual>().Initialize(Mathf.Max(0f, radius), elements);
        }

        private static void PlayImpactSound(Vector3 position)
        {
            float volumeMultiplier = DevConfig.ImpactSoundVolume.Value;

            if (volumeMultiplier <= 0f)
            {
                return;
            }

            const string impactPrefabName = "fx_fireball_staff_explosion";
            const float minimumDistance = 1f;
            const float maximumDistance = 20f;
            var prefab = PrefabManager.Instance.GetPrefab(impactPrefabName);

            if (prefab == null)
            {
                DebugLog.LogWarning("Wand impact sound prefab is unavailable.");
                return;
            }

            var sound = prefab.GetComponentInChildren<ZSFX>(true);

            if (sound == null)
            {
                return;
            }

            if (sound.m_audioClips.Length == 0)
            {
                return;
            }

            var clip = sound.m_audioClips[UnityEngine.Random.Range(0, sound.m_audioClips.Length)];

            if (clip == null)
            {
                return;
            }

            var root = new GameObject("HexWandImpactSound");

            root.transform.position = position;

            var audio = root.AddComponent<AudioSource>();
            var original = sound.GetComponent<AudioSource>();

            audio.playOnAwake = false;
            audio.clip = clip;
            audio.spatialBlend = 1f;
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.minDistance = minimumDistance;
            audio.maxDistance = maximumDistance;
            audio.volume = UnityEngine.Random.Range(sound.m_minVol, sound.m_maxVol) * volumeMultiplier;
            audio.pitch = UnityEngine.Random.Range(sound.m_minPitch, sound.m_maxPitch);

            if (original != null)
            {
                audio.outputAudioMixerGroup = original.outputAudioMixerGroup;
            }

            audio.Play();
            Destroy(root, clip.length / Mathf.Max(0.1f, audio.pitch) + 0.1f);
        }

        private void Initialize(float radius, WandImpactElement elements)
        {
            _started = Time.time;
            _duration = DevConfig.ImpactVisualDuration.Value;
            _opacity = DevConfig.ImpactBurstOpacity.Value;
            _elementColors = GetElementColors(elements);

            _radius = radius > 0f ? Mathf.Min(radius, MaximumBurstRadius) : DirectHitBurstRadius;

            for (int index = 0; index < SparkCount; index++)
            {
                _directions[index] = UnityEngine.Random.onUnitSphere;
                _lengths[index] = UnityEngine.Random.Range(0.55f, 1f);
                _sparks[index] = CreateLine("BurstSpark", 3, DevConfig.ImpactLineWidth.Value * 2f);
                _sparks[index].widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                _sparks[index].widthMultiplier = DevConfig.ImpactLineWidth.Value * 2f;
            }

            UpdateBurst(0f);
            Destroy(gameObject, _duration);
            DebugLog.LogInfo($"Smoke-free wand explosion displayed: burst radius={_radius:0.##} m, AoE radius={radius:0.##} m, elements={elements}.");
        }

        private void Update()
        {
            float progress = Mathf.Clamp01((Time.time - _started) / _duration);

            UpdateBurst(progress);
        }

        private void UpdateBurst(float progress)
        {
            float expansion = 1f - (1f - progress) * (1f - progress);
            float opacity = _opacity * (1f - progress);

            for (int index = 0; index < SparkCount; index++)
            {
                float reach = _radius * _lengths[index] * Mathf.Lerp(0.15f, 1f, expansion);
                var direction = _directions[index];
                var spark = _sparks[index];

                spark.SetPosition(0, direction * reach * progress);
                spark.SetPosition(1, direction * reach * Mathf.Lerp(progress, 1f, 0.65f));
                spark.SetPosition(2, direction * reach);
                var color = _elementColors[index % _elementColors.Length];
                var coreColor = Color.Lerp(color, Color.white, 0.2f);

                coreColor.a = opacity;
                color.a = opacity * 0.15f;
                spark.startColor = coreColor;
                spark.endColor = color;
            }
        }

        private static Color[] GetElementColors(WandImpactElement elements)
        {
            var colors = new List<Color>();

            if ((elements & WandImpactElement.Fire) != 0)
            {
                colors.Add(new Color(1f, 0.3f, 0.04f));
            }

            if ((elements & WandImpactElement.Frost) != 0)
            {
                colors.Add(new Color(0.3f, 0.8f, 1f));
            }

            if ((elements & WandImpactElement.Spirit) != 0)
            {
                colors.Add(new Color(0.65f, 0.5f, 1f));
            }

            if ((elements & WandImpactElement.Lightning) != 0)
            {
                colors.Add(new Color(0.5f, 0.6f, 1f));
            }

            if ((elements & WandImpactElement.Poison) != 0)
            {
                colors.Add(new Color(0.4f, 1f, 0.12f));
            }

            if (colors.Count == 0)
            {
                colors.Add(DevConfig.BoltColor.Value);
            }

            return colors.ToArray();
        }

        private LineRenderer CreateLine(string name, int pointCount, float width)
        {
            var root = new GameObject(name);

            root.transform.SetParent(transform, false);

            var line = root.AddComponent<LineRenderer>();

            line.sharedMaterial = _material;
            line.useWorldSpace = false;
            line.positionCount = pointCount;
            line.widthMultiplier = width;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }
    }
}

