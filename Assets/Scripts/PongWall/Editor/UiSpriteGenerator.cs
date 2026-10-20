#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PongWall.Editor
{
    /// <summary>
    /// Generates the UI sprite sheet procedurally so the project does not need
    /// externally sourced UI art. Everything is white + alpha and 9-sliced,
    /// which means a single sprite can be tinted to any palette colour.
    /// </summary>
    public static class UiSpriteGenerator
    {
        private const string OutputFolder = "Assets/Resources/UI";

        [MenuItem("PongWall/Generate UI Sprites")]
        public static void GenerateAll()
        {
            EnsureFolder();

            // A rounded rectangle with a generous radius: panels, cards, buttons.
            CreateRoundedRect("panel", size: 96, radius: 30);
            // Slightly rounder, for large cards.
            CreateRoundedRect("card", size: 96, radius: 42);
            // Fully rounded ends, for the HUD bar and chips.
            CreateRoundedRect("pill", size: 96, radius: 48);
            CreateRoundedRect("chip", size: 96, radius: 40);

            CreateCircle("disc", size: 128);
            CreateRing("ring", size: 128, thickness: 10f);
            CreateGlow("glow", size: 192, falloff: 2.1f);

            CreateVerticalFade("fade_down", top: 1f, bottom: 0f);
            CreateVerticalFade("fade_up", top: 0f, bottom: 1f);

            CreateSolid("solid", size: 8);

            AssetDatabase.SaveAssets();
            Debug.Log($"[PongWall] UI sprites generated into '{OutputFolder}'.");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/Resources", "UI");
        }

        // ------------------------------------------------------------------ shapes

        private static void CreateRoundedRect(string name, int size, float radius)
        {
            var texture = NewTexture(size);
            var pixels = new Color32[size * size];
            var half = size * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    var d = RoundedBoxDistance(p, half, radius);
                    pixels[y * size + x] = White(Coverage(d));
                }
            }

            Write(name, texture, pixels, border: new Vector4(radius + 1, radius + 1, radius + 1, radius + 1));
        }

        private static void CreateCircle(string name, int size)
        {
            var texture = NewTexture(size);
            var pixels = new Color32[size * size];
            var half = size * 0.5f;
            var radius = half - 1f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    var d = p.magnitude - radius;
                    pixels[y * size + x] = White(Coverage(d));
                }
            }

            Write(name, texture, pixels, border: Vector4.zero);
        }

        private static void CreateRing(string name, int size, float thickness)
        {
            var texture = NewTexture(size);
            var pixels = new Color32[size * size];
            var half = size * 0.5f;
            var radius = half - thickness * 0.5f - 1f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    var d = Mathf.Abs(p.magnitude - radius) - thickness * 0.5f;
                    pixels[y * size + x] = White(Coverage(d));
                }
            }

            Write(name, texture, pixels, border: Vector4.zero);
        }

        private static void CreateGlow(string name, int size, float falloff)
        {
            var texture = NewTexture(size);
            var pixels = new Color32[size * size];
            var half = size * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    var t = Mathf.Clamp01(p.magnitude / half);
                    var a = Mathf.Pow(1f - t, falloff);
                    pixels[y * size + x] = White(a);
                }
            }

            Write(name, texture, pixels, border: Vector4.zero);
        }

        private static void CreateVerticalFade(string name, float top, float bottom)
        {
            const int width = 8;
            const int height = 256;

            var texture = NewTexture(height, width);
            var pixels = new Color32[width * height];

            for (var y = 0; y < height; y++)
            {
                // Texture rows start at the bottom, so invert to get "top" first.
                var t = 1f - y / (float)(height - 1);
                var a = Mathf.Lerp(bottom, top, t);

                for (var x = 0; x < width; x++)
                    pixels[y * width + x] = White(a);
            }

            Write(name, texture, pixels, border: Vector4.zero);
        }

        private static void CreateSolid(string name, int size)
        {
            var texture = NewTexture(size);
            var pixels = new Color32[size * size];

            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = White(1f);

            Write(name, texture, pixels, border: Vector4.zero);
        }

        // ------------------------------------------------------------------- maths

        private static float RoundedBoxDistance(Vector2 p, float half, float radius)
        {
            var inset = half - radius;
            var q = new Vector2(Mathf.Abs(p.x) - inset, Mathf.Abs(p.y) - inset);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        /// <summary>Signed distance to coverage with a one pixel anti-aliased edge.</summary>
        private static float Coverage(float signedDistance)
        {
            return Mathf.Clamp01(0.5f - signedDistance);
        }

        private static Color32 White(float alpha)
        {
            var a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
            return new Color32(255, 255, 255, a);
        }

        // -------------------------------------------------------------------- io

        private static Texture2D NewTexture(int size)
        {
            return NewTexture(size, size);
        }

        private static Texture2D NewTexture(int height, int width)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private static void Write(string name, Texture2D texture, Color32[] pixels, Vector4 border)
        {
            texture.SetPixels32(pixels);
            texture.Apply();

            var path = $"{OutputFolder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }
    }
}
#endif