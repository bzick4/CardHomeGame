using System.Collections.Generic;
using UnityEngine;

namespace CardHome
{
    public enum GuestBehaviour
    {
        Wanderer, // идёт в самую приятную доступную комнату
        Random,   // бродит случайно
        Seeker,   // кратчайшим путём идёт к цели
    }

    public enum GuestRole { Friendly, Threat, Special, Boss }

    public class GuestDef
    {
        public string Id; // для файлов арта: Art/Guests/<Id>.png
        public string Name;
        public string Description;
        public Color Color;
        public GuestBehaviour Behaviour;
        public int Stamina;     // сколько комнат (не коридоров) успеет обойти
        public int Need;        // нужное настроение, чтобы остаться довольным
        public bool Trappable;  // ловушка его ловит
        public bool Ethereal;   // проходит ловушки насквозь
        public bool HatesWarmth;
        public RoomType SeekTarget;
        public int CatchReward; // золото, если пойман
        public int StealAmount; // сколько золота уносит, если дошёл
        public Dictionary<RoomType, int> Likes = new Dictionary<RoomType, int>();

        // Особые гости.
        public bool Special;        // приходит редко, со своим правилом
        public bool Boss;           // хозяин дома: только в последние ночи
        public bool Romantic;       // спальня рядом с ванной: +2
        public bool EdgeHater;      // злится на двери, ведущие за край дома
        public int BreakChance;     // шанс сломать комнату даже довольным (%)
        public int GoldMultiplier = 1;
        public int UnhappyPenalty = 1; // сколько репутации снимает недовольный

        // Постоянные гости: довольный возвращается и платит больше.
        public bool CanBecomeRegular;
        public bool Regular;
        public int RegularBonus;

        public int Tier;

        public int Like(RoomType t) => Likes.TryGetValue(t, out var v) ? v : 0;
        public bool IsThreat => Behaviour == GuestBehaviour.Seeker;
        public GuestRole Role => Boss ? GuestRole.Boss : IsThreat ? GuestRole.Threat : Special ? GuestRole.Special : GuestRole.Friendly;

        // Тот же гость на круге сложности tier: угодить ему труднее.
        public GuestDef Harder(int tier)
        {
            if (tier <= 0) return this;
            var g = (GuestDef)MemberwiseClone();
            g.Tier = tier;
            if (!IsThreat) g.Need += tier;
            return g;
        }

        // Тот же гость, но уже постоянный: +3 золота, если снова доволен, и −2 репутации, если разочарован.
        public GuestDef AsRegular()
        {
            var g = (GuestDef)MemberwiseClone();
            g.Regular = true;
            g.Name = "★ " + Name;
            g.RegularBonus = 3;
            g.UnhappyPenalty = 2;
            return g;
        }
    }

    public static class GuestDatabase
    {
        public static readonly GuestDef Traveler = new GuestDef
        {
            Id = "Traveler",
            Name = "Путник",
            Description = "Хочет поесть и выспаться. Выносливость 4, нужно настроение 4.",
            Color = RoomDatabase.Hex("e0c27a"),
            Behaviour = GuestBehaviour.Wanderer,
            Stamina = 4,
            Need = 4,
            CanBecomeRegular = true,
            Likes = { { RoomType.Bedroom, 3 }, { RoomType.Kitchen, 2 }, { RoomType.Bath, 2 }, { RoomType.Library, -1 } },
        };

        public static readonly GuestDef Scholar = new GuestDef
        {
            Id = "Scholar",
            Name = "Учёный",
            Description = "Ищет книги, не любит детей. Выносливость 5, нужно настроение 6.",
            Color = RoomDatabase.Hex("a99be0"),
            Behaviour = GuestBehaviour.Wanderer,
            Stamina = 5,
            Need = 6,
            CanBecomeRegular = true,
            Likes =
            {
                { RoomType.Library, 3 }, { RoomType.Kitchen, 1 }, { RoomType.Bedroom, 1 },
                { RoomType.Bath, 1 }, { RoomType.Nursery, -2 },
            },
        };

        public static readonly GuestDef Ghost = new GuestDef
        {
            Id = "Ghost",
            Name = "Призрак",
            Description = "Бродит случайно, сквозь ловушки. Не любит тепло. Выносливость 4, нужно 3.",
            Color = RoomDatabase.Hex("d8e8f0"),
            Behaviour = GuestBehaviour.Random,
            Stamina = 4,
            Need = 3,
            Ethereal = true,
            HatesWarmth = true,
            Likes = { { RoomType.Nursery, 4 }, { RoomType.Cellar, 2 }, { RoomType.Library, 1 } },
        };

