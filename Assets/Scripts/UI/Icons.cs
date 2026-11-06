using UnityEngine;

namespace CardHome
{
    // Значки комнат, гостей и домов, трещины и монета — из Assets/Resources/Art.
    // Картинки есть для всего; если файла нет — предупреждение и серая заглушка (ArtStore.Require).
    public static class Icons
    {
        public static Sprite Room(RoomType t) => ArtStore.Require("Rooms/" + t);
        public static Sprite Guest(GuestDef g) => ArtStore.Require("Guests/" + g.Id);
        public static Sprite House(HouseDef h) => ArtStore.Require("Houses/" + h.Id);
        // Трещины поверх сломанной комнаты.
        public static Sprite Crack => UiArt.Crack ?? ArtStore.Require("Board/Broken");
        // Картинка комнаты для лица карты и клетки поля: Art/RoomArt/<Тип>.png.
        public static Sprite RoomArt(RoomType t) => ArtStore.Require("RoomArt/" + t);
        // Пергамент под описание (растягивается по 9 частям). Нет — null (тогда подложка кодом).
        public static Sprite TextPanel => ArtStore.Get("Cards/TextPanel", null);
        public static Sprite Coin => ArtStore.Require("UI/Icons/Gold");
    }
}
