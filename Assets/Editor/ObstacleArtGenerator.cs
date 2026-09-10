using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Draws the sprites for the obstacle/booster set procedurally — flat shapes with
// 1px anti-aliased edges, matching the game's flat look — and imports them as
// 100 PPU sprites under Assets/Art/Obstacles/. Rerun from the menu after
// changing any colour or size below; the prefabs reference the sprites by GUID,
// so regenerating in place keeps every level intact.
public static class ObstacleArtGenerator
{
    const string Folder = "Assets/Art/Obstacles";

    // Palette, picked against the purple board: warm orange is the game's own
    // accent, everything else is chosen to read at a glance as "cold", "hot" or
    // "reward".
    static readonly Color Orange = Hex("F07D2A");
    static readonly Color OrangeDark = Hex("B9561A");
    static readonly Color Cream = Hex("FFF3D6");
    static readonly Color Navy = Hex("2E3A6B");
    static readonly Color SkyBlue = Hex("8FD8FF");
    static readonly Color Ice = Hex("CFF3FF");
    static readonly Color Red = Hex("E8383F");
    static readonly Color RedDark = Hex("A5232A");
    static readonly Color Silver = Hex("EDEDED");
    static readonly Color Gunmetal = Hex("2B2B3D");
    static readonly Color Yellow = Hex("FFD43B");
    static readonly Color YellowDark = Hex("C99A0E");
    static readonly Color Cyan = Hex("5FE0FF");
    static readonly Color CyanDark = Hex("2AA3C4");
    static readonly Color Ink = Hex("3B2A00");
    static readonly Color White = Color.white;

    [MenuItem("Tools/Tilt Ball/Generate Obstacle Art")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(Folder);

        Bumper();
        FanRotor();
        FanHousing();
        Chevron();
        Band();
        Magnet();
        Ring();
        Dot();
        LaserEmitter();
        Beam();
        IcePatch();
        JetBoost();
        Shield();
        Bubble();
        Spark();

        AssetDatabase.Refresh();
        Debug.Log("[ObstacleArtGenerator] Sprites written to " + Folder);
    }

    // ---- individual sprites --------------------------------------------------

    static void Bumper()
    {
        var c = new Canvas(160, 160);
        c.Fill(Sdf.Circle(80, 80, 78), OrangeDark);
        c.Fill(Sdf.Circle(80, 80, 70), Orange);
        c.Fill(Sdf.Circle(80, 80, 52), Cream);
        c.Fill(Sdf.Circle(80, 80, 16), Orange);
        c.Fill(Sdf.Circle(68, 94, 8), White);            // specular
        c.Save("Bumper");
    }

