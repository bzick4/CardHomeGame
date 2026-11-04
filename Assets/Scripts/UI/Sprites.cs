using UnityEngine;

namespace CardHome
{
    // Рисует мультяшные спрайты прямо в коде: скругления, тёмная обводка, объём, мягкие тени.
    // Все белые/серые — цвет задаётся через Image.color, обводка при этом остаётся тёмной.
    public static class Sprites
    {
        static Sprite panel, flat, soft, shadow, circle, shadowCircle, pill, dayBg, nightBg, light, frame;

        // Объёмная плашка: светлый верх, тёмная «губа» снизу, обводка.
        public static Sprite Panel => panel != null ? panel : panel = RoundRect(64, 18, 5, bevel: true);
        // Плоская плашка с обводкой.
        public static Sprite Flat => flat != null ? flat : flat = RoundRect(64, 18, 4, bevel: false);
        // Плоская без обводки — для подложек и «пола» комнат.
        public static Sprite Soft => soft != null ? soft : soft = RoundRect(64, 18, 0, bevel: false);
        // Размытая тень. Ставить на 10 единиц шире объекта с каждой стороны.
        public static Sprite Shadow => shadow != null ? shadow : shadow = RoundRect(64, 18, 0, bevel: false, blur: 10);
        public static Sprite Circle => circle != null ? circle : circle = RoundRect(128, 64, 7, bevel: true, sliced: false);
        public static Sprite ShadowCircle => shadowCircle != null ? shadowCircle : shadowCircle = RoundRect(64, 32, 0, bevel: false, blur: 10, sliced: false);
        // Маленькая плашка для дверей.
        public static Sprite Pill => pill != null ? pill : pill = RoundRect(32, 7, 3, bevel: false);

        public static Sprite DayBackground => dayBg != null ? dayBg : dayBg = Gradient(RoomDatabase.Hex("7cc8f2"), RoomDatabase.Hex("3b78c4"));
        public static Sprite NightBackground => nightBg != null ? nightBg : nightBg = Gradient(RoomDatabase.Hex("1b1f45"), RoomDatabase.Hex("0d0d1f"));

        // Мягкое круглое пятно света: яркий центр, плавно гаснет к краю.
        public static Sprite Light => light != null ? light : light = Radial(128);
        // Рамка без заливки — для подсветки в обучении.
        public static Sprite Frame => frame != null ? frame : frame = Ring(64, 18, 6);

        public const float ShadowPad = 10;

        static Sprite Radial(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude / half;
                float a = Mathf.Clamp01(1 - d);
                px[y * size + x] = new Color(1, 1, 1, a * a);
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        static Sprite Ring(int size, float radius, float width)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            float half = size / 2f;
            float boxHalf = half - 1.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float sd = RoundBox(new Vector2(x + 0.5f - half, y + 0.5f - half), new Vector2(boxHalf, boxHalf), radius);
                float a = Mathf.Clamp01(0.5f - sd) * Mathf.Clamp01(sd + width + 0.5f);
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            float border = radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static Sprite RoundRect(int size, float radius, float outline, bool bevel, float blur = 0, bool sliced = true)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            float half = size / 2f;
            float boxHalf = half - blur - 0.5f;
            float r = Mathf.Min(radius, boxHalf);
            var dark = new Color(0.10f, 0.07f, 0.06f, 1);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                float sd = RoundBox(p, new Vector2(boxHalf, boxHalf), r);
                Color c;

                if (blur > 0)
                {
                    float a = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(blur, -blur, sd));
                    c = new Color(1, 1, 1, a);
                }
                else
                {
                    float a = Mathf.Clamp01(0.5f - sd);
                    float b = 1;
                    if (bevel)
                    {
                        float fy = (y + 0.5f) / size;
                        b = fy < 0.22f ? 0.66f : Mathf.Lerp(0.84f, 1f, (fy - 0.22f) / 0.78f);
                        if (fy > 0.80f && sd < -outline - 2) b = 1.1f; // блик сверху
                    }
                    var fill = new Color(Mathf.Min(b, 1), Mathf.Min(b, 1), Mathf.Min(b, 1), 1);
                    if (b > 1) fill = Color.white;
                    c = outline > 0 ? Color.Lerp(fill, dark, Mathf.Clamp01(sd + outline + 0.5f)) : fill;
                    c.a = a;
                }
                px[y * size + x] = c;
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);

            float border = sliced ? Mathf.Ceil(r + blur + 2) : 0;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static float RoundBox(Vector2 p, Vector2 b, float r)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - b + new Vector2(r, r);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
        }

        static Sprite Gradient(Color top, Color bottom)
        {
            const int h = 128;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++) tex.SetPixel(0, y, Color.Lerp(bottom, top, y / (h - 1f)));
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 100);
        }
    }
}
