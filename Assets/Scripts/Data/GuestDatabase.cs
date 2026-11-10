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
        public string Id { get; init; } // для файлов арта: Art/Guests/<Id>.png
        string name;
        public string Name { get => name; init => name = value; }
        public string Description { get; init; } // правила — в крупном просмотре карты
        public string Tagline { get; init; }     // 3–5 слов на самой карте
        public Color Color { get; init; }
        public GuestBehaviour Behaviour { get; init; }
        public int Stamina { get; init; }     // сколько комнат (не коридоров) успеет обойти
        int need;
        public int Need { get => need; init => need = value; }        // нужное настроение, чтобы остаться довольным
        public bool Trappable { get; init; }  // ловушка его ловит
        public bool Ethereal { get; init; }   // проходит ловушки насквозь
        public bool HatesWarmth { get; init; }
        public RoomType SeekTarget { get; init; }
        public int CatchReward { get; init; } // золото, если пойман
        public int StealAmount { get; init; } // сколько золота уносит, если дошёл
        public Dictionary<RoomType, int> Likes { get; } = new Dictionary<RoomType, int>();

        // Особые гости.
        public bool Special { get; init; }        // приходит редко, со своим правилом
        public bool Boss { get; init; }           // хозяин дома: только в последние ночи
        public bool Romantic { get; init; }       // спальня рядом с ванной: +2
        public bool EdgeHater { get; init; }      // злится на двери, ведущие за край дома
        public int BreakChance { get; init; }     // шанс сломать комнату даже довольным (%)
        public int GoldMultiplier { get; init; } = 1;
        int unhappyPenalty = 1;
        public int UnhappyPenalty { get => unhappyPenalty; init => unhappyPenalty = value; } // сколько репутации снимает недовольный

        // С какого уровня гость появляется у соперника (1 — сразу).
        public int MinStage { get; init; } = 1;
        public bool Snob { get; init; }        // замечает только улучшенные комнаты (ур. II+)
        public bool Moonstruck { get; init; }  // в полнолуние сильнее
        public bool HouseCat { get; init; }    // довольная — остаётся жить в доме и распугивает крыс
        public bool FearsCat { get; init; }    // в доме живёт кошка — уходит ни с чем
        public bool Arsonist { get; init; }    // дошёл до цели — поджигает её и соседние комнаты
        public bool Curser { get; init; }      // недовольная — превращает комнату в подвал

        // Постоянные гости: довольный возвращается и платит больше.
        public bool CanBecomeRegular { get; init; }
        public bool Regular { get; private set; }
        public int RegularBonus { get; private set; }

        int Tier { get; set; }

        public int Like(RoomType t) => Likes.TryGetValue(t, out var v) ? v : 0;
        public bool IsThreat => Behaviour == GuestBehaviour.Seeker;
        public GuestRole Role => Boss ? GuestRole.Boss : IsThreat ? GuestRole.Threat : Special ? GuestRole.Special : GuestRole.Friendly;

        // Тот же гость на круге сложности tier: угодить ему труднее.
        public GuestDef Harder(int tier)
        {
            if (tier <= 0) return this;
            var g = (GuestDef)MemberwiseClone();
            g.Tier = tier;
            if (!IsThreat) g.need += tier;
            return g;
        }

        // Тот же гость, но уже постоянный: +3 золота, если снова доволен, и −2 репутации, если разочарован.
        public GuestDef AsRegular()
        {
            var g = (GuestDef)MemberwiseClone();
            g.Regular = true;
            g.name = "Свой · " + Name;
            g.RegularBonus = 3;
            g.unhappyPenalty = 2;
            return g;
        }
    }

    public static class GuestDatabase
    {
        public static readonly GuestDef Traveler = new GuestDef
        {
            Id = "Traveler",
            Tagline = "Устал с дальней дороги",
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
            Tagline = "Ищет редкие книги",
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
            Tagline = "Бродит сквозь стены",
            Name = "Призрак",
            Description = "Бродит случайно, сквозь ловушки. Не любит тепло. Выносливость 4, нужно 3.",
            Color = RoomDatabase.Hex("d8e8f0"),
            Behaviour = GuestBehaviour.Random,
            Stamina = 4,
            Need = 3,
            Ethereal = true,
            HatesWarmth = true,
            Moonstruck = true,
            Likes = { { RoomType.Nursery, 4 }, { RoomType.Cellar, 2 }, { RoomType.Library, 1 } },
        };

        public static readonly GuestDef Thief = new GuestDef
        {
            Id = "Thief",
            Tagline = "Охотится за чужим золотом",
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
            Tagline = "Голоден и очень зол",
            Name = "Волк",
            Description = "Кратчайшим путём бежит на кухню. Поймай ловушкой — +4 золота.",
            Color = RoomDatabase.Hex("9a8c80"),
            Behaviour = GuestBehaviour.Seeker,
            SeekTarget = RoomType.Kitchen,
            Trappable = true,
            CatchReward = 4,
            Moonstruck = true,
        };

        public static readonly GuestDef Gourmet = new GuestDef
        {
            Id = "Gourmet",
            Tagline = "Приехал ради ужина",
            Name = "Гурман",
            Description = "Живёт ради кухни и столовой, на сон ему всё равно. Выносливость 3, нужно настроение 7.",
            Color = RoomDatabase.Hex("d9a441"),
            Behaviour = GuestBehaviour.Wanderer,
            Stamina = 3,
            Need = 7,
            CanBecomeRegular = true,
            Likes = { { RoomType.Kitchen, 3 }, { RoomType.Dining, 3 }, { RoomType.WineCellar, 2 }, { RoomType.Cellar, 1 }, { RoomType.Bath, -1 } },
        };

        public static readonly GuestDef Family = new GuestDef
        {
            Id = "Family",
            Tagline = "Мама и шумный малыш",
            Name = "Семья",
            Description = "Ищут детскую и спальню, боятся подвала. Выносливость 5, нужно настроение 6.",
            Color = RoomDatabase.Hex("7cc4a0"),
            Behaviour = GuestBehaviour.Wanderer,
            Stamina = 5,
            Need = 6,
            CanBecomeRegular = true,
            Likes =
            {
                { RoomType.Nursery, 4 }, { RoomType.Bedroom, 2 }, { RoomType.Kitchen, 1 },
                { RoomType.Library, -1 }, { RoomType.Cellar, -3 }, { RoomType.WineCellar, -3 },
            },
        };

        public static readonly GuestDef Vampire = new GuestDef
        {
            Id = "Vampire",
            Tagline = "Ищет спящих гостей",
            Name = "Вампир",
            Description = "Кратчайшим путём крадётся в спальню. Дошёл — репутация −2. Поймай ловушкой — +6 золота.",
            Color = RoomDatabase.Hex("8a3a5c"),
            Behaviour = GuestBehaviour.Seeker,
            SeekTarget = RoomType.Bedroom,
            Trappable = true,
            CatchReward = 6,
            UnhappyPenalty = 2,
        };

        // ───── Особые гости ─────

        public static readonly GuestDef Inspector = new GuestDef
        {
            Id = "Inspector",
            Tagline = "Проверит каждую мелочь",
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
            Tagline = "Ищут уютный вечер",
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
            Tagline = "Весёлый, шумный, неловкий",
            Name = "Пьяница",
            Description = "Шатается куда попало, любит кухню и столовую. Может что-нибудь сломать. Нужно 2.",
            Color = RoomDatabase.Hex("b5654a"),
            Behaviour = GuestBehaviour.Random,
            Stamina = 5,
            Need = 2,
            Special = true,
            BreakChance = 50,
            Likes =
            {
                { RoomType.Kitchen, 2 }, { RoomType.Dining, 2 }, { RoomType.WineCellar, 3 },
                { RoomType.Lounge, 1 }, { RoomType.Library, -1 },
            },
        };

        // ───── Новые гости: открываются с определённого уровня ─────

        public static readonly GuestDef Cat = new GuestDef
        {
            Id = "Cat",
            Tagline = "Гуляет сама по себе",
            Name = "Кошка",
            Description = "Бродит где хочет. Довольная остаётся жить в доме — крысы больше не сунутся. Выносливость 4, нужно 3.",
            Color = RoomDatabase.Hex("e3a35a"),
            Behaviour = GuestBehaviour.Random,
            Stamina = 4,
            Need = 3,
            HouseCat = true,
            MinStage = 2,
            Likes = { { RoomType.Cellar, 3 }, { RoomType.Fireplace, 2 }, { RoomType.Kitchen, 1 }, { RoomType.Bath, -2 } },
        };

        public static readonly GuestDef Rats = new GuestDef
        {
            Id = "Rats",
            Tagline = "Шуршат за стеной",
            Name = "Крысы",
            Description = "Кратчайшим путём бегут в подвал: −2 золота, репутация −1. Ловушка поймает (+2), кошка в доме — даже не сунутся.",
            Color = RoomDatabase.Hex("7d6a5a"),
            Behaviour = GuestBehaviour.Seeker,
            SeekTarget = RoomType.Cellar,
            Trappable = true,
            CatchReward = 2,
            StealAmount = 2,
            FearsCat = true,
            MinStage = 2,
        };

        public static readonly GuestDef Aristocrat = new GuestDef
        {
            Id = "Aristocrat",
            Tagline = "Привык только к лучшему",
            Name = "Аристократ",
            Description = "Замечает только улучшенные комнаты (ур. II и выше), обычные ему безразличны. Платит вдвое. Нужно 6.",
            Color = RoomDatabase.Hex("9b7bd1"),
            Behaviour = GuestBehaviour.Wanderer,
            Stamina = 5,
            Need = 6,
            Special = true,
            Snob = true,
            GoldMultiplier = 2,
            CanBecomeRegular = true,
            MinStage = 4,
            Likes =
            {
                { RoomType.Lounge, 2 }, { RoomType.Library, 2 }, { RoomType.Bath, 2 },
                { RoomType.Bedroom, 2 }, { RoomType.WineCellar, 2 }, { RoomType.Dining, 1 },
            },
        };

        public static readonly GuestDef Arsonist = new GuestDef
        {
            Id = "Arsonist",
            Tagline = "Любит смотреть на огонь",
            Name = "Поджигатель",
            Description = "Кратчайшим путём идёт к камину. Дошёл — пожар: горят камин и все соседние комнаты, репутация −1. Поймай ловушкой — +5 золота.",
            Color = RoomDatabase.Hex("e0662f"),
            Behaviour = GuestBehaviour.Seeker,
            SeekTarget = RoomType.Fireplace,
            Trappable = true,
            CatchReward = 5,
            Arsonist = true,
            MinStage = 5,
        };

        public static readonly GuestDef Witch = new GuestDef
        {
            Id = "Witch",
            Tagline = "Варит зелья по ночам",
            Name = "Ведьма",
            Description = "Бродит где хочет, любит подвал и книги. Довольна — платит вдвое. Недовольна — превращает комнату, где закончила путь, в подвал. Нужно 5.",
            Color = RoomDatabase.Hex("5e8f4a"),
            Behaviour = GuestBehaviour.Random,
            Stamina = 5,
            Need = 5,
            Special = true,
            Curser = true,
            GoldMultiplier = 2,
            MinStage = 6,
            Likes =
            {
                { RoomType.Cellar, 3 }, { RoomType.WineCellar, 2 }, { RoomType.Library, 2 },
                { RoomType.Fireplace, 1 }, { RoomType.Nursery, -2 },
            },
        };

        public static readonly GuestDef GhostLord = new GuestDef
        {
            Id = "GhostLord",
            Tagline = "Настоящий хозяин дома",
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

        // Обычные гости: соперник берёт их поровну, без перекоса в какого-то одного.
        public static readonly GuestDef[] Pool =
        {
            Traveler, Scholar, Ghost, Thief, Wolf, Inspector, Couple, Drunkard, Gourmet, Family, Vampire,
            Cat, Rats, Aristocrat, Arsonist, Witch,
        };

        public const int BossChance = 5; // шанс хозяина дома вместо карты в две последние ночи

        public static readonly GuestDef[] All =
        {
            Traveler, Scholar, Ghost, Thief, Wolf, Inspector, Couple, Drunkard, Gourmet, Family, Vampire,
            Cat, Rats, Aristocrat, Arsonist, Witch, GhostLord,
        };

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
