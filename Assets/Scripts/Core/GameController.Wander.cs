using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CardHome
{
    // Режим «Странствие» — рогалик. Забег идёт по случайной карте с развилками снизу вверх:
    // дома по 3 ночи, ярмарки, наверху — Хозяин. Колода, золото и репутация переходят из точки в точку,
    // поражение заканчивает забег. Сохраняется на карте (между точками).
    public partial class GameController
    {
        public const int WanderNights = 3;
        public const int WanderStartReputation = 3;
        public const int FairCardPrice = 5, FairRarePrice = 9;
        public const int WanderMaxReputation = 6;
        public const int EliteGold = 8, HouseGold = 3;

        public bool IsWander { get; private set; }
        public RunMap Map { get; private set; }
        public WanderSave Wander { get; private set; }
        public bool HouseFinished { get; private set; }
        // Уровень карт в награде: в опасном доме после последней ночи — бронзовые.
        public int RewardLevel => IsWander && HouseFinished && activeNode != null && activeNode.Kind == NodeKind.Elite ? 2 : 1;
        public WanderEvent CurrentEvent { get; private set; }
        public string EventResult { get; private set; }
        NodeKind ActiveKind => activeNode != null ? activeNode.Kind : NodeKind.House;
        public bool HasWanderSave => SaveSystem.HasWander;
        public int RunDeckSize => runDeck.Count;

        // Товары ярмарки: карта и цена; проданные убираются.
        readonly List<(Card card, int price)> fairOffers = new List<(Card card, int price)>();
        public IReadOnlyList<(Card card, int price)> FairOffers => fairOffers;

        readonly List<Card> runDeck = new List<Card>();
        MapNode activeNode;

        public MapNode CurrentNode => Wander != null && Map != null ? Map.Get(Wander.Row, Wander.Col) : null;

        // Продолжить сохранённый забег или начать новый.
        public void StartWander()
        {
            var save = SaveSystem.LoadWander();
            if (save == null)
            {
                NewWander();
                return;
            }
            IsWander = true;
            Wander = save;
            Map = RunMap.Generate(save.Seed);
            runDeck.Clear();
            runDeck.AddRange(save.Deck.Select(Card.FromCode));
            Gold = save.Gold;
            Reputation = save.Reputation;
            ShowMap();
        }

        void NewWander()
        {
            IsWander = true;
            int seed = rng.Next(1, int.MaxValue);
            Map = RunMap.Generate(seed);
            runDeck.Clear();
            runDeck.AddRange(HouseDatabase.All[1].BuildDeck().Select(t => new Card(t))); // стартовая колода — как в Усадьбе
            Wander = new WanderSave
            {
                Seed = seed,
                Gold = 0,
                Reputation = WanderStartReputation,
                Deck = runDeck.Select(c => c.Code).ToList(),
            };
            Gold = 0;
            Reputation = WanderStartReputation;
            SaveSystem.SaveWander(Wander);
            ShowMap();
        }

        // Сдаться: забег окончен, сохранение стирается.
        public void AbandonWander()
        {
            SaveSystem.ClearWander();
            Wander = null;
            IsWander = false;
            ShowMenu();
        }

        void ShowMap()
        {
            LeaveGameView();
            if (view == null || IsTutorial)
            {
                IsTutorial = false;
                Gate = null;
                ResetView();
            }
            EnsureView();
            activeNode = null;
            Selected = -1;
            MovingFrom = null;
            SetPhase(Phase.Map);
            Changed?.Invoke();
            Hint("wander", "Странствие",
                "Это рогалик-режим. Иди по карте снизу вверх: каждая точка — дом на 3 ночи или ярмарка. " +
                "Наверху ждёт Хозяин.\n\nКолода, золото и репутация идут с тобой из дома в дом. " +
                "Комнаты в каждом доме строишь заново. Репутация упала до нуля — забег окончен.");
        }

        // Куда можно пойти: в начале — любая точка первого ряда, дальше — только по тропам из текущей.
        public bool CanEnter(MapNode n)
        {
            if (n == null || Wander == null) return false;
            if (Wander.Row < 0) return n.Row == 0;
            var cur = CurrentNode;
            return cur != null && n.Row == cur.Row + 1 && cur.Next.Contains(n.Col);
        }

        public bool Visited(MapNode n) => Wander != null && Wander.Path.Contains(n.Row * 10 + n.Col);

        public void EnterNode(int row, int col)
        {
            if (State != Phase.Map) return;
            var n = Map.Get(row, col);
            if (!CanEnter(n)) return;
            activeNode = n;

            switch (n.Kind)
            {
                case NodeKind.Fair: OpenFair(); return;
                case NodeKind.Rest: OpenRest(); return;
                case NodeKind.Event: OpenEvent(); return;
            }

            // Дом: чем выше по карте, тем злее соперник и тем больше гостей.
            bool elite = n.Kind == NodeKind.Elite;
            int tier = n.Row / 3 + (elite ? 1 : 0);
            var level = new LevelInfo
            {
                Index = tier * HouseDatabase.Count + n.House,
                House = HouseDatabase.All[n.House],
                Tier = tier,
                NightCount = WanderNights,
                NightOffset = Mathf.Min(4, n.Row / 2 + (n.Kind == NodeKind.Boss ? 4 : 0) + (elite ? 2 : 0)),
                MaliceBonus = elite ? 0.25f : 0,
                TitleOverride = n.Kind == NodeKind.Boss ? $"Логово Хозяина · {HouseDatabase.All[n.House].Name}"
                    : elite ? $"Опасно! {HouseDatabase.All[n.House].Name} · шаг {n.Row + 1}"
                    : $"{HouseDatabase.All[n.House].Name} · шаг {n.Row + 1}",
            };
            if (elite)
                Hint("elite", "Опасный дом",
                    "Здесь соперник злее и гостей больше. Зато в конце — +8 золота и выбор из бронзовых карт.");
            if (n.Kind == NodeKind.Boss)
            {
                level.ExtraGuests.Add(GuestDatabase.GhostLord);
                level.ExtraGuests.Add(GuestDatabase.GhostLord);
            }
            HouseFinished = false;
            StartHouse(level, runDeck, Reputation, Gold);
        }

        // После итогов ночи в доме странствия.
        void WanderAfterNight()
        {
            if (Reputation <= 0)
            {
                int steps = Wander.Path.Count;
                SaveSystem.ClearWander();
                EndGame($"Странствие окончено: репутация упала до нуля.\nПройдено точек: {steps}. Золото: {Gold}", false);
                return;
            }

            Night++;
            if (Night > MaxNights)
            {
                HouseFinished = true;
                if (activeNode != null && activeNode.Kind == NodeKind.Boss)
                {
                    SaveSystem.ClearWander();
                    EndGame($"Хозяин повержен! Странствие пройдено.\nЗолото: {Gold}, репутация: {Reputation}", true);
                    return;
                }
                int bonus = ActiveKind == NodeKind.Elite ? EliteGold : HouseGold;
                Gold += bonus;
                AddLog($"<color=#ffd34d>Дом пройден! +{bonus} золота. Выбери карту в колоду странствия.</color>");
            }

            // Награда после каждой ночи; после последней — карта в колоду и обратно на карту.
            rewardOptions.Clear();
            rewardOptions.AddRange(RoomDatabase.RewardOffer(rng, 3));
            SetPhase(Phase.Reward);
            Changed?.Invoke();
        }

        // Точка пройдена: запоминаем путь и состояние и возвращаемся на карту.
        void CompleteNode()
        {
            if (activeNode == null)
            {
                ShowMap();
                return;
            }
            Wander.Row = activeNode.Row;
            Wander.Col = activeNode.Col;
            Wander.Path.Add(activeNode.Row * 10 + activeNode.Col);
            Wander.Gold = Gold;
            Wander.Reputation = Reputation;
            Wander.Deck = runDeck.Select(c => c.Code).ToList();
            SaveSystem.SaveWander(Wander);
            HouseFinished = false;
            handCards.Clear();
            ShowMap();
        }

        // ───────────── Ярмарка ─────────────

        void OpenFair()
        {
            fairOffers.Clear();
            var types = RoomDatabase.RewardOffer(rng, 3);
            for (int i = 0; i < types.Count; i++)
            {
                // Одна карта на ярмарке — сразу бронзовая, подороже.
                bool rare = i == types.Count - 1 && RoomDatabase.Get(types[i]).CanUpgrade;
                fairOffers.Add((new Card(types[i], rare ? 2 : 1), rare ? FairRarePrice : FairCardPrice));
            }
            SetPhase(Phase.Fair);
            Changed?.Invoke();
            Hint("fair", "Ярмарка",
                "Здесь можно купить новые карты, улучшить или убрать карты из колоды странствия. " +
                "Золото здесь — главное, на что его стоит копить в забеге.");
        }

        public void BuyFairCard(int index)
        {
            if (State != Phase.Fair || index < 0 || index >= fairOffers.Count) return;
            var (card, price) = fairOffers[index];
            if (!Pay(price)) return;
            runDeck.Add(card);
            fairOffers.RemoveAt(index);
            AddLog($"Ярмарка: куплена карта «{card.Title}» (−{price})");
            Changed?.Invoke();
        }

        public void LeaveFair()
        {
            if (State != Phase.Fair) return;
            CompleteNode();
        }

        // ───────────── Привал ─────────────

        void OpenRest()
        {
            SetPhase(Phase.Rest);
            Changed?.Invoke();
            Hint("rest", "Привал",
                "Выбери одно: отдохнуть (+1 ♥, максимум " + WanderMaxReputation + ") или бесплатно улучшить любую карту в мастерской.");
        }

        public bool CanRestHeal => Reputation < WanderMaxReputation;

        public void RestHeal()
        {
            if (State != Phase.Rest || !CanRestHeal) return;
            Reputation++;
            AddLog("Привал: ты отдохнул, +1 ♥");
            CompleteNode();
        }

        // Мастерская на привале: улучшить одну карту бесплатно.
        public void RestUpgrade(Card card)
        {
            if (State != Phase.Rest || !card.CanLevelUp) return;
            int i = runDeck.IndexOf(card);
            if (i < 0) return;
            runDeck[i] = new Card(card.Type, card.Level + 1);
            AddLog($"<color=#ffd34d>Мастерская: улучшена карта {runDeck[i].Title}</color>");
            CompleteNode();
        }

        // ───────────── Случай ─────────────

        void OpenEvent()
        {
            // Не повторяем случаи в одном забеге, пока есть новые.
            var pool = Enumerable.Range(0, WanderEvents.All.Length).Where(i => !Wander.SeenEvents.Contains(i)).ToList();
            if (pool.Count == 0)
            {
                Wander.SeenEvents.Clear();
                pool = Enumerable.Range(0, WanderEvents.All.Length).ToList();
            }
            int pick = pool[rng.Next(pool.Count)];
            Wander.SeenEvents.Add(pick);
            CurrentEvent = WanderEvents.All[pick];
            EventResult = null;
            SetPhase(Phase.Event);
            Changed?.Invoke();
        }

        public bool CanChoose(int index)
        {
            if (CurrentEvent == null || EventResult != null || index < 0 || index >= CurrentEvent.Choices.Length) return false;
            var c = CurrentEvent.Choices[index];
            return c.Can == null || c.Can(this);
        }

        public void ChooseEvent(int index)
        {
            if (State != Phase.Event || !CanChoose(index)) return;
            EventResult = CurrentEvent.Choices[index].Apply(this);
            AddLog($"{CurrentEvent.Title}: {EventResult}");
            Changed?.Invoke();
        }

        public void FinishEvent()
        {
            if (State != Phase.Event || EventResult == null) return;
            CurrentEvent = null;
            EventResult = null;
            if (Reputation <= 0)
            {
                int steps = Wander.Path.Count;
                SaveSystem.ClearWander();
                EndGame($"Странствие окончено: репутация упала до нуля.\nПройдено точек: {steps}. Золото: {Gold}", false);
                return;
            }
            CompleteNode();
        }

        // ───────────── Последствия случаев ─────────────

        public bool Chance(int percent) => rng.Next(100) < percent;

        public void ChangeGold(int delta) => Gold = Mathf.Max(0, Gold + delta);

        public void ChangeReputation(int delta) => Reputation = Mathf.Min(WanderMaxReputation, Reputation + delta);

        public Card AddRunCard(Card card)
        {
            runDeck.Add(card);
            return card;
        }

        // Случайная карта из наградного набора нужного уровня.
        public Card AddRandomRunCard(int level)
        {
            var type = RoomDatabase.RewardOffer(rng, 1)[0];
            if (!RoomDatabase.Get(type).CanUpgrade) level = 1;
            return AddRunCard(new Card(type, level));
        }

        public Card[] UpgradeRandomRunCards(int count)
        {
            var result = new List<Card>();
            for (int k = 0; k < count; k++)
            {
                var options = Enumerable.Range(0, runDeck.Count).Where(i => runDeck[i].CanLevelUp).ToList();
                if (options.Count == 0) break;
                int i = options[rng.Next(options.Count)];
                runDeck[i] = new Card(runDeck[i].Type, runDeck[i].Level + 1);
                result.Add(runDeck[i]);
            }
            return result.ToArray();
        }

        // Выбросить самую слабую карту: сначала коридоры, потом — самую низкоуровневую.
        public Card? RemoveWeakestRunCard()
        {
            if (runDeck.Count <= MinDeck) return null;
            var weakest = runDeck.OrderBy(c => c.Def.IsPassage ? 0 : 1).ThenBy(c => c.Level).First();
            runDeck.Remove(weakest);
            return weakest;
        }
    }
}
