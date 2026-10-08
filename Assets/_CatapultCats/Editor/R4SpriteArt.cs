using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CatapultCats.Editor
{
    // Small original flat illustrations, generated once into native Texture2D/Sprite assets.
    // No runtime texture creation, external art dependency, or lighting pipeline is required.
    internal static class R4SpriteArt
    {
        internal const string Folder = "Assets/_CatapultCats/Art/R4";
        private static readonly Color32 Ink = new Color32(53, 68, 68, 255);
        private static readonly Color32 Cream = new Color32(255, 244, 216, 255);
        private static readonly Color32 Coral = new Color32(238, 141, 105, 255);

        internal static Sprite Create(string name)
        {
            // Existing native sprite assets are accepted art, not regeneration targets.
            Sprite[] existing = AssetDatabase.LoadAllAssetsAtPath($"{Folder}/{name}.asset").OfType<Sprite>().ToArray();
            if (existing.Length > 1) throw new InvalidOperationException("Duplicate R4 sprite subassets: " + name);
            if (existing.Length == 1) return existing[0];
            bool beam = name.EndsWith("Beam", StringComparison.Ordinal) || name == "Ramp";
            var c = new Canvas(beam ? 384 : 192, beam ? 64 : 192);
            switch (name)
            {
                case "Cat": DrawCat(c); break;
                case "Mouse": DrawMouse(c); break;
                case "WoodBeam": case "WoodBlock": DrawWood(c); break;
                case "GlassBeam": case "GlassBlock": DrawGlass(c); break;
                case "HeavyBlock": DrawHeavy(c); break;
                case "Ramp": DrawRamp(c); break;
                case "Cloud": DrawCloud(c); break;
                case "Bush": DrawBush(c); break;
                case "Dot": c.Circle(0.5f, 0.5f, 0.42f, Cream); break;
                case "Spark": c.Polygon(Cream, new Vector2(0.5f, 0.97f), new Vector2(0.62f, 0.62f),
                    new Vector2(0.97f, 0.5f), new Vector2(0.62f, 0.38f), new Vector2(0.5f, 0.03f),
                    new Vector2(0.38f, 0.38f), new Vector2(0.03f, 0.5f), new Vector2(0.38f, 0.62f)); break;
                case "Panel": c.RoundRect(0.02f, 0.02f, 0.98f, 0.98f, 0.15f, Color.white); break;
                case "Square": c.Rect(0f, 0f, 1f, 1f, Color.white); break;
                default: throw new ArgumentException("Unknown R4 sprite " + name);
            }
            return Save(name, c, name == "Panel" ? new Vector4(32, 32, 32, 32) : Vector4.zero);
        }

        private static void DrawCat(Canvas c)
        {
            Color32 fur = new Color32(245, 185, 91, 255);
            c.Polygon(Ink, new Vector2(0.14f, 0.64f), new Vector2(0.14f, 0.94f), new Vector2(0.4f, 0.8f));
            c.Polygon(Ink, new Vector2(0.6f, 0.8f), new Vector2(0.86f, 0.94f), new Vector2(0.86f, 0.64f));
            c.Polygon(fur, new Vector2(0.18f, 0.65f), new Vector2(0.18f, 0.87f), new Vector2(0.38f, 0.77f));
            c.Polygon(fur, new Vector2(0.62f, 0.77f), new Vector2(0.82f, 0.87f), new Vector2(0.82f, 0.65f));
            c.Polygon(Coral, new Vector2(0.21f, 0.73f), new Vector2(0.21f, 0.83f), new Vector2(0.3f, 0.77f));
            c.Polygon(Coral, new Vector2(0.7f, 0.77f), new Vector2(0.79f, 0.83f), new Vector2(0.79f, 0.73f));
            c.Ellipse(0.5f, 0.47f, 0.42f, 0.41f, Ink);
            c.Ellipse(0.5f, 0.47f, 0.39f, 0.38f, fur);
            c.Ellipse(0.5f, 0.27f, 0.24f, 0.15f, Cream);
            c.Line(0.39f, 0.83f, 0.42f, 0.7f, 0.025f, Coral);
            c.Line(0.5f, 0.85f, 0.5f, 0.71f, 0.025f, Coral);
            c.Line(0.61f, 0.83f, 0.58f, 0.7f, 0.025f, Coral);
            c.Ellipse(0.34f, 0.51f, 0.035f, 0.065f, Ink);
            c.Ellipse(0.66f, 0.51f, 0.035f, 0.065f, Ink);
            c.Circle(0.33f, 0.54f, 0.011f, Color.white);
            c.Circle(0.65f, 0.54f, 0.011f, Color.white);
            c.Polygon(Coral, new Vector2(0.46f, 0.36f), new Vector2(0.54f, 0.36f), new Vector2(0.5f, 0.31f));
            c.Line(0.5f, 0.31f, 0.5f, 0.27f, 0.008f, Ink);
            c.Line(0.5f, 0.27f, 0.45f, 0.25f, 0.008f, Ink);
            c.Line(0.5f, 0.27f, 0.55f, 0.25f, 0.008f, Ink);
            c.Line(0.12f, 0.38f, 0.28f, 0.34f, 0.009f, Ink);
            c.Line(0.12f, 0.28f, 0.28f, 0.3f, 0.009f, Ink);
            c.Line(0.72f, 0.34f, 0.88f, 0.38f, 0.009f, Ink);
            c.Line(0.72f, 0.3f, 0.88f, 0.28f, 0.009f, Ink);
        }

        private static void DrawMouse(Canvas c)
        {
            Color32 fur = new Color32(203, 218, 222, 255);
            c.Line(0.75f, 0.28f, 0.91f, 0.24f, 0.022f, Coral);
            c.Line(0.91f, 0.24f, 0.94f, 0.37f, 0.022f, Coral);
            c.Circle(0.24f, 0.74f, 0.2f, Ink);
            c.Circle(0.72f, 0.74f, 0.2f, Ink);
            c.Circle(0.24f, 0.74f, 0.177f, fur);
            c.Circle(0.72f, 0.74f, 0.177f, fur);
            c.Circle(0.24f, 0.74f, 0.12f, new Color32(242, 184, 184, 255));
            c.Circle(0.72f, 0.74f, 0.12f, new Color32(242, 184, 184, 255));
            c.Ellipse(0.48f, 0.39f, 0.35f, 0.33f, Ink);
            c.Ellipse(0.48f, 0.4f, 0.324f, 0.304f, fur);
            c.Ellipse(0.48f, 0.29f, 0.2f, 0.15f, Cream);
            c.Circle(0.34f, 0.45f, 0.03f, Ink);
            c.Circle(0.63f, 0.45f, 0.03f, Ink);
            c.Circle(0.33f, 0.46f, 0.009f, Color.white);
            c.Circle(0.62f, 0.46f, 0.009f, Color.white);
            c.Circle(0.48f, 0.31f, 0.035f, Coral);
            c.Line(0.48f, 0.28f, 0.48f, 0.22f, 0.007f, Ink);
            c.Rect(0.45f, 0.16f, 0.475f, 0.22f, Color.white);
            c.Rect(0.485f, 0.16f, 0.51f, 0.22f, Color.white);
            c.Line(0.15f, 0.34f, 0.31f, 0.3f, 0.007f, Ink);
            c.Line(0.65f, 0.3f, 0.81f, 0.34f, 0.007f, Ink);
        }

        private static void DrawWood(Canvas c)
        {
            c.Rect(0f, 0f, 1f, 1f, new Color32(104, 65, 45, 255));
            c.Rect(0.01f, 0.08f, 0.99f, 0.92f, new Color32(199, 139, 77, 255));
            c.Rect(0.02f, 0.78f, 0.98f, 0.87f, new Color32(234, 179, 108, 255));
            c.Line(0.07f, 0.38f, 0.64f, 0.38f, 0.014f, new Color32(166, 104, 59, 255));
            c.Line(0.34f, 0.58f, 0.92f, 0.58f, 0.013f, new Color32(166, 104, 59, 255));
            c.Ellipse(0.74f, 0.32f, 0.04f, 0.085f, new Color32(166, 104, 59, 255));
            c.Ellipse(0.74f, 0.32f, 0.027f, 0.05f, new Color32(213, 153, 84, 255));
            c.Circle(0.035f, 0.5f, 0.015f, Ink);
            c.Circle(0.965f, 0.5f, 0.015f, Ink);
        }

        private static void DrawGlass(Canvas c)
        {
            c.Rect(0f, 0f, 1f, 1f, new Color32(30, 141, 157, 255));
            c.Rect(0.015f, 0.08f, 0.985f, 0.92f, new Color32(103, 222, 230, 235));
            c.Line(0.12f, 0.15f, 0.34f, 0.85f, 0.026f, new Color32(224, 255, 247, 230));
            c.Line(0.25f, 0.15f, 0.47f, 0.85f, 0.01f, new Color32(224, 255, 247, 230));
            c.Rect(0.015f, 0.82f, 0.985f, 0.92f, new Color32(191, 255, 249, 255));
        }

        private static void DrawHeavy(Canvas c)
        {
            c.Rect(0f, 0f, 1f, 1f, Ink);
            c.Rect(0.03f, 0.04f, 0.97f, 0.96f, new Color32(104, 122, 137, 255));
            c.Rect(0.05f, 0.85f, 0.95f, 0.94f, new Color32(146, 168, 181, 255));
            c.Rect(0.16f, 0.25f, 0.84f, 0.7f, new Color32(70, 87, 104, 255));
            for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++)
            {
                c.Circle(0.095f + x * 0.81f, 0.13f + y * 0.69f, 0.035f, Ink);
                c.Circle(0.095f + x * 0.81f, 0.14f + y * 0.69f, 0.022f, new Color32(208, 222, 218, 255));
            }
            c.Polygon(new Color32(194, 202, 204, 255), new Vector2(0.4f, 0.6f), new Vector2(0.6f, 0.6f),
                new Vector2(0.67f, 0.32f), new Vector2(0.33f, 0.32f));
        }

        private static void DrawRamp(Canvas c)
        {
            c.Rect(0f, 0f, 1f, 1f, new Color32(49, 105, 92, 255));
            c.Rect(0.01f, 0.1f, 0.99f, 0.9f, new Color32(155, 204, 89, 255));
            c.Rect(0.01f, 0.75f, 0.99f, 0.9f, new Color32(224, 242, 158, 255));
            for (int i = 0; i < 4; i++)
            {
                float x = 0.18f + i * 0.2f;
                c.Line(x - 0.03f, 0.3f, x + 0.02f, 0.5f, 0.011f, Cream);
                c.Line(x + 0.02f, 0.5f, x - 0.03f, 0.7f, 0.011f, Cream);
            }
        }

        private static void DrawCloud(Canvas c)
        {
            Color32 cloud = new Color32(250, 252, 238, 255);
            c.Ellipse(0.5f, 0.38f, 0.44f, 0.18f, cloud);
            c.Circle(0.35f, 0.51f, 0.2f, cloud);
            c.Circle(0.59f, 0.6f, 0.23f, cloud);
            c.Circle(0.75f, 0.48f, 0.14f, cloud);
        }

        private static void DrawBush(Canvas c)
        {
            Color32 green = new Color32(119, 178, 134, 255);
            c.Circle(0.25f, 0.24f, 0.23f, green);
            c.Circle(0.5f, 0.32f, 0.28f, green);
            c.Circle(0.77f, 0.23f, 0.23f, green);
            c.Rect(0.03f, 0.01f, 0.97f, 0.25f, green);
        }

        private static Sprite Save(string name, Canvas canvas, Vector4 border)
        {
            string path = $"{Folder}/{name}.asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false);
                AssetDatabase.CreateAsset(texture, path);
            }
            else texture.Reinitialize(canvas.Width, canvas.Height);
            texture.name = name;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(canvas.Pixels);
            texture.Apply();
            Sprite existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            Sprite created = Sprite.Create(texture, new Rect(0, 0, canvas.Width, canvas.Height),
                new Vector2(0.5f, 0.5f), canvas.Width, 0, SpriteMeshType.FullRect, border);
            created.name = name;
            if (existing == null)
            {
                AssetDatabase.AddObjectToAsset(created, texture);
                existing = created;
            }
            else
            {
                EditorUtility.CopySerialized(created, existing);
                UnityEngine.Object.DestroyImmediate(created);
            }
            EditorUtility.SetDirty(texture);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private sealed class Canvas
        {
            internal readonly int Width;
            internal readonly int Height;
            internal readonly Color32[] Pixels;
            internal Canvas(int width, int height) { Width = width; Height = height; Pixels = new Color32[width * height]; }
            private void Paint(Color32 color, Func<float, float, bool> inside)
            {
                for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
                    if (inside((x + 0.5f) / Width, (y + 0.5f) / Height)) Pixels[y * Width + x] = color;
            }
            internal void Rect(float x0, float y0, float x1, float y1, Color32 color)
                => Paint(color, (x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1);
            internal void Circle(float x, float y, float radius, Color32 color) => Ellipse(x, y, radius, radius, color);
            internal void Ellipse(float cx, float cy, float rx, float ry, Color32 color)
                => Paint(color, (x, y) => (x - cx) * (x - cx) / (rx * rx) + (y - cy) * (y - cy) / (ry * ry) <= 1f);
            internal void RoundRect(float x0, float y0, float x1, float y1, float radius, Color32 color)
                => Paint(color, (x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1 &&
                    new Vector2(x - Mathf.Clamp(x, x0 + radius, x1 - radius),
                        y - Mathf.Clamp(y, y0 + radius, y1 - radius)).sqrMagnitude <= radius * radius);
            internal void Line(float x0, float y0, float x1, float y1, float thickness, Color32 color)
            {
                Vector2 a = new Vector2(x0, y0);
                Vector2 delta = new Vector2(x1 - x0, y1 - y0);
                Paint(color, (x, y) =>
                {
                    Vector2 p = new Vector2(x, y) - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p, delta) / delta.sqrMagnitude);
                    return (p - delta * t).sqrMagnitude <= thickness * thickness;
                });
            }
            internal void Polygon(Color32 color, params Vector2[] vertices)
            {
                Paint(color, (x, y) =>
                {
                    bool inside = false;
                    for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
                    {
                        Vector2 a = vertices[i], b = vertices[j];
                        if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
                    }
                    return inside;
                });
            }
        }
    }
}
