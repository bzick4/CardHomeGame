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
        public string Name { get; init; }
        public Color Main { get; init; }
        public Color Dark { get; init; }
        public Color Gem { get; init; }
        public bool Shimmer { get; init; }   // бегущий блик (с изумрудной)
        public bool Glow { get; init; }      // пульсирующее сияние вокруг (эпичная+)
        public bool Sparkles { get; init; }  // искры (легендарная)
    }

    // Рубашки карт гостей: у каждого дома своя тема, у каждого круга — своя окантовка.
    // Соперник играет рубашкой текущего уровня; пройдя дом, игрок получает её себе.
    //
    // Код рубашки: 0 — обычная «Странник» (есть всегда), иначе 1 + тема·100 + окантовка.
    //
    // Свой арт: Assets/Resources/Art/Backs/Theme<N>.png (рисунок без рамки, 0..9 по домам, 10 — Странник)
    // и Art/Frames/Frame<N>.png (рамка с прозрачной серединой, 0..8 по кругам) — одна и та же рамка
    // и для рубашек, и для лица карт комнат.
    public static class CardBacks
    {
        public const int DefaultCode = 0;
        public const int ThemeCount = 10;
        const int WanderTheme = 10;

        public static readonly BackTier[] Tiers =
        {
            T("Дерево", "8a5a32", "5a3a20", "c9945a"),
            T("Бронза", "c27a3e", "7a4520", "e8a868"),
            T("Серебро", "c5ccd4", "6d7680", "eef3f8"),
            T("Золото", "e8b830", "8a6410", "fff0a0"),
            T("Изумруд", "c9a43a", "6a5a10", "2fd07a"),
            T("Сапфир", "9fb8e0", "3f5f90", "3f8fff"),
            T("Аметист", "b48ae0", "4a2a80", "c86bff", shimmer: true),
            T("Рубин", "e8a028", "8a4a08", "e0304a", shimmer: true, glow: true),
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

        // Рисунок рубашки без рамки: Art/Backs/Theme<N>.png.
        public static Sprite ThemeSprite(int theme) => ArtStore.Require("Backs/Theme" + theme);

        // Рамка-картинка: холст 380×516, сама карта 300×420 стоит на нём со смещением (40, 48) —
        // запас по краям нужен под крылья, короны и кристаллы.
        static readonly Vector2 ArtFrameOut = new Vector2(40f / 300f, 48f / 420f);
        // Отступ содержимого карты (рубашки или лица) от её края — 8 из 300×420.
        static readonly Vector2 ArtInset = new Vector2(8f / 300f, 8f / 420f);
        public static Sprite FrameArt(int tier) => ArtStore.Require("Frames/Frame" + tier);

        // Рамка слоем поверх карты (лица или рубашки). Привязки в долях карты, поэтому размер карты знать не нужно.
        public static Image AddFrame(RectTransform card, int tier)
        {
            var img = UIKit.Image("Frame", card, Color.white, FrameArt(tier));
            img.raycastTarget = false;
            var foil = UiShaders.Foil(tier); // блик по рамке у кругов 4+
            if (foil != null) img.material = foil;
            var o = ArtFrameOut;
            var rt = img.rectTransform;
            rt.anchorMin = -o;
            rt.anchorMax = Vector2.one + o;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

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

            // Рисунок чуть меньше карты: планки рамок начинаются немного внутри края, иначе он выглядывает.
            var art = UIKit.Image("Art", root, Color.white, ThemeSprite(theme));
            art.raycastTarget = false;
            var ar = art.rectTransform;
            ar.anchorMin = ArtInset;
            ar.anchorMax = Vector2.one - ArtInset;
            ar.offsetMin = ar.offsetMax = Vector2.zero;

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

            AddFrame(root, TierOf(code));

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
    }
}
