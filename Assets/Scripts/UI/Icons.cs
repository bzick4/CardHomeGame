using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardHome
{
    // Мультяшные иконки комнат и гостей, нарисованные кодом из простых фигур с тёмной обводкой.
    // Свой арт: положи PNG (тип Sprite) в Assets/Resources/Art/Rooms/<Тип>.png
    // или Assets/Resources/Art/Guests/<Id>.png — он заменит нарисованную иконку.
    // Имена файлов: Kitchen, Bedroom, Treasury… (как в RoomType) и Traveler, Scholar, Ghost, Thief, Wolf.
    public static class Icons
    {
        const int Size = 128;

        public static Sprite Room(RoomType t) => Get("Rooms/" + t, () => PaintRoom(t));
        public static Sprite Guest(GuestDef g) => Get("Guests/" + g.Id, () => PaintGuest(g.Id));
        public static Sprite House(HouseDef h) => Get("Houses/" + h.Id, () => PaintRoom(h.IconRoom));
        // Трещины для сломанной комнаты (полупрозрачные, поверх иконки).
        public static Sprite Crack => UiArt.Crack ?? Get("Crack", PaintCrack);
        // Картинка комнаты для лица карты: Art/RoomArt/<Тип>.png (квадрат с прозрачными углами). Нет — null.
        public static Sprite RoomArt(RoomType t) => ArtStore.Get("RoomArt/" + t, null);
        // Пергамент под описание (растягивается по 9 частям). Нет — null.
        public static Sprite TextPanel => ArtStore.Get("Cards/TextPanel", null);

        public static Sprite Coin => UiArt.Icon("Gold") ?? Get("Coin", () => new Painter().Circle(0, 0, 0.8f, Gold).Circle(0, 0, 0.5f, H("ffe27a"), false).Box(0, 0, 0.12f, 0.36f, 0.05f, H("e0a91f"), false));

        static Painter PaintCrack()
        {
            var p = new Painter();
            var c = H("1c1410");
            p.Capsule(-0.7f, 0.75f, -0.25f, 0.2f, 0.05f, c, false);
            p.Capsule(-0.25f, 0.2f, -0.4f, -0.2f, 0.05f, c, false);
            p.Capsule(-0.4f, -0.2f, 0.1f, -0.55f, 0.05f, c, false);
            p.Capsule(-0.25f, 0.2f, 0.35f, 0.3f, 0.04f, c, false);
            p.Capsule(0.35f, 0.3f, 0.7f, 0.7f, 0.04f, c, false);
            p.Capsule(0.1f, -0.55f, 0.2f, -0.85f, 0.04f, c, false);
            p.Capsule(0.35f, 0.3f, 0.55f, -0.1f, 0.035f, c, false);
            return p;
        }

        // Файл из Resources/Art/<key>, иначе рисуем. Хранение и выгрузка — в ArtStore.
        static Sprite Get(string key, Func<Painter> paint) => ArtStore.Get(key, () => paint().Render(Size));

        static Color H(string hex) => RoomDatabase.Hex(hex);

        static readonly Color Wood = H("b06f3c"), WoodDark = H("7a4824"), WoodLight = H("d39257");
        static readonly Color Gold = H("ffcf3a"), White = H("fbf7ee"), Gray = H("9aa6ad"), Dark = H("2b2420");
        static readonly Color Skin = H("f2c49b");

        static Painter PaintRoom(RoomType t)
        {
            var p = new Painter();
            switch (t)
            {
                case RoomType.Hall: // дверь
                    p.Box(0, -0.05f, 0.58f, 0.88f, 0.1f, WoodDark);
                    p.Box(0, -0.1f, 0.44f, 0.78f, 0.06f, Wood);
                    p.Box(0, 0.3f, 0.28f, 0.2f, 0.05f, WoodLight);
                    p.Box(0, -0.35f, 0.28f, 0.22f, 0.05f, WoodLight);
                    p.Circle(0.28f, -0.05f, 0.09f, Gold);
                    break;

                case RoomType.Corridor: // дорожка
                    p.Box(0, 0, 0.42f, 0.9f, 0.08f, H("c8483c"));
                    p.Box(0, 0.5f, 0.42f, 0.07f, 0, Gold, false);
                    p.Box(0, -0.5f, 0.42f, 0.07f, 0, Gold, false);
                    p.Box(0, 0, 0.12f, 0.12f, 0.02f, Gold, false, 45);
                    break;

                case RoomType.Crossroad: // крестовая дорожка
                    p.Union(H("c8483c"),
                        Painter.BoxSdf(0, 0, 0.36f, 0.9f, 0.08f, 0),
                        Painter.BoxSdf(0, 0, 0.9f, 0.36f, 0.08f, 0));
                    p.Box(0, 0, 0.2f, 0.2f, 0.03f, Gold, true, 45);
                    break;

                case RoomType.Kitchen: // кастрюля с паром
                    p.Capsule(-0.25f, 0.55f, -0.15f, 0.88f, 0.07f, White);
                    p.Capsule(0.18f, 0.55f, 0.28f, 0.88f, 0.07f, White);
                    p.Capsule(-0.88f, -0.05f, -0.6f, -0.05f, 0.09f, Dark);
                    p.Capsule(0.6f, -0.05f, 0.88f, -0.05f, 0.09f, Dark);
                    p.Box(0, -0.22f, 0.64f, 0.42f, 0.2f, H("c3ced6"));
                    p.Box(0, -0.08f, 0.6f, 0.06f, 0, H("8d9aa5"), false);
                    p.Box(0, 0.3f, 0.72f, 0.09f, 0.08f, H("dfe6ea"));
                    p.Circle(0, 0.46f, 0.1f, Dark);
                    break;

                case RoomType.Dining: // тарелка с вилкой и ножом
                    p.Capsule(-0.86f, -0.6f, -0.86f, 0.6f, 0.07f, Gray);
                    p.Capsule(0.86f, -0.6f, 0.86f, 0.6f, 0.09f, Gray);
                    p.Circle(0, 0, 0.64f, White);
                    p.Circle(0, 0, 0.44f, H("efe4d0"), false);
                    p.Circle(0, 0, 0.27f, H("e0793a"));
                    p.Circle(-0.08f, 0.08f, 0.08f, H("ffb37a"), false);
                    break;

                case RoomType.Bedroom: // кровать
                    p.Box(-0.66f, -0.6f, 0.06f, 0.12f, 0.03f, WoodDark);
                    p.Box(0.78f, -0.6f, 0.06f, 0.12f, 0.03f, WoodDark);
                    p.Box(-0.74f, 0.0f, 0.13f, 0.58f, 0.1f, Wood);
                    p.Box(0.06f, -0.38f, 0.82f, 0.18f, 0.06f, Wood);
                    p.Box(0.1f, -0.1f, 0.76f, 0.2f, 0.14f, H("5b7fd6"));
                    p.Box(-0.4f, 0.16f, 0.24f, 0.14f, 0.13f, White);
                    break;

                case RoomType.Bath: // ванна с пеной
                    p.Capsule(0.6f, 0.05f, 0.6f, 0.55f, 0.07f, Gray);
                    p.Capsule(0.6f, 0.55f, 0.32f, 0.55f, 0.07f, Gray);
                    p.Circle(-0.42f, 0.25f, 0.18f, H("e8f6ff"));
                    p.Circle(-0.12f, 0.36f, 0.16f, H("e8f6ff"));
                    p.Circle(0.15f, 0.24f, 0.17f, H("e8f6ff"));
                    p.Circle(-0.6f, -0.62f, 0.1f, Gold);
                    p.Circle(0.6f, -0.62f, 0.1f, Gold);
                    p.Box(0, -0.22f, 0.86f, 0.34f, 0.3f, White);
                    p.Box(0, 0.02f, 0.74f, 0.07f, 0.05f, H("7ec8e3"), false);
                    break;

                case RoomType.Library: // книги на полке
                    p.Box(-0.56f, -0.05f, 0.15f, 0.6f, 0.04f, H("d0473c"));
                    p.Box(-0.22f, -0.15f, 0.15f, 0.5f, 0.04f, H("4caf6a"));
                    p.Box(0.1f, 0.0f, 0.14f, 0.65f, 0.04f, H("4a74d4"));
                    p.Box(0.5f, -0.14f, 0.15f, 0.52f, 0.04f, H("f2b53a"), true, -16);
                    p.Box(-0.56f, 0.3f, 0.15f, 0.05f, 0, Gold, false);
                    p.Box(-0.22f, 0.15f, 0.15f, 0.05f, 0, Gold, false);
                    p.Box(0.1f, 0.38f, 0.14f, 0.05f, 0, Gold, false);
                    p.Box(0, -0.72f, 0.92f, 0.08f, 0.03f, WoodDark);
                    break;

                case RoomType.Fireplace: // огонь на поленьях
                    p.Capsule(-0.6f, -0.62f, 0.6f, -0.48f, 0.13f, Wood);
                    p.Capsule(-0.6f, -0.48f, 0.6f, -0.62f, 0.13f, WoodDark);
                    p.Union(H("f2602c"),
                        Painter.CircleSdf(0, -0.12f, 0.46f),
                        Painter.BoxSdf(0, 0.2f, 0.32f, 0.32f, 0.05f, 45));
                    p.Union(H("ffc23a"), false,
                        Painter.CircleSdf(0, -0.2f, 0.26f),
                        Painter.BoxSdf(0, -0.02f, 0.18f, 0.18f, 0.03f, 45));
                    break;

                case RoomType.Cellar: // бочка
                    p.Box(0, 0, 0.56f, 0.78f, 0.36f, Wood);
                    p.Box(-0.26f, 0, 0.025f, 0.7f, 0, WoodDark, false);
                    p.Box(0.26f, 0, 0.025f, 0.7f, 0, WoodDark, false);
                    p.Box(0, 0.42f, 0.57f, 0.07f, 0.02f, H("5a5f66"));
                    p.Box(0, -0.42f, 0.57f, 0.07f, 0.02f, H("5a5f66"));
                    p.Circle(0.3f, 0.05f, 0.07f, Dark, false);
                    break;

                case RoomType.Trap: // капкан
                    p.Capsule(0.45f, -0.45f, 0.85f, -0.85f, 0.06f, Gray);
                    p.Circle(0, 0, 0.66f, H("6d767d"));
                    p.Circle(0, 0, 0.48f, H("3a3f44"), false);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4;
                        p.Box(Mathf.Cos(a) * 0.42f, Mathf.Sin(a) * 0.42f, 0.1f, 0.1f, 0.02f, White, true, i * 45 + 45);
                    }
                    p.Circle(0, 0, 0.18f, H("c8483c"));
                    break;

                case RoomType.Treasury: // сундук с монетами
                    p.Circle(-0.35f, 0.58f, 0.18f, Gold);
                    p.Circle(0.35f, 0.58f, 0.18f, Gold);
                    p.Circle(0, 0.68f, 0.2f, Gold);
                    p.Box(0, -0.28f, 0.8f, 0.4f, 0.08f, Wood);
                    p.Box(0, 0.2f, 0.8f, 0.2f, 0.18f, WoodLight);
                    p.Box(-0.5f, -0.05f, 0.08f, 0.62f, 0, Gold, false);
                    p.Box(0.5f, -0.05f, 0.08f, 0.62f, 0, Gold, false);
                    p.Box(0, 0.0f, 0.15f, 0.17f, 0.04f, Gold);
                    p.Circle(0, 0.0f, 0.05f, Dark, false);
                    break;

                case RoomType.Nursery: // плюшевый мишка
                    p.Circle(-0.42f, 0.48f, 0.2f, H("c98a52"));
                    p.Circle(0.42f, 0.48f, 0.2f, H("c98a52"));
                    p.Circle(0, 0.0f, 0.58f, H("c98a52"));
                    p.Circle(-0.42f, 0.48f, 0.09f, H("f0c49a"), false);
                    p.Circle(0.42f, 0.48f, 0.09f, H("f0c49a"), false);
                    p.Oval(0, -0.2f, 0.28f, 0.22f, H("f0c49a"));
                    p.Oval(0, -0.1f, 0.1f, 0.07f, Dark, false);
                    p.Circle(-0.22f, 0.12f, 0.07f, Dark, false);
                    p.Circle(0.22f, 0.12f, 0.07f, Dark, false);
                    break;

                case RoomType.Lounge: // диван
                    p.Box(-0.62f, -0.62f, 0.06f, 0.1f, 0.03f, WoodDark);
                    p.Box(0.62f, -0.62f, 0.06f, 0.1f, 0.03f, WoodDark);
                    p.Box(0, 0.12f, 0.78f, 0.32f, 0.16f, H("4f9a68"));
                    p.Box(0, -0.28f, 0.7f, 0.2f, 0.1f, H("6cbf86"));
                    p.Box(-0.8f, -0.18f, 0.15f, 0.32f, 0.13f, H("3f8257"));
                    p.Box(0.8f, -0.18f, 0.15f, 0.32f, 0.13f, H("3f8257"));
                    p.Box(-0.3f, 0.15f, 0.18f, 0.16f, 0.12f, Gold);
                    break;
            }
            return p;
        }

        static Painter PaintGuest(string id)
        {
            var p = new Painter();
            switch (id)
            {
                case "Traveler": // лицо в шляпе
                    p.Circle(0, -0.12f, 0.6f, Skin);
                    p.Circle(-0.22f, -0.08f, 0.07f, Dark, false);
                    p.Circle(0.22f, -0.08f, 0.07f, Dark, false);
                    p.Capsule(-0.16f, -0.38f, 0.16f, -0.38f, 0.045f, Dark, false);
                    p.Capsule(-0.82f, 0.3f, 0.82f, 0.3f, 0.1f, H("8a5a2b"));
                    p.Box(0, 0.58f, 0.42f, 0.26f, 0.12f, H("a86d35"));
                    p.Box(0, 0.4f, 0.42f, 0.06f, 0, H("c8483c"), false);
                    break;

                case "Scholar": // очки и шапочка
                    p.Circle(0, -0.12f, 0.6f, Skin);
                    p.Ring(-0.25f, -0.08f, 0.17f, 0.045f, Dark);
                    p.Ring(0.25f, -0.08f, 0.17f, 0.045f, Dark);
                    p.Capsule(-0.08f, -0.06f, 0.08f, -0.06f, 0.04f, Dark, false);
                    p.Circle(-0.25f, -0.08f, 0.05f, Dark, false);
                    p.Circle(0.25f, -0.08f, 0.05f, Dark, false);
                    p.Box(0, 0.44f, 0.36f, 0.16f, 0.05f, H("3a3570"));
                    p.Box(0, 0.62f, 0.82f, 0.08f, 0.02f, H("2c2860"));
                    p.Capsule(0.6f, 0.62f, 0.7f, 0.2f, 0.04f, Gold);
                    break;

                case "Ghost": // простыня с глазами
                    p.Union(White,
                        Painter.CircleSdf(0, 0.1f, 0.6f),
                        Painter.BoxSdf(0, -0.35f, 0.6f, 0.45f, 0.12f, 0));
                    p.Oval(-0.22f, 0.15f, 0.1f, 0.16f, Dark, false);
                    p.Oval(0.22f, 0.15f, 0.1f, 0.16f, Dark, false);
                    p.Oval(0, -0.22f, 0.1f, 0.13f, Dark, false);
                    break;

                case "Thief": // шапка и маска
                    p.Circle(0, -0.12f, 0.6f, Skin);
                    p.Box(0, 0.4f, 0.62f, 0.3f, 0.3f, H("33363d"));
                    p.Box(0, -0.05f, 0.66f, 0.15f, 0.12f, Dark);
                    p.Circle(-0.25f, -0.05f, 0.08f, White, false);
                    p.Circle(0.25f, -0.05f, 0.08f, White, false);
                    p.Capsule(-0.14f, -0.42f, 0.14f, -0.36f, 0.04f, Dark, false);
                    break;

                case "Inspector": // котелок и монокль
                    p.Circle(0, -0.12f, 0.6f, Skin);
                    p.Ring(0.24f, -0.06f, 0.16f, 0.045f, Gold);
                    p.Circle(-0.24f, -0.06f, 0.07f, Dark, false);
                    p.Circle(0.24f, -0.06f, 0.06f, Dark, false);
                    p.Capsule(-0.22f, -0.4f, 0.22f, -0.4f, 0.04f, Dark, false);
                    p.Capsule(-0.3f, -0.3f, -0.05f, -0.28f, 0.05f, H("5a3a22"), false);
                    p.Capsule(0.05f, -0.28f, 0.3f, -0.3f, 0.05f, H("5a3a22"), false);
                    p.Capsule(-0.7f, 0.36f, 0.7f, 0.36f, 0.08f, H("2c2c34"));
                    p.Box(0, 0.6f, 0.42f, 0.26f, 0.22f, H("3a3a44"));
                    break;

                case "Couple": // два сердца
                    p.Union(H("e0506e"),
                        Painter.CircleSdf(-0.48f, 0.1f, 0.26f), Painter.CircleSdf(-0.12f, 0.1f, 0.26f),
                        Painter.BoxSdf(-0.3f, -0.16f, 0.27f, 0.27f, 0.04f, 45));
                    p.Union(H("ff8fa8"),
                        Painter.CircleSdf(0.12f, 0.3f, 0.26f), Painter.CircleSdf(0.48f, 0.3f, 0.26f),
                        Painter.BoxSdf(0.3f, 0.04f, 0.27f, 0.27f, 0.04f, 45));
                    p.Circle(0.38f, 0.42f, 0.07f, White, false);
                    break;

                case "Drunkard": // красный нос и кружка
                    p.Circle(-0.1f, -0.05f, 0.58f, Skin);
                    p.Circle(-0.32f, 0.05f, 0.06f, Dark, false);
                    p.Circle(0.12f, 0.05f, 0.06f, Dark, false);
                    p.Circle(-0.1f, -0.15f, 0.15f, H("e0503c"));
                    p.Capsule(-0.3f, -0.4f, 0.1f, -0.36f, 0.04f, Dark, false);
                    p.Box(0.6f, -0.35f, 0.22f, 0.3f, 0.06f, H("c98a3a"));
                    p.Box(0.6f, -0.08f, 0.24f, 0.08f, 0.06f, White);
                    p.Ring(0.88f, -0.35f, 0.12f, 0.04f, H("c98a3a"));
                    break;

                case "GhostLord": // призрак в короне
                    p.Union(H("bff0e6"),
                        Painter.CircleSdf(0, 0.0f, 0.6f),
                        Painter.BoxSdf(0, -0.45f, 0.6f, 0.4f, 0.12f, 0));
                    p.Oval(-0.22f, 0.02f, 0.11f, 0.17f, Dark, false);
                    p.Oval(0.22f, 0.02f, 0.11f, 0.17f, Dark, false);
                    p.Oval(0, -0.36f, 0.16f, 0.1f, Dark, false);
                    p.Union(Gold,
                        Painter.BoxSdf(0, 0.62f, 0.4f, 0.1f, 0.02f, 0),
                        Painter.BoxSdf(-0.3f, 0.76f, 0.1f, 0.1f, 0.02f, 45),
                        Painter.BoxSdf(0, 0.8f, 0.1f, 0.1f, 0.02f, 45),
                        Painter.BoxSdf(0.3f, 0.76f, 0.1f, 0.1f, 0.02f, 45));
                    break;

                case "Wolf": // волчья морда
                    p.Box(-0.42f, 0.5f, 0.18f, 0.26f, 0.06f, H("6f6a66"), true, 22);
                    p.Box(0.42f, 0.5f, 0.18f, 0.26f, 0.06f, H("6f6a66"), true, -22);
                    p.Circle(0, 0, 0.6f, H("8f8984"));
                    p.Oval(0, -0.26f, 0.34f, 0.26f, H("d8d0c8"));
                    p.Oval(0, -0.12f, 0.12f, 0.08f, Dark, false);
                    p.Circle(-0.25f, 0.15f, 0.1f, Gold);
                    p.Circle(0.25f, 0.15f, 0.1f, Gold);
                    p.Circle(-0.25f, 0.15f, 0.04f, Dark, false);
                    p.Circle(0.25f, 0.15f, 0.04f, Dark, false);
                    break;
            }
            return p;
        }
    }

    // Рисует фигуры по очереди (как художник слоями), у каждой — своя тёмная обводка.
    // Координаты от −1 до 1, ось Y вверх.
    public class Painter
    {
        struct Shape
        {
            public Func<Vector2, float> Sdf { get; init; }
            public Color Color { get; init; }
            public bool Outline { get; init; }
        }

        const float OutlineWidth = 0.075f;
        static readonly Color Ink = new Color(0.10f, 0.07f, 0.06f, 1);
        readonly List<Shape> shapes = new List<Shape>();

        public static Func<Vector2, float> CircleSdf(float x, float y, float r)
            => p => (p - new Vector2(x, y)).magnitude - r;

        public static Func<Vector2, float> BoxSdf(float x, float y, float hx, float hy, float r, float angleDeg)
        {
            float a = -angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            return p =>
            {
                var d = p - new Vector2(x, y);
                var q = new Vector2(Mathf.Abs(d.x * cos - d.y * sin), Mathf.Abs(d.x * sin + d.y * cos));
                q -= new Vector2(hx - r, hy - r);
                return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
            };
        }

        Painter Add(Func<Vector2, float> sdf, Color c, bool outline)
        {
            shapes.Add(new Shape { Sdf = sdf, Color = c, Outline = outline });
            return this;
        }

        public Painter Circle(float x, float y, float r, Color c, bool outline = true) => Add(CircleSdf(x, y, r), c, outline);

        public Painter Box(float x, float y, float hx, float hy, float r, Color c, bool outline = true, float angleDeg = 0)
            => Add(BoxSdf(x, y, hx, hy, Mathf.Min(r, Mathf.Min(hx, hy)), angleDeg), c, outline);

        public static Func<Vector2, float> CapsuleSdf(float ax, float ay, float bx, float by, float r)
        {
            var a = new Vector2(ax, ay);
            var ba = new Vector2(bx, by) - a;
            return p =>
            {
                var pa = p - a;
                float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(ba.sqrMagnitude, 1e-6f));
                return (pa - ba * h).magnitude - r;
            };
        }

        public Painter Capsule(float ax, float ay, float bx, float by, float r, Color c, bool outline = true)
        {
            var a = new Vector2(ax, ay);
            var ba = new Vector2(bx, by) - a;
            return Add(p =>
            {
                var pa = p - a;
                float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(ba.sqrMagnitude, 1e-6f));
                return (pa - ba * h).magnitude - r;
            }, c, outline);
        }

        public Painter Ring(float x, float y, float r, float w, Color c, bool outline = false)
            => Add(p => Mathf.Abs((p - new Vector2(x, y)).magnitude - r) - w, c, outline);

        public Painter Oval(float x, float y, float rx, float ry, Color c, bool outline = true)
            => Add(p =>
            {
                var d = p - new Vector2(x, y);
                var s = new Vector2(d.x / rx, d.y / ry);
                return (s.magnitude - 1) * Mathf.Min(rx, ry);
            }, c, outline);

        // Многоугольник по точкам (любой, в том числе невыпуклый).
        public static Func<Vector2, float> PolySdf(params Vector2[] v)
        {
            return p =>
            {
                float d = Vector2.Dot(p - v[0], p - v[0]);
                float s = 1;
                for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
                {
                    var e = v[j] - v[i];
                    var w = p - v[i];
                    var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                    d = Mathf.Min(d, Vector2.Dot(b, b));
                    bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                    if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
                }
                return s * Mathf.Sqrt(d);
            };
        }

        // Любая фигура по своей функции расстояния (например, рамка = внешний прямоугольник минус внутренний).
        public Painter Custom(Func<Vector2, float> sdf, Color c, bool outline = true) => Add(sdf, c, outline);

        public Painter Poly(Color c, bool outline, params Vector2[] pts) => Add(PolySdf(pts), c, outline);
        public Painter Poly(Color c, params Vector2[] pts) => Add(PolySdf(pts), c, true);

        // Несколько фигур как одна — с общей обводкой без швов внутри.
        public Painter Union(Color c, params Func<Vector2, float>[] parts) => Union(c, true, parts);

        public Painter Union(Color c, bool outline, params Func<Vector2, float>[] parts)
            => Add(p =>
            {
                float d = float.MaxValue;
                foreach (var f in parts) d = Mathf.Min(d, f(p));
                return d;
            }, c, outline);

        public Sprite Render(int size) => Render(size, size);

        // Ширина всегда от −pad до pad, высота — пропорционально (для 256×320: от −1.25·pad до 1.25·pad).
        public Sprite Render(int width, int height, float pad = 1.1f, float outlineScale = 1)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color[width * height];
            float pixel = 2f / width;
            float halfH = height / (float)width;
            float outlineWidth = OutlineWidth * outlineScale;

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                // Небольшой отступ, чтобы обводка не обрезалась краем.
                var p = new Vector2((x + 0.5f) * pixel - 1, (y + 0.5f) * pixel - halfH) * pad;
                var c = new Color(0, 0, 0, 0);
                foreach (var s in shapes)
                {
                    float d = s.Sdf(p);
                    if (s.Outline) c = Over(c, Ink, Mathf.Clamp01(0.5f - (d - outlineWidth) / (pixel * pad)));
                    // Лёгкий объём: верх фигуры чуть светлее.
                    var fill = Color.Lerp(s.Color * 0.88f, s.Color, Mathf.Clamp01(0.5f + p.y * 0.5f));
                    fill.a = 1;
                    c = Over(c, fill, Mathf.Clamp01(0.5f - d / (pixel * pad)));
                }
                px[y * width + x] = c;
            }

            tex.SetPixels(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100);
        }

        static Color Over(Color dst, Color src, float a)
        {
            if (a <= 0) return dst;
            float outA = a + dst.a * (1 - a);
            if (outA <= 0) return new Color(0, 0, 0, 0);
            var rgb = ((Vector4)src * a + (Vector4)dst * dst.a * (1 - a)) / outA;
            return new Color(rgb.x, rgb.y, rgb.z, outA);
        }
    }
}
