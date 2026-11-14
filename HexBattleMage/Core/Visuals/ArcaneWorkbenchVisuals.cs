using UnityEngine;
using UnityEngine.Rendering;
using HexBattleMage.Assets;

namespace HexBattleMage.Core.Visuals
{
    internal static class ArcaneWorkbenchVisuals
    {
        private const string GlowShaderName = "Custom/Particle (Unlit)";
        private const int CircleSegments = 48;
        private const int RuneCount = 8;
        private const float SurfaceClearance = 0.04f;
        private const float RuneLineWidth = 0.02f;
        private const int SparkTextureSize = 32;
        private static readonly Color ArcaneColor = new Color(0.7f, 0.3f, 1f, 0.9f);

        internal static bool AddTo(GameObject prefab)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return true;
            }

            var shader = Shader.Find(GlowShaderName);

            if (shader == null)
            {
                var sparkRenderer = AssetBundleHelper.HexMagicBolt.GetComponentInChildren<ParticleSystemRenderer>(true);

                if (sparkRenderer != null)
                {
                    if (sparkRenderer.sharedMaterial != null)
                    {
                        shader = sparkRenderer.sharedMaterial.shader;
                    }
                }
            }

            if (shader == null)
            {
                DebugLog.LogError($"Cannot create workbench visuals: shader '{GlowShaderName}' is unavailable.");
                return false;
            }

            var material = new Material(shader)
            {
                name = "HexArcaneWorkbenchGlow",
                mainTexture = Texture2D.whiteTexture
            };

            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_SoftParticles", 0f);

            float surfaceHeight = GetSurfaceHeight(prefab);
            var root = CreateChild(prefab.transform, "HexArcaneVisuals", new Vector3(0f, surfaceHeight + SurfaceClearance, 0f));

            AddOrbitingRing(root, material, "OuterRuneHalo", 0.62f, 0.3f, 18f);
            AddOrbitingRing(root, material, "InnerRuneHalo", 0.42f, 0.48f, -55f);
            AddCrystals(root, material);
            AddSparks(root, material);

            var light = root.gameObject.AddComponent<Light>();

            light.type = LightType.Point;
            light.color = ArcaneColor;
            light.intensity = 1.4f;
            light.range = 3.5f;
            light.shadows = LightShadows.None;
            return true;
        }

        private static float GetSurfaceHeight(GameObject prefab)
        {
            float height = 0f;

            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                Vector3 highestPoint = renderer.bounds.center + Vector3.up * renderer.bounds.extents.y;

                height = Mathf.Max(height, prefab.transform.InverseTransformPoint(highestPoint).y);
            }

            return height;
        }

        private static void AddOrbitingRing(Transform root, Material material, string name, float radius, float height, float tilt)
        {
            var ringRoot = CreateChild(root, name, new Vector3(0f, height, 0f));

            ringRoot.localRotation = Quaternion.Euler(tilt, 0f, 0f);
            ringRoot.gameObject.AddComponent<RotatingArcaneRing>();

            var circle = CreateLine(ringRoot, "Halo", material, CircleSegments);

            circle.loop = true;

            for (int index = 0; index < CircleSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / CircleSegments;

                circle.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            for (int index = 0; index < RuneCount; index++)
            {
                float angle = index * Mathf.PI * 2f / RuneCount;
                var glyphRoot = CreateChild(ringRoot, $"HaloGlyph{index}", new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));

                glyphRoot.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);

                var glyph = CreateLine(glyphRoot, "Diamond", material, 4);

                glyph.loop = true;
                glyph.SetPosition(0, new Vector3(0.07f, 0f, 0f));
                glyph.SetPosition(1, new Vector3(0f, 0f, 0.04f));
                glyph.SetPosition(2, new Vector3(-0.07f, 0f, 0f));
                glyph.SetPosition(3, new Vector3(0f, 0f, -0.04f));
            }
        }

        private static LineRenderer CreateLine(Transform parent, string name, Material material, int pointCount)
        {
            var line = CreateChild(parent, name, Vector3.zero).gameObject.AddComponent<LineRenderer>();

            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.positionCount = pointCount;
            line.widthMultiplier = RuneLineWidth;
            line.startColor = ArcaneColor;
            line.endColor = ArcaneColor;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private static void AddCrystals(Transform root, Material material)
        {
            var mesh = new Mesh
            {
                name = "HexArcaneCrystal",
                vertices = new[]
                {
                    new Vector3(0f, 0.5f, 0f), new Vector3(0.25f, 0f, 0f), new Vector3(0f, 0f, 0.25f),
                    new Vector3(-0.25f, 0f, 0f), new Vector3(0f, 0f, -0.25f), new Vector3(0f, -0.5f, 0f)
                },
                triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4, 5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1 },
                colors = new[] { ArcaneColor, ArcaneColor, ArcaneColor, ArcaneColor, ArcaneColor, ArcaneColor }
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AddCrystal(root, material, mesh, "CentralCrystal", new Vector3(0f, 0.55f, 0f), 0.8f);
            AddCrystal(root, material, mesh, "LeftCrystal", new Vector3(-0.48f, 0.3f, 0.12f), 0.38f);
            AddCrystal(root, material, mesh, "RightCrystal", new Vector3(0.48f, 0.3f, 0.12f), 0.38f);
        }

        private static void AddCrystal(Transform root, Material material, Mesh mesh, string name, Vector3 position, float scale)
        {
            var crystal = CreateChild(root, name, position);

            crystal.localScale = Vector3.one * scale;
            crystal.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = crystal.gameObject.AddComponent<MeshRenderer>();

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            crystal.gameObject.AddComponent<FloatingArcaneCrystal>();
        }

        private static void AddSparks(Transform root, Material material)
        {
            var particles = CreateChild(root, "ArcaneSparks", new Vector3(0f, 0.1f, 0f)).gameObject.AddComponent<ParticleSystem>();

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;

            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 2f;
            main.startSpeed = 0.18f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.055f);
            main.startColor = ArcaneColor;
            main.maxParticles = 40;

            var emission = particles.emission;

            emission.rateOverTime = 12f;

            var shape = particles.shape;

            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var fade = particles.colorOverLifetime;

            fade.enabled = true;
            fade.color = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                alphaKeys = new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) }
            };

            var renderer = particles.GetComponent<ParticleSystemRenderer>();

            renderer.sharedMaterial = CreateSparkMaterial(material);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Material CreateSparkMaterial(Material source)
        {
            var texture = new Texture2D(SparkTextureSize, SparkTextureSize, TextureFormat.RGBA32, false)
            {
                name = "MageTableSpark",
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[SparkTextureSize * SparkTextureSize];

            for (int y = 0; y < SparkTextureSize; y++)
            {
                for (int x = 0; x < SparkTextureSize; x++)
                {
                    float horizontal = (x + 0.5f) / SparkTextureSize * 2f - 1f;
                    float vertical = (y + 0.5f) / SparkTextureSize * 2f - 1f;
                    float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(horizontal * horizontal + vertical * vertical));

                    pixels[y * SparkTextureSize + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            return new Material(source)
            {
                name = "MageTableSparkGlow",
                mainTexture = texture
            };
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 position)
        {
            var child = new GameObject(name).transform;

            child.SetParent(parent, false);
            child.localPosition = position;
            return child;
        }
    }
}
