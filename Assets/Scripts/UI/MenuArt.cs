using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardHome
{
    // Значки меню в стиле игры: мультяшные формы с толстой тёмной обводкой, как на картах.
    // Свой арт: Assets/Resources/Art/Menu/<Имя>.png (Gear, Back, Flag и т.д.).
    public static class MenuArt
    {
        // Файл из Resources/Art/Menu/<key>, иначе рисуем. Хранение и выгрузка — в ArtStore.
        static Sprite Get(string key, Func<Sprite> make) => ArtStore.Get("Menu/" + key, make);

        static Color H(string hex) => RoomDatabase.Hex(hex);

        // ───────────── Фон меню ─────────────
        // Две иллюстрации: Assets/Resources/Art/Menu/Background.png (вертикальная, ~9:16)
        // и BackgroundWide.png (горизонтальная, ~16:9). Игра берёт ту, что подходит экрану;
        // если одной нет — показывает другую.
        // Вертикальная: название слева вверху, кнопки слева внизу, настройки справа внизу.
        // Горизонтальная: луна слева вверху, Путник в центре, дом справа → название наверху по центру, кнопки слева.
        // true — отзеркалить картинку; название и кнопки сами переедут на другую сторону.
        public const bool MirrorBackground = false;

        static Sprite LoadArt(string name) => ArtStore.Get("Menu/" + name, null);

        static Sprite Background => LoadArt("Background");
        static Sprite BackgroundWide => LoadArt("BackgroundWide");

        public static Sprite BackgroundFor(bool portrait) =>
            portrait ? (Background != null ? Background : BackgroundWide) : (BackgroundWide != null ? BackgroundWide : Background);

        static Vector2 V(float x, float y) => new Vector2(x, y);

        // ───────────── Карта кампании ─────────────
        // Art/Map/Campaign.png (9:16) и CampaignWide.png (16:9) — фон выбора домов; точки домов — в CampaignPoints.
        // Art/Map/Nodes/PlatformOpen|Current|Locked.png — платформы под домами.

        public static Sprite Campaign(bool wide) => ArtStore.Get("Map/Campaign" + (wide ? "Wide" : ""), null);
        public static Sprite Platform(string kind) => ArtStore.Get("Map/Nodes/Platform" + kind, null);

        // ───────────── Карта странствия ─────────────
        // Свои картинки (необязательно), Assets/Resources/Art/Wander/:
        //   MapBackground.png — свиток с пейзажем БЕЗ точек и троп (вертикальный, ~4:5);
        //   Pedestal.png — пустой каменный постамент;
        //   NodeHouse / NodeFair / NodeRest / NodeEvent / NodeElite / NodeBoss.png — точка целиком
        //   (постамент со значком на прозрачном фоне). Если картинки нет — рисуем кодом.

        public static Sprite WanderArt(string name) => ArtStore.Get("Wander/" + name, null);

        // Каменный постамент с золотой табличкой и звездой — подставка для значка точки.
        public static Sprite Pedestal => WanderArt("Pedestal") ?? Get("PedestalPainted", () =>
        {
            var p = new Painter();
            p.Oval(0, -0.36f, 0.9f, 0.3f, H("3a3e46"));
            p.Box(0, -0.2f, 0.86f, 0.2f, 0.04f, H("6b7079"));
            p.Capsule(-0.55f, -0.26f, -0.2f, -0.24f, 0.012f, H("4f545c"), false);
            p.Capsule(0.15f, -0.18f, 0.6f, -0.2f, 0.012f, H("4f545c"), false);
            p.Oval(0, 0.0f, 0.86f, 0.3f, H("9aa0a8"));
            p.Oval(0, 0.03f, 0.64f, 0.19f, H("b3b9c1"), false);
            p.Box(0, -0.34f, 0.25f, 0.14f, 0.03f, H("c9933a"));
            var star = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 5, r = i % 2 == 0 ? 0.09f : 0.04f;
                star[i] = V(Mathf.Cos(a) * r, -0.34f + Mathf.Sin(a) * r);
            }
            p.Poly(H("ffe27a"), false, star);
            return p.Render(160, 120, 1.05f, 0.6f);
        });

        // Фиолетовое знамя на перекладине — точка «Случай» (знак «?» дописывается текстом).
        public static Sprite EventBanner => Get("EventBanner", () => new Painter()
            .Box(0, 0.62f, 0.62f, 0.06f, 0.03f, H("8a5a32"))
            .Box(-0.56f, 0.1f, 0.05f, 0.58f, 0.02f, H("6b4423"))
            .Box(0.56f, 0.1f, 0.05f, 0.58f, 0.02f, H("6b4423"))
            .Poly(H("6b3fa0"), V(-0.46f, 0.56f), V(0.46f, 0.56f), V(0.46f, -0.5f), V(0, -0.32f), V(-0.46f, -0.5f))
            .Poly(H("8a5ac8"), false, V(-0.38f, 0.5f), V(0.38f, 0.5f), V(0.38f, 0.4f), V(-0.38f, 0.4f))
            .Render(128));

        // Костёр: поленья и пламя.
        public static Sprite Campfire => Get("Campfire", () => new Painter()
            .Capsule(-0.6f, -0.55f, 0.6f, -0.35f, 0.12f, H("8a5a32"))
            .Capsule(-0.6f, -0.35f, 0.6f, -0.55f, 0.12f, H("6b4423"))
            .Union(H("f2602c"), Painter.CircleSdf(0, -0.1f, 0.42f), Painter.PolySdf(V(-0.32f, 0), V(0, 0.75f), V(0.32f, 0)))
            .Union(H("ffc23a"), false, Painter.CircleSdf(0, -0.18f, 0.24f), Painter.PolySdf(V(-0.18f, -0.1f), V(0, 0.38f), V(0.18f, -0.1f)))
            .Render(128));

        // Скрещённые мечи — опасный дом.
        public static Sprite Swords => Get("Swords", () =>
        {
            var p = new Painter();
            foreach (int sgn in new[] { -1, 1 })
            {
                p.Poly(H("dfe6ea"), V(sgn * -0.62f, 0.72f), V(sgn * -0.46f, 0.78f), V(sgn * 0.42f, -0.3f), V(sgn * 0.3f, -0.4f));
                p.Box(sgn * 0.3f, -0.42f, 0.2f, 0.05f, 0.02f, H("d4a62c"), true, sgn * 45);
                p.Capsule(sgn * 0.42f, -0.52f, sgn * 0.6f, -0.72f, 0.06f, H("8a5a32"));
                p.Circle(sgn * 0.64f, -0.76f, 0.07f, H("d4a62c"));
            }
            return p.Render(128);
        });

        // Сундук — дом (гостевой дом с наградой).
        static Sprite Chest => Get("Chest", () => new Painter()
            .Box(0, -0.25f, 0.72f, 0.38f, 0.06f, H("8a5a32"))
            .Box(0, 0.22f, 0.72f, 0.2f, 0.18f, H("b07a44"))
            .Box(-0.45f, -0.05f, 0.07f, 0.6f, 0, H("d4a62c"), false)
            .Box(0.45f, -0.05f, 0.07f, 0.6f, 0, H("d4a62c"), false)
            .Box(0, 0.0f, 0.14f, 0.16f, 0.03f, H("d4a62c"))
            .Circle(0, 0.0f, 0.045f, H("2b2420"), false)
            .Render(128));

        // ───────────── Иконки (цветные, с обводкой — как в игре) ─────────────

        static Sprite Icon(string key, Func<Painter> paint) => Get(key, () => paint().Render(128));

        public static Sprite Gear => Icon("Gear", () =>
        {
            var p = new Painter();
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f, r = a * Mathf.Deg2Rad;
                p.Box(Mathf.Cos(r) * 0.64f, Mathf.Sin(r) * 0.64f, 0.16f, 0.14f, 0.03f, H("c5ccd4"), true, a);
            }
            p.Circle(0, 0, 0.58f, H("c5ccd4"));
            p.Circle(0, 0, 0.24f, H("5a4632"));
            return p;
        });

        static Sprite Compass => Icon("Compass", () => new Painter()
            .Circle(0, 0, 0.8f, H("d4a62c"))
            .Circle(0, 0, 0.64f, H("f3e6c6"))
            .Poly(H("d0473c"), V(0, 0.55f), V(0.13f, 0), V(-0.13f, 0))
            .Poly(H("9aa6ad"), V(0, -0.55f), V(0.13f, 0), V(-0.13f, 0))
            .Circle(0, 0, 0.07f, H("2b2420"), false));

        public static Sprite BackIcon => Icon("Back", () => new Painter()
            .Poly(H("6b4423"), V(0.35f, 0.6f), V(0.35f, -0.6f), V(-0.5f, 0)));

    }
}
