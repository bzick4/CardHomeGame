using System.Collections.Generic;
using System.Linq;

namespace CardHome
{
    // Соперник: колода и рука карт гостей. Игрок видит только рубашки.
    // Каждую ночь соперник выкладывает гостей на вход по одному, выбирая карту
    // по-разному: чаще всего — самую неудобную для текущего дома (это его «злость», Malice).
    public class Opponent
    {
        readonly List<GuestDef> deckCards = new List<GuestDef>();
        public IReadOnlyList<GuestDef> Deck => deckCards;
        readonly List<GuestDef> handCards = new List<GuestDef>();
        public IReadOnlyList<GuestDef> Hand => handCards;

        readonly LevelInfo level;
        readonly System.Random rng;
        readonly List<GuestDef> template;

        public Opponent(LevelInfo level, System.Random rng, IEnumerable<GuestDef> customDeck = null)
        {
            this.level = level;
            this.rng = rng;
            // Все обычные гости — поровну, в любом доме. Боссы — только если дом (или странствие) их добавляет.
            template = customDeck != null
                ? customDeck.ToList()
                : GuestDatabase.Pool.Concat(level.House.Guests.Where(e => e.guest.Boss).Select(e => e.guest)).ToList();
            template.AddRange(level.ExtraGuests);

            // Колода конечная, как в ККИ: хватает на стартовую руку и все ночи дома с небольшим запасом.
            int needed = OpeningHand + 2;
            if (customDeck == null)
                for (int n = 1; n <= level.NightCount; n++) needed += level.GuestsOnNight(n);
            while (deckCards.Count < needed && template.Count > 0)
            {
                var batch = template.Select(level.Make).ToList();
                GuestDatabase.Shuffle(rng, batch);
                deckCards.AddRange(batch);
            }
        }

        public const int OpeningHand = 5;
        public const int MaxHand = 10;

        // Взять n карт. В последние ночи вместо карты изредка приходит хозяин дома.
        // Пустая колода — карт больше нет; полная рука — лишняя карта сгорает.
        public void Draw(int n, int night, bool allowSpecials)
        {
            for (int i = 0; i < n && deckCards.Count > 0; i++)
            {
                var g = deckCards[deckCards.Count - 1];
                deckCards.RemoveAt(deckCards.Count - 1);
                if (allowSpecials && night >= LevelInfo.Nights - 1 && rng.Next(100) < GuestDatabase.BossChance)
                    g = level.Make(GuestDatabase.GhostLord);
                if (handCards.Count < MaxHand) handCards.Add(g);
            }
        }

        // Восстановить колоду и руку из сохранения.
        public void Restore(IEnumerable<GuestDef> deck, IEnumerable<GuestDef> hand)
        {
            deckCards.Clear();
            deckCards.AddRange(deck);
            handCards.Clear();
            handCards.AddRange(hand);
        }

        // Сыграть гостя из руки: он уходит из руки и возвращается вызвавшему.
        public GuestDef Play(int index)
        {
            var g = handCards[index];
            handCards.RemoveAt(index);
            return g;
        }

        // Постоянный гость возвращается в колоду соперника.
        public void ShuffleIn(GuestDef g) => deckCards.Insert(rng.Next(deckCards.Count + 1), g);

        // Угрозы — с 3-й ночи, хозяин дома — только в две последние.
        static bool Playable(GuestDef g, int night) =>
            g.Boss ? night >= LevelInfo.Nights - 1 : !g.IsThreat || night >= 3;

        // Какую карту из руки сыграть. Если играть нечем — в руку приходит случайный гость, которого уже можно.
        public int Choose(HouseGrid grid, int night, float malice)
        {
            var playable = Enumerable.Range(0, handCards.Count).Where(i => Playable(handCards[i], night)).ToList();
            if (playable.Count == 0)
            {
                var allowed = GuestDatabase.Pool.Where(g => Playable(g, night)).ToList();
                handCards.Add(level.Make(allowed[rng.Next(allowed.Count)]));
                return handCards.Count - 1;
            }

            if (rng.NextDouble() >= malice) return playable[rng.Next(playable.Count)];

            // Злой выбор: гость, который принесёт игроку меньше всего (репутация весит как 6 золота).
            int best = playable[0];
            float bestScore = float.MaxValue;
            foreach (int i in playable)
            {
                var res = NightSimulator.Run(grid, handCards[i], new System.Random(1234));
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
