using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Размеры элементов для одной ориентации экрана.
    public class Layout
    {
        public bool Portrait;
        public Vector2 ReferenceResolution;
        public float MatchWidthOrHeight;
        public float Cell, Step;
        public float CardW, CardH, CardGap, MiniTile;
        public int TileFont, CardNameFont, CardDescFont, StatsFont, BodyFont, LogFont, ButtonFont, LogLines;

        // Основной режим: телефон вертикально. Ширина фиксирована, лишняя высота уходит в середину.
        public static readonly Layout PortraitLayout = new Layout
        {
            Portrait = true,
            ReferenceResolution = new Vector2(1080, 1920),
            MatchWidthOrHeight = 0f,
            Cell = 164, Step = 176,
            CardW = 230, CardH = 300, CardGap = 12, MiniTile = 96,
            TileFont = 22, CardNameFont = 24, CardDescFont = 18,
            StatsFont = 34, BodyFont = 24, LogFont = 21, ButtonFont = 34, LogLines = 2,
        };

        // Запасной режим: горизонтальный экран (планшет, ПК).
        public static readonly Layout LandscapeLayout = new Layout
        {
            Portrait = false,
            ReferenceResolution = new Vector2(1920, 1080),
            MatchWidthOrHeight = 1f,
            Cell = 116, Step = 124,
            CardW = 170, CardH = 236, CardGap = 16, MiniTile = 76,
            TileFont = 16, CardNameFont = 19, CardDescFont = 14,
            StatsFont = 26, BodyFont = 19, LogFont = 17, ButtonFont = 24, LogLines = 10,
        };
    }

    // Весь интерфейс. Без Update и корутин: реагирует на события GameController,
    // анимирует через DOTween, перестраивается по событию изменения размера экрана.
    // Части: здесь доска, рука и колода; соперник и ночь — GameView.Night.cs;
    // экраны поверх доски — GameView.Overlays.cs; меню и выбор домов — GameView.Menu.cs.
    public partial class GameView : MonoBehaviour
    {
        static readonly Color Wood = RoomDatabase.Hex("9a6438");
        static readonly Color DarkWood = RoomDatabase.Hex("4a2e20");
        static readonly Color Cream = RoomDatabase.Hex("f7ecd2");
        static readonly Color InkBrown = RoomDatabase.Hex("3b2a1e");
        static readonly Color Gold = RoomDatabase.Hex("ffd34d");
        static readonly Color RotateColor = RoomDatabase.Hex("f59a2c");
        static readonly Color NightColor = RoomDatabase.Hex("6b5cff");
        static readonly Color LetInColor = RoomDatabase.Hex("4cbb5a");
        static readonly Color RestartColor = RoomDatabase.Hex("4cbb5a");
        static readonly Color Good = RoomDatabase.Hex("8be36f");
        static readonly Color Bad = RoomDatabase.Hex("ff6b5e");
        static readonly Color PanelDim = new Color(0.08f, 0.05f, 0.12f, 0.55f);

        const float PortraitTop = 350;     // сверху: статус, подсказка, рука соперника
        const float PortraitBottom = 590;  // снизу: кнопки, рука, журнал

        GameController game;
        CanvasScaler scaler;
        Layout L;
        Rect builtSafeArea;
        Vector2Int builtScreen;
        bool pendingRebuild;

        readonly TileView[,] tiles = new TileView[HouseGrid.Width, HouseGrid.Height];
        RectTransform root, gridRoot, handRoot, statsBar, pileRoot;
        HandView handView;
        Text statsText, phaseText, logText, pileCount;
        Button rotateButton, nightButton, menuButton;

        // Золото на счётчике держится, пока ночью не прилетят монеты на экране итогов.
        bool goldHeld;
        int shownGold;

        public static GameView Create(GameController game)
        {
            var go = new GameObject("CardHomeUI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<GraphicRaycaster>();

            var view = go.AddComponent<GameView>();
            view.game = game;
            view.scaler = scaler;
            Canvas.ForceUpdateCanvases(); // чтобы размеры канваса были известны до первой раскладки
            view.Build();
            view.Bind();
            view.Refresh();
            return view;
        }

        void Bind()
        {
            game.Changed += Refresh;
            game.LogAdded += OnLogAdded;
            game.PhaseChanged += OnPhaseChanged;
            game.RoomPlaced += AnimatePlaced;
            game.RoomBroken += AnimateBroken;
            game.GuestPlayed += OnGuestPlayed;
            game.GuestWalk += OnGuestWalk;
            game.HintRequested += OnHint;
        }

        public void Unbind()
        {
            if (game == null) return;
            if (tutorial != null) tutorial.Detach();
            game.Changed -= Refresh;
            game.LogAdded -= OnLogAdded;
            game.PhaseChanged -= OnPhaseChanged;
            game.RoomPlaced -= AnimatePlaced;
            game.RoomBroken -= AnimateBroken;
            game.GuestPlayed -= OnGuestPlayed;
            game.GuestWalk -= OnGuestWalk;
            game.HintRequested -= OnHint;
            game = null;
        }

        void OnDestroy() => Unbind();

        // Unity вызывает это, когда меняется размер канваса: поворот телефона, другое окно.
        // Игра закрывается (выход из Play или из приложения) — ничего не перестраиваем.
        static bool quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetQuitting()
        {
            quitting = false;
            Application.quitting -= OnQuit;
            Application.quitting += OnQuit;
        }

        static void OnQuit() => quitting = true;

        void OnRectTransformDimensionsChange()
        {
            // Канвас выключается или сцена закрывается — Unity шлёт «смену размера», но строить нельзя.
            if (quitting || !isActiveAndEnabled || !gameObject.activeInHierarchy) return;
            if (game == null || root == null) return;
            if (builtScreen == new Vector2Int(Screen.width, Screen.height) && builtSafeArea == Screen.safeArea) return;

            // Анимацию гостя не рвём — перестроимся, когда она закончится.
            if (nightAnim)
            {
                pendingRebuild = true;
                return;
            }
            Rebuild();
        }

        void Rebuild()
        {
            pendingRebuild = false;
            Build();
            Refresh();
        }

        void Build()
        {
            if (tutorial != null) tutorial.Detach();
            tutorial = null;
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            routeKey = null;

            builtScreen = new Vector2Int(Screen.width, Screen.height);
            builtSafeArea = Screen.safeArea;
            L = Screen.height >= Screen.width ? Layout.PortraitLayout : Layout.LandscapeLayout;
            scaler.referenceResolution = L.ReferenceResolution;
            scaler.matchWidthOrHeight = L.MatchWidthOrHeight;

            var day = UIKit.Image("DaySky", transform, Color.white, Sprites.DayBackground);
            UIKit.Stretch(day.rectTransform);
            BuildNightSky();

            // Всё остальное — внутри безопасной зоны (без чёлки и полоски «домой»).
            root = UIKit.Rect("SafeArea", transform);
            var safe = Screen.safeArea;
            root.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            root.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            root.offsetMin = root.offsetMax = Vector2.zero;

            BuildCommon();
            BuildOpponent();
            if (L.Portrait) ArrangePortrait();
            else ArrangeLandscape();

            // Слой для летающих карт — над доской, под экранами.
            fxLayer = UIKit.Rect("Fx", transform);
            UIKit.Stretch(fxLayer);

            BuildOverlays();
            BuildMenus();
            RestoreNightState();

            if (game.IsTutorial)
                tutorial = TutorialView.Create(this, game, transform, tutorialStep);
        }

        // Создаёт элементы; расставляют их Arrange* ниже.
        void BuildCommon()
        {
            gridRoot = UIKit.Rect("Grid", root);
            float span = L.Step * (HouseGrid.Width - 1) + L.Cell;
            gridRoot.sizeDelta = new Vector2(span + 48, GridHeight);

            // Деревянная доска под домом.
            var board = UIKit.Rect("Board", gridRoot);
            UIKit.Place(board, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, BoardY), new Vector2(span + 44, span + 44));
            UIKit.DropShadow(board, new Vector2(0, -14), 0.5f);
            var boardBody = UIKit.Image("Wood", board, Wood, Sprites.Panel);
            UIKit.Stretch(boardBody.rectTransform);
            boardBody.raycastTarget = false;

            for (int x = 0; x < HouseGrid.Width; x++)
            for (int y = 0; y < HouseGrid.Height; y++)
            {
                var p = new Vector2Int(x, y);
                var tile = new TileView(gridRoot, L.Cell, L.TileFont, true);
                tile.Root.anchoredPosition = CellPos(p);
                tile.Button.onClick.AddListener(() => game?.ClickCell(p));
                tiles[x, y] = tile;
            }

            BuildPorch();

            // Пунктир маршрута — над комнатами, под фишкой гостя.
            routeLayer = UIKit.Rect("Routes", gridRoot);
            UIKit.Stretch(routeLayer);

            handRoot = UIKit.Rect("Hand", root);
            handRoot.sizeDelta = new Vector2(L.Portrait ? 820 : 900, L.CardH + 30);
            handView = new HandView(handRoot, L.CardW, L.CardH, handRoot.sizeDelta.x,
                (card, doors) => BuildCardVisual(handRoot, card.Def, doors, card.Level));
            handView.CardTapped += i => game?.SelectCard(i);
            handView.DragBegan = OnCardDragBegan;
            handView.Dragging = OnCardDragging;
            handView.Dropped = OnCardDropped;
            handView.DealOrigin = () =>
            {
                if (pileRoot == null) return null;
                // Координаты карт отсчитываются от нижнего центра руки (там её pivot).
                Vector2 local = handRoot.InverseTransformPoint(pileRoot.TransformPoint(pileRoot.rect.center));
                return local - new Vector2(0, L.CardH * 0.3f);
            };

            BuildPile();
            BuildDragGhost();
            BuildGuestToken();

            statsText = UIKit.Outlined(UIKit.Text(root, "", L.StatsFont, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold));
            statsText.resizeTextForBestFit = true;
            statsText.resizeTextMinSize = L.StatsFont / 2;
            statsText.resizeTextMaxSize = L.StatsFont;
            phaseText = UIKit.Outlined(UIKit.Text(root, "", L.BodyFont - 2, TextAnchor.MiddleCenter, Color.white), 2);
            phaseText.resizeTextForBestFit = true;
            phaseText.resizeTextMinSize = L.BodyFont - 8;
            phaseText.resizeTextMaxSize = L.BodyFont - 2;
            logText = UIKit.Text(root, "", L.LogFont, TextAnchor.LowerLeft, Cream);

            rotateButton = UIKit.Button(root, "ПОВЕРНУТЬ ⟳", RotateColor, () => game?.Rotate(), L.ButtonFont);
            nightButton = UIKit.Button(root, "НОЧЬ ☾", NightColor, OnNightButton, L.ButtonFont);
            menuButton = UIKit.Button(root, "≡", DarkWood, () => game?.ShowMenu(), L.ButtonFont + 6);
        }

        void OnNightButton()
        {
            if (game == null) return;
            if (game.State == Phase.Day) game.StartNight();
            else game.LetGuestIn();
        }

        // Колода справа от руки: стопка рубашек и число карт. Коснись — посмотреть, что в ней.
        void BuildPile()
        {
            pileRoot = UIKit.Rect("Pile", root);
            float w = L.CardW * 0.62f, h = L.CardH * 0.62f;
            pileRoot.sizeDelta = new Vector2(w, h + 40);
            // Стопка из трёх рубашек игрока — той, что выбрана в коллекции.
            for (int i = 2; i >= 0; i--)
            {
                var card = CardBacks.Create(pileRoot, game.Progress != null ? game.Progress.EquippedBack : 0, new Vector2(w, h));
                card.anchorMin = card.anchorMax = new Vector2(0.5f, 0);
                card.pivot = new Vector2(0.5f, 0);
                card.anchoredPosition = new Vector2(i * 4, 40 + i * 6);
                var shadow = UIKit.DropShadow(card, new Vector2(2, -6), 0.4f);
                shadow.transform.SetAsFirstSibling();
            }
            pileCount = UIKit.Outlined(UIKit.Text(pileRoot, "", L.BodyFont, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold), 2);
            UIKit.Place(pileCount.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(w + 60, 40));

            var hit = pileRoot.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var b = pileRoot.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = hit;
            b.onClick.AddListener(() =>
            {
                Sfx.Click();
                OpenDeckView();
            });
            pileRoot.gameObject.AddComponent<PressScale>().Pressed = 0.94f;
        }

        // Полупрозрачная подложка под текстом; текст переезжает внутрь неё.
        RectTransform Backing(Text text, Color color, Sprite sprite, float pad)
        {
            var bg = UIKit.Image(text.name + "Bg", text.transform.parent, color, sprite);
            bg.raycastTarget = false;
            bg.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
            text.transform.SetParent(bg.transform, false);
            UIKit.Stretch(text.rectTransform, pad);
            return bg.rectTransform;
        }

        float GridHeight => L.Step * (HouseGrid.Width - 1) + L.Cell + 44 + 12 + GuestCard.H * PorchScale + 40;
        float BoardY => GridHeight / 2 - (L.Step * (HouseGrid.Width - 1) + L.Cell + 44) / 2 - 10;

        // Вертикально: сверху статус, подсказка и рука соперника; в центре дом с входом;
        // снизу журнал, рука с колодой справа и кнопки.
        void ArrangePortrait()
        {
            const float pad = 32;
            statsBar = Backing(statsText, DarkWood, Sprites.Panel, 6);
            UIKit.TopBand(statsBar, 16, 76, pad);
            statsBar.offsetMin += new Vector2(96, 0);
            statsText.rectTransform.anchoredPosition = new Vector2(0, 3);
            UIKit.Place((RectTransform)menuButton.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(pad, -16), new Vector2(84, 76));
            UIKit.TopBand(phaseText.rectTransform, 96, 56, pad);
            UIKit.TopBand(oppRoot, 156, 180, pad);

            // Кнопки в самом низу, над ними карты и колода, над ними журнал.
            float buttonH = 116, buttonW = (1080 - pad * 3) / 2;
            PlaceBottom(rotateButton, new Vector2(0, 0), new Vector2(pad, 28), new Vector2(buttonW, buttonH));
            PlaceBottom(nightButton, new Vector2(1, 0), new Vector2(-pad, 28), new Vector2(buttonW, buttonH));

            float handBottom = 28 + buttonH + 26;
            float pileW = pileRoot.sizeDelta.x;
            UIKit.Place(handRoot, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-(pileW + 20) / 2, handBottom), handRoot.sizeDelta);
            UIKit.Place(pileRoot, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-pad, handBottom), pileRoot.sizeDelta);

            float logBottom = handBottom + L.CardH + 24;
            var log = Backing(logText, PanelDim, Sprites.Soft, 10);
            log.anchorMin = new Vector2(0, 0);
            log.anchorMax = new Vector2(1, 0);
            log.pivot = new Vector2(0.5f, 0);
            log.offsetMin = new Vector2(pad, logBottom);
            log.offsetMax = new Vector2(-pad, logBottom + 70);

            // Дом с входом — посередине свободного места; если места мало, чуть уменьшается.
            float safeH = Screen.height * 1080f / Mathf.Max(1, Screen.width) * Screen.safeArea.height / Mathf.Max(1, Screen.height);
            float available = safeH - PortraitTop - PortraitBottom;
            float fit = Mathf.Clamp(available / GridHeight, 0.55f, 1f);
            gridRoot.anchorMin = gridRoot.anchorMax = new Vector2(0.5f, 0);
            gridRoot.pivot = new Vector2(0.5f, 0.5f);
            gridRoot.anchoredPosition = new Vector2(0, PortraitBottom + available / 2);
            gridRoot.localScale = Vector3.one * fit;
        }

        // Горизонтально (просторнее всего): слева крупная доска с входом, под ней рука и колода,
        // справа деревянная панель — статус, соперник, журнал и кнопки.
        void ArrangeLandscape()
        {
            const float panelW = 600, margin = 20;
            var panelRoot = UIKit.Rect("Panel", root);
            panelRoot.SetAsFirstSibling();
            panelRoot.anchorMin = new Vector2(1, 0);
            panelRoot.anchorMax = new Vector2(1, 1);
            panelRoot.pivot = new Vector2(1, 0.5f);
            panelRoot.sizeDelta = new Vector2(panelW, -margin * 2);
            panelRoot.anchoredPosition = new Vector2(-margin, 0);
            UIKit.DropShadow(panelRoot, new Vector2(0, -12), 0.5f);
            var panel = UIKit.Image("Body", panelRoot, DarkWood, Sprites.Panel).rectTransform;
            UIKit.Stretch(panel);

            foreach (var t in new[] { statsText, phaseText, logText }) t.transform.SetParent(panel, false);
            oppRoot.SetParent(panel, false);
            UIKit.TopBand(statsText.rectTransform, 18, 56, 18);
            statsBar = statsText.rectTransform;
            UIKit.TopBand(phaseText.rectTransform, 80, 80, 18);
            UIKit.TopBand(oppRoot, 168, 190, 14);
            // Соперник в узкой панели: подпись слева, рубашки в середине, колода справа.
            oppInfo.rectTransform.offsetMax = new Vector2(210, -8);
            UIKit.Place(oppHandRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30, 0), new Vector2(230, 160));

            var log = Backing(logText, new Color(0, 0, 0, 0.3f), Sprites.Soft, 12);
            log.anchorMin = Vector2.zero;
            log.anchorMax = Vector2.one;
            log.offsetMin = new Vector2(18, 144);
            log.offsetMax = new Vector2(-18, -378);

            foreach (var b in new[] { rotateButton, nightButton }) b.transform.SetParent(panel, false);
            PlaceBottom(rotateButton, new Vector2(0, 0), new Vector2(18, 24), new Vector2(272, 100));
            PlaceBottom(nightButton, new Vector2(1, 0), new Vector2(-18, 24), new Vector2(272, 100));

            // Всё остальное — в области слева от панели (она растягивается на широких телефонах).
            var main = UIKit.Rect("Main", root);
            main.SetAsFirstSibling();
            main.anchorMin = Vector2.zero;
            main.anchorMax = Vector2.one;
            main.offsetMin = Vector2.zero;
            main.offsetMax = new Vector2(-(panelW + margin), 0);

            UIKit.Place((RectTransform)menuButton.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(84, 76));

            // Рука немного уходит за нижний край — при наведении и выборе карта поднимается целиком.
            const float handPeek = 0.85f;
            float handVisible = L.CardH * handPeek + 16;
            float safeH = 1080f * Screen.safeArea.height / Mathf.Max(1, Screen.height);
            float fit = Mathf.Clamp((safeH - 20 - handVisible) / GridHeight, 0.4f, 1f);

            gridRoot.SetParent(main, false);
            gridRoot.anchorMin = gridRoot.anchorMax = new Vector2(0.5f, 1);
            gridRoot.pivot = new Vector2(0.5f, 1);
            gridRoot.anchoredPosition = new Vector2(0, -20);
            gridRoot.localScale = Vector3.one * fit;

            handRoot.SetParent(main, false);
            UIKit.Place(handRoot, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-70, -L.CardH * (1 - handPeek)), handRoot.sizeDelta);
            pileRoot.SetParent(main, false);
            UIKit.Place(pileRoot, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 12), pileRoot.sizeDelta);
        }

        static void PlaceBottom(Button b, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            UIKit.Place((RectTransform)b.transform, anchor, anchor, pos, size);
        }

        Vector2 CellPos(Vector2Int p)
        {
            float half = (HouseGrid.Width - 1) / 2f;
            return new Vector2((p.x - half) * L.Step, (p.y - half) * L.Step + BoardY);
        }

        // ───────────── Цели для обучения ─────────────

        internal Layout CurrentLayout => L;
        internal RectTransform StatsRect => statsBar;
        internal RectTransform OpponentRect => oppRoot;
        internal RectTransform GridRect => gridRoot;
        internal RectTransform PileRect => pileRoot;
        internal RectTransform PorchRect => porchCard != null ? porchCard.Root : porchSlot;
        internal RectTransform RotateRect => (RectTransform)rotateButton.transform;
        internal RectTransform NightRect => (RectTransform)nightButton.transform;
        internal RectTransform SummaryRect => summaryPanel;
        internal RectTransform TileRect(Vector2Int p) => tiles[p.x, p.y].Root;
        internal RectTransform CardRect(int index) => handView.CardRect(index);
        internal void SetTutorialStep(int step) => tutorialStep = step;

        // ───────────── Реакция на события ─────────────

        void OnLogAdded(string _) => RefreshLog();

        static bool InGame(Phase p) => p != Phase.Menu && p != Phase.Levels && p != Phase.Map && p != Phase.Fair && p != Phase.Rest && p != Phase.Event;

        void OnPhaseChanged(Phase phase)
        {
            FadeNightSky(phase);

            if (phase == Phase.Night)
            {
                goldHeld = true;
                shownGold = game.Report.GoldBefore;
                Sfx.Owl();
            }
            else if (phase != Phase.Summary)
            {
                goldHeld = false;
            }

            bool lit = phase == Phase.Night || phase == Phase.Summary;
            for (int x = 0; x < HouseGrid.Width; x++)
            for (int y = 0; y < HouseGrid.Height; y++)
                tiles[x, y].SetNight(lit, lit ? 0.3f + (x + y) * 0.08f : 0);

            if (phase != Phase.Night) ClearPorch();
            ShowOverlays(phase, true);
            ShowMenus(phase, true);
            if (phase == Phase.GameOver && !game.Won) Sfx.Break();
        }

        public void Refresh()
        {
            if (game == null) return;
            var state = game.State;
            root.gameObject.SetActive(InGame(state));
            RefreshMenus();
            if (!InGame(state)) return;

            bool lit = state == Phase.Night || state == Phase.Summary;

            for (int x = 0; x < HouseGrid.Width; x++)
            for (int y = 0; y < HouseGrid.Height; y++)
            {
                var p = new Vector2Int(x, y);
                var room = game.Grid.Get(p);
                bool picked = game.MovingFrom == p;
                if (room != null)
                    tiles[x, y].SetRoom(room.Def, picked ? game.SelectedDoors : room.Doors, picked,
                        room.Level, room.Broken, game.CanUpgradeAt(p));
                else
                    tiles[x, y].SetEmpty(game.CanDropAt(p));
                tiles[x, y].SetNight(lit);
                if (picked) tiles[x, y].Root.SetAsLastSibling();
            }
            routeLayer.SetAsLastSibling();
            guestToken.SetAsLastSibling();

            handView.Sync(game.Hand, game.Selected, game.SelectedDoors, game.CanAct);
            pileCount.text = $"Колода {game.DeckCount}";

            UpdateStats();
            phaseText.text = PhaseHint();

            bool playing = state == Phase.Day || state == Phase.Night;
            rotateButton.gameObject.SetActive(playing);
            rotateButton.interactable = game.CanAct && (game.Selected >= 0 || game.MovingFrom.HasValue);
            nightButton.gameObject.SetActive(playing);
            nightButton.interactable = state == Phase.Day || game.Responding;
            bool letIn = state == Phase.Night;
            SetButton(nightButton, letIn ? "ВПУСТИТЬ ▶" : "НОЧЬ ☾", letIn ? LetInColor : NightColor);
            menuButton.gameObject.SetActive(state == Phase.Day && !game.IsTutorial);

            RefreshOpponent();
            RefreshRoutes();
            RefreshLog();
            RefreshOverlays();
        }

        static void SetButton(Button b, string label, Color color)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t.text != label) t.text = label;
            ((Image)b.targetGraphic).color = color;
        }

        string PhaseHint()
        {
            switch (game.State)
            {
                case Phase.Day:
                    if (game.ActionsLeft <= 0)
                        return "Ходы на сегодня закончились — жми «Ночь»";
                    if (game.MovingFrom.HasValue)
                        return $"Перенос: коснись зелёной клетки или поверни и коснись комнаты ещё раз. Ходов: {game.ActionsLeft}";
                    if (game.Selected >= 0 && game.Grid.AllPositions().Any(game.CanUpgradeAt))
                        return $"Коснись такой же комнаты в зелёной рамке — улучшишь её до II. Ходов: {game.ActionsLeft}";
                    if (game.Grid.BrokenCount() > 0)
                        return $"Коснись сломанной комнаты, чтобы починить (1 ход). Ходов: {game.ActionsLeft}";
                    return $"День: построй комнаты, пока гости не пришли. Ходов: {game.ActionsLeft} · в руке {game.Hand.Count}/{GameController.MaxHand}";
                case Phase.Night:
                    var g = game.CurrentGuest;
                    switch (game.Step)
                    {
                        case NightStep.Reveal: return "Соперник выкладывает гостя…";
                        case NightStep.Response:
                            return game.ActionsLeft > 0
                                ? $"<b>{g?.Name}</b> на пороге. Ответь одним ходом — или впусти его."
                                : $"<b>{g?.Name}</b> входит…";
                        case NightStep.Walk: return $"<b>{g?.Name}</b> обходит дом…";
                        default: return "Ночь…";
                    }
                case Phase.Summary: return "Итоги ночи";
                case Phase.Reward: return "Ночь пережита! Выбери награду";
                case Phase.LevelDone: return "Дом выстоял!";
                default: return "Игра окончена";
            }
        }

        void UpdateStats()
        {
            if (game == null || statsText == null) return;
            int gold = goldHeld ? shownGold : game.Gold;
            string coins = $"<color=#ffd34d>● {gold}</color>";
            string hearts = $"<color=#ff6b6b>{new string('♥', Mathf.Max(0, game.Reputation))}</color>";
            string title = game.IsTutorial ? "ОБУЧЕНИЕ · ДЕНЬ 0"
                : $"{game.Level.Title.ToUpper()} · НОЧЬ {Mathf.Min(game.Night, game.MaxNights)}/{game.MaxNights}";
            statsText.text = $"{title}    {coins}    {hearts}";
        }

        void RefreshLog()
        {
            if (logText == null || game == null) return;
            var log = game.Log;
            logText.text = string.Join("\n", log.Skip(Mathf.Max(0, log.Count - L.LogLines)));
        }

        // ───────────── Карты комнат ─────────────

        // Карта в стиле ККИ для экранов выбора: обычная кнопка.
        RectTransform BuildCard(Transform parent, RoomDef def, System.Action onClick, int level = 1)
        {
            var parts = BuildCardVisual(parent, def, def.Doors, level);
            var btn = parts.Root.gameObject.AddComponent<Button>();
            btn.targetGraphic = parts.Frame;
            btn.onClick.AddListener(() =>
            {
                Sfx.Click();
                onClick();
            });
            parts.Root.gameObject.AddComponent<PressScale>().Pressed = 0.95f;
            return parts.Root;
        }

        // Внешний вид карты: цветная рамка, окно с иконкой комнаты, лента с названием, светлая плашка описания.
        // Без логики нажатий — её добавляют рука (HandCard) или экраны выбора (Button).
        CardParts BuildCardVisual(Transform parent, RoomDef def, Doors doors, int level = 1)
        {
            var face = Icons.RoomCard(def.Type);
            if (face != null) return BuildPaintedCard(parent, def, doors, level, face);

            var card = UIKit.Rect("Card", parent);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0);
            card.pivot = new Vector2(0.5f, 0);
            card.sizeDelta = new Vector2(L.CardW, L.CardH);

            var glow = UIKit.Image("Glow", card, new Color(1, 0.85f, 0.2f, 0.95f), Sprites.Shadow);
            glow.raycastTarget = false;
            UIKit.Stretch(glow.rectTransform, -Sprites.ShadowPad - 14);
            glow.enabled = false;

            UIKit.DropShadow(card, new Vector2(4, -12), 0.55f);

            var frame = UIKit.Image("Frame", card, def.Color, Sprites.Panel);
            UIKit.Stretch(frame.rectTransform);

            // Окно «арта»: комната с иконкой и дверями.
            float inset = L.CardW * 0.06f;
            float artH = L.MiniTile + 22;
            var art = UIKit.Image("Art", card, RoomDatabase.Hex("2a1f1a"), Sprites.Flat);
            art.raycastTarget = false;
            UIKit.TopBand(art.rectTransform, inset, artH, inset);
            var tile = new TileView(art.rectTransform, L.MiniTile, 0, false, showLabel: false);
            UIKit.Place(tile.Root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(L.MiniTile, L.MiniTile));
            tile.SetRoom(def, doors, false, level);

            // Лента с названием поверх нижнего края окна.
            float ribbonTop = inset + artH - L.CardNameFont * 0.6f;
            float ribbonH = L.CardNameFont + 18;
            var ribbon = UIKit.Image("Ribbon", card, DarkWood, Sprites.Panel);
            ribbon.raycastTarget = false;
            UIKit.TopBand(ribbon.rectTransform, ribbonTop, ribbonH, inset * 0.4f);
            var name = UIKit.Outlined(UIKit.Text(ribbon.transform, def.Name.ToUpper(), L.CardNameFont, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold), 2);
            UIKit.Stretch(name.rectTransform, 2);
            name.rectTransform.anchoredPosition = new Vector2(0, 2);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = L.CardNameFont / 2;
            name.resizeTextMaxSize = L.CardNameFont;

            // Плашка описания.
            float descTop = ribbonTop + ribbonH + 4;
            var descBg = UIKit.Image("DescBg", card, Cream, Sprites.Flat);
            descBg.raycastTarget = false;
            UIKit.TopBand(descBg.rectTransform, descTop, L.CardH - descTop - inset, inset);
            var desc = UIKit.Text(descBg.transform, def.Description, L.CardDescFont, TextAnchor.MiddleCenter, InkBrown, FontStyle.Bold);
            UIKit.Stretch(desc.rectTransform, 8);
            desc.resizeTextForBestFit = true;
            desc.resizeTextMinSize = L.CardDescFont - 6;
            desc.resizeTextMaxSize = L.CardDescFont;

            // Улучшенная карта — в металлической рамке, как у рубашек: бронза, серебро, золото.
            if (level >= 2)
            {
                int tier = Card.FrameTier(level);
                var metal = UIKit.Image("Metal", card, Color.white, CardBacks.FrameSprite(tier));
                metal.raycastTarget = false;
                UIKit.Stretch(metal.rectTransform);
                var extra = new Vector2(L.CardW, L.CardH) * 0.07f;
                metal.rectTransform.offsetMin = -extra;
                metal.rectTransform.offsetMax = extra;
                var tag = UIKit.Outlined(UIKit.Text(card, Card.LevelName(level).ToUpper(), L.CardDescFont - 2, TextAnchor.MiddleCenter,
                    CardBacks.Tiers[tier].Gem, FontStyle.Bold), 2);
                UIKit.Place(tag.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(L.CardW, 30));
            }
            return new CardParts { Root = card, Frame = frame, Glow = glow, Tile = tile, Def = def, Level = level };
        }

        // Карта из нарисованного бланка (Art/RoomCards/<Тип>.png): рамка, окно с картинкой, лента и пергамент
        // уже нарисованы; поверх игра пишет название и описание, а в углу окна — маленькая плитка со схемой дверей
        // (она поворачивается вместе с картой). Бланк вписывается в размер карты без искажений.
        CardParts BuildPaintedCard(Transform parent, RoomDef def, Doors doors, int level, Sprite face)
        {
            var card = UIKit.Rect("Card", parent);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0);
            card.pivot = new Vector2(0.5f, 0);
            card.sizeDelta = new Vector2(L.CardW, L.CardH);

            // Область самого бланка — с его пропорциями, по центру карты.
            var area = UIKit.Rect("Face", card);
            UIKit.Stretch(area);
            var fit = area.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = face.rect.width / face.rect.height;

            var glow = UIKit.Image("Glow", area, new Color(1, 0.85f, 0.2f, 0.95f), Sprites.Shadow);
            glow.raycastTarget = false;
            UIKit.Stretch(glow.rectTransform, -Sprites.ShadowPad - 10);
            glow.enabled = false;

            UIKit.DropShadow(area, new Vector2(4, -12), 0.5f);
            var frame = UIKit.Image("Frame", area, Color.white, face);
            UIKit.Stretch(frame.rectTransform);

            // Отметки на бланке (в долях высоты снизу): лента ~44–52%, пергамент ~13–41%, окно ~55–92%.
            var name = UIKit.Outlined(UIKit.Text(area, def.Name.ToUpper(), L.CardNameFont, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold), 2);
            Band(name.rectTransform, 0.12f, 0.445f, 0.88f, 0.53f);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = L.CardNameFont / 2;
            name.resizeTextMaxSize = L.CardNameFont;

            var desc = UIKit.Text(area, def.Description, L.CardDescFont, TextAnchor.MiddleCenter, InkBrown, FontStyle.Bold);
            Band(desc.rectTransform, 0.17f, 0.15f, 0.83f, 0.40f);
            desc.resizeTextForBestFit = true;
            desc.resizeTextMinSize = L.CardDescFont - 7;
            desc.resizeTextMaxSize = L.CardDescFont;

            // Схема дверей — маленькая плитка в правом верхнем углу окна.
            float tileSize = L.MiniTile * 0.5f;
            var tile = new TileView(area, tileSize, 0, false, showLabel: false);
            tile.Root.anchorMin = tile.Root.anchorMax = new Vector2(0.84f, 0.89f);
            tile.Root.pivot = new Vector2(1, 1);
            tile.Root.anchoredPosition = Vector2.zero;
            tile.SetRoom(def, doors, false, level);

            // Улучшенная карта — в металлической рамке, как у рубашек.
            if (level >= 2)
            {
                int tier = Card.FrameTier(level);
                var metal = UIKit.Image("Metal", area, Color.white, CardBacks.FrameSprite(tier));
                metal.raycastTarget = false;
                UIKit.Stretch(metal.rectTransform);
                var extra = new Vector2(L.CardW, L.CardH) * 0.07f;
                metal.rectTransform.offsetMin = -extra;
                metal.rectTransform.offsetMax = extra;
                var tag = UIKit.Outlined(UIKit.Text(area, Card.LevelName(level).ToUpper(), L.CardDescFont - 2, TextAnchor.MiddleCenter,
                    CardBacks.Tiers[tier].Gem, FontStyle.Bold), 2);
                Band(tag.rectTransform, 0.1f, 0.0f, 0.9f, 0.1f);
            }
            return new CardParts { Root = card, Frame = frame, Glow = glow, Tile = tile, Def = def, Level = level, Painted = true };
        }

        // Растянуть элемент на прямоугольник бланка, заданный долями (лево, низ, право, верх).
        static void Band(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ───────────── Перетаскивание карты на поле ─────────────

        TileView dragGhost;
        Image dragRing;
        Vector2Int? dragCell;

        // «Призрак» комнаты: показывает, куда и как встанет карта, пока её тянут над доской.
        void BuildDragGhost()
        {
            dragGhost = new TileView(gridRoot, L.Cell, L.TileFont, false);
            dragGhost.Root.gameObject.AddComponent<CanvasGroup>().alpha = 0.85f;
            dragGhost.Root.GetComponent<CanvasGroup>().blocksRaycasts = false;
            dragRing = UIKit.Image("Ring", dragGhost.Root, Good, Sprites.Frame);
            dragRing.raycastTarget = false;
            UIKit.Stretch(dragRing.rectTransform, -10);
            dragGhost.Root.gameObject.SetActive(false);
        }

        // Клетка под пальцем (чуть выше пальца, чтобы он не закрывал комнату).
        Vector2Int? CellAt(Vector2 screen)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRoot, screen, null, out var local)) return null;
            local += new Vector2(0, L.Cell * 0.35f);
            float half = (HouseGrid.Width - 1) / 2f;
            var p = new Vector2Int(Mathf.RoundToInt(local.x / L.Step + half), Mathf.RoundToInt((local.y - BoardY) / L.Step + half));
            if (!game.Grid.InBounds(p)) return null;
            return (local - CellPos(p)).magnitude <= L.Step * 0.7f ? p : (Vector2Int?)null;
        }

        bool CanPlayAt(Vector2Int p) => game.CanDropAt(p) || game.CanUpgradeAt(p);

        void OnCardDragBegan(int index)
        {
            if (game == null) return;
            if (game.Selected != index) game.SelectCard(index);
            dragCell = null;
        }

        void OnCardDragging(int index, Vector2 screen)
        {
            if (game == null || game.Selected != index) return;
            var cell = CellAt(screen);
            handView.SetCardHidden(index, cell.HasValue);
            if (cell == dragCell) return;
            dragCell = cell;

            var ghost = dragGhost.Root;
            if (!cell.HasValue)
            {
                ghost.gameObject.SetActive(false);
                return;
            }

            var p = cell.Value;
            var room = game.Grid.Get(p);
            bool ok = CanPlayAt(p);
            var card = game.Hand[index];
            // Над такой же комнатой покажем, каким станет её уровень.
            if (room != null && ok) dragGhost.SetRoom(card.Def, room.Doors, false, Mathf.Min(HouseGrid.MaxLevel, room.Level + card.Level), false, true);
            else dragGhost.SetRoom(card.Def, game.SelectedDoors, false, card.Level);
            dragRing.color = ok ? Good : Bad;

            ghost.gameObject.SetActive(true);
            ghost.SetAsLastSibling();
            guestToken.SetAsLastSibling();
            ghost.DOKill();
            ghost.DOAnchorPos(CellPos(p), 0.08f).SetEase(Ease.OutQuad).SetLink(ghost.gameObject);
            ghost.DOScale(ok ? 1.04f : 0.96f, 0.12f).SetEase(Ease.OutBack).SetLink(ghost.gameObject);
            if (ok) Sfx.Click();
        }

        bool OnCardDropped(int index, Vector2 screen)
        {
            dragGhost.Root.gameObject.SetActive(false);
            dragCell = null;
            if (game == null) return false;
            var cell = CellAt(screen);
            if (!cell.HasValue) return false;
            if (!CanPlayAt(cell.Value))
            {
                // Нельзя: короткая «тряска» клетки и подсказка в журнале.
                var rt = tiles[cell.Value.x, cell.Value.y].Root;
                rt.DOKill(true);
                rt.DOShakeAnchorPos(0.25f, 10, 25).SetLink(rt.gameObject);
                game.AddLog("<color=#ffb070>Сюда нельзя: двери должны совпасть с дверью соседа. Поверни карту (тап по ней).</color>");
                return false;
            }
            int before = game.Hand.Count;
            game.ClickCell(cell.Value);
            return game.Hand.Count < before;
        }

        // ───────────── Анимации доски ─────────────

        // Комната «плюхается» на доску.
        void AnimatePlaced(Vector2Int p)
        {
            var rt = tiles[p.x, p.y].Root;
            rt.SetAsLastSibling();
            routeLayer.SetAsLastSibling();
            guestToken.SetAsLastSibling();
            rt.DOKill();
            rt.localScale = Vector3.one * 1.45f;
            rt.DOScale(1, 0.4f).SetEase(Ease.OutBack, 3f).SetLink(rt.gameObject);
            Sfx.Plop();
        }

        void AnimateBroken(Vector2Int p)
        {
            tiles[p.x, p.y].PlayBreak();
            Sfx.Break();
        }
    }
}
