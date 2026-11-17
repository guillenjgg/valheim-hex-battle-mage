using UnityEngine;
using HexBattleMage.Core.Managers;
using UnityEngine.Rendering;

namespace HexBattleMage.Core.Visuals
{
    internal static class ArcaneWorkbenchVisuals
    {
        private const string GlowShaderName = "Sprites/Default";
        private const int SparkTextureSize = 32;

        internal static bool AddTo(GameObject prefab)
        {
            var profile = PrefabSettings.Require<MageTableProfile>(prefab);

            if (!profile.EnableVisuals)
            {
                return true;
            }

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return true;
            }

            var shader = Shader.Find(GlowShaderName);

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

            material.renderQueue = (int)RenderQueue.Transparent;

            float surfaceHeight = GetSurfaceHeight(prefab);
            var root = CreateChild(prefab.transform, "HexArcaneVisuals", new Vector3(0f, surfaceHeight + profile.SurfaceClearance, 0f));

            AddOrbitingRing(root, material, "OuterRuneHalo", profile.OuterHalo.x, profile.OuterHalo.y, profile.OuterHalo.z);
            AddOrbitingRing(root, material, "InnerRuneHalo", profile.InnerHalo.x, profile.InnerHalo.y, profile.InnerHalo.z);
            AddCrystals(root, material);
            AddSparks(root, material);

            var light = root.gameObject.AddComponent<Light>();

            light.type = LightType.Point;
            light.color = profile.ArcaneColor;
            light.intensity = profile.LightIntensity;
            light.range = profile.LightRange;
            light.shadows = LightShadows.None;
            return true;
        }

        private static float GetSurfaceHeight(GameObject prefab)
        {
            float height = 0f;
            bool foundTabletop = false;

            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.name.StartsWith("Oak_Top_Plank_", System.StringComparison.Ordinal))
                {
                    continue;
                }

                foundTabletop = true;
                Vector3 highestPoint = renderer.bounds.center + Vector3.up * renderer.bounds.extents.y;

                height = Mathf.Max(height, prefab.transform.InverseTransformPoint(highestPoint).y);
            }

            if (foundTabletop)
            {
                return height;
            }

            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                Vector3 highestPoint = renderer.bounds.center + Vector3.up * renderer.bounds.extents.y;

                height = Mathf.Max(height, prefab.transform.InverseTransformPoint(highestPoint).y);
            }

            return height;
        }

        private static void AddOrbitingRing(Transform root, Material material, string name, float radius, float height, float tilt)
        {
            var profile = root.GetComponentInParent<MageTableProfile>(true);
            var ringRoot = CreateChild(root, name, new Vector3(0f, height, 0f));

            ringRoot.localRotation = Quaternion.Euler(tilt, 0f, 0f);
            ringRoot.gameObject.AddComponent<RotatingArcaneRing>();

            var circle = CreateLine(ringRoot, "Halo", material, profile.CircleSegments);

            circle.loop = true;

            for (int index = 0; index < profile.CircleSegments; index++)
            {
                float angle = index * Mathf.PI * 2f / profile.CircleSegments;

                circle.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            for (int index = 0; index < profile.RuneCount; index++)
            {
                float angle = index * Mathf.PI * 2f / profile.RuneCount;
                var glyphRoot = CreateChild(ringRoot, $"HaloGlyph{index}", new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));

                glyphRoot.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);

                var glyph = CreateLine(glyphRoot, "Diamond", material, 4);

                glyph.loop = true;
                glyph.SetPosition(0, new Vector3(profile.GlyphHalfSize.x, 0f, 0f));
                glyph.SetPosition(1, new Vector3(0f, 0f, profile.GlyphHalfSize.y));
                glyph.SetPosition(2, new Vector3(-profile.GlyphHalfSize.x, 0f, 0f));
                glyph.SetPosition(3, new Vector3(0f, 0f, -profile.GlyphHalfSize.y));
            }
        }

        private static LineRenderer CreateLine(Transform parent, string name, Material material, int pointCount)
        {
            var profile = parent.GetComponentInParent<MageTableProfile>(true);
            var line = CreateChild(parent, name, Vector3.zero).gameObject.AddComponent<LineRenderer>();

            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.positionCount = pointCount;
            line.widthMultiplier = profile.RuneLineWidth;
            line.startColor = profile.ArcaneColor;
            line.endColor = profile.ArcaneColor;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private static void AddCrystals(Transform root, Material material)
        {
            var profile = root.GetComponentInParent<MageTableProfile>(true);
            var mesh = new Mesh
            {
                name = "HexArcaneCrystal",
                vertices = new[]
                {
                    new Vector3(0f, 0.5f, 0f), new Vector3(0.25f, 0f, 0f), new Vector3(0f, 0f, 0.25f),
                    new Vector3(-0.25f, 0f, 0f), new Vector3(0f, 0f, -0.25f), new Vector3(0f, -0.5f, 0f)
                },
                triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4, 5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1 },
                colors = new[] { profile.ArcaneColor, profile.ArcaneColor, profile.ArcaneColor, profile.ArcaneColor, profile.ArcaneColor, profile.ArcaneColor }
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AddCrystal(root, material, mesh, "CentralCrystal", profile.CentralCrystalPosition, profile.CentralCrystalScale);
            AddCrystal(root, material, mesh, "LeftCrystal", new Vector3(-profile.SideCrystalPosition.x, profile.SideCrystalPosition.y, profile.SideCrystalPosition.z), profile.SideCrystalScale);
            AddCrystal(root, material, mesh, "RightCrystal", profile.SideCrystalPosition, profile.SideCrystalScale);
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
            var profile = root.GetComponentInParent<MageTableProfile>(true);
            var particles = CreateChild(root, "ArcaneSparks", new Vector3(0f, profile.SparkHeight, 0f)).gameObject.AddComponent<ParticleSystem>();

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;

            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = profile.SparkLifetime;
            main.startSpeed = profile.SparkSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(profile.SparkSize.x, profile.SparkSize.y);
            main.startColor = profile.ArcaneColor;
            main.maxParticles = profile.MaximumSparks;

            var emission = particles.emission;

            emission.rateOverTime = profile.SparkRate;

            var shape = particles.shape;

            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = profile.SparkRadius;

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
