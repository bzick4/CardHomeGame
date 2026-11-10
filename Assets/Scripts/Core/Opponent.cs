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
                : Available(level).Concat(level.House.Guests.Where(e => e.guest.Boss).Select(e => e.guest)).ToList();
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

        // Обычные гости, которые уже открылись на этом уровне.
        static IEnumerable<GuestDef> Available(LevelInfo level) => GuestDatabase.Pool.Where(g => g.MinStage <= level.Stage);

        // Гости, которых соперник выложит первыми (план на первые ночи). Под них игрок получает стартовые карты.
        readonly List<GuestDef> planned = new List<GuestDef>();

        // Выбрать из руки до count мирных гостей, которых соперник сыграет первыми.
        public IReadOnlyList<GuestDef> PlanOpening(int count, int night)
        {
            planned.Clear();
            var options = handCards.Where(g => !g.IsThreat && Playable(g, night)).ToList();
            GuestDatabase.Shuffle(rng, options);
            planned.AddRange(options.Take(count));
            if (planned.Count > 0) Log("план на первые ночи: " + string.Join(", ", planned.Select(g => g.Name)));
            return planned;
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
            Log($"ночь {night}: взял {n}");
        }

        // Восстановить колоду и руку из сохранения.
        public void Restore(IEnumerable<GuestDef> deck, IEnumerable<GuestDef> hand)
        {
            deckCards.Clear();
            deckCards.AddRange(deck);
            handCards.Clear();
            handCards.AddRange(hand);
            Log("загружен из сохранения");
        }

        // Сыграть гостя из руки: он уходит из руки и возвращается вызвавшему.
        public GuestDef Play(int index)
        {
            var g = handCards[index];
            handCards.RemoveAt(index);
            Log($"выложил «{g.Name}»");
            return g;
        }

        // Постоянный гость возвращается в колоду соперника.
        public void ShuffleIn(GuestDef g)
        {
            deckCards.Insert(rng.Next(deckCards.Count + 1), g);
            Log($"«{g.Name}» вернулся в колоду");
        }

        // Угрозы — с 3-й ночи, хозяин дома — только в две последние.
        static bool Playable(GuestDef g, int night) =>
            g.Boss ? night >= LevelInfo.Nights - 1 : !g.IsThreat || night >= 3;

        // Какую карту из руки сыграть. Если играть нечем — в руку приходит случайный гость, которого уже можно.
        public int Choose(HouseGrid grid, int night, float malice)
        {
            // Сначала — запланированные гости, если они ещё в руке.
            while (planned.Count > 0)
            {
                var next = planned[0];
                planned.RemoveAt(0);
                int at = handCards.IndexOf(next);
                if (at >= 0 && Playable(next, night)) return at;
            }

            var playable = Enumerable.Range(0, handCards.Count).Where(i => Playable(handCards[i], night)).ToList();
            if (playable.Count == 0)
            {
                var allowed = Available(level).Where(g => Playable(g, night)).ToList();
                handCards.Add(level.Make(allowed[rng.Next(allowed.Count)]));
                Log($"играть нечем — в руку пришёл «{handCards[handCards.Count - 1].Name}»");
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

        // Отладка: состав колоды и руки в консоль. В релизной сборке вызовы вырезаются целиком.
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        void Log(string what)
        {
            UnityEngine.Debug.Log($"<b>[Соперник]</b> {what}\n" +
                $"Колода ({deckCards.Count}): {Count(deckCards)}\n" +
                $"Рука ({handCards.Count}): {Count(handCards)}");
        }

        static string Count(IEnumerable<GuestDef> cards)
        {
            var groups = cards.GroupBy(g => g.Name).OrderByDescending(x => x.Count()).ThenBy(x => x.Key)
                .Select(x => $"{x.Key} ×{x.Count()}").ToList();
            return groups.Count == 0 ? "пусто" : string.Join(", ", groups);
        }
    }
}
