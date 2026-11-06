using UnityEngine;

namespace CardHome
{
    // Картинки меню, карты кампании и карты странствия (Assets/Resources/Art/Menu, Map, Wander).
    public static class MenuArt
    {
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
        //   (постамент со значком на прозрачном фоне). Дом — постамент с круглой картинкой дома.

        public static Sprite WanderArt(string name) => ArtStore.Get("Wander/" + name, null);
        public static Sprite Pedestal => ArtStore.Require("Wander/Pedestal");

        // Значки кнопок — из Art/UI/Icons.
        public static Sprite Gear => ArtStore.Require("UI/Icons/Gear");
        public static Sprite BackIcon => ArtStore.Require("UI/Icons/Back");
    }
}
