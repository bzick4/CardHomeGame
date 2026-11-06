using System.Collections.Generic;
using UnityEngine;

namespace CardHome
{
    // Дом-уровень: своя колода комнат, готовые комнаты и колода гостей соперника.
    public class HouseDef
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public Color Color { get; init; }
        public RoomType IconRoom { get; init; }
        public int StartReputation { get; init; } = 3;
        public int StartGold { get; init; }
        public float Malice { get; init; } = 0.3f; // как часто соперник выбирает самого неудобного гостя
        public List<(RoomType type, int count)> Deck { get; } = new List<(RoomType, int)>();
        public List<(Vector2Int pos, RoomType type, Doors doors)> Rooms { get; } = new List<(Vector2Int, RoomType, Doors)>();
        public List<(GuestDef guest, int count)> Guests { get; } = new List<(GuestDef, int)>();

        public List<RoomType> BuildDeck()
        {
            var deck = new List<RoomType>();
            foreach (var (type, count) in Deck)
                for (int i = 0; i < count; i++) deck.Add(type);
            return deck;
        }
    }

    // Конкретный уровень: дом + круг сложности. После 10 домов они повторяются: «Хостел II», «Особняк IV»…
    public class LevelInfo
    {
        public const int Nights = 7;
        static readonly int[] BaseGuests = { 1, 1, 2, 2, 2, 3, 3 };

        public int Index { get; init; }
        public HouseDef House { get; init; }
        public int Tier { get; init; }
        public int NightCount { get; init; } = Nights;   // сколько ночей длится дом (в странствии — меньше)
        public int NightOffset { get; init; }           // ночь n играется как ночь n + сдвиг (в странствии дома глубже — труднее)
        public string TitleOverride { get; init; }
        public List<GuestDef> ExtraGuests { get; } = new List<GuestDef>(); // добавка к колоде соперника (босс)

        public string Title => TitleOverride ?? (Tier == 0 ? House.Name : $"{House.Name} {HouseDatabase.Roman(Tier + 1)}");

        // «Настоящая» ночь для правил появления гостей.
        public int Effective(int n) => n + NightOffset;
        public float MaliceBonus { get; init; }         // опасный дом: соперник злее
        public float Malice => Mathf.Min(0.95f, House.Malice + Tier * 0.12f + MaliceBonus);
        public int StartReputation => Mathf.Max(2, House.StartReputation - (Tier >= 3 ? 1 : 0));

        // Сколько гостей соперник выложит в ночь n (1..7).
        public int GuestsOnNight(int night)
        {
            int n = Effective(night);
            int c = BaseGuests[Mathf.Clamp(n - 1, 0, BaseGuests.Length - 1)];
            if (Tier >= 1 && n >= 4) c++;
            if (Tier >= 2 && n >= 2) c++;
            return Mathf.Min(c, 4);
        }

        // На высоких кругах гостям угодить труднее.
        public GuestDef Make(GuestDef g) => g.Harder(Tier);
    }

    public static class HouseDatabase
    {
        static HouseDef H(string id, string name, string desc, string color, RoomType icon,
            int reputation = 3, int gold = 0, float malice = 0.3f) =>
            new HouseDef
            {
                Id = id, Name = name, Description = desc, Color = RoomDatabase.Hex(color), IconRoom = icon,
                StartReputation = reputation, StartGold = gold, Malice = malice,
            };

        static readonly GuestDef Tr = GuestDatabase.Traveler, Sc = GuestDatabase.Scholar, Gh = GuestDatabase.Ghost,
            Th = GuestDatabase.Thief, Wo = GuestDatabase.Wolf, In = GuestDatabase.Inspector, Co = GuestDatabase.Couple,
            Dr = GuestDatabase.Drunkard, Gl = GuestDatabase.GhostLord;

        public static readonly HouseDef[] All = Build();

        static HouseDef[] Build()
        {
            var hostel = H("Hostel", "Хостел", "Простой дом для начала: много путников, мало угроз, лишнее сердце репутации.", "8a7f72", RoomType.Bedroom, reputation: 4, malice: 0.15f);
            hostel.Deck.AddRange(new[]
            {
                (RoomType.Corridor, 3), (RoomType.Crossroad, 1), (RoomType.Kitchen, 2), (RoomType.Dining, 2),
                (RoomType.Bedroom, 4), (RoomType.Bath, 2), (RoomType.Library, 1), (RoomType.Fireplace, 2),
                (RoomType.Trap, 2), (RoomType.Treasury, 1),
            });
            hostel.Guests.AddRange(new[] { (Tr, 6), (Sc, 2), (Gh, 1), (Th, 1), (Wo, 1) });

            var manor = H("Manor", "Усадьба", "Обычный дом: всего понемногу.", "6c7fb0", RoomType.Library, malice: 0.3f);
            manor.Deck.AddRange(new[]
            {
                (RoomType.Corridor, 3), (RoomType.Crossroad, 1), (RoomType.Kitchen, 2), (RoomType.Dining, 2),
                (RoomType.Bedroom, 3), (RoomType.Bath, 1), (RoomType.Library, 2), (RoomType.Fireplace, 2),
                (RoomType.Cellar, 1), (RoomType.Trap, 2), (RoomType.Treasury, 1), (RoomType.Nursery, 1),
            });
            manor.Guests.AddRange(new[] { (Tr, 4), (Sc, 3), (Gh, 2), (Th, 2), (Wo, 2) });

            var tavern = H("Tavern", "Таверна", "Кухня и столовая уже стоят, в колоде гостиные. Волки приходят чаще.", "c9824a", RoomType.Kitchen, malice: 0.35f);
            tavern.Deck.AddRange(new[]
            {
                (RoomType.Corridor, 3), (RoomType.Crossroad, 1), (RoomType.Kitchen, 1), (RoomType.Dining, 1),
                (RoomType.Bedroom, 3), (RoomType.Bath, 2), (RoomType.Library, 1), (RoomType.Fireplace, 2),
                (RoomType.Cellar, 1), (RoomType.Trap, 2), (RoomType.Treasury, 1), (RoomType.Lounge, 2),
            });
            tavern.Rooms.Add((new Vector2Int(3, 0), RoomType.Kitchen, Doors.N | Doors.E | Doors.W));
            tavern.Rooms.Add((new Vector2Int(3, 1), RoomType.Dining, Doors.E | Doors.S | Doors.W));
            tavern.Guests.AddRange(new[] { (Tr, 5), (Sc, 1), (Gh, 1), (Wo, 4), (Th, 1), (Dr, 1) });

            var castle = H("Castle", "Замок", "Ловушка стережёт сокровищницу. Меньше репутации, больше золота. Воры чаще.", "8f3b3b", RoomType.Treasury, reputation: 2, gold: 4, malice: 0.4f);
            castle.Deck.AddRange(new[]
            {
                (RoomType.Corridor, 3), (RoomType.Crossroad, 2), (RoomType.Kitchen, 2), (RoomType.Dining, 1),
                (RoomType.Bedroom, 2), (RoomType.Bath, 1), (RoomType.Library, 2), (RoomType.Fireplace, 1),
                (RoomType.Trap, 3), (RoomType.Treasury, 2),
            });
            castle.Rooms.Add((new Vector2Int(1, 0), RoomType.Trap, Doors.N | Doors.E | Doors.W));
            castle.Rooms.Add((new Vector2Int(0, 0), RoomType.Treasury, Doors.N | Doors.E | Doors.W));
            castle.Guests.AddRange(new[] { (Tr, 3), (Sc, 2), (Gh, 1), (Th, 4), (Wo, 1) });

            var haunted = H("Haunted", "Особняк", "Детская и подвал на месте. Призраки приходят почти каждую ночь.", "7a5c8f", RoomType.Nursery, malice: 0.4f);
            haunted.Deck.AddRange(new[]
            {
                (RoomType.Corridor, 3), (RoomType.Crossroad, 1), (RoomType.Kitchen, 2), (RoomType.Dining, 1),
                (RoomType.Bedroom, 2), (RoomType.Bath, 1), (RoomType.Library, 3), (RoomType.Fireplace, 1),
                (RoomType.Cellar, 2), (RoomType.Trap, 2), (RoomType.Treasury, 1), (RoomType.Nursery, 2),
            });
            haunted.Rooms.Add((new Vector2Int(1, 0), RoomType.Nursery, Doors.N | Doors.E | Doors.W));
            haunted.Rooms.Add((new Vector2Int(1, 1), RoomType.Cellar, Doors.N | Doors.E | Doors.S));
            haunted.Guests.AddRange(new[] { (Tr, 2), (Sc, 3), (Gh, 5), (Th, 1), (Wo, 1) });

            var inn = H("Inn", "Постоялый двор", "Всегда людно: столовая у входа, заглядывают инспектор и влюблённые.", "b9934f", RoomType.Dining, malice: 0.45f);
            inn.Deck.AddRange(new[]
            {
                (RoomType.Corridor, 3), (RoomType.Crossroad, 1), (RoomType.Kitchen, 2), (RoomType.Dining, 1),
                (RoomType.Bedroom, 4), (RoomType.Bath, 2), (RoomType.Library, 1), (RoomType.Fireplace, 1),
                (RoomType.Trap, 2), (RoomType.Treasury, 1), (RoomType.Lounge, 1),
            });
            inn.Rooms.Add((new Vector2Int(2, 1), RoomType.Dining, Doors.E | Doors.S | Doors.W));
            inn.Guests.AddRange(new[] { (Tr, 5), (Sc, 2), (In, 2), (Co, 1), (Th, 2), (Wo, 1) });

            var monastery = H("Monastery", "Монастырь", "Тишина и книги: учёные толпами, воры охотятся за реликвиями.", "7a5c8f", RoomType.Library, malice: 0.5f);
            monastery.Deck.AddRange(new[]
            {
                (RoomType.Corridor, 3), (RoomType.Crossroad, 1), (RoomType.Kitchen, 2), (RoomType.Dining, 1),
                (RoomType.Bedroom, 3), (RoomType.Library, 4), (RoomType.Fireplace, 1), (RoomType.Cellar, 1),
                (RoomType.Trap, 2), (RoomType.Treasury, 1),
            });
            monastery.Rooms.Add((new Vector2Int(2, 1), RoomType.Library, Doors.N | Doors.E | Doors.S));
            monastery.Guests.AddRange(new[] { (Sc, 6), (Tr, 2), (Gh, 2), (Th, 2), (Wo, 1) });

            var lodge = H("Lodge", "Охотничий домик", "Лес вокруг — волки каждую ночь. Зато камин уже горит.", "c4553f", RoomType.Fireplace, malice: 0.55f);
            lodge.Deck.AddRange(new[]
            {
                (RoomType.Corridor, 3), (RoomType.Kitchen, 3), (RoomType.Dining, 2), (RoomType.Bedroom, 3),
                (RoomType.Bath, 1), (RoomType.Fireplace, 2), (RoomType.Trap, 3), (RoomType.Treasury, 1),
            });
            lodge.Rooms.Add((new Vector2Int(1, 0), RoomType.Fireplace, Doors.N | Doors.E | Doors.S | Doors.W));
            lodge.Guests.AddRange(new[] { (Tr, 4), (Wo, 5), (Th, 1), (Gh, 1) });

            var spa = H("Spa", "Купальни", "Ванная у входа. Сюда едут влюблённые — и за ними воры.", "5aa3a8", RoomType.Bath, malice: 0.55f);
            spa.Deck.AddRange(new[]
            {
                (RoomType.Corridor, 3), (RoomType.Crossroad, 1), (RoomType.Kitchen, 1), (RoomType.Dining, 1),
                (RoomType.Bedroom, 4), (RoomType.Bath, 3), (RoomType.Fireplace, 1), (RoomType.Trap, 2),
                (RoomType.Treasury, 1), (RoomType.Lounge, 2),
            });
            spa.Rooms.Add((new Vector2Int(3, 0), RoomType.Bath, Doors.N | Doors.E | Doors.W));
            spa.Guests.AddRange(new[] { (Co, 3), (Tr, 4), (Sc, 1), (Dr, 1), (Th, 2), (Wo, 1) });

            var palace = H("Palace", "Дворец", "Богатый дом и самые требовательные гости. В конце может явиться Хозяин.", "c9a93a", RoomType.Lounge, gold: 5, malice: 0.6f);
            palace.Deck.AddRange(new[]
            {
                (RoomType.Corridor, 2), (RoomType.Crossroad, 1), (RoomType.Kitchen, 2), (RoomType.Dining, 2),
                (RoomType.Bedroom, 3), (RoomType.Bath, 2), (RoomType.Library, 1), (RoomType.Fireplace, 1),
                (RoomType.Trap, 3), (RoomType.Treasury, 2), (RoomType.Lounge, 2),
            });
            palace.Rooms.Add((new Vector2Int(1, 0), RoomType.Trap, Doors.N | Doors.E | Doors.W));
            palace.Rooms.Add((new Vector2Int(0, 0), RoomType.Treasury, Doors.N | Doors.E | Doors.W));
            palace.Rooms.Add((new Vector2Int(2, 1), RoomType.Lounge, Doors.E | Doors.S | Doors.W));
            palace.Guests.AddRange(new[] { (Tr, 3), (Sc, 2), (In, 2), (Co, 1), (Th, 3), (Wo, 2), (Gh, 1), (Gl, 1) });

            return new[] { hostel, manor, tavern, castle, haunted, inn, monastery, lodge, spa, palace };
        }

        public static int Count => All.Length;

        public static LevelInfo Level(int index) => new LevelInfo
        {
            Index = index,
            House = All[index % All.Length],
            Tier = index / All.Length,
        };

        // Римские цифры для любого числа (до 3999, дальше — обычные).
        public static string Roman(int n)
        {
            if (n <= 0 || n >= 4000) return n.ToString();
            int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            string[] digits = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < values.Length; i++)
                while (n >= values[i])
                {
                    sb.Append(digits[i]);
                    n -= values[i];
                }
            return sb.ToString();
        }
    }
}
