using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Окантовка рубашки — выдаётся за круг сложности.
    public class BackTier
    {
        public string Name;
        public Color Main, Dark, Gem;
        public bool Shimmer;   // бегущий блик (с изумрудной)
        public bool Glow;      // пульсирующее сияние вокруг (эпичная+)
        public bool Sparkles;  // искры (легендарная)
    }

    // Рубашки карт гостей: у каждого дома своя тема, у каждого круга — своя окантовка.
    // Соперник играет рубашкой текущего уровня; пройдя дом, игрок получает её себе.
    //
    // Код рубашки: 0 — обычная «Странник» (есть всегда), иначе 1 + тема·100 + окантовка.
    //
    // Свой арт: Assets/Resources/Art/Backs/Theme<N>.png (рисунок без рамки, 0..9 по домам, 10 — Странник)
    // и Art/Backs/Frame<N>.png (рамка с прозрачной серединой, 0..8 по кругам).
    public static class CardBacks
    {
        public const int DefaultCode = 0;
        public const int ThemeCount = 10;
        const int WanderTheme = 10;
        const float FramePad = 1.1f; // рамка рисуется с запасом: кристаллы и корона выходят за край

        public static readonly BackTier[] Tiers =
        {
            T("Дерево", "8a5a32", "5a3a20", "c9945a"),
            T("Бронза", "c27a3e", "7a4520", "e8a868"),
            T("Серебро", "c5ccd4", "6d7680", "eef3f8"),
            T("Золото", "e8b830", "8a6410", "fff0a0"),
            T("Алмаз", "a8e6ff", "3f8fb0", "f2fdff"),
            T("Рубин", "d4a62c", "7a5a10", "e0304a"),
            T("Изумруд", "d4a62c", "7a5a10", "2fd07a", shimmer: true),
            T("Эпичная", "8a4ad8", "3f1a70", "e0a8ff", shimmer: true, glow: true),
            T("Легендарная", "f2a51e", "8a4a08", "ff5a2a", shimmer: true, glow: true, sparkles: true),
        };

        static BackTier T(string name, string main, string dark, string gem, bool shimmer = false, bool glow = false, bool sparkles = false) =>
            new BackTier
            {
                Name = name, Main = RoomDatabase.Hex(main), Dark = RoomDatabase.Hex(dark), Gem = RoomDatabase.Hex(gem),
                Shimmer = shimmer, Glow = glow, Sparkles = sparkles,
            };

        public static int Code(int theme, int tier) => 1 + theme * 100 + tier;
        public static int CodeForLevel(int level) => Code(level % ThemeCount, Mathf.Min(level / ThemeCount, Tiers.Length - 1));
        public static int ThemeOf(int code) => code <= 0 ? WanderTheme : (code - 1) / 100;
        public static int TierOf(int code) => code <= 0 ? 0 : (code - 1) % 100;

        public static string Name(int code)
        {
            if (code <= 0) return "Странник";
            return $"{HouseDatabase.All[ThemeOf(code)].Name} · {Tiers[TierOf(code)].Name}";
        }

        // Какой уровень нужно пройти, чтобы получить рубашку.
        public static string Condition(int code) =>
            code <= 0 ? "Есть всегда" : $"Пройди «{HouseDatabase.Level(TierOf(code) * ThemeCount + ThemeOf(code)).Title}»";

        // ───────────── Отрисовка ─────────────

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Get(string key, Func<Sprite> make)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = Resources.Load<Sprite>("Art/Backs/" + key);
            if (s == null) s = make();
            cache[key] = s;
            return s;
        }

        public static Sprite ThemeSprite(int theme) => Get("Theme" + theme, () => PaintTheme(theme).Render(160, 224, 1f, 0.4f));
        public static Sprite FrameSprite(int tier) => Get("Frame" + tier, () => PaintFrame(tier).Render(176, 246, FramePad, 0.5f));

        // Рубашка как элемент интерфейса: рисунок, рамка и эффекты окантовки.
        public static RectTransform Create(Transform parent, int code, Vector2 size)
        {
            var root = UIKit.Rect("CardBack", parent);
            root.sizeDelta = size;
            var tier = Tiers[TierOf(code)];
            int theme = ThemeOf(code);

            if (tier.Glow)
            {
                var glow = UIKit.Image("Glow", root, tier.Gem, Sprites.Shadow);
                glow.raycastTarget = false;
                UIKit.Stretch(glow.rectTransform, -Sprites.ShadowPad - size.x * 0.06f);
                glow.DOFade(0.25f, 0.9f).From(0.85f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(glow.gameObject);
            }

            var art = UIKit.Image("Art", root, Color.white, ThemeSprite(theme));
            art.raycastTarget = false;
            UIKit.Stretch(art.rectTransform);

            if (tier.Shimmer)
            {
                // Блик бежит наискосок по рисунку и обрезается краями карты.
                var clip = UIKit.Rect("Shine", root);
                UIKit.Stretch(clip, size.x * 0.06f);
                clip.gameObject.AddComponent<RectMask2D>();
                var band = UIKit.Image("Band", clip, new Color(1, 1, 1, 0.35f), Sprites.Soft);
                band.raycastTarget = false;
                var bt = band.rectTransform;
                UIKit.Place(bt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-size.x * 1.2f, 0), new Vector2(size.x * 0.22f, size.y * 1.6f));
                bt.localEulerAngles = new Vector3(0, 0, -25);
                DOTween.Sequence()
                    .Append(bt.DOAnchorPosX(size.x * 1.2f, 1.1f).SetEase(Ease.InOutSine))
                    .AppendInterval(1.4f)
                    .SetLoops(-1, LoopType.Restart)
                    .SetLink(band.gameObject);
            }

            var frame = UIKit.Image("Frame", root, Color.white, FrameSprite(TierOf(code)));
            frame.raycastTarget = false;
            UIKit.Stretch(frame.rectTransform);
            var extra = size * (FramePad - 1) / 2;
            frame.rectTransform.offsetMin = -extra;
            frame.rectTransform.offsetMax = extra;

            if (tier.Sparkles)
            {
                var rnd = new System.Random(code);
                for (int i = 0; i < 5; i++)
                {
                    var star = UIKit.Image("Spark", root, Color.white, GuestArt.Star);
                    star.raycastTarget = false;
                    var pos = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
                    float s = size.x * (0.08f + (float)rnd.NextDouble() * 0.06f);
                    UIKit.Place(star.rectTransform, pos, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s, s));
                    star.rectTransform.localScale = Vector3.zero;
                    DOTween.Sequence()
                        .AppendInterval((float)rnd.NextDouble() * 1.5f)
                        .Append(star.rectTransform.DOScale(1, 0.35f).SetEase(Ease.OutBack))
                        .Join(star.rectTransform.DOLocalRotate(new Vector3(0, 0, 90), 0.7f))
                        .Append(star.rectTransform.DOScale(0, 0.35f))
                        .AppendInterval(0.6f + (float)rnd.NextDouble())
                        .SetLoops(-1)
                        .SetLink(star.gameObject);
                }
            }
            return root;
        }

        // ───────────── Темы ─────────────

        static Vector2 V(float x, float y) => new Vector2(x, y);
        static Color H(string hex) => RoomDatabase.Hex(hex);

        static Painter PaintTheme(int theme)
        {
            var p = new Painter();
            Color inner;
            switch (theme)
            {
                case 0: // Хостел: светлые доски и ключи
                    p.Box(0, 0, 1.05f, 1.45f, 0, H("c9945a"), false);
                    foreach (float x in new[] { -0.55f, -0.18f, 0.18f, 0.55f })
                        p.Box(x, 0, 0.012f, 1.45f, 0, H("9a6a38"), false);
                    foreach (float y in new[] { 0.95f, -0.95f })
                    {
                        p.Ring(-0.12f, y, 0.09f, 0.03f, H("ffcf3a"), true);
                        p.Box(0.08f, y, 0.16f, 0.03f, 0.01f, H("ffcf3a"));
                        p.Box(0.2f, y - 0.06f, 0.03f, 0.05f, 0, H("ffcf3a"));
                    }
                    inner = H("e8c48a");
                    break;

                case 1: // Усадьба: синий бархат с ромбами
                    p.Box(0, 0, 1.05f, 1.45f, 0, H("2c3e7a"), false);
                    for (int x = -2; x <= 2; x++)
                    for (int y = -3; y <= 3; y++)
                        p.Box(x * 0.42f + (y % 2 == 0 ? 0 : 0.21f), y * 0.42f, 0.07f, 0.07f, 0, H("4a62b0"), false, 45);
                    p.Poly(H("d4a62c"), V(-0.18f, 1.0f), V(0.18f, 1.0f), V(0.18f, 0.82f), V(0, 0.68f), V(-0.18f, 0.82f));
                    inner = H("3a52a0");
                    break;

                case 2: // Таверна: очаг и кружки
                    p.Box(0, 0, 1.05f, 1.45f, 0, H("7a2e1e"), false);
                    p.Union(H("f2602c"), false, Painter.CircleSdf(-0.4f, -1.2f, 0.3f), Painter.CircleSdf(0.0f, -1.15f, 0.35f), Painter.CircleSdf(0.4f, -1.2f, 0.3f));
                    p.Poly(H("f2602c"), false, V(-0.6f, -1.1f), V(-0.4f, -0.7f), V(-0.2f, -1.0f), V(0, -0.6f), V(0.2f, -1.0f), V(0.4f, -0.7f), V(0.6f, -1.1f));
                    p.Poly(H("ffc23a"), false, V(-0.3f, -1.15f), V(-0.15f, -0.9f), V(0, -1.05f), V(0.15f, -0.85f), V(0.3f, -1.15f));
                    foreach (float x in new[] { -0.55f, 0.55f })
                    {
                        p.Box(x, 0.95f, 0.12f, 0.16f, 0.03f, H("c98a3a"));
                        p.Box(x, 1.1f, 0.13f, 0.05f, 0.04f, H("fbf7ee"));
                    }
                    inner = H("a0422a");
                    break;

                case 3: // Замок: каменная кладка
                    p.Box(0, 0, 1.05f, 1.45f, 0, H("4f4c48"), false);
                    for (int y = -4; y <= 4; y++)
                    for (int x = -2; x <= 2; x++)
                        p.Box(x * 0.44f + (y % 2 == 0 ? 0 : 0.22f), y * 0.32f, 0.2f, 0.14f, 0.03f, H(y % 3 == 0 ? "7a7671" : "6d6a66"));
                    inner = H("8a8680");
                    break;

                case 4: // Особняк: фиолетовый туман и свечи
                    p.Box(0, 0, 1.05f, 1.45f, 0, H("2e1c44"), false);
                    p.Union(H("4d3368"), false, Painter.CircleSdf(-0.6f, 0.9f, 0.35f), Painter.CircleSdf(0.5f, -0.6f, 0.4f),
                        Painter.CircleSdf(-0.4f, -1.0f, 0.3f), Painter.CircleSdf(0.7f, 1.1f, 0.3f));
                    foreach (var c in new[] { V(-0.6f, 1.0f), V(0.6f, 1.0f), V(-0.6f, -1.0f), V(0.6f, -1.0f) })
                    {
                        p.Box(c.x, c.y - 0.05f, 0.05f, 0.12f, 0.02f, H("f2e8d0"));
                        p.Oval(c.x, c.y + 0.12f, 0.035f, 0.06f, H("ffc23a"));
                    }
                    inner = H("5a3a80");
                    break;

                case 5: // Постоялый двор: фонари и подковы
                    p.Box(0, 0, 1.05f, 1.45f, 0, H("2f5a3a"), false);
                    foreach (var c in new[] { V(-0.55f, 0.95f), V(0.55f, 0.95f) })
                    {
                        p.Box(c.x, c.y + 0.16f, 0.06f, 0.03f, 0.01f, H("3a2a20"));
                        p.Box(c.x, c.y, 0.1f, 0.13f, 0.03f, H("ffd23a"));
                    }
                    foreach (var c in new[] { V(-0.55f, -0.95f), V(0.55f, -0.95f) })
                        p.Custom(q => Mathf.Max(Mathf.Abs((q - c).magnitude - 0.13f) - 0.035f, -(q.y - c.y + 0.02f)), H("c5ccd4"));
                    inner = H("3f7a4a");
                    break;

                case 6: // Монастырь: витраж
                    p.Box(0, 0, 1.05f, 1.45f, 0, H("2b2420"), false);
                    var glass = new[] { "d0473c", "4a74d4", "f2b53a", "4caf6a", "8a4ad8", "5aa3a8" };
                    int k = 0;
                    for (int y = -3; y <= 3; y++)
                    for (int x = -2; x <= 2; x++)
                    {
                        float cx = x * 0.42f, cy = y * 0.42f;
                        p.Poly(H(glass[k++ % glass.Length]), V(cx - 0.19f, cy - 0.19f), V(cx + 0.19f, cy - 0.19f), V(cx, cy + 0.19f));
                        p.Poly(H(glass[(k + 2) % glass.Length]), V(cx - 0.19f, cy + 0.21f), V(cx + 0.19f, cy + 0.21f), V(cx, cy - 0.17f));
                    }
                    inner = H("f2e3bd");
                    break;

                case 7: // Охотничий домик: мех и рога
                    p.Box(0, 0, 1.05f, 1.45f, 0, H("6b4423"), false);
                    var rnd = new System.Random(7);
                    for (int i = 0; i < 26; i++)
                    {
                        float x = (float)rnd.NextDouble() * 1.8f - 0.9f, y = (float)rnd.NextDouble() * 2.6f - 1.3f;
                        p.Capsule(x, y, x + 0.08f, y - 0.14f, 0.022f, H("8a5a32"), false);
                    }
                    foreach (int sgn in new[] { -1, 1 })
                    {
                        p.Capsule(sgn * 0.12f, 0.75f, sgn * 0.45f, 1.15f, 0.04f, H("f2e8d0"));
                        p.Capsule(sgn * 0.3f, 0.95f, sgn * 0.2f, 1.18f, 0.03f, H("f2e8d0"));
                    }
                    inner = H("8a5a32");
                    break;

                case 8: // Купальни: волны и пузыри
                    p.Box(0, 0, 1.05f, 1.45f, 0, H("1f6f8b"), false);
                    for (int y = -3; y <= 3; y++)
                        for (int x = -2; x <= 1; x++)
                            p.Capsule(x * 0.5f + 0.05f, y * 0.4f, x * 0.5f + 0.3f, y * 0.4f + 0.06f, 0.025f, H("3fa3c4"), false);
                    var rb = new System.Random(8);
                    for (int i = 0; i < 9; i++)
                        p.Ring((float)rb.NextDouble() * 1.6f - 0.8f, (float)rb.NextDouble() * 2.4f - 1.2f, 0.05f + (float)rb.NextDouble() * 0.05f, 0.015f, H("bff0ff"));
                    inner = H("2f8fb0");
                    break;

                case 9: // Дворец: золотые лучи и корона
                    p.Box(0, 0, 1.05f, 1.45f, 0, H("f0e2b6"), false);
                    for (int i = 0; i < 16; i++)
                    {
                        float a = i * Mathf.PI / 8, b = a + 0.12f;
                        p.Poly(H("e8c860"), false, V(0, 0), V(Mathf.Cos(a) * 2, Mathf.Sin(a) * 2), V(Mathf.Cos(b) * 2, Mathf.Sin(b) * 2));
                    }
                    p.Union(H("e8b830"), Painter.BoxSdf(0, 0.86f, 0.24f, 0.06f, 0.01f, 0),
                        Painter.PolySdf(V(-0.24f, 0.9f), V(-0.2f, 1.08f), V(-0.1f, 0.9f)),
                        Painter.PolySdf(V(-0.08f, 0.9f), V(0, 1.12f), V(0.08f, 0.9f)),
                        Painter.PolySdf(V(0.1f, 0.9f), V(0.2f, 1.08f), V(0.24f, 0.9f)));
                    inner = H("c9a030");
                    break;

                default: // Странник: тёмное дерево и звёзды
                    p.Box(0, 0, 1.05f, 1.45f, 0, H("3a2a20"), false);
                    var rs = new System.Random(10);
                    for (int i = 0; i < 12; i++)
                    {
                        float x = (float)rs.NextDouble() * 1.6f - 0.8f, y = (float)rs.NextDouble() * 2.4f - 1.2f;
                        if (Mathf.Abs(x) < 0.55f && Mathf.Abs(y) < 0.55f) continue;
                        p.Circle(x, y, 0.025f, H("ffe98a"), false);
                    }
                    inner = H("5a4030");
                    break;
            }
            Medallion(p, inner);
            return p;
        }

        // Общий знак игры в центре любой рубашки: дверь с замочной скважиной и месяц.
        static void Medallion(Painter p, Color inner)
        {
            p.Circle(0, 0, 0.52f, H("d4a62c"));
            p.Circle(0, 0, 0.42f, inner);
            p.Union(H("6b4423"), Painter.CircleSdf(0, 0.06f, 0.17f), Painter.BoxSdf(0, -0.1f, 0.17f, 0.17f, 0.02f, 0));
            p.Union(H("8a5a32"), false, Painter.CircleSdf(0, 0.06f, 0.12f), Painter.BoxSdf(0, -0.1f, 0.12f, 0.14f, 0.02f, 0));
            p.Circle(0, -0.04f, 0.035f, H("2b2420"), false);
            p.Poly(H("2b2420"), false, V(-0.025f, -0.05f), V(0.025f, -0.05f), V(0.04f, -0.15f), V(-0.04f, -0.15f));
            p.Circle(0.25f, 0.24f, 0.1f, H("fff3c4"));
            p.Circle(0.3f, 0.27f, 0.085f, inner, false);
        }

        // ───────────── Окантовки ─────────────

        static Func<Vector2, float> Band(float ox, float oy, float ix, float iy)
        {
            var outer = Painter.BoxSdf(0, 0, ox, oy, 0.12f, 0);
            var inside = Painter.BoxSdf(0, 0, ix, iy, 0.08f, 0);
            return q => Mathf.Max(outer(q), -inside(q));
        }

        static Painter PaintFrame(int tier)
        {
            var t = Tiers[Mathf.Clamp(tier, 0, Tiers.Length - 1)];
            var p = new Painter();
            p.Custom(Band(1.0f, 1.4f, 0.84f, 1.24f), t.Main);
            p.Custom(Band(0.86f, 1.26f, 0.82f, 1.22f), t.Dark, false);

            var corners = new[] { V(-0.92f, 1.32f), V(0.92f, 1.32f), V(-0.92f, -1.32f), V(0.92f, -1.32f) };
            if (tier == 0)
            {
                // Дерево: гвоздики по углам.
                foreach (var c in corners) p.Circle(c.x, c.y, 0.05f, H("9aa6ad"));
                return p;
            }

            // Бронза и выше: накладки по углам.
            foreach (var c in corners) p.Box(c.x, c.y, 0.11f, 0.11f, 0.02f, t.Main, true, 45);
            if (tier >= 2) foreach (var c in corners) p.Circle(c.x, c.y, 0.05f, t.Gem);

            // Золото и выше: ромбы сверху и снизу.
            if (tier >= 3)
            {
                p.Box(0, 1.32f, 0.12f, 0.12f, 0.02f, t.Main, true, 45);
                p.Box(0, -1.32f, 0.12f, 0.12f, 0.02f, t.Main, true, 45);
                p.Box(0, 1.32f, 0.06f, 0.06f, 0.01f, t.Gem, true, 45);
                p.Box(0, -1.32f, 0.06f, 0.06f, 0.01f, t.Gem, true, 45);
            }
            // Алмаз и выше: кристаллы, торчащие из рамки.
            if (tier >= 4)
            {
                foreach (int s in new[] { -1, 1 })
                {
                    p.Poly(t.Gem, V(s * 0.5f, 1.32f), V(s * 0.58f, 1.48f), V(s * 0.66f, 1.32f));
                    p.Poly(t.Gem, V(s * 0.5f, -1.32f), V(s * 0.58f, -1.48f), V(s * 0.66f, -1.32f));
                }
            }
            // Рубин и выше: камни по бокам.
            if (tier >= 5)
            {
                foreach (int s in new[] { -1, 1 })
                {
                    p.Oval(s * 0.92f, 0, 0.07f, 0.13f, t.Main);
                    p.Oval(s * 0.92f, 0, 0.045f, 0.09f, t.Gem);
                }
            }
            // Изумруд и выше: завитки у углов.
            if (tier >= 6)
            {
                foreach (var c in corners)
                {
                    var d = new Vector2(-Mathf.Sign(c.x), -Mathf.Sign(c.y));
                    p.Ring(c.x + d.x * 0.22f, c.y + d.y * 0.05f, 0.07f, 0.018f, t.Main, true);
                    p.Ring(c.x + d.x * 0.05f, c.y + d.y * 0.24f, 0.07f, 0.018f, t.Main, true);
                }
            }
            // Эпичная и выше: корона сверху.
            if (tier >= 7)
            {
                p.Poly(t.Main, V(-0.3f, 1.36f), V(-0.2f, 1.5f), V(-0.08f, 1.38f), V(0, 1.54f), V(0.08f, 1.38f), V(0.2f, 1.5f), V(0.3f, 1.36f));
                p.Circle(0, 1.46f, 0.04f, t.Gem);
            }
            // Легендарная: языки пламени по бокам.
            if (tier >= 8)
            {
                foreach (int s in new[] { -1, 1 })
                    p.Poly(t.Gem, V(s * 1.0f, 0.4f), V(s * 1.04f, 0.6f), V(s * 0.98f, 0.75f), V(s * 1.04f, 0.9f), V(s * 0.95f, 1.0f), V(s * 0.9f, 0.4f));
            }
            return p;
        }
    }
}