    static void FanRotor()
    {
        var c = new Canvas(120, 120);
        Func<float, float, float> blades = (x, y) => float.MaxValue;
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad + 0.35f;
            float ex = 60 + Mathf.Cos(a) * 42, ey = 60 + Mathf.Sin(a) * 42;
            float sx = 60 + Mathf.Cos(a + 0.9f) * 12, sy = 60 + Mathf.Sin(a + 0.9f) * 12;
            var blade = Sdf.Capsule(sx, sy, ex, ey, 11);
            var prev = blades;
            blades = (x, y) => Mathf.Min(prev(x, y), blade(x, y));
        }
        c.Fill(blades, SkyBlue);
        c.Fill(Sdf.Circle(60, 60, 15), Navy);
        c.Fill(Sdf.Circle(60, 60, 6), Silver);
        c.Save("FanRotor");
    }

    static void FanHousing()
    {
        var c = new Canvas(140, 140);
        c.Fill(Sdf.Circle(70, 70, 66), Navy.WithAlpha(0.35f));
        c.Fill(Sdf.Ring(70, 70, 62, 12), Navy);
        c.Fill(Sdf.Ring(70, 70, 62, 3), SkyBlue.WithAlpha(0.6f));
        c.Save("FanHousing");
    }

    static void Chevron()
    {
        var c = new Canvas(80, 80);
        var a = Sdf.Capsule(22, 66, 58, 40, 9);
        var b = Sdf.Capsule(22, 14, 58, 40, 9);
        c.Fill((x, y) => Mathf.Min(a(x, y), b(x, y)), White);
        c.Save("Chevron");
    }

    // 9-sliced white band tinted per zone.
    static void Band()
    {
        var c = new Canvas(200, 120);
        c.Fill(Sdf.RoundedRect(100, 60, 96, 56, 28), White.WithAlpha(0.22f));
        c.Fill(Sdf.RingRect(100, 60, 96, 56, 28, 4), White.WithAlpha(0.55f));
        c.Save("Band", border: new Vector4(40, 40, 40, 40));
    }

    static void Magnet()
    {
        var c = new Canvas(160, 180);
        float cx = 80, cy = 96, r = 50, t = 34;
        var arch = Sdf.Intersect(Sdf.Ring(cx, cy, r, t), (x, y) => cy - y);   // upper half only
        var legL = Sdf.RoundedRect(cx - r, cy - 30, t / 2, 32, 4);
        var legR = Sdf.RoundedRect(cx + r, cy - 30, t / 2, 32, 4);
        var body = Sdf.Union(arch, Sdf.Union(legL, legR));
        c.Fill(Sdf.Offset(body, 5), RedDark);
        c.Fill(body, Red);
        c.Fill(Sdf.RoundedRect(cx - r, cy - 48, t / 2 - 2, 14, 3), Silver);
        c.Fill(Sdf.RoundedRect(cx + r, cy - 48, t / 2 - 2, 14, 3), Silver);
        c.Fill(Sdf.Capsule(cx - 22, cy + 34, cx + 22, cy + 46, 5), White.WithAlpha(0.5f)); // highlight
        c.Save("Magnet");
    }

    static void Ring()
    {
        var c = new Canvas(256, 256);
        c.Fill(Sdf.Ring(128, 128, 120, 8), White);
        c.Fill(Sdf.Circle(128, 128, 116), White.WithAlpha(0.12f));
        c.Save("Ring");
    }

    static void Dot()
    {
        var c = new Canvas(64, 64);
        c.Fill(Sdf.Circle(32, 32, 30), White);
        c.Save("Dot");
    }

    static void LaserEmitter()
    {
        var c = new Canvas(100, 100);
        c.Fill(Sdf.RoundedRect(50, 50, 34, 44, 10), Gunmetal);
        c.Fill(Sdf.RingRect(50, 50, 34, 44, 10, 3), Navy);
        c.Fill(Sdf.Circle(50, 50, 24), Hex("15151F"));
        c.Save("LaserEmitter");
    }

    // Bright core fading out vertically; stretched along X by the gate.
    static void Beam()
    {
        var c = new Canvas(32, 32);
        c.FillGradientY(y => Mathf.Pow(1f - Mathf.Abs(y - 16f) / 16f, 1.6f), White);
        c.Save("Beam");
    }

    static void IcePatch()
    {
        var c = new Canvas(240, 140);
        c.Fill(Sdf.RoundedRect(120, 70, 116, 66, 40), Ice.WithAlpha(0.78f));
        c.Fill(Sdf.RingRect(120, 70, 116, 66, 40, 4), White.WithAlpha(0.9f));
        c.Fill(Sdf.Capsule(40, 40, 80, 100, 4), White.WithAlpha(0.7f));
        c.Fill(Sdf.Capsule(70, 30, 100, 76, 3), White.WithAlpha(0.5f));
        c.Fill(Sdf.Capsule(150, 40, 200, 110, 4), White.WithAlpha(0.7f));
        c.Save("IcePatch", border: new Vector4(50, 50, 50, 50));
    }

    static void JetBoost()
    {
        var c = new Canvas(150, 150);
        c.Fill(Sdf.Circle(75, 75, 72), YellowDark);
        c.Fill(Sdf.Circle(75, 75, 64), Yellow);
        // Double up-chevron
        var c1 = Sdf.Union(Sdf.Capsule(45, 62, 75, 92, 9), Sdf.Capsule(75, 92, 105, 62, 9));
        var c2 = Sdf.Union(Sdf.Capsule(45, 36, 75, 66, 9), Sdf.Capsule(75, 66, 105, 36, 9));
        c.Fill(Sdf.Union(c1, c2), Ink);
        c.Fill(Sdf.Circle(52, 100, 9), White.WithAlpha(0.8f));
        c.Save("JetBoost");
    }

    static void Shield()
    {
        var c = new Canvas(150, 150);
        c.Fill(Sdf.Circle(75, 75, 72), CyanDark);
        c.Fill(Sdf.Circle(75, 75, 64), Cyan);
        c.Fill(Sdf.Ring(75, 75, 34, 11), White);
        c.Fill(Sdf.Circle(75, 75, 13), White);
        c.Fill(Sdf.Circle(52, 100, 9), White.WithAlpha(0.8f));
        c.Save("Shield");
    }

    static void Bubble()
    {
        var c = new Canvas(200, 200);
        c.Fill(Sdf.Circle(100, 100, 94), Cyan.WithAlpha(0.28f));
        c.Fill(Sdf.Ring(100, 100, 92, 7), White.WithAlpha(0.95f));
        c.Fill(Sdf.Intersect(Sdf.Ring(100, 100, 74, 8), (x, y) => Mathf.Max(x - 92, 100 - y)), White.WithAlpha(0.75f)); // upper-left highlight arc
        c.Save("Bubble");
    }

    static void Spark()
    {
        var c = new Canvas(32, 32);
        c.FillRadial(16, 16, 15, White);
        c.Save("Spark");
    }

    // ---- tiny SDF rasterizer -------------------------------------------------

    static class Sdf
    {
        public static Func<float, float, float> Circle(float cx, float cy, float r)
            => (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        public static Func<float, float, float> Ring(float cx, float cy, float r, float thickness)
            => (x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - thickness * 0.5f;

        public static Func<float, float, float> RoundedRect(float cx, float cy, float halfW, float halfH, float radius)
            => (x, y) =>
            {
                float qx = Mathf.Abs(x - cx) - halfW + radius, qy = Mathf.Abs(y - cy) - halfH + radius;
                float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            };

        public static Func<float, float, float> RingRect(float cx, float cy, float halfW, float halfH, float radius, float thickness)
        {
            var box = RoundedRect(cx, cy, halfW, halfH, radius);
            return (x, y) => Mathf.Abs(box(x, y)) - thickness * 0.5f;
        }

        public static Func<float, float, float> Capsule(float ax, float ay, float bx, float by, float r)
            => (x, y) =>
            {
                float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
                float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
                float dx = pax - bax * h, dy = pay - bay * h;
                return Mathf.Sqrt(dx * dx + dy * dy) - r;
            };

        public static Func<float, float, float> Union(Func<float, float, float> a, Func<float, float, float> b) => (x, y) => Mathf.Min(a(x, y), b(x, y));
        public static Func<float, float, float> Intersect(Func<float, float, float> a, Func<float, float, float> b) => (x, y) => Mathf.Max(a(x, y), b(x, y));
        public static Func<float, float, float> Offset(Func<float, float, float> a, float grow) => (x, y) => a(x, y) - grow;
    }

    class Canvas
    {
        readonly int w, h;
        readonly Color[] px;

        public Canvas(int width, int height)
        {
            w = width; h = height;
            px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0, 0, 0, 0);
        }

        // Alpha-composites `color` wherever the SDF is inside, with a 1px AA edge.
        public void Fill(Func<float, float, float> sdf, Color color)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = sdf(x + 0.5f, y + 0.5f);
                    float coverage = Mathf.Clamp01(0.5f - d);
                    if (coverage <= 0f) continue;
                    Blend(x, y, color, coverage);
                }
        }

        public void FillGradientY(Func<float, float> alphaAtY, Color color)
        {
            for (int y = 0; y < h; y++)
            {
                float a = Mathf.Clamp01(alphaAtY(y + 0.5f));
                for (int x = 0; x < w; x++) Blend(x, y, color, a);
            }
        }

        public void FillRadial(float cx, float cy, float r, Color color)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy)) / r;
                    Blend(x, y, color, Mathf.Clamp01(1f - d * d));
                }
        }

        void Blend(int x, int y, Color c, float coverage)
        {
            float a = c.a * coverage;
            var dst = px[y * w + x];
            float outA = a + dst.a * (1f - a);
            if (outA <= 0f) return;
            px[y * w + x] = new Color(
                (c.r * a + dst.r * dst.a * (1f - a)) / outA,
                (c.g * a + dst.g * dst.a * (1f - a)) / outA,
                (c.b * a + dst.b * dst.a * (1f - a)) / outA,
                outA);
        }

        public void Save(string name, Vector4? border = null)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            string path = Folder + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
            if (border.HasValue) importer.spriteBorder = border.Value;
            importer.SaveAndReimport();
        }
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }

    static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);
}
