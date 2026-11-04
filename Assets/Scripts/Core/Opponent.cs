using System.Collections.Generic;
using System.Linq;

namespace CardHome
{
    // Соперник: колода и рука карт гостей. Игрок видит только рубашки.
    // Каждую ночь соперник выкладывает гостей на вход по одному, выбирая карту
    // по-разному: чаще всего — самую неудобную для текущего дома (это его «злость», Malice).
    public class Opponent
    {
        public readonly List<GuestDef> Deck = new List<GuestDef>();
        public readonly List<GuestDef> Hand = new List<GuestDef>();

        readonly LevelInfo level;
        readonly System.Random rng;
        readonly List<GuestDef> template;

        public Opponent(LevelInfo level, System.Random rng, IEnumerable<GuestDef> customDeck = null)
        {
            this.level = level;
            this.rng = rng;
            template = customDeck != null
                ? customDeck.ToList()
                : level.House.Guests.SelectMany(e => Enumerable.Repeat(e.guest, e.count)).ToList();
            template.AddRange(level.ExtraGuests);

            // Колода конечная, как в ККИ: хватает на стартовую руку и все ночи дома с небольшим запасом.
            int needed = OpeningHand + 2;
            if (customDeck == null)
                for (int n = 1; n <= level.NightCount; n++) needed += level.GuestsOnNight(n);
            while (Deck.Count < needed && template.Count > 0)
            {
                var batch = template.Select(level.Make).ToList();
                GuestDatabase.Shuffle(rng, batch);
                Deck.AddRange(batch);
            }
        }

        public const int OpeningHand = 5;
        public const int MaxHand = 10;

        // Взять n карт. Со 2-й ночи вместо карты с шансом 15% приходит особый гость.
        // Пустая колода — карт больше нет; полная рука — лишняя карта сгорает.
        public void Draw(int n, int night, bool allowSpecials)
        {
            for (int i = 0; i < n && Deck.Count > 0; i++)
            {
                var g = Deck[Deck.Count - 1];
                Deck.RemoveAt(Deck.Count - 1);
                if (allowSpecials && night >= 2 && rng.Next(100) < GuestDatabase.SpecialChance)
                {
                    bool boss = night >= LevelInfo.Nights - 1 && rng.Next(100) < 30;
                    var special = boss ? GuestDatabase.GhostLord : GuestDatabase.Specials[rng.Next(GuestDatabase.Specials.Length)];
                    g = level.Make(special);
                }
                if (Hand.Count < MaxHand) Hand.Add(g);
            }
        }

        // Постоянный гость возвращается в колоду соперника.
        public void ShuffleIn(GuestDef g) => Deck.Insert(rng.Next(Deck.Count + 1), g);

        // Угрозы — с 3-й ночи, хозяин дома — только в две последние.
        public static bool Playable(GuestDef g, int night) =>
            g.Boss ? night >= LevelInfo.Nights - 1 : !g.IsThreat || night >= 3;

        // Какую карту из руки сыграть. Если играть нечем — в руку приходит путник.
        public int Choose(HouseGrid grid, int night, float malice)
        {
            var playable = Enumerable.Range(0, Hand.Count).Where(i => Playable(Hand[i], night)).ToList();
            if (playable.Count == 0)
            {
                Hand.Add(level.Make(GuestDatabase.Traveler));
                return Hand.Count - 1;
            }

            if (rng.NextDouble() >= malice) return playable[rng.Next(playable.Count)];

            // Злой выбор: гость, который принесёт игроку меньше всего (репутация весит как 6 золота).
            int best = playable[0];
            float bestScore = float.MaxValue;
            foreach (int i in playable)
            {
                var res = NightSimulator.Run(grid, Hand[i], new System.Random(1234));
                float score = res.Gold + res.Reputation * 6 + (float)rng.NextDouble() * 0.5f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best;
        }
    }
}