        public static readonly GuestDef Thief = new GuestDef
        {
            Id = "Thief",
            Name = "Вор",
            Description = "Кратчайшим путём идёт к сокровищнице. Поймай ловушкой — +5 золота.",
            Color = RoomDatabase.Hex("555a66"),
            Behaviour = GuestBehaviour.Seeker,
            SeekTarget = RoomType.Treasury,
            Trappable = true,
            CatchReward = 5,
            StealAmount = 5,
        };

        public static readonly GuestDef Wolf = new GuestDef
        {
            Id = "Wolf",
            Name = "Волк",
            Description = "Кратчайшим путём бежит на кухню. Поймай ловушкой — +4 золота.",
            Color = RoomDatabase.Hex("9a8c80"),
            Behaviour = GuestBehaviour.Seeker,
            SeekTarget = RoomType.Kitchen,
            Trappable = true,
            CatchReward = 4,
        };

        // ───── Особые гости ─────

        public static readonly GuestDef Inspector = new GuestDef
        {
            Id = "Inspector",
            Name = "Инспектор",
            Description = "Обходит всё подряд (+1 за комнату), но −1 за каждую дверь за край дома. Нужно 5.",
            Color = RoomDatabase.Hex("4f6b8a"),
            Behaviour = GuestBehaviour.Wanderer,
            Stamina = 6,
            Need = 5,
            Special = true,
            EdgeHater = true,
            GoldMultiplier = 2,
        };

        public static readonly GuestDef Couple = new GuestDef
        {
            Id = "Couple",
            Name = "Влюблённые",
            Description = "Спальня +3, ванная +2, спальня рядом с ванной ещё +2. Платят вдвое. Нужно 6.",
            Color = RoomDatabase.Hex("e57b9a"),
            Behaviour = GuestBehaviour.Wanderer,
            Stamina = 4,
            Need = 6,
            Special = true,
            Romantic = true,
            GoldMultiplier = 2,
            CanBecomeRegular = true,
            Likes = { { RoomType.Bedroom, 3 }, { RoomType.Bath, 2 }, { RoomType.Lounge, 1 }, { RoomType.Cellar, -2 } },
        };

        public static readonly GuestDef Drunkard = new GuestDef
        {
            Id = "Drunkard",
            Name = "Пьяница",
            Description = "Шатается куда попало, любит кухню и столовую. Может что-нибудь сломать. Нужно 2.",
            Color = RoomDatabase.Hex("b5654a"),
            Behaviour = GuestBehaviour.Random,
            Stamina = 5,
            Need = 2,
            Special = true,
            BreakChance = 50,
            Likes = { { RoomType.Kitchen, 2 }, { RoomType.Dining, 2 }, { RoomType.Lounge, 1 }, { RoomType.Library, -1 } },
        };

        public static readonly GuestDef GhostLord = new GuestDef
        {
            Id = "GhostLord",
            Name = "Хозяин дома",
            Description = "БОСС. Призрак-хозяин: сквозь ловушки, не любит тепло. Нужно 8. Недоволен — репутация −2.",
            Color = RoomDatabase.Hex("8fd3c8"),
            Behaviour = GuestBehaviour.Wanderer,
            Stamina = 6,
            Need = 8,
            Special = true,
            Boss = true,
            Ethereal = true,
            HatesWarmth = true,
            GoldMultiplier = 2,
            UnhappyPenalty = 2,
            Likes =
            {
                { RoomType.Nursery, 3 }, { RoomType.Cellar, 3 }, { RoomType.Library, 2 },
                { RoomType.Bedroom, 1 }, { RoomType.Lounge, -1 },
            },
        };

        public static readonly GuestDef[] Specials = { Inspector, Couple, Drunkard };

        public const int SpecialChance = 15;

        public static readonly GuestDef[] All = { Traveler, Scholar, Ghost, Thief, Wolf, Inspector, Couple, Drunkard, GhostLord };

        public static GuestDef ById(string id)
        {
            foreach (var g in All)
                if (g.Id == id) return g;
            return Traveler;
        }

        public static void Shuffle<T>(System.Random rng, List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
