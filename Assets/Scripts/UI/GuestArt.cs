using UnityEngine;

namespace CardHome
{
    // Картинки карт гостей из Assets/Resources/Art/Cards: персонаж в полный рост (Cards/<Id>.png),
    // пейзаж-фон, лента, солнце и луна, значки ролей, замок и звёзды.
    // Если файла нет — предупреждение и серая заглушка (ArtStore.Require).
    public static class GuestArt
    {
        static Sprite Get(string key) => ArtStore.Require("Cards/" + key);

        public static Sprite Backdrop => Get("Backdrop");
        public static Sprite Character(GuestDef g) => Get(g.Id);
        public static Sprite Banner => Get("Banner");
        public static Sprite Sun => Get("Sun");
        public static Sprite Moon => Get("Moon");
        public static Sprite Role(GuestRole r) => Get("Role" + r);
        public static Sprite Lock => Get("Lock");
        public static Sprite Star => Get("Star");
        public static Sprite StarEmpty => Get("StarEmpty");
    }
}
