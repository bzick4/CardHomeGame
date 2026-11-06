using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardHome
{
    // Арт карт гостей в стиле мультяшной ККИ: пейзаж с замком и горами, персонаж в полный рост,
    // лента-стрелка под имя, солнце и луна в углах, рубашка карты. Всё нарисовано кодом.
    //
    // Свой арт: положи PNG (тип Sprite) в Assets/Resources/Art/Cards/<Id>.png — персонаж в полный рост
    // на прозрачном фоне (Traveler, Scholar, Ghost, Thief, Wolf, Inspector, Couple, Drunkard, GhostLord).
    // Фон карты — Art/Cards/Backdrop.png, рубашка — Art/Cards/Back.png.
    public static class GuestArt
    {
        // Файл из Resources/Art/Cards/<key>, иначе рисуем. Хранение и выгрузка — в ArtStore.
        static Sprite Get(string key, Func<Sprite> make) => ArtStore.Get("Cards/" + key, make);

        public static Sprite Backdrop => Get("Backdrop", () => PaintBackdrop().Render(256, 320, 1f, 0.5f));
        public static Sprite Character(GuestDef g) => Get(g.Id, () => PaintCharacter(g.Id).Render(256, 256, 1.05f, 0.6f));
        public static Sprite Banner => Get("Banner", () => new Painter()
            .Poly(Parchment, V(-0.98f, -0.2f), V(0.72f, -0.2f), V(0.98f, 0f), V(0.72f, 0.2f), V(-0.98f, 0.2f))
            .Poly(ParchmentDark, false, V(-0.9f, -0.13f), V(0.68f, -0.13f), V(0.7f, -0.16f), V(-0.9f, -0.16f))
            .Render(256, 64, 1.02f, 0.5f));
        public static Sprite Sun => Get("Sun", PaintSun);
        public static Sprite Moon => Get("Moon", PaintMoon);
        static Sprite Back => Get("Back", () => PaintBack().Render(160, 224, 1.02f, 0.6f));
        public static Sprite Role(GuestRole r) => Get("Role" + r, () => PaintRole(r).Render(128));
        public static Sprite Lock => Get("Lock", () => new Painter()
            .Ring(0, 0.25f, 0.38f, 0.1f, H("9aa6ad"), true)
            .Box(0, -0.3f, 0.6f, 0.45f, 0.12f, Gold)
            .Circle(0, -0.22f, 0.11f, Dark, false)
            .Box(0, -0.42f, 0.05f, 0.14f, 0.02f, Dark, false)
            .Render(128));
        public static Sprite Star => Get("Star", () => new Painter().Poly(Gold, StarPoints(0, 0, 0.92f, 0.4f)).Render(128));
        public static Sprite StarEmpty => Get("StarEmpty", () => new Painter().Poly(H("5a4632"), StarPoints(0, 0, 0.92f, 0.4f)).Render(128));

        // Карта мира на пергаменте (для выбора домов) и облако над закрытыми местами.
        public static Sprite MapPaper => Get("MapPaper", () => PaintMap().Render(384, 504, 1f, 0.22f));
        public static Sprite Cloud => Get("Cloud", () => new Painter()
            .Union(H("f3e6c6"), Painter.CircleSdf(-0.5f, -0.1f, 0.3f), Painter.CircleSdf(-0.15f, 0.12f, 0.4f),
                Painter.CircleSdf(0.3f, 0.05f, 0.35f), Painter.CircleSdf(0.62f, -0.12f, 0.24f),
                Painter.BoxSdf(0.05f, -0.22f, 0.75f, 0.14f, 0.1f, 0))
            .Union(H("fff8e6"), false, Painter.CircleSdf(-0.2f, 0.2f, 0.22f), Painter.CircleSdf(0.25f, 0.14f, 0.18f))
            .Render(160, 96, 1.05f, 0.7f));

        // Карта на пергаменте: спокойный фон, тонкие штрихи холмов и деревьев, река, роза ветров в углу.
        static Painter PaintMap()
        {
            var p = new Painter();
            p.Box(0, 0, 1.05f, 1.4f, 0, H("e3cc98"), false);
            p.Box(0, 0, 0.95f, 1.22f, 0.05f, H("f2e3bd"), false);

            var water = H("b9d6d8");
            var river = new[] { V(-1.1f, 0.95f), V(-0.62f, 0.7f), V(-0.7f, 0.25f), V(-0.3f, -0.1f), V(-0.45f, -0.6f), V(-0.1f, -1.4f) };
            for (int i = 0; i < river.Length - 1; i++)
                p.Capsule(river[i].x, river[i].y, river[i + 1].x, river[i + 1].y, 0.035f, water, false);

            var hill = H("e6d3a6");
            foreach (var (x, y, w) in new[] { (0.62f, 0.98f, 0.1f), (0.78f, 0.9f, 0.08f), (0.55f, 0.2f, 0.08f), (0.82f, -0.35f, 0.09f),
                         (-0.82f, -0.25f, 0.08f), (0.7f, -0.95f, 0.09f), (-0.72f, 1.08f, 0.07f), (0.2f, -1.12f, 0.07f) })
                p.Poly(hill, V(x - w, y), V(x, y + w * 0.9f), V(x + w, y));

            foreach (var (x, y) in new[] { (-0.85f, 0.5f), (-0.78f, 0.42f), (0.86f, 0.42f), (0.86f, -0.6f), (-0.86f, -0.92f),
                         (0.45f, -0.5f), (0.87f, -1.12f), (-0.2f, 1.13f), (-0.52f, -1.12f) })
            {
                p.Box(x, y - 0.035f, 0.008f, 0.025f, 0, H("b08a55"), false);
                p.Poly(H("c7d39a"), V(x - 0.045f, y - 0.02f), V(x, y + 0.08f), V(x + 0.045f, y - 0.02f));
            }

            // Роза ветров в углу.
            p.Poly(H("d9b47a"), V(0.78f, 1.25f), V(0.81f, 1.15f), V(0.9f, 1.12f), V(0.81f, 1.09f), V(0.78f, 0.99f),
                V(0.75f, 1.09f), V(0.66f, 1.12f), V(0.75f, 1.15f));
            return p;
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);
        static Color H(string hex) => RoomDatabase.Hex(hex);

        static readonly Color Parchment = H("f3d79b"), ParchmentDark = H("d9b46a");
        static readonly Color Gold = H("ffcf3a"), Dark = H("2b2420"), White = H("fbf7ee");
        static readonly Color Skin = H("f2c49b"), SkinShade = H("dba27a"), Blush = H("f08a7a");

        static Vector2[] StarPoints(float cx, float cy, float outer, float inner)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 5;
                float r = i % 2 == 0 ? outer : inner;
                pts[i] = V(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            return pts;
        }

        // ───────────── Пейзаж ─────────────

        static Painter PaintBackdrop()
        {
            var p = new Painter();
            // Небо и облака.
            p.Box(0, 0, 1.2f, 1.4f, 0, H("aedcf0"), false);
            p.Union(White, false, Painter.CircleSdf(-0.6f, 1.02f, 0.14f), Painter.CircleSdf(-0.42f, 1.08f, 0.17f),
                Painter.CircleSdf(-0.24f, 1.02f, 0.12f));
            p.Union(White, false, Painter.CircleSdf(0.35f, 0.9f, 0.1f), Painter.CircleSdf(0.5f, 0.95f, 0.13f),
                Painter.CircleSdf(0.66f, 0.9f, 0.09f));

            // Горы со снежными шапками.
            var rock = H("c8bfd4");
            p.Poly(rock, V(-0.1f, 0.15f), V(0.5f, 0.95f), V(1.1f, 0.15f));
            p.Poly(White, V(0.36f, 0.77f), V(0.5f, 0.95f), V(0.64f, 0.77f), V(0.57f, 0.71f), V(0.5f, 0.77f), V(0.43f, 0.7f));
            p.Poly(H("b5acc4"), V(0.55f, 0.15f), V(0.95f, 0.7f), V(1.3f, 0.15f));
            p.Poly(White, V(0.86f, 0.58f), V(0.95f, 0.7f), V(1.04f, 0.58f), V(0.98f, 0.53f), V(0.92f, 0.56f));

            // Замок вдали.
            var wall = H("e6c3b3");
            var roof = H("c26d61");
            p.Box(-0.6f, 0.22f, 0.26f, 0.14f, 0.01f, wall);
            foreach (float x in new[] { -0.84f, -0.6f, -0.36f })
            {
                float top = x == -0.6f ? 0.6f : 0.48f;
                p.Box(x, (top + 0.12f) / 2, 0.07f, (top - 0.12f) / 2, 0.01f, wall);
                p.Poly(roof, V(x - 0.1f, top), V(x, top + 0.2f), V(x + 0.1f, top));
            }
            p.Box(-0.6f, 0.17f, 0.05f, 0.08f, 0.05f, H("7a5040"));

            // Холмы, деревья, домики.
            p.Union(H("b7d07a"), Painter.CircleSdf(-0.8f, -0.55f, 0.85f), Painter.CircleSdf(0.7f, -0.6f, 0.9f));
            p.Union(H("6f9a4a"), Painter.CircleSdf(-0.95f, 0.18f, 0.16f), Painter.CircleSdf(-0.82f, 0.25f, 0.13f));
            p.Box(0.62f, 0.12f, 0.12f, 0.08f, 0.01f, White);
            p.Poly(H("c26d61"), V(0.47f, 0.2f), V(0.62f, 0.32f), V(0.77f, 0.2f));
            p.Box(0.84f, 0.08f, 0.09f, 0.06f, 0.01f, White);
            p.Poly(H("c26d61"), V(0.73f, 0.14f), V(0.84f, 0.24f), V(0.95f, 0.14f));

            // Луг и извилистая тропа к зрителю.
            p.Box(0, -0.8f, 1.2f, 0.85f, 0, H("cfe29a"), false);
            p.Poly(H("e8d49a"), V(-0.05f, 0.05f), V(0.08f, 0.05f), V(0.25f, -0.3f), V(-0.05f, -0.7f), V(0.35f, -1.3f),
                V(-0.55f, -1.3f), V(-0.35f, -0.7f), V(-0.15f, -0.3f));
            p.Union(H("9cbf62"), Painter.CircleSdf(-0.9f, -0.45f, 0.13f), Painter.CircleSdf(-0.78f, -0.5f, 0.1f));
            p.Union(H("9cbf62"), Painter.CircleSdf(0.85f, -0.2f, 0.12f), Painter.CircleSdf(0.95f, -0.26f, 0.09f));
            p.Box(-0.5f, -0.2f, 0.04f, 0.015f, 0.01f, H("8fb15a"), false);
            p.Box(0.55f, -0.75f, 0.05f, 0.015f, 0.01f, H("8fb15a"), false);
            return p;
        }

        // ───────────── Персонажи ─────────────

        // Лицо: глаза, нос, улыбка, румянец.
        static void Face(Painter p, float x, float y, float r, bool smile = true, bool closedEyes = false)
        {
            float e = r * 0.38f;
            if (closedEyes)
            {
                p.Capsule(x - e - r * 0.08f, y + r * 0.08f, x - e + r * 0.08f, y + r * 0.08f, r * 0.04f, Dark, false);
                p.Capsule(x + e - r * 0.08f, y + r * 0.08f, x + e + r * 0.08f, y + r * 0.08f, r * 0.04f, Dark, false);
            }
            else
            {
                p.Circle(x - e, y + r * 0.08f, r * 0.09f, Dark, false);
                p.Circle(x + e, y + r * 0.08f, r * 0.09f, Dark, false);
            }
            p.Oval(x - e - r * 0.08f, y - r * 0.25f, r * 0.13f, r * 0.08f, Blush, false);
            p.Oval(x + e + r * 0.08f, y - r * 0.25f, r * 0.13f, r * 0.08f, Blush, false);
            p.Oval(x + r * 0.05f, y - r * 0.08f, r * 0.12f, r * 0.15f, SkinShade);
            if (smile) p.Capsule(x - r * 0.18f, y - r * 0.42f, x + r * 0.18f, y - r * 0.38f, r * 0.045f, Dark, false);
            else p.Capsule(x - r * 0.15f, y - r * 0.42f, x + r * 0.15f, y - r * 0.42f, r * 0.04f, Dark, false);
        }

        static void Legs(Painter p, Color pants, Color boots, float top = -0.4f, float spread = 0.11f)
        {
            p.Capsule(-spread, top, -spread - 0.02f, -0.82f, 0.075f, pants);
            p.Capsule(spread, top, spread + 0.02f, -0.82f, 0.075f, pants);
            p.Box(-spread - 0.05f, -0.88f, 0.12f, 0.065f, 0.05f, boots);
            p.Box(spread + 0.05f, -0.88f, 0.12f, 0.065f, 0.05f, boots);
        }

        static void Arm(Painter p, float sx, float sy, float hx, float hy, Color sleeve, float r = 0.075f)
        {
            p.Capsule(sx, sy, hx, hy, r, sleeve);
            p.Circle(hx, hy, r * 0.85f, Skin);
        }

        static Painter PaintCharacter(string id)
        {
            var p = new Painter();
            switch (id)
            {
                case "Traveler": // путник: шляпа, плащ, рюкзак и посох
                {
                    p.Box(0.3f, 0.02f, 0.17f, 0.24f, 0.06f, H("8a5a2b"));
                    p.Box(0.3f, 0.2f, 0.17f, 0.05f, 0.03f, H("a86d35"));
                    Legs(p, H("5a4632"), H("3a2a20"));
                    p.Capsule(-0.5f, 0.42f, -0.46f, -0.95f, 0.035f, H("8a5a2b"));
                    p.Poly(H("6f8a4a"), V(-0.24f, 0.2f), V(0.24f, 0.2f), V(0.32f, -0.52f), V(-0.32f, -0.52f));
                    p.Box(0, -0.16f, 0.29f, 0.045f, 0, H("4a3020"));
                    p.Box(0.08f, -0.16f, 0.05f, 0.05f, 0.01f, Gold);
                    Arm(p, -0.22f, 0.13f, -0.47f, -0.12f, H("6f8a4a"));
                    Arm(p, 0.22f, 0.13f, 0.34f, -0.25f, H("6f8a4a"));
                    p.Box(0, 0.23f, 0.15f, 0.05f, 0.03f, H("c8483c"));
                    p.Circle(0, 0.46f, 0.22f, Skin);
                    Face(p, 0, 0.44f, 0.22f);
                    p.Capsule(-0.36f, 0.6f, 0.36f, 0.6f, 0.05f, H("8a5a2b"));
                    p.Box(0, 0.72f, 0.17f, 0.13f, 0.07f, H("a86d35"));
                    p.Box(0, 0.65f, 0.17f, 0.03f, 0, H("c8483c"), false);
                    break;
                }

                case "Scholar": // учёный: мантия, шапочка, очки, книга, борода
                {
                    Legs(p, H("3a3570"), H("2b2420"), -0.6f, 0.09f);
                    p.Poly(H("5c4f9e"), V(-0.22f, 0.22f), V(0.22f, 0.22f), V(0.38f, -0.84f), V(-0.38f, -0.84f));
                    p.Poly(H("4a3f85"), false, V(-0.03f, 0.2f), V(0.03f, 0.2f), V(0.06f, -0.82f), V(-0.06f, -0.82f));
                    Arm(p, -0.2f, 0.14f, -0.16f, -0.18f, H("5c4f9e"));
                    Arm(p, 0.2f, 0.14f, 0.16f, -0.18f, H("5c4f9e"));
                    p.Box(0, -0.2f, 0.24f, 0.17f, 0.03f, H("8f3b3b"), true, -8);
                    p.Box(0.02f, -0.19f, 0.2f, 0.13f, 0.02f, White, false, -8);
                    p.Box(0.02f, -0.19f, 0.01f, 0.13f, 0, H("8f3b3b"), false, -8);
                    p.Circle(0, 0.46f, 0.22f, Skin);
                    Face(p, 0, 0.46f, 0.22f);
                    p.Poly(White, V(-0.17f, 0.36f), V(0.17f, 0.36f), V(0.1f, 0.12f), V(0, 0.05f), V(-0.1f, 0.12f));
                    p.Ring(-0.085f, 0.48f, 0.065f, 0.018f, Dark);
                    p.Ring(0.085f, 0.48f, 0.065f, 0.018f, Dark);
                    p.Box(0, 0.66f, 0.17f, 0.06f, 0.02f, H("2c2860"));
                    p.Box(0, 0.73f, 0.3f, 0.035f, 0.01f, H("2c2860"), true, 3);
                    p.Capsule(0.27f, 0.73f, 0.3f, 0.5f, 0.02f, Gold, false);
                    p.Circle(0.3f, 0.48f, 0.04f, Gold, false);
                    break;
                }

                case "Ghost": // призрак: простыня с волнистым краем и цепь
                {
                    var sheet = H("eef6fa");
                    p.Capsule(-0.5f, -0.55f, -0.25f, -0.7f, 0.035f, H("9aa6ad"));
                    p.Circle(-0.54f, -0.53f, 0.07f, H("6d767d"));
                    p.Union(sheet,
                        Painter.CircleSdf(0, 0.3f, 0.45f),
                        Painter.BoxSdf(0, -0.25f, 0.45f, 0.55f, 0.05f, 0),
                        Painter.CircleSdf(-0.3f, -0.78f, 0.15f),
                        Painter.CircleSdf(0, -0.8f, 0.15f),
                        Painter.CircleSdf(0.3f, -0.78f, 0.15f),
                        Painter.CapsuleSdf(-0.4f, 0.0f, -0.62f, -0.25f, 0.1f),
                        Painter.CapsuleSdf(0.4f, 0.0f, 0.62f, 0.15f, 0.1f));
                    p.Oval(-0.16f, 0.32f, 0.08f, 0.13f, Dark, false);
                    p.Oval(0.16f, 0.32f, 0.08f, 0.13f, Dark, false);
                    p.Circle(-0.14f, 0.36f, 0.025f, White, false);
                    p.Circle(0.18f, 0.36f, 0.025f, White, false);
                    p.Oval(0, 0.05f, 0.09f, 0.12f, Dark, false);
                    p.Oval(-0.3f, 0.15f, 0.07f, 0.04f, H("cfe3ef"), false);
                    p.Oval(0.3f, 0.15f, 0.07f, 0.04f, H("cfe3ef"), false);
                    break;
                }

                case "Thief": // вор: капюшон, маска, два кинжала, пояс с зельем
                {
                    var cloak = H("2f5a4a");
                    p.Poly(H("244a3c"), V(-0.36f, 0.4f), V(0.36f, 0.4f), V(0.5f, -0.85f), V(0.1f, -0.7f), V(-0.2f, -0.86f), V(-0.48f, -0.75f));
                    Legs(p, H("3a3030"), H("2b2420"), -0.4f, 0.12f);
                    p.Poly(H("6f5a40"), V(-0.2f, 0.22f), V(0.2f, 0.22f), V(0.24f, -0.45f), V(-0.24f, -0.45f));
                    p.Box(0, -0.18f, 0.25f, 0.05f, 0, H("3a2a20"));
                    p.Box(0, -0.18f, 0.05f, 0.05f, 0.01f, H("9aa6ad"));
                    p.Circle(0.18f, -0.3f, 0.08f, H("6ee05a"));
                    p.Box(0.18f, -0.2f, 0.025f, 0.03f, 0, H("8a5a2b"));
                    // Кинжалы.
                    p.Poly(H("d8dde0"), V(-0.5f, 0.0f), V(-0.46f, 0.0f), V(-0.48f, 0.38f));
                    p.Box(-0.48f, -0.04f, 0.06f, 0.02f, 0.01f, H("6d767d"));
                    p.Poly(H("d8dde0"), V(0.48f, 0.05f), V(0.52f, 0.05f), V(0.6f, 0.42f));
                    p.Box(0.49f, 0.01f, 0.06f, 0.02f, 0.01f, H("6d767d"), true, -12);
                    Arm(p, -0.18f, 0.15f, -0.48f, -0.08f, cloak);
                    Arm(p, 0.18f, 0.15f, 0.5f, -0.04f, cloak);
                    // Капюшон с острым кончиком и тёмное лицо в тени.
                    p.Union(cloak, Painter.CircleSdf(0, 0.45f, 0.27f),
                        Painter.PolySdf(V(-0.2f, 0.6f), V(0.05f, 0.92f), V(0.2f, 0.6f)));
                    p.Circle(0, 0.42f, 0.19f, H("3a2a2a"), false);
                    p.Box(0, 0.36f, 0.19f, 0.09f, 0.05f, H("1c1414"), false);
                    p.Oval(-0.07f, 0.47f, 0.05f, 0.025f, White, false);
                    p.Oval(0.07f, 0.47f, 0.05f, 0.025f, White, false);
                    p.Circle(-0.06f, 0.47f, 0.015f, Dark, false);
                    p.Circle(0.08f, 0.47f, 0.015f, Dark, false);
                    break;
                }

                case "Wolf": // волк на задних лапах в рваной жилетке
                {
                    var fur = H("8f8984");
                    var light = H("d8d0c8");
                    p.Capsule(0.25f, -0.45f, 0.62f, -0.1f, 0.11f, fur);
                    p.Circle(0.63f, -0.08f, 0.1f, light);
                    p.Capsule(-0.14f, -0.4f, -0.18f, -0.82f, 0.09f, fur);
                    p.Capsule(0.14f, -0.4f, 0.18f, -0.82f, 0.09f, fur);
                    p.Oval(-0.22f, -0.88f, 0.12f, 0.06f, fur);
                    p.Oval(0.22f, -0.88f, 0.12f, 0.06f, fur);
                    p.Oval(0, -0.15f, 0.3f, 0.36f, fur);
                    p.Oval(0, -0.2f, 0.17f, 0.25f, light, false);
                    p.Poly(H("8f3b3b"), V(-0.29f, 0.12f), V(-0.08f, 0.12f), V(-0.1f, -0.3f), V(-0.27f, -0.25f));
                    p.Poly(H("8f3b3b"), V(0.29f, 0.12f), V(0.08f, 0.12f), V(0.1f, -0.3f), V(0.27f, -0.25f));
                    p.Capsule(-0.26f, 0.05f, -0.45f, -0.25f, 0.08f, fur);
                    p.Capsule(0.26f, 0.05f, 0.45f, -0.25f, 0.08f, fur);
                    p.Circle(-0.45f, -0.27f, 0.075f, light);
                    p.Circle(0.45f, -0.27f, 0.075f, light);
                    p.Poly(fur, V(-0.27f, 0.45f), V(-0.3f, 0.8f), V(-0.08f, 0.6f));
                    p.Poly(fur, V(0.27f, 0.45f), V(0.3f, 0.8f), V(0.08f, 0.6f));
                    p.Poly(H("e8a0a0"), false, V(-0.24f, 0.52f), V(-0.26f, 0.7f), V(-0.14f, 0.58f));
                    p.Poly(H("e8a0a0"), false, V(0.24f, 0.52f), V(0.26f, 0.7f), V(0.14f, 0.58f));
                    p.Circle(0, 0.42f, 0.27f, fur);
                    p.Oval(0, 0.28f, 0.17f, 0.13f, light);
                    p.Oval(0, 0.36f, 0.06f, 0.04f, Dark, false);
                    p.Poly(White, false, V(-0.08f, 0.2f), V(-0.05f, 0.14f), V(-0.02f, 0.2f));
                    p.Poly(White, false, V(0.02f, 0.2f), V(0.05f, 0.14f), V(0.08f, 0.2f));
                    p.Circle(-0.11f, 0.5f, 0.055f, Gold);
                    p.Circle(0.11f, 0.5f, 0.055f, Gold);
                    p.Circle(-0.11f, 0.5f, 0.02f, Dark, false);
                    p.Circle(0.11f, 0.5f, 0.02f, Dark, false);
                    p.Capsule(-0.18f, 0.58f, -0.05f, 0.55f, 0.02f, Dark, false);
                    p.Capsule(0.18f, 0.58f, 0.05f, 0.55f, 0.02f, Dark, false);
                    break;
                }

                case "Inspector": // инспектор: котелок, костюм, монокль, планшет
                {
                    Legs(p, H("3a3a44"), H("1c1c22"));
                    p.Poly(H("4f5866"), V(-0.24f, 0.22f), V(0.24f, 0.22f), V(0.28f, -0.48f), V(-0.28f, -0.48f));
                    p.Poly(White, false, V(-0.07f, 0.22f), V(0.07f, 0.22f), V(0, 0.0f));
                    p.Poly(H("8f3b3b"), false, V(-0.03f, 0.16f), V(0.03f, 0.16f), V(0.02f, -0.02f), V(0, -0.05f), V(-0.02f, -0.02f));
                    Arm(p, -0.22f, 0.15f, -0.3f, -0.3f, H("4f5866"));
                    p.Box(0.28f, -0.05f, 0.15f, 0.2f, 0.02f, H("a86d35"), true, -10);
                    p.Box(0.29f, -0.07f, 0.11f, 0.15f, 0.01f, White, false, -10);
                    p.Box(0.29f, 0.13f, 0.06f, 0.03f, 0.01f, H("9aa6ad"), false, -10);
                    for (int i = 0; i < 3; i++)
                        p.Box(0.3f, -0.0f - i * 0.07f, 0.07f, 0.008f, 0, Dark, false, -10);
                    Arm(p, 0.22f, 0.15f, 0.2f, -0.12f, H("4f5866"));
                    p.Circle(0, 0.46f, 0.21f, Skin);
                    Face(p, 0, 0.45f, 0.21f, smile: false);
                    p.Union(H("5a3a22"), Painter.CapsuleSdf(-0.14f, 0.35f, -0.02f, 0.37f, 0.035f), Painter.CapsuleSdf(0.02f, 0.37f, 0.14f, 0.35f, 0.035f));
                    p.Ring(0.08f, 0.47f, 0.06f, 0.015f, Gold);
                    p.Capsule(-0.3f, 0.62f, 0.3f, 0.62f, 0.04f, H("2c2c34"));
                    p.Union(H("3a3a44"), Painter.CircleSdf(0, 0.68f, 0.17f), Painter.BoxSdf(0, 0.64f, 0.17f, 0.04f, 0, 0));
                    p.Box(0, 0.65f, 0.17f, 0.025f, 0, H("8f3b3b"), false);
                    break;
                }

                case "Couple": // влюблённые: двое за руку и сердечко
                {
                    // Она.
                    p.Capsule(-0.32f, -0.6f, -0.33f, -0.85f, 0.05f, Skin);
                    p.Poly(H("e57b9a"), V(-0.42f, 0.12f), V(-0.24f, 0.12f), V(-0.12f, -0.62f), V(-0.56f, -0.62f));
                    p.Box(-0.34f, -0.88f, 0.08f, 0.04f, 0.03f, H("8f3b3b"));
                    p.Union(H("c9822a"), Painter.CircleSdf(-0.33f, 0.33f, 0.19f), Painter.BoxSdf(-0.33f, 0.15f, 0.19f, 0.18f, 0.08f, 0));
                    p.Circle(-0.33f, 0.3f, 0.16f, Skin);
                    Face(p, -0.33f, 0.28f, 0.16f, closedEyes: true);
                    p.Circle(-0.2f, 0.46f, 0.05f, H("e0506e"));
                    // Он.
                    Legs(p, H("2c3a5c"), H("2b2420"), -0.4f, 0.0f);
                    p.Capsule(0.3f, -0.4f, 0.3f, -0.82f, 0.06f, H("2c3a5c"));
                    p.Capsule(0.46f, -0.4f, 0.48f, -0.82f, 0.06f, H("2c3a5c"));
                    p.Box(0.29f, -0.88f, 0.08f, 0.05f, 0.04f, H("2b2420"));
                    p.Box(0.49f, -0.88f, 0.08f, 0.05f, 0.04f, H("2b2420"));
                    p.Poly(H("4a74d4"), V(0.26f, 0.15f), V(0.52f, 0.15f), V(0.56f, -0.45f), V(0.22f, -0.45f));
                    p.Capsule(0.26f, 0.08f, -0.05f, -0.18f, 0.07f, H("4a74d4"));
                    p.Circle(-0.05f, -0.18f, 0.06f, Skin);
                    p.Circle(0.39f, 0.36f, 0.17f, Skin);
                    Face(p, 0.39f, 0.34f, 0.17f, closedEyes: true);
                    p.Union(H("6b4423"), Painter.CircleSdf(0.39f, 0.48f, 0.15f), Painter.BoxSdf(0.39f, 0.5f, 0.17f, 0.06f, 0.04f, 0));
                    // Сердце.
                    p.Union(H("e0506e"), Painter.CircleSdf(-0.06f, 0.7f, 0.1f), Painter.CircleSdf(0.1f, 0.7f, 0.1f),
                        Painter.BoxSdf(0.02f, 0.58f, 0.1f, 0.1f, 0.02f, 45));
                    break;
                }

                case "Drunkard": // пьяница: большой живот, красный нос, кружка
                {
                    Legs(p, H("5a4632"), H("3a2a20"), -0.45f, 0.14f);
                    p.Oval(0, -0.15f, 0.38f, 0.38f, H("c8483c"));
                    p.Oval(0, -0.2f, 0.22f, 0.25f, H("e8d8b8"), false);
                    p.Box(0, -0.42f, 0.36f, 0.05f, 0.02f, H("4a3020"));
                    Arm(p, -0.3f, 0.1f, -0.48f, -0.18f, H("c8483c"));
                    p.Box(0.48f, 0.0f, 0.12f, 0.16f, 0.03f, H("c98a3a"));
                    p.Union(White, Painter.CircleSdf(0.42f, 0.18f, 0.07f), Painter.CircleSdf(0.54f, 0.18f, 0.07f));
                    p.Ring(0.62f, 0.0f, 0.07f, 0.025f, H("c98a3a"), true);
                    Arm(p, 0.3f, 0.1f, 0.4f, -0.05f, H("c8483c"));
                    p.Circle(0, 0.4f, 0.22f, Skin);
                    Face(p, 0, 0.38f, 0.22f, closedEyes: true);
                    p.Circle(0.02f, 0.36f, 0.06f, H("e0503c"));
                    p.Oval(0, 0.24f, 0.17f, 0.07f, H("8a7f72"), false);
                    p.Union(H("3f8257"), Painter.BoxSdf(0, 0.58f, 0.22f, 0.07f, 0.06f, 0),
                        Painter.CapsuleSdf(0.2f, 0.58f, 0.32f, 0.5f, 0.035f));
                    break;
                }

                case "GhostLord": // хозяин дома: призрак в короне и плаще
                {
                    p.Poly(H("5a2b6f"), V(-0.42f, 0.3f), V(0.42f, 0.3f), V(0.7f, -0.9f), V(-0.7f, -0.9f));
                    p.Poly(H("8f3b9a"), false, V(-0.32f, 0.25f), V(0.32f, 0.25f), V(0.55f, -0.85f), V(-0.55f, -0.85f));
                    p.Union(H("bff0e6"),
                        Painter.CircleSdf(0, 0.3f, 0.36f),
                        Painter.BoxSdf(0, -0.25f, 0.34f, 0.5f, 0.05f, 0),
                        Painter.CircleSdf(-0.22f, -0.75f, 0.12f),
                        Painter.CircleSdf(0.0f, -0.78f, 0.12f),
                        Painter.CircleSdf(0.22f, -0.75f, 0.12f));
                    p.Oval(-0.13f, 0.32f, 0.07f, 0.11f, Dark, false);
                    p.Oval(0.13f, 0.32f, 0.07f, 0.11f, Dark, false);
                    p.Circle(-0.13f, 0.34f, 0.025f, H("ff6b5e"), false);
                    p.Circle(0.13f, 0.34f, 0.025f, H("ff6b5e"), false);
                    p.Oval(0, 0.08f, 0.1f, 0.07f, Dark, false);
                    p.Union(Gold,
                        Painter.BoxSdf(0, 0.66f, 0.24f, 0.07f, 0.01f, 0),
                        Painter.PolySdf(V(-0.24f, 0.7f), V(-0.2f, 0.9f), V(-0.1f, 0.7f)),
                        Painter.PolySdf(V(-0.08f, 0.7f), V(0, 0.95f), V(0.08f, 0.7f)),
                        Painter.PolySdf(V(0.1f, 0.7f), V(0.2f, 0.9f), V(0.24f, 0.7f)));
                    p.Circle(0, 0.66f, 0.04f, H("c8483c"), false);
                    p.Capsule(-0.55f, -0.2f, -0.3f, -0.4f, 0.03f, H("9aa6ad"));
                    p.Circle(-0.58f, -0.18f, 0.06f, H("6d767d"));
                    break;
                }
            }
            return p;
        }

        // ───────────── Мелочи ─────────────

        static Sprite PaintSun()
        {
            var p = new Painter();
            var rays = new List<Vector2>();
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8;
                float r = i % 2 == 0 ? 0.95f : 0.62f;
                rays.Add(V(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
            }
            p.Poly(H("f59a2c"), rays.ToArray());
            p.Circle(0, 0, 0.62f, H("ffd23a"));
            p.Circle(-0.18f, 0.2f, 0.16f, H("ffe98a"), false);
            return p.Render(128);
        }

        static Sprite PaintMoon()
        {
            var p = new Painter();
            p.Circle(0, 0, 0.85f, H("f6eaa0"));
            p.Circle(-0.3f, 0.25f, 0.16f, H("e2d07a"), false);
            p.Circle(0.3f, -0.15f, 0.2f, H("e2d07a"), false);
            p.Circle(-0.1f, -0.45f, 0.1f, H("e2d07a"), false);
            p.Circle(0.25f, 0.45f, 0.08f, H("e2d07a"), false);
            return p.Render(128);
        }

        // Рубашка карты гостя: тёмно-синее поле, луна, дверь и звёзды.
        static Painter PaintBack()
        {
            var p = new Painter();
            p.Box(0, 0, 1.0f, 1.4f, 0.08f, H("2c2550"));
            p.Box(0, 0, 0.86f, 1.26f, 0.06f, H("3a3170"));
            p.Box(0, 0, 0.86f, 1.26f, 0.06f, H("ffcf3a"), false);
            p.Box(0, 0, 0.8f, 1.2f, 0.05f, H("3a3170"), false);
            p.Circle(0, 0.05f, 0.62f, H("2c2550"));
            p.Union(H("6b4423"), Painter.CircleSdf(0, 0.12f, 0.28f), Painter.BoxSdf(0, -0.15f, 0.28f, 0.28f, 0.02f, 0));
            p.Union(H("8a5a2b"), false, Painter.CircleSdf(0, 0.12f, 0.2f), Painter.BoxSdf(0, -0.15f, 0.2f, 0.24f, 0.02f, 0));
            p.Circle(0.12f, -0.12f, 0.04f, Gold, false);
            p.Union(H("f6eaa0"), Painter.CircleSdf(0.42f, 0.48f, 0.17f));
            p.Circle(0.5f, 0.54f, 0.15f, H("2c2550"), false);
            foreach (var (x, y, s) in new[] { (-0.5f, 0.9f, 0.09f), (0.55f, -0.85f, 0.08f), (-0.6f, -0.6f, 0.06f), (0.62f, 0.95f, 0.05f), (-0.35f, 0.5f, 0.05f) })
                p.Poly(Gold, false, StarPoints(x, y, s, s * 0.4f));
            return p;
        }

        static Painter PaintRole(GuestRole r)
        {
            var p = new Painter();
            switch (r)
            {
                case GuestRole.Friendly: // домик с сердцем
                    p.Poly(H("c26d61"), V(-0.7f, 0.1f), V(0, 0.75f), V(0.7f, 0.1f));
                    p.Box(0, -0.35f, 0.5f, 0.45f, 0.04f, White);
                    p.Union(H("e0506e"), Painter.CircleSdf(-0.1f, -0.22f, 0.12f), Painter.CircleSdf(0.1f, -0.22f, 0.12f),
                        Painter.BoxSdf(0, -0.36f, 0.12f, 0.12f, 0.02f, 45));
                    break;
                case GuestRole.Threat: // скрещённые кинжалы
                    p.Poly(H("d8dde0"), V(-0.62f, 0.7f), V(-0.5f, 0.78f), V(0.42f, -0.36f), V(0.3f, -0.44f));
                    p.Poly(H("d8dde0"), V(0.62f, 0.7f), V(0.5f, 0.78f), V(-0.42f, -0.36f), V(-0.3f, -0.44f));
                    p.Capsule(0.28f, -0.58f, 0.55f, -0.3f, 0.07f, H("8a5a2b"));
                    p.Capsule(-0.28f, -0.58f, -0.55f, -0.3f, 0.07f, H("8a5a2b"));
                    p.Circle(0.6f, -0.72f, 0.1f, Gold);
                    p.Circle(-0.6f, -0.72f, 0.1f, Gold);
                    break;
                case GuestRole.Special: // звезда
                    p.Poly(Gold, StarPoints(0, 0, 0.85f, 0.38f));
                    break;
                default: // корона
                    p.Union(Gold,
                        Painter.BoxSdf(0, -0.35f, 0.7f, 0.22f, 0.04f, 0),
                        Painter.PolySdf(V(-0.7f, -0.2f), V(-0.62f, 0.6f), V(-0.3f, -0.2f)),
                        Painter.PolySdf(V(-0.25f, -0.2f), V(0, 0.75f), V(0.25f, -0.2f)),
                        Painter.PolySdf(V(0.3f, -0.2f), V(0.62f, 0.6f), V(0.7f, -0.2f)));
                    p.Circle(0, -0.35f, 0.1f, H("c8483c"), false);
                    break;
            }
            return p;
        }
    }
}
