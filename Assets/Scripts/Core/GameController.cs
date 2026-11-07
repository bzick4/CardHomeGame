using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CardHome
{
    public enum Phase { Menu, Levels, Day, Night, Summary, Reward, LevelDone, GameOver, Map, Fair, Rest, Event }

    // Что сейчас происходит ночью.
    public enum NightStep { None, Reveal, Response, Walk }

    // Что случилось за ночь — для экрана итогов.
    public class NightReport
    {
        public List<GuestResult> Results { get; } = new List<GuestResult>();
        public int GoldBefore { get; init; }
        public int ReputationBefore { get; init; }
        public int TreasuryGold { get; internal set; }
        public List<string> Notes { get; } = new List<string>();
    }

    // Ограничения обучения: что игроку сейчас можно нажать. null у контроллера — можно всё.
    public class TutorialGate
    {
        public int Card { get; internal set; } = -1;          // индекс карты в руке, которую можно выбрать
        public Vector2Int? Cell { get; internal set; }       // клетка, которую можно нажать
        public bool Rotate { get; internal set; }
        public bool Night { get; internal set; }
        public bool LetIn { get; internal set; }
    }

    // Состояние игры и правила. Ничего не рисует и не ждёт кадров:
    // меняет данные и сообщает об этом событиями. Интерфейс подписывается и анимирует.
    //
    // Ход партии: днём игрок строит (2 хода). Ночью соперник выкладывает карты гостей на вход
    // по одной; на каждую игрок отвечает одним ходом (или сразу впускает гостя), потом гость обходит дом.
    public partial class GameController : MonoBehaviour
    {
        // Как в ККИ: стартовая рука, добор по ходам, лимит руки, усталость при пустой колоде.
        public const int OpeningHand = 5;
        public const int DayDraw = 2;     // утром (кроме первого дня)
        public const int NightDraw = 1;   // перед ночью
        public const int MaxHand = 10;
        public const int ActionsPerDay = 2;
        public const int ResponseActions = 1;
        public const int MinDeck = 8;
        public const int MaxHeirlooms = 3;

        // Цены в лавке.
        public const int RerollPrice = 2, ExtraActionPrice = 4, RemovePrice = 3, RepairPrice = 3;
        // Улучшение карты: до II — 4, до III — 7, до IV — 11, до V — 16… каждый шаг дороже на 1 больше прошлого.
        public static int UpgradePrice(int toLevel)
        {
            int n = Mathf.Max(0, toLevel - 2);
            return 4 + n * 3 + n * (n - 1) / 2;
        }

        // ── События ──
        public event Action Changed;
        public event Action<Phase> PhaseChanged;
        public event Action<string> LogAdded;
        // Комнату построили, перенесли, повернули, улучшили или починили.
        public event Action<Vector2Int> RoomPlaced;
        // Гость сломал комнату.
        public event Action<Vector2Int> RoomBroken;
        // Соперник выложил карту гостя (индекс — из какой позиции руки). Интерфейс показывает карту
        // и вызывает BeginResponse.
        public event Action<GuestDef, int> GuestPlayed;
        // Гость вошёл: маршрут посчитан. Интерфейс проигрывает его и вызывает GuestDone.
        public event Action<GuestResult> GuestWalk;
        // Одноразовая подсказка о новой механике.
        public event Action<string, string> HintRequested;

        // ── Партия ──
        public HouseGrid Grid { get; private set; } = new HouseGrid();
        readonly List<Card> handCards = new List<Card>();
        public IReadOnlyList<Card> Hand => handCards;
        public int Selected { get; private set; } = -1;
        public Doors SelectedDoors { get; private set; }
        public int ActionsLeft { get; private set; }
        public int Night { get; internal set; }
        public int Gold { get; private set; }
        public int Reputation { get; private set; }
        public Phase State { get; private set; }
        public NightStep Step { get; private set; }
        public string GameOverText { get; private set; }
        public bool Won { get; private set; }
        readonly List<string> logLines = new List<string>();
        public IReadOnlyList<string> Log => logLines;
        readonly List<RoomType> rewardOptions = new List<RoomType>();
        public IReadOnlyList<RoomType> RewardOptions => rewardOptions;
        readonly List<RoomType> inheritOptions = new List<RoomType>();
        public IReadOnlyList<RoomType> InheritOptions => inheritOptions;

        public LevelInfo Level { get; private set; } = HouseDatabase.Level(0);
        public Opponent Opp { get; private set; }
        public GuestDef CurrentGuest { get; private set; }
        public int GuestsLeftTonight { get; private set; }
        public int LastStars { get; private set; }
        public int NewBack { get; private set; }  // рубашка, полученная за последний дом (0 — ничего нового)
        public int OpponentBack => CardBacks.CodeForLevel(Level.Index);

        public ProgressData Progress { get; private set; }
        public bool IsTutorial { get; private set; }
        public TutorialGate Gate { get; internal set; }
        public NightReport Report { get; private set; }
        public bool ExtraActionBought { get; private set; }
        // Комната на доске, которую сейчас переносим или поворачиваем.
        public Vector2Int? MovingFrom { get; private set; }

        public int MaxNights => IsTutorial ? 1 : Level.NightCount;
        public int DeckCount => OnRunDeck ? runDeck.Count : deck.Count;
        public int DeckSize => OnRunDeck ? runDeck.Count : deck.Count + discard.Count + handCards.Count;
        public bool Responding => State == Phase.Night && Step == NightStep.Response;

        readonly List<Card> deck = new List<Card>();
        readonly List<Card> discard = new List<Card>();
        int fatigue;
        readonly System.Random rng = new System.Random();
        GameView view;
        int bonusActions;

        // Ввод с клавиатуры/мыши для редактора — через события Input System, без опроса в Update.
        InputAction rotateAction, nightAction;

        void Awake()
        {
            rotateAction = new InputAction("Rotate", InputActionType.Button);
            rotateAction.AddBinding("<Keyboard>/r");
            rotateAction.AddBinding("<Mouse>/rightButton");
            rotateAction.performed += _ => Rotate();

            nightAction = new InputAction("Night", InputActionType.Button, "<Keyboard>/space");
            nightAction.performed += _ =>
            {
                if (State == Phase.Day) StartNight();
                else LetGuestIn();
            };
        }

        void OnEnable()
        {
            rotateAction.Enable();
            nightAction.Enable();
        }

        void OnDisable()
        {
            rotateAction.Disable();
            nightAction.Disable();
        }

        void OnDestroy()
        {
            rotateAction.Dispose();
            nightAction.Dispose();
        }

        void Start()
        {
            Progress = SaveSystem.LoadProgress();
            // Рубашку, выбранную когда-то в тестовом режиме коллекции (его больше нет), но не полученную, — сбросить.
            if (!OwnsBack(Progress.EquippedBack)) Progress.EquippedBack = CardBacks.DefaultCode;
            ApplySettings();
            ShowMenu(); // игра всегда начинается с главного меню
        }

        void EnsureView()
        {
            if (view == null) view = GameView.Create(this);
        }

        void ResetView()
        {
            if (view != null)
            {
                view.Unbind();
                Destroy(view.gameObject);
            }
            view = null;
        }

        void SetPhase(Phase phase)
        {
            State = phase;
            if (phase != Phase.Night) Step = NightStep.None;
            PhaseChanged?.Invoke(phase);
        }

        // ───────────── Меню ─────────────

        // ───────────── Загрузка ─────────────
        // Партия и меню не живут одновременно: из партии в меню — партия снимается с экрана и её картинки
        // выгружаются; перед домом — меню снимается, а картинки дома готовятся под экраном загрузки.

        bool loading;

        static bool InGamePhase(Phase p) =>
            p == Phase.Day || p == Phase.Night || p == Phase.Summary || p == Phase.Reward || p == Phase.LevelDone || p == Phase.GameOver;

        void LeaveGameView()
        {
            if (view == null || !InGamePhase(State)) return;
            ResetView();
            ArtStore.Release(ArtScope.Level);
        }

        void LoadThen(LevelInfo level, string title, Action start)
        {
            if (loading) return;
            loading = true;
            ResetView();
            ArtStore.Release(ArtScope.Screen);
            ArtStore.Release(ArtScope.Level);
            var screen = LoadingScreen.Show(title);
            ArtStore.Preload(ArtStore.ForLevel(level, Progress.EquippedBack), screen.SetProgress, () =>
            {
                loading = false;
                // Уже партия: интерфейс создаётся без экранов меню.
                State = Phase.Day;
                start();
                screen.Hide();
            });
        }

        public void ShowMenu()
        {
            if (State == Phase.Night || loading) return;
            if (State == Phase.Day && !IsTutorial) SaveRun();
            if (IsTutorial)
            {
                IsTutorial = false;
                Gate = null;
                ResetView();
                ArtStore.Release(ArtScope.Level);
            }
            LeaveGameView();
            EnsureView();
            Selected = -1;
            MovingFrom = null;
            SetPhase(Phase.Menu);
            Changed?.Invoke();
        }

        void ShowLevels()
        {
            if (loading) return;
            LeaveGameView();
            EnsureView();
            SetPhase(Phase.Levels);
            Changed?.Invoke();
        }

        public void ToggleSound()
        {
            Sfx.Muted = !Sfx.Muted;
            Progress.Muted = Sfx.Muted;
            SaveSystem.SaveProgress(Progress);
            Changed?.Invoke();
        }

        public void ToggleReduceEffects()
        {
            Progress.ReduceEffects = !Progress.ReduceEffects;
            ApplySettings();
            SaveSystem.SaveProgress(Progress);
            Changed?.Invoke();
        }

        public void ToggleVibration()
        {
            Progress.NoVibration = !Progress.NoVibration;
            ApplySettings();
            SaveSystem.SaveProgress(Progress);
            if (Haptic.Enabled) Haptic.Medium(); // включили — сразу почувствовать
            Changed?.Invoke();
        }

        void ApplySettings()
        {
            Sfx.Muted = Progress.Muted;
            Haptic.Enabled = !Progress.NoVibration;
            Juice.Reduced = Progress.ReduceEffects;
        }

        // Новые данные прогресса с прежними настройками (звук, вибрация, эффекты).
        ProgressData FreshProgress() => new ProgressData { Muted = Progress.Muted, NoVibration = Progress.NoVibration, ReduceEffects = Progress.ReduceEffects };

        public void ResetProgress()
        {
            SaveSystem.ResetAll();
            Progress = FreshProgress();
            Progress.TutorialDone = true;
            SaveSystem.SaveProgress(Progress);
            Changed?.Invoke();
        }

        public bool IsUnlocked(int level) => level <= Progress.Unlocked;

        // Есть ли что продолжать: пройденное обучение, открытые дома или начатый день.
        public bool HasProgress => Progress.TutorialDone || Progress.Unlocked > 0 || SaveSystem.HasRun;

        // «Новая игра»: всё с нуля (кроме настроек) и сразу обучение.
        public void NewGame()
        {
            var fresh = FreshProgress();
            SaveSystem.ResetAll();
            Progress = fresh;
            SaveSystem.SaveProgress(Progress);
            StartTutorial();
        }

        // «Продолжить»: на карту домов (оттуда же продолжается начатый день).
        public void ContinueGame()
        {
            if (!Progress.TutorialDone)
            {
                Progress.TutorialDone = true;
                SaveSystem.SaveProgress(Progress);
            }
            ShowLevels();
        }

        // Пропустить обучение в любой момент.
        public void SkipTutorial()
        {
            if (!IsTutorial) return;
            AddLog("Обучение пропущено");
            FinishTutorial();
        }

        // Номер уровня, на котором есть начатый день (−1 — нет).
        public int SavedLevel
        {
            get
            {
                var s = SaveSystem.LoadRun();
                return s != null ? s.Level : -1;
            }
        }

        // ───────────── Рубашки ─────────────

        public bool OwnsBack(int code) => code == CardBacks.DefaultCode || Progress.Backs.Contains(code);

        public void EquipBack(int code)
        {
            if (!OwnsBack(code)) return;
            Progress.EquippedBack = code;
            SaveSystem.SaveProgress(Progress);
            Changed?.Invoke();
        }


        // ───────────── Уровень ─────────────

        public void StartLevel(int index)
        {
            if (!IsUnlocked(index) || loading) return;
            IsWander = false;
            var level = HouseDatabase.Level(index);
            var cards = level.House.BuildDeck().Select(t => new Card(t))
                .Concat(Progress.Heirlooms.Select(t => new Card((RoomType)t)));
            StartHouse(level, cards, level.StartReputation, level.House.StartGold);
        }

        // Общий старт дома — и для кампании, и для странствия: сначала загрузка его картинок.
        void StartHouse(LevelInfo level, IEnumerable<Card> cards, int reputation, int gold)
        {
            var list = cards.ToList();
            LoadThen(level, level.Title, () => StartHouseNow(level, list, reputation, gold));
        }

        void StartHouseNow(LevelInfo level, IEnumerable<Card> cards, int reputation, int gold)
        {
            ResetView();
            IsTutorial = false;
            Gate = null;
            Level = level;
            var house = Level.House;

            Grid = new HouseGrid();
            foreach (var (pos, type, doors) in house.Rooms) Grid.Place(pos, type, doors);

            deck.Clear();
            deck.AddRange(cards);
            Shuffle(deck);
            fatigue = 0;
            discard.Clear();
            handCards.Clear();
            rewardOptions.Clear();
            logLines.Clear();
            bonusActions = 0;
            Night = 1;
            Reputation = reputation;
            Gold = gold;
            Opp = new Opponent(Level, rng);

            EnsureView();
            AddLog($"<b>{Level.Title}. Продержись {MaxNights} ночей.</b>");
            if (!IsWander && Progress.Heirlooms.Count > 0)
                AddLog($"<color=#ffd34d>Наследство в колоде: {string.Join(", ", Progress.Heirlooms.Select(t => RoomDatabase.Get((RoomType)t).Name))}</color>");
            OpeningDeal();
        }

        public void RetryLevel()
        {
            if (IsWander) NewWander();
            else StartLevel(Level.Index);
        }

        // Продолжить сохранённую партию (сохраняется днём, после каждого хода).
        public void ContinueRun()
        {
            var save = SaveSystem.LoadRun();
            if (save == null) return;
            var level = HouseDatabase.Level(save.Level);
            LoadThen(level, level.Title, () => ContinueRunNow(save));
        }

        void ContinueRunNow(RunSave save)
        {
            IsWander = false;
            ResetView();
            IsTutorial = false;
            Gate = null;
            Level = HouseDatabase.Level(save.Level);

            Grid = new HouseGrid();
            Grid.Remove(Grid.Entrance);
            foreach (var r in save.Rooms)
            {
                var p = new Vector2Int(r.X, r.Y);
                Grid.Place(p, (RoomType)r.Type, (Doors)r.Doors);
                var room = Grid.Get(p);
                room.Level = r.Level;
                room.Broken = r.Broken;
            }

            deck.Clear();
            deck.AddRange(save.Deck.Select(Card.FromCode));
            discard.Clear();
            discard.AddRange(save.Discard.Select(Card.FromCode));
            handCards.Clear();
            handCards.AddRange(save.Hand.Select(Card.FromCode));
            fatigue = save.Fatigue;
            Opp = new Opponent(Level, rng);
            Opp.Restore(save.OppDeck.Select(DecodeGuest), save.OppHand.Select(DecodeGuest));

            Night = save.Night;
            Gold = save.Gold;
            Reputation = save.Reputation;
            ActionsLeft = save.Actions;
            Selected = -1;
            MovingFrom = null;
            rewardOptions.Clear();
            logLines.Clear();

            EnsureView();
            AddLog($"<b>{Level.Title}. День {Night}: продолжаем.</b>");
            SetPhase(Phase.Day);
            Changed?.Invoke();
        }

        string EncodeGuest(GuestDef g) => g.Id + (g.Regular ? "|R" : "");

        GuestDef DecodeGuest(string s)
        {
            var parts = s.Split('|');
            var g = Level.Make(GuestDatabase.ById(parts[0]));
            return parts.Length > 1 ? g.AsRegular() : g;
        }

        void SaveRun()
        {
            if (IsTutorial || IsWander || Opp == null) return;
            var save = new RunSave
            {
                Level = Level.Index, Night = Night, Gold = Gold, Reputation = Reputation, Actions = ActionsLeft,
                Deck = deck.Select(c => c.Code).ToList(),
                Discard = discard.Select(c => c.Code).ToList(),
                Hand = handCards.Select(c => c.Code).ToList(),
                Fatigue = fatigue,
                OppDeck = Opp.Deck.Select(EncodeGuest).ToList(),
                OppHand = Opp.Hand.Select(EncodeGuest).ToList(),
            };
            foreach (var p in Grid.AllPositions())
            {
                var r = Grid.Get(p);
                save.Rooms.Add(new RoomSave { X = p.x, Y = p.y, Type = (int)r.Type, Doors = (int)r.Doors, Level = r.Level, Broken = r.Broken });
            }
            SaveSystem.SaveRun(save);
        }

        // ───────────── Обучение (день 0) ─────────────

        public void StartTutorial() => LoadThen(HouseDatabase.Level(0), "Обучение", StartTutorialNow);

        void StartTutorialNow()
        {
            IsWander = false;
            ResetView();
            IsTutorial = true;
            Gate = new TutorialGate();
            Level = HouseDatabase.Level(0);
            Grid = new HouseGrid();
            logLines.Clear();
            rewardOptions.Clear();

            // Рука берётся с конца колоды: выйдет Кухня, Спальня, Коридор, Ловушка, Ванная.
            deck.Clear();
            deck.AddRange(new[] { RoomType.Lounge, RoomType.Dining, RoomType.Bath, RoomType.Trap, RoomType.Corridor, RoomType.Bedroom, RoomType.Kitchen }
                .Select(t => new Card(t)));
            fatigue = 0;
            discard.Clear();
            handCards.Clear();
            bonusActions = 0;
            Night = 1;
            Gold = 0;
            Reputation = 3;
            Opp = new Opponent(Level, rng, new[] { GuestDatabase.Traveler });

            EnsureView();
            AddLog("<b>День 0. Обучение.</b>");
            OpeningDeal();
        }

        void FinishTutorial()
        {
            Progress.TutorialDone = true;
            SaveSystem.SaveProgress(Progress);
            // Об этом уже рассказало обучение.
            Storage.SetInt(HintKey("response"), 1);
            Storage.Save();
            IsTutorial = false;
            Gate = null;
            ResetView();
            ArtStore.Release(ArtScope.Level);
            EnsureView();
            SetPhase(Phase.Levels);
            Changed?.Invoke();
        }

        // ───────────── День ─────────────

        // Начало дома: руки пусты, у каждого только колода. Раздаём по 5 карт — интерфейс показывает это анимацией.
        void OpeningDeal()
        {
            handCards.Clear();
            Draw(OpeningHand, false);
            Opp.Draw(Opponent.OpeningHand, 1, false);
            AddLog($"Раздача: по {OpeningHand} карт тебе и сопернику");
            StartDay(true);
        }

        // Взять карты из колоды. Полная рука — карта сгорает; пустая колода — усталость: −1 репутации за каждую карту.
        void Draw(int n, bool log = true)
        {
            for (int i = 0; i < n; i++)
            {
                if (deck.Count == 0)
                {
                    fatigue++;
                    Reputation -= 1;
                    AddLog("<color=#ff9a5e>Колода пуста — усталость: репутация −1</color>");
                    Hint("fatigue", "Усталость",
                        "Твоя колода закончилась. Как в любой ККИ, каждая карта, которую нужно взять из пустой колоды, " +
                        "наказывает: репутация −1.\n\nБери награды после ночи и не увлекайся удалением карт в лавке.");
                    continue;
                }
                var card = deck[deck.Count - 1];
                deck.RemoveAt(deck.Count - 1);
                if (handCards.Count >= MaxHand)
                {
                    AddLog($"<color=#ff9a5e>Рука полна ({MaxHand}) — карта «{card.Title}» сгорела</color>");
                    continue;
                }
                handCards.Add(card);
            }
            if (log && n > 0) AddLog($"Ты берёшь карт: {n}");
        }

        // Утро: рука остаётся, из колоды приходят новые карты (в первый день — только стартовая раздача).
        void StartDay(bool first)
        {
            if (!first) Draw(DayDraw);

            Selected = -1;
            MovingFrom = null;
            CurrentGuest = null;
            ActionsLeft = ActionsPerDay + bonusActions;
            bonusActions = 0;
            ExtraActionBought = false;
            SetPhase(Phase.Day);
            AddLog(IsTutorial ? "<b>— День 0 —</b>" : $"<b>— День {Night} —</b>");
            Changed?.Invoke();
            SaveRun();
        }

        bool Allowed(Func<TutorialGate, bool> rule) => Gate == null || rule(Gate);

        // Можно ли сейчас что-то делать с картами и доской: днём или в ответ на гостя.
        public bool CanAct => (State == Phase.Day || Responding) && ActionsLeft > 0;

        public void SelectCard(int index)
        {
            if (!CanAct || index < 0 || index >= handCards.Count) return;
            if (!Allowed(g => g.Card == index)) return;
            if (Selected == index)
            {
                // Повторное касание той же карты поворачивает комнату — удобно на телефоне.
                Rotate();
                return;
            }
            MovingFrom = null;
            Selected = index;
            SelectedDoors = handCards[index].Def.Doors;
            Changed?.Invoke();

            var card = handCards[index];
            if (Grid.AllPositions().Any(p => Grid.CanUpgrade(p, card)))
                Hint("upgrade", "Улучшение",
                    "Такая комната уже стоит в доме. Положи на неё эту карту — уровни сложатся (I + I = II), " +
                    "и комната будет давать вдвое, втрое или вчетверо больше.\n\n" +
                    "Улучшенные карты (бронза, серебро, золото) покупаются в лавке и сразу ставят комнату высокого уровня.");
        }

        public void Rotate()
        {
            if (!CanAct || (Selected < 0 && MovingFrom == null)) return;
            if (!Allowed(g => g.Rotate)) return;
            SelectedDoors = DoorsUtil.RotateCW(SelectedDoors);
            Changed?.Invoke();
        }

        // Можно ли сейчас опустить выбранную карту или переносимую комнату в клетку p.
        public bool CanDropAt(Vector2Int p)
        {
            if (!CanAct) return false;
            if (MovingFrom.HasValue) return Grid.CanPlace(p, SelectedDoors, MovingFrom);
            return Selected >= 0 && Grid.CanPlace(p, SelectedDoors);
        }

        // Выбранная карта может улучшить комнату в клетке p.
        public bool CanUpgradeAt(Vector2Int p) =>
            CanAct && Selected >= 0 && !MovingFrom.HasValue && Grid.CanUpgrade(p, handCards[Selected]);

        public void ClickCell(Vector2Int p)
        {
            if (!CanAct) return;
            if (!Allowed(g => g.Cell == p)) return;
            var room = Grid.Get(p);

            if (MovingFrom.HasValue)
            {
                var from = MovingFrom.Value;
                var moving = Grid.Get(from);

                if (p == from)
                {
                    // Та же клетка: без поворота — отмена, с поворотом — повернуть на месте за 1 ход.
                    if (SelectedDoors == moving.Doors)
                    {
                        MovingFrom = null;
                        Changed?.Invoke();
                        return;
                    }
                    if (!Grid.CanPlace(p, SelectedDoors, from))
                    {
                        AddLog("<color=#ffb070>Так не повернуть: ни одна дверь не совпадёт с соседями.</color>");
                        return;
                    }
                    moving.Doors = SelectedDoors;
                    FinishAction(p, $"Повёрнута комната: {moving.Def.Name}");
                    return;
                }

                if (room != null)
                {
                    TryPickRoom(p);
                    return;
                }

                if (!Grid.CanPlace(p, SelectedDoors, from))
                {
                    AddLog("<color=#ffb070>Сюда нельзя: нужна дверь, совпадающая с дверью соседней комнаты.</color>");
                    return;
                }
                Grid.Move(from, p, SelectedDoors);
                FinishAction(p, $"Перенесена комната: {moving.Def.Name}");
                return;
            }

            if (room != null)
            {
                // Такая же карта на такую же комнату — улучшение.
                if (Selected >= 0 && Grid.CanUpgrade(p, handCards[Selected]))
                {
                    room.Level = Mathf.Min(HouseGrid.MaxLevel, room.Level + handCards[Selected].Level);
                    handCards.RemoveAt(Selected);
                    FinishAction(p, $"<color=#ffd34d>Улучшена комната: {room.Def.Name} {HouseDatabase.Roman(room.Level)}</color>");
                    return;
                }
                // Сломанную — чиним.
                if (room.Broken)
                {
                    room.Broken = false;
                    FinishAction(p, $"Починена комната: {room.Def.Name}");
                    return;
                }
                // Иначе берём для переноса.
                TryPickRoom(p);
                return;
            }

            if (Selected < 0) return;
            if (!Grid.CanPlace(p, SelectedDoors))
            {
                AddLog("<color=#ffb070>Сюда нельзя: нужна дверь, совпадающая с дверью соседней комнаты.</color>");
                return;
            }

            var placed = handCards[Selected];
            Grid.Place(p, placed.Type, SelectedDoors, placed.Level);
            handCards.RemoveAt(Selected);
            FinishAction(p, $"Построена комната: {placed.Title}");
        }

        void TryPickRoom(Vector2Int p)
        {
            if (p == Grid.Entrance)
            {
                AddLog("<color=#ffb070>Прихожую двигать нельзя.</color>");
                return;
            }
            Selected = -1;
            MovingFrom = p;
            SelectedDoors = Grid.Get(p).Doors;
            Changed?.Invoke();
        }

        void FinishAction(Vector2Int p, string log)
        {
            Selected = -1;
            MovingFrom = null;
            ActionsLeft--;
            AddLog(log);
            Changed?.Invoke();
            RoomPlaced?.Invoke(p);

            if (State == Phase.Day) SaveRun();
            // Ответ сделан — гость входит.
            else if (Responding && ActionsLeft <= 0) LetGuestIn(force: true);
        }

        // ───────────── Ночь: дуэль с соперником ─────────────

        public void StartNight()
        {
            if (State != Phase.Day) return;
            if (!Allowed(g => g.Night)) return;
            Selected = -1;
            MovingFrom = null;
            ActionsLeft = 0;

            Report = new NightReport { GoldBefore = Gold, ReputationBefore = Reputation };
            GuestsLeftTonight = IsTutorial ? 1 : Level.GuestsOnNight(Night);
            // Перед ночью: ты берёшь карту на ответы, соперник добирает по карте на каждого гостя.
            Draw(NightDraw);
            Opp.Draw(GuestsLeftTonight, Level.Effective(Night), !IsTutorial);
            SetPhase(Phase.Night);
            AddLog(IsTutorial ? "<b>— Ночь 0 —</b>" : $"<b>— Ночь {Night}: гостей {GuestsLeftTonight} —</b>");
            Changed?.Invoke();
            NextGuest();
        }

        // Соперник выбирает карту и кладёт её на вход.
        void NextGuest()
        {
            if (GuestsLeftTonight <= 0 || Opp.Hand.Count == 0 && Opp.Deck.Count == 0)
            {
                FinishNight();
                return;
            }

            int index = Opp.Choose(Grid, Level.Effective(Night), IsTutorial ? 0 : Level.Malice);
            CurrentGuest = Opp.Play(index);
            GuestsLeftTonight--;
            Step = NightStep.Reveal;
            AddLog($"Соперник выкладывает гостя: <b>{CurrentGuest.Name}</b>");

            if (GuestPlayed != null)
            {
                GuestPlayed.Invoke(CurrentGuest, index);
                Changed?.Invoke();
            }
            else
            {
                Changed?.Invoke();
                BeginResponse();
            }
        }

        // Карта гостя легла на вход — твой ответ.
        public void BeginResponse()
        {
            if (State != Phase.Night || Step != NightStep.Reveal) return;
            Step = NightStep.Response;
            ActionsLeft = ResponseActions;
            Selected = -1;
            MovingFrom = null;
            Changed?.Invoke();

            var g = CurrentGuest;
            Hint("response", "Твой ответ",
                "Соперник выложил гостя на вход. Пунктир показывает, куда он пойдёт.\n\n" +
                "У тебя один ход в ответ: построй комнату, улучши, почини, перенеси или поверни. " +
                "Или сразу нажми «Впустить».");
            if (g.IsThreat)
                Hint("threat", "Угроза!",
                    "Вор и волк идут к цели кратчайшим путём — пунктир показывает маршрут.\n\n" +
                    "Успей ответом поставить ловушку на пути — она поймает их и принесёт золото.");
            if (g.Special)
                Hint("special_" + g.Id, g.Boss ? "Босс: " + g.Name : "Особый гость: " + g.Name,
                    g.Description + "\n\nОсобые гости попадают в руку соперника редко — с шансом 15%.");
        }

        // Впустить гостя: сразу или после ответного хода.
        public void LetGuestIn() => LetGuestIn(false);

        void LetGuestIn(bool force)
        {
            if (!Responding) return;
            if (!force && !Allowed(g => g.LetIn)) return;
            ActionsLeft = 0;
            Selected = -1;
            MovingFrom = null;
            Step = NightStep.Walk;

            var res = NightSimulator.Run(Grid, CurrentGuest, rng);
            Report.Results.Add(res);
            Changed?.Invoke();
            if (GuestWalk != null) GuestWalk.Invoke(res);
            else GuestDone(res);
        }

        // Интерфейс доиграл обход гостя.
        public void GuestDone(GuestResult res)
        {
            if (State != Phase.Night || Step != NightStep.Walk) return;
            ApplyGuestResult(res);
            CurrentGuest = null;
            NextGuest();
        }

        void ApplyGuestResult(GuestResult res)
        {
            Gold = Mathf.Max(0, Gold + res.Gold);
            Reputation += res.Reputation;
            AddLog(res.Summary);

            if (res.Broke.HasValue)
            {
                var room = Grid.Get(res.Broke.Value);
                if (room != null && !room.Broken)
                {
                    room.Broken = true;
                    AddLog(res.BreakText);
                    Report.Notes.Add(res.BreakText);
                    RoomBroken?.Invoke(res.Broke.Value);
                    Hint("breakage", "Поломка!",
                        "Недовольный гость иногда что-нибудь ломает (шанс 20%), а волк может разорить кухню.\n\n" +
                        "Сломанная комната не работает: гостям она только портит настроение (−1), сокровищница не платит, ловушка не ловит.\n\n" +
                        "Почини: коснись её — это стоит 1 ход. Или купи починку всего дома в лавке.");
                }
            }

            if (res.BecomesRegular && !IsTutorial)
            {
                var g = res.Guest.Regular ? res.Guest : res.Guest.AsRegular();
                Opp.ShuffleIn(g);
                string note = $"<color=#ffd34d>{res.Guest.Name} ещё вернётся постоянным гостем</color>";
                AddLog(note);
                Report.Notes.Add(note);
                Hint("regular", "Постоянный гость",
                    "Довольный гость уходит обратно в колоду соперника — он ещё вернётся. Постоянный гость («Свой · …») платит на 3 золота больше,\n" +
                    "но если разочаруется — репутация −2.");
            }
            Changed?.Invoke();
        }

        void FinishNight()
        {
            CurrentGuest = null;
            int treasury = Grid.Count(RoomType.Treasury) * 2;
            if (treasury > 0)
            {
                Gold += treasury;
                Report.TreasuryGold = treasury;
                AddLog($"Сокровищница приносит +{treasury} золота");
            }
            SetPhase(Phase.Summary);
            Changed?.Invoke();
        }

        // Кнопка «Дальше» на экране итогов ночи.
        public void ContinueAfterSummary()
        {
            if (State != Phase.Summary) return;
            if (IsTutorial)
            {
                FinishTutorial();
                return;
            }

            if (IsWander)
            {
                WanderAfterNight();
                return;
            }

            if (Reputation <= 0)
            {
                SaveSystem.ClearRun();
                EndGame($"Репутация дома «{Level.Title}» упала до нуля на ночи {Night}.", false);
                return;
            }

            Night++;
            if (Night > MaxNights)
            {
                LevelComplete();
                return;
            }

            // Награда: выбор 1 из 3 карт. Выбранная придёт в руку уже завтра.
            rewardOptions.Clear();
            rewardOptions.AddRange(RoomDatabase.RewardOffer(rng, 3));
            SetPhase(Phase.Reward);
            Changed?.Invoke();
            Hint("reward", "Награда и лавка",
                "Выбери одну карту — она придёт в руку уже завтра.\n\n" +
                "Ниже — лавка. За золото можно: перебросить награду, получить лишний ход завтра, " +
                "убрать слабую карту из колоды (чтобы чаще приходили сильные) или починить весь дом.");
        }

        public void PickReward(int index)
        {
            if (State != Phase.Reward) return;
            if (index >= 0 && index < rewardOptions.Count)
            {
                var t = rewardOptions[index];
                var card = new Card(t, RewardLevel);
                deck.Add(card); // тянем с конца колоды — карта придёт завтра
                if (IsWander) runDeck.Add(card);
                AddLog($"<color=#ffd34d>Новая карта в колоде: {card.Title}</color>");
            }
            rewardOptions.Clear();
            if (IsWander && HouseFinished)
            {
                CompleteNode();
                return;
            }
            StartDay(false);
        }

        // Дом выстоял 7 ночей: звёзды, открытие следующего дома, выбор наследства.
        void LevelComplete()
        {
            SaveSystem.ClearRun();
            int start = Level.StartReputation;
            LastStars = Reputation >= start ? 3 : Reputation >= 2 ? 2 : 1;
            Progress.SetStars(Level.Index, LastStars);
            bool unlockedNew = Level.Index + 1 > Progress.Unlocked;
            Progress.Unlocked = Mathf.Max(Progress.Unlocked, Level.Index + 1);
            SaveSystem.SaveProgress(Progress);

            // Рубашка этого дома с окантовкой этого круга переходит к игроку.
            int back = CardBacks.CodeForLevel(Level.Index);
            NewBack = Progress.Backs.Contains(back) ? 0 : back;
            if (NewBack != 0)
            {
                Progress.Backs.Add(back);
                SaveSystem.SaveProgress(Progress);
            }

            var pool = AllCards().Select(c => c.Type).Where(t => RoomDatabase.Get(t).CanUpgrade).Distinct().ToList();
            GuestDatabase.Shuffle(rng, pool);
            inheritOptions.Clear();
            inheritOptions.AddRange(pool.Take(3));

            var next = HouseDatabase.Level(Level.Index + 1);
            AddLog($"<b><color=#ffd34d>«{Level.Title}» выстоял! Звёзд: {LastStars} из 3</color></b>");
            if (unlockedNew) AddLog($"<color=#ffd34d>Открыт новый дом: {next.Title}</color>");
            SetPhase(Phase.LevelDone);
            Changed?.Invoke();
            Sfx.Fanfare();
        }

        public void PickInherit(int index)
        {
            if (State != Phase.LevelDone) return;
            if (index >= 0 && index < inheritOptions.Count)
            {
                Progress.Heirlooms.Add((int)inheritOptions[index]);
                while (Progress.Heirlooms.Count > MaxHeirlooms) Progress.Heirlooms.RemoveAt(0);
                SaveSystem.SaveProgress(Progress);
            }
            handCards.Clear();
            ShowLevels();
        }

        void EndGame(string text, bool won)
        {
            Won = won;
            GameOverText = text;
            SetPhase(Phase.GameOver);
            AddLog($"<b>{text}</b>");
            Changed?.Invoke();
        }

        // ───────────── Колода и лавка ─────────────

        // На ярмарке и на карте странствия лавка работает с колодой забега.
        bool OnRunDeck => State == Phase.Fair || State == Phase.Map || State == Phase.Rest || State == Phase.Event;
        IEnumerable<Card> AllCards() => OnRunDeck ? runDeck : deck.Concat(discard).Concat(handCards);

        // Сколько карт каждого вида (вместе с рукой) — для лавки.
        public List<(Card card, int count)> DeckCounts() => Group(AllCards());

        // Что ещё лежит в колоде добора — для просмотра колоды.
        public List<(Card card, int count)> DrawPileCounts() => Group(OnRunDeck ? runDeck : deck);

        static List<(Card card, int count)> Group(IEnumerable<Card> cards) =>
            cards.GroupBy(c => c).OrderBy(g => g.Key.Type).ThenBy(g => g.Key.Level).Select(g => (g.Key, g.Count())).ToList();

        bool Pay(int price)
        {
            if ((State != Phase.Reward && State != Phase.Fair) || Gold < price) return false;
            Gold -= price;
            return true;
        }

        public void RerollReward()
        {
            if (!Pay(RerollPrice)) return;
            rewardOptions.Clear();
            rewardOptions.AddRange(RoomDatabase.RewardOffer(rng, 3));
            AddLog($"Лавка: награда переброшена (−{RerollPrice})");
            Changed?.Invoke();
        }

        public void BuyExtraAction()
        {
            if (ExtraActionBought || !Pay(ExtraActionPrice)) return;
            ExtraActionBought = true;
            bonusActions = 1;
            AddLog($"Лавка: завтра на 1 ход больше (−{ExtraActionPrice})");
            Changed?.Invoke();
        }

        public bool CanRemoveCards => DeckSize > MinDeck;

        public void RemoveCard(Card card)
        {
            if (!CanRemoveCards || !AllCards().Contains(card) || !Pay(RemovePrice)) return;
            if (OnRunDeck) runDeck.Remove(card);
            else if (!deck.Remove(card) && !discard.Remove(card)) handCards.Remove(card);
            AddLog($"Лавка: из колоды убрана карта «{card.Title}» (−{RemovePrice})");
            Changed?.Invoke();
        }

        // Улучшить одну карту такого вида на уровень: обычная → бронза → серебро → золото.
        public void UpgradeCard(Card card)
        {
            if (!card.CanLevelUp || !AllCards().Contains(card) || !Pay(UpgradePrice(card.Level + 1))) return;
            var better = new Card(card.Type, card.Level + 1);
            foreach (var list in OnRunDeck ? new[] { runDeck } : new[] { deck, discard, handCards })
            {
                int i = list.IndexOf(card);
                if (i < 0) continue;
                list[i] = better;
                break;
            }
            AddLog($"<color=#ffd34d>Лавка: карта улучшена — {better.Title} ({Card.LevelName(better.Level)})</color>");
            Changed?.Invoke();
        }

        public void RepairAll()
        {
            if (Grid.BrokenCount() == 0 || !Pay(RepairPrice)) return;
            foreach (var p in Grid.AllPositions()) Grid.Get(p).Broken = false;
            AddLog($"Лавка: весь дом починен (−{RepairPrice})");
            Changed?.Invoke();
        }

        // ───────────── Подсказки маршрута ─────────────

        // Куда пойдёт гость на входе, если впустить его прямо сейчас.
        // Для случайных бродяг маршрута нет (null).
        public GuestResult PredictCurrent()
        {
            if (CurrentGuest == null || CurrentGuest.Behaviour == GuestBehaviour.Random) return null;
            return NightSimulator.Run(Grid, CurrentGuest, new System.Random(1));
        }

        // ───────────── Служебное ─────────────

        static string HintKey(string key) => "cardhome_hint_" + key;

        void Hint(string key, string title, string text)
        {
            if (IsTutorial || Storage.GetInt(HintKey(key), 0) == 1) return;
            Storage.SetInt(HintKey(key), 1);
            Storage.Save();
            HintRequested?.Invoke(title, text);
        }

        public void AddLog(string line)
        {
            logLines.Add(line);
            if (logLines.Count > 200) logLines.RemoveAt(0);
            LogAdded?.Invoke(line);
        }

        void Shuffle<T>(List<T> list) => GuestDatabase.Shuffle(rng, list);
    }
}
