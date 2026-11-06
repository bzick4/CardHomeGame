using UnityEngine;

namespace CardHome
{
    // Цвета нарисованных кнопок (Art/UI/Buttons/Button<Цвет>.png).
    public enum UiButtonColor { Orange, Purple, Green, Red, Gold, Grey }

    // Картинки интерфейса из Assets/Resources/Art/UI: кнопки, окна, плашки, значки, накладки на поле, экран загрузки.
    // Все пути — здесь. Нет картинки — подставляется прежний спрайт, нарисованный кодом (Sprites.*).
    public static class UiArt
    {
        static Sprite Get(string path, Sprite fallback = null)
        {
            var s = ArtStore.Get("UI/" + path, null);
            return s != null ? s : fallback;
        }

        // ───── Кнопки ─────
        public static Sprite Button(UiButtonColor color) => Get("Buttons/Button" + color, Sprites.Panel);
        public static Sprite RoundButton => Get("Buttons/ButtonRound", Sprites.Circle);

        // ───── Окна и плашки ─────
        public static Sprite Window => Get("Panels/Window", Sprites.Panel);
        public static Sprite WindowTitle => Get("Panels/WindowTitle", Sprites.Panel);
        public static Sprite TopBar => Get("Panels/TopBar", Sprites.Panel);
        public static Sprite Plate => Get("Panels/Plate", Sprites.Soft);
        public static Sprite Tag => Get("Panels/Tag", Sprites.Flat);
        public static Sprite Bubble => Get("Panels/Bubble", Sprites.Flat);
        public static Sprite BubbleTail => Get("Panels/BubbleTail");
        public static Sprite Slot => Get("Panels/Slot", Sprites.Soft);

        // ───── Значки (Art/UI/Icons/<имя>.png) ─────
        // Gold, Heart, HeartEmpty, Moves, Deck, Hand, Menu, Gear, Back, Home, Close, Check, Rotate, Night, Day, Info,
        // SoundOn/Off, MusicOn/Off, Shop, Upgrade, Remove, Repair, Reroll, ExtraMove, Campaign, Wander, Trophy, Skull.
        public static Sprite Icon(string name) => Get("Icons/" + name);

        // ───── Накладки на поле ─────
        public static Sprite Crack => Get("BoardOverlays/Crack");
        public static Sprite TileHighlight => Get("BoardOverlays/TileHighlight", Sprites.Frame);
        public static Sprite GuestBadge => Get("BoardOverlays/GuestBadge");
        public static Sprite MoodBar => Get("BoardOverlays/MoodBar");

        // ───── Экран загрузки ─────
        public static Sprite Loading(bool wide) => Get(wide ? "Loading/LoadingWide" : "Loading/Loading");
    }
}
