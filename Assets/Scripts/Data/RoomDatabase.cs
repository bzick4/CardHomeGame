using System.Collections.Generic;
using UnityEngine;

namespace CardHome
{
    public enum RoomType
    {
        Hall,
        Corridor,
        Crossroad,
        Kitchen,
        Dining,
        Bedroom,
        Bath,
        Library,
        Fireplace,
        Cellar,
        Trap,
        Treasury,
        Nursery,
        Lounge,
    }

    // Карта в колоде игрока: комната и её уровень. Уровни бесконечные:
    // 1 — обычная, 2 — бронза, 3 — серебро, 4 — золото … 9 — легендарная, дальше «легендарная +N».
    // Улучшенная карта ставит комнату сразу нужного уровня.
    public struct Card : System.IEquatable<Card>
    {
        // Практический потолок, чтобы числа не переполнились. В игре до него не дойти.
        public const int MaxLevel = 999;
        static readonly string[] Names = { "", "Обычная", "Бронза", "Серебро", "Золото", "Алмаз", "Рубин", "Изумруд", "Эпичная", "Легендарная" };

        public static string LevelName(int level) =>
            level < Names.Length ? Names[Mathf.Max(1, level)] : $"{Names[Names.Length - 1]} +{level - Names.Length + 1}";

        // Какая окантовка (из рубашек) у карты этого уровня: 0 — бронза … последняя — легендарная, дальше она же.
        public static int FrameTier(int level) => Mathf.Clamp(level - 1, 0, 8);

        public RoomType Type;
        public int Level;

        public Card(RoomType type, int level = 1)
        {
            Type = type;
            Level = Mathf.Clamp(level, 1, MaxLevel);
        }

        public RoomDef Def => RoomDatabase.Get(Type);
        public bool CanLevelUp => Def.CanUpgrade && Level < MaxLevel;
        public string Title => Level > 1 ? $"{Def.Name} {HouseDatabase.Roman(Level)}" : Def.Name;

        // Для сохранений: тип + уровень·100 (старые сохранения без уровня читаются как уровень 1).
        public int Code => (int)Type + Level * 100;
        public static Card FromCode(int code) => new Card((RoomType)(code % 100), Mathf.Max(1, code / 100));

        public bool Equals(Card o) => Type == o.Type && Level == o.Level;
        public override bool Equals(object obj) => obj is Card c && Equals(c);
        public override int GetHashCode() => (int)Type * 31 + Level;
        public static bool operator ==(Card a, Card b) => a.Equals(b);
        public static bool operator !=(Card a, Card b) => !a.Equals(b);
    }

    public class RoomDef
    {
        public RoomType Type;
        public string Name;
        public string Description;
        public Doors Doors;
        public Color Color;

        // Коридоры не тратят выносливость гостя.
        public bool IsPassage => Type == RoomType.Corridor || Type == RoomType.Crossroad || Type == RoomType.Hall;
        // Проходы не улучшаются: им нечего удваивать.
        public bool CanUpgrade => !IsPassage;
    }

    // Все комнаты и стартовая колода. Позже это можно вынести в ScriptableObject.
    public static class RoomDatabase
    {
        static readonly Dictionary<RoomType, RoomDef> Defs = new Dictionary<RoomType, RoomDef>();

        static RoomDatabase()
        {
            Add(RoomType.Hall, "Прихожая", "Вход. Отсюда гости начинают обход.",
                Doors.N | Doors.E | Doors.W, Hex("8a7f72"));
            Add(RoomType.Corridor, "Коридор", "Ничего не даёт, но гость проходит его, не уставая.",
                Doors.N | Doors.S, Hex("6f6a63"));
            Add(RoomType.Crossroad, "Перекрёсток", "Коридор на 4 двери. Не тратит выносливость.",
                Doors.N | Doors.E | Doors.S | Doors.W, Hex("6f6a63"));
            Add(RoomType.Kitchen, "Кухня", "Путнику +2, учёному +1. Рядом столовая: ×2. Цель волка!",
                Doors.N | Doors.E | Doors.S, Hex("c9824a"));
            Add(RoomType.Dining, "Столовая", "+1 любому гостю.",
                Doors.E | Doors.S | Doors.W, Hex("b9934f"));
            Add(RoomType.Bedroom, "Спальня", "Путнику +3, учёному +1. Рядом подвал: кошмары −3.",
                Doors.S, Hex("6c7fb0"));
            Add(RoomType.Bath, "Ванная", "Путнику +2, учёному +1.",
                Doors.S, Hex("5aa3a8"));
            Add(RoomType.Library, "Библиотека", "Учёному +3, призраку +1, путнику −1.",
                Doors.N | Doors.S, Hex("7a5c8f"));
            Add(RoomType.Fireplace, "Камин", "Соседние комнаты тёплые: +1 (призраку −1).",
                Doors.N | Doors.E | Doors.S | Doors.W, Hex("c4553f"));
            Add(RoomType.Cellar, "Подвал", "Призраку +2. Соседним спальням снятся кошмары.",
                Doors.S, Hex("4d4a45"));
            Add(RoomType.Trap, "Ловушка", "Ловит вора и волка (+золото). Обычному гостю −3.",
                Doors.N | Doors.S, Hex("8f3b3b"));
            Add(RoomType.Treasury, "Сокровищница", "+2 золота каждую ночь. Цель вора!",
                Doors.S, Hex("c9a93a"));
            Add(RoomType.Nursery, "Детская", "Призраку +4, учёному −2.",
                Doors.S | Doors.E, Hex("d68aa0"));
            Add(RoomType.Lounge, "Гостиная", "+2 любому гостю. Бывает только в наградах.",
                Doors.E | Doors.S | Doors.W, Hex("5f9e6e"));
        }

        static void Add(RoomType t, string name, string desc, Doors doors, Color c)
        {
            Defs[t] = new RoomDef { Type = t, Name = name, Description = desc, Doors = doors, Color = c };
        }

        public static RoomDef Get(RoomType t) => Defs[t];

        // Три разные карты на выбор после ночи. Гостиная выпадает чаще остальных, коридор — реже.
        public static List<RoomType> RewardOffer(System.Random rng, int count)
        {
            var pool = new List<(RoomType type, int weight)>();
            foreach (var t in Defs.Keys)
            {
                if (t == RoomType.Hall) continue;
                int w = t == RoomType.Lounge ? 4 : t == RoomType.Corridor || t == RoomType.Crossroad ? 1 : 2;
                pool.Add((t, w));
            }

            var result = new List<RoomType>();
            while (result.Count < count && pool.Count > 0)
            {
                int total = 0;
                foreach (var e in pool) total += e.weight;
                int roll = rng.Next(total);
                for (int i = 0; i < pool.Count; i++)
                {
                    roll -= pool[i].weight;
                    if (roll >= 0) continue;
                    result.Add(pool[i].type);
                    pool.RemoveAt(i);
                    break;
                }
            }
            return result;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
