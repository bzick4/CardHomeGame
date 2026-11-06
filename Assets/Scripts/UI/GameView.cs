using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Размеры элементов для одной ориентации экрана.
    public class Layout
    {
        public bool Portrait { get; private set; }
        public Vector2 ReferenceResolution { get; init; }
        public float MatchWidthOrHeight { get; init; }
        public float Cell { get; private set; }
        public float Step { get; private set; }
        public float CardW { get; private set; }
        public float CardH { get; private set; }
        public float CardGap { get; private set; }
        public float MiniTile { get; init; }
        public int TileFont { get; private set; }
        public int CardNameFont { get; init; }
        public int CardDescFont { get; init; }
        public int StatsFont { get; private set; }
        public int BodyFont { get; private set; }
        public int LogFont { get; init; }
        public int ButtonFont { get; private set; }
        public int LogLines { get; init; }
        public int MinFont { get; private set; }      // меньше этого текст на этом экране не читается
        public float PanelShare { get; private set; } // горизонталь: доля ширины под правую панель (0 — 600 единиц)
        public float PanelScale { get; private set; } = 1; // горизонталь: во сколько раз крупнее строки и кнопки панели

        // Основной режим: телефон вертикально. Ширина 1080 = ~390 pt на iPhone, поэтому текст не меньше 32
        // (иначе меньше 11 pt). Ширина фиксирована, лишняя высота уходит в середину.
        public static readonly Layout PortraitLayout = new Layout
        {
            Portrait = true,
            ReferenceResolution = new Vector2(1080, 1920),
            MatchWidthOrHeight = 0f,
            Cell = 164, Step = 176,
            CardW = 230, CardH = 300, CardGap = 12, MiniTile = 96,
            TileFont = 30, CardNameFont = 34, CardDescFont = 30,
            StatsFont = 44, BodyFont = 34, LogFont = 32, ButtonFont = 40, LogLines = 1,
            MinFont = 31, // 31 единица = 11 pt на iPhone (ширина 390 pt)
        };

        // Горизонтальный экран планшета или ПК.
        public static readonly Layout LandscapeLayout = new Layout
        {
            Portrait = false,
            ReferenceResolution = new Vector2(1920, 1080),
            MatchWidthOrHeight = 1f,
            Cell = 116, Step = 124,
            CardW = 170, CardH = 236, CardGap = 16, MiniTile = 76,
            TileFont = 16, CardNameFont = 19, CardDescFont = 14,
            StatsFont = 26, BodyFont = 19, LogFont = 17, ButtonFont = 24, LogLines = 10,
            MinFont = 13,
        };

        // Телефон горизонтально: высота экрана ~390 pt = 1080 единиц, поэтому шрифты ×1.8,
        // правая панель шире (34% ширины), карты в руке крупнее.
        public static readonly Layout LandscapePhoneLayout = new Layout
        {
            Portrait = false,
            ReferenceResolution = new Vector2(1920, 1080),
            MatchWidthOrHeight = 1f,
            Cell = 116, Step = 124,
            CardW = 220, CardH = 300, CardGap = 16, MiniTile = 76,
            TileFont = 29, CardNameFont = 34, CardDescFont = 26,
            StatsFont = 47, BodyFont = 34, LogFont = 31, ButtonFont = 43, LogLines = 3,
            MinFont = 31, PanelShare = 0.34f, PanelScale = 1.45f, // 31 единица = 11 pt при высоте 390 pt
        };

        // Какой макет подходит экрану. Телефон в горизонтали узнаём по короткой стороне (если браузер сообщает dpi)
        // или по вытянутости экрана: у телефонов ~2:1 и больше, у планшетов и мониторов — до 16:9.
        public static Layout For(int width, int height, float dpi)
        {
            if (height >= width) return PortraitLayout;
            float aspect = width / (float)Mathf.Max(1, height);
            bool shortSide = dpi > 0 && height / (dpi / 160f) < 600;
            return shortSide || aspect >= 1.9f ? LandscapePhoneLayout : LandscapeLayout;
        }
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
        static readonly Color PlateTint = new Color(1, 1, 1, 0.85f); // тёмная плашка Panels/Plate под текстом поверх арта

        const float PortraitTop = 350;     // сверху: статус, подсказка, рука соперника
        const float PortraitBottom = 590;  // снизу: кнопки, рука, журнал

        GameController game;
        CanvasScaler scaler;
        Layout L;
        Rect builtSafeArea;
        Vector2Int builtScreen;
        bool pendingRebuild;

        readonly TileView[,] tiles = new TileView[HouseGrid.Width, HouseGrid.Height];
        RectTransform root, gridRoot, handRoot, statsBar, phaseBar, pileRoot;
        HandView handView;
        Text statsText, phaseText, logText, pileCount;
        // Верхняя панель: название и ночь — текстом, золото и сердца — значками справа.
        RectTransform statsRight;
        IconValue statGold;
        HeartsRow statHearts;
        float statsTextRight;
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

        Material locationMat; // свой экземпляр: _Night анимируется

        void OnDestroy()
        {
            Unbind();
            if (locationMat != null) Destroy(locationMat);
            if (guestDissolve != null) Destroy(guestDissolve);
        }

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

        // Картинки поля: Art/Board/<имя>.png (доска, пазы, стены, двери, крыльцо). Нет — рисуем кодом.
        public static Sprite BoardArt(string name) => ArtStore.Get("Board/" + name, null);

        // Фон текущего дома под ориентацию экрана. В меню не грузим — там свои фоны.
        Sprite LocationArt()
        {
            if (!InGame(game.State)) return null;
            string id = game.Level.House.Id;
            return ArtStore.Get("Locations/" + id + (L.Portrait ? "" : "Wide"), null)
                ?? ArtStore.Get("Locations/" + id + (L.Portrait ? "Wide" : ""), null);
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
            L = Layout.For(Screen.width, Screen.height, Screen.dpi);
            scaler.referenceResolution = L.ReferenceResolution;
            scaler.matchWidthOrHeight = L.MatchWidthOrHeight;

            // Фон дома (Art/Locations/<Дом> или <Дом>Wide), если он есть; иначе — общее небо.
            var location = LocationArt();
            if (location != null)
            {
                var loc = UIKit.Image("Location", transform, Color.white, location);
                loc.raycastTarget = false;
                UIKit.Cover(loc);
                // Ночь на самом фоне: синее и темнее, а окна и фонари остаются тёплыми (шейдер UI_NightGrade).
                if (locationMat != null) Destroy(locationMat);
                locationMat = UiShaders.NewNightGrade();
                if (locationMat != null)
                {
                    locationMat.SetFloat("_Night", NightAlpha(game.State));
                    loc.material = locationMat;
                }
                var dim = UIKit.Image("Dim", transform, new Color(0, 0, 0, 0.3f));
                dim.raycastTarget = false;
                UIKit.Stretch(dim.rectTransform);
                var vignette = UIKit.Image("Vignette", transform, new Color(0, 0, 0, 0.55f), Sprites.Vignette);
                vignette.raycastTarget = false;
                UIKit.Stretch(vignette.rectTransform);
            }
            else
            {
                // В меню фона дома нет — под интерфейсом просто тёмный цвет (экраны меню закрывают его своими фонами).
                var under = UIKit.Image("Under", transform, RoomDatabase.Hex("141b33"));
                UIKit.Stretch(under.rectTransform);
            }
            BuildNightSky(location != null, locationMat != null);

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
            var boardArt = BoardArt("Board");
            if (boardArt != null)
            {
                // Нарисованная доска: сетка (плюс поля по 12) должна лечь на пол внутри рамки.
                // Рамка картинки: ~8.3% по бокам, 6.7% сверху, 8.8% снизу (там порожек входа).
                var size = BoardArtSize;
                float shift = BoardShift * size.y; // середина пола выше середины картинки
                UIKit.Place(board, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, BoardY - shift), size);
                UIKit.DropShadow(board, new Vector2(0, -14), 0.45f);
                var img = UIKit.Image("Art", board, Color.white, boardArt);
                img.raycastTarget = false;
                UIKit.Stretch(img.rectTransform);
            }
            else
            {
                UIKit.Place(board, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, BoardY), new Vector2(span + 44, span + 44));
                UIKit.DropShadow(board, new Vector2(0, -14), 0.5f);
                var boardBody = UIKit.Image("Wood", board, Wood, Sprites.Panel);
                UIKit.Stretch(boardBody.rectTransform);
                boardBody.raycastTarget = false;
            }

            for (int x = 0; x < HouseGrid.Width; x++)
            for (int y = 0; y < HouseGrid.Height; y++)
            {
                var p = new Vector2Int(x, y);
                // Подписи на клетках не читаются на телефоне — название появляется по нажатию.
                var tile = new TileView(gridRoot, L.Cell, L.TileFont, true, showLabel: false);
                tile.Root.anchoredPosition = CellPos(p);
                tile.Button.onClick.AddListener(() =>
                {
                    ShowRoomToast(p);
                    game?.ClickCell(p);
                });
                tiles[x, y] = tile;
            }

            BuildPorch();

            // Пунктир маршрута — над комнатами, под фишкой гостя.
            routeLayer = UIKit.Rect("Routes", gridRoot);
            UIKit.Stretch(routeLayer);

            handRoot = UIKit.Rect("Hand", root);
            handRoot.sizeDelta = new Vector2(L.Portrait ? 820 : 900, L.CardH + 30);
            handView = new HandView(handRoot, L.CardW, L.CardH, handRoot.sizeDelta.x,
                (card, doors) => BuildCardVisual(handRoot, card.Def, doors, card.Level, compact: true));
            handView.CardTapped += i => game?.SelectCard(i);
            handView.CardHeld += i =>
            {
                if (game == null || i < 0 || i >= game.Hand.Count) return;
                var c = game.Hand[i];
                InspectRoom(c.Def, c.Level, i == game.Selected ? game.SelectedDoors : c.Def.Doors, handView.CardRect(i));
            };
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

            statsText = UIKit.Outlined(UIKit.Text(root, "", L.StatsFont, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold));
            statsText.resizeTextForBestFit = true;
            statsText.resizeTextMinSize = Fs(L.StatsFont / 2);
            statsRight = UIKit.Rect("StatsRight", root);
            statGold = new IconValue(statsRight, UiArt.Icon("Gold"), L.StatsFont - 4, Gold);
            statHearts = new HeartsRow(statsRight, L.StatsFont * 1.05f);
            statsText.resizeTextMaxSize = L.StatsFont;
            phaseText = UIKit.Outlined(UIKit.Text(root, "", Fs(L.BodyFont - 2), TextAnchor.MiddleCenter, Color.white), 2);
            phaseText.resizeTextForBestFit = true;
            phaseText.resizeTextMinSize = Fs(L.BodyFont - 8);
            phaseText.resizeTextMaxSize = Fs(L.BodyFont - 2);
            logText = UIKit.Text(root, "", L.LogFont, TextAnchor.LowerLeft, Cream);
            logText.resizeTextForBestFit = true;
            logText.resizeTextMinSize = Fs(L.LogFont - 6);
            logText.resizeTextMaxSize = L.LogFont;

            rotateButton = UIKit.Button(root, "ПОВЕРНУТЬ", RotateColor, () => game?.Rotate(), L.ButtonFont);
            nightButton = UIKit.Button(root, "НОЧЬ", NightColor, OnNightButton, L.ButtonFont);
            menuButton = RoundButton(root, UiArt.Icon("Menu"), Vector2.zero, () => game?.ShowMenu(), 96);
        }

        // Название комнаты над клеткой: всплывает, держится секунду и тает.
        RectTransform roomToast;
        Text roomToastText;

        void ShowRoomToast(Vector2Int p)
        {
            var room = game?.Grid.Get(p);
            if (room == null) return;
            if (roomToast == null)
            {
                var img = UIKit.Image("RoomToast", gridRoot, PlateTint, UiArt.Plate);
                img.raycastTarget = false;
                roomToast = img.rectTransform;
                roomToast.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
                roomToastText = UIKit.Outlined(UIKit.Text(roomToast, "", L.BodyFont, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold));
                UIKit.Stretch(roomToastText.rectTransform, 10);
                roomToastText.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            var def = RoomDatabase.Get(room.Type);
            string title = room.Broken ? $"{def.Name} — сломана" : room.Level > 1 ? $"{def.Name} {HouseDatabase.Roman(room.Level)}" : def.Name;
            roomToastText.text = title;
            float w = Mathf.Max(L.Cell * 1.2f, roomToastText.preferredWidth + 60);
            roomToast.sizeDelta = new Vector2(w, L.BodyFont + 34);
            roomToast.anchoredPosition = CellPos(p) + new Vector2(0, L.Cell * 0.62f);
            roomToast.SetAsLastSibling();

            var group = roomToast.GetComponent<CanvasGroup>();
            roomToast.DOKill();
            group.DOKill();
            group.alpha = 0;
            DOTween.Sequence()
                .Append(group.DOFade(1, 0.12f))
                .Join(roomToast.DOScale(1, 0.18f).From(0.8f).SetEase(Ease.OutBack))
                .AppendInterval(1.1f)
                .Append(group.DOFade(0, 0.3f))
                .SetLink(roomToast.gameObject)
                .SetTarget(roomToast);
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

        // Доска: сколько она занимает выше и ниже центра сетки. У нарисованной рамка снизу толще (порожек),
        // поэтому середина пола чуть выше середины картинки.
        const float BoardShift = (0.088f - 0.067f) / 2;
        float Span => L.Step * (HouseGrid.Width - 1) + L.Cell;
        Vector2 BoardArtSize => new Vector2((Span + 24) / (1 - 0.083f * 2), (Span + 24) / (1 - 0.067f - 0.088f));
        float BoardAbove => BoardArt("Board") != null ? BoardArtSize.y * (0.5f + BoardShift) : (Span + 44) / 2;
        float BoardBelow => BoardArt("Board") != null ? BoardArtSize.y * (0.5f - BoardShift) : (Span + 44) / 2;
        float GridHeight => BoardAbove + BoardBelow + 12 + GuestCard.H * PorchScale + 40;
        float BoardY => GridHeight / 2 - BoardAbove - 10;

        // Вертикально: сверху статус, подсказка и рука соперника; в центре дом с входом;
        // снизу журнал, рука с колодой справа и кнопки.
        void ArrangePortrait()
        {
            const float pad = 32;
            statsBar = Backing(statsText, Color.white, UiArt.TopBar, 12);
            var bar = statsBar.GetComponent<Image>();
            if (bar.sprite != Sprites.Panel) bar.gameObject.AddComponent<SlicedFit>().Height = 140;
            else bar.color = DarkWood;
            UIKit.TopBand(statsBar, 16, 84, pad);
            statsText.rectTransform.offsetMin = new Vector2(34, statsText.rectTransform.offsetMin.y);
            AttachStatsRight(statsBar, 30);
            statsBar.offsetMin += new Vector2(104, 0);
            statsText.rectTransform.anchoredPosition = new Vector2(0, 3);
            UIKit.Place((RectTransform)menuButton.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(pad - 8, -8), new Vector2(100, 100));
            phaseBar = Backing(phaseText, PlateTint, UiArt.Plate, 10);
            UIKit.TopBand(phaseBar, 108, 60, pad + 40);
            UIKit.TopBand(oppRoot, 176, 166, pad);

            // Кнопки в самом низу, над ними карты и колода, над ними журнал.
            float buttonH = 116, buttonW = (1080 - pad * 3) / 2;
            PlaceBottom(rotateButton, new Vector2(0, 0), new Vector2(pad, 28), new Vector2(buttonW, buttonH));
            PlaceBottom(nightButton, new Vector2(1, 0), new Vector2(-pad, 28), new Vector2(buttonW, buttonH));

            float handBottom = 28 + buttonH + 26;
            float pileW = pileRoot.sizeDelta.x;
            UIKit.Place(handRoot, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-(pileW + 20) / 2, handBottom), handRoot.sizeDelta);
            UIKit.Place(pileRoot, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-pad, handBottom), pileRoot.sizeDelta);

            float logBottom = handBottom + L.CardH + 24;
            var log = Backing(logText, PlateTint, UiArt.Plate, 12);
            logText.alignment = TextAnchor.MiddleLeft;
            log.anchorMin = new Vector2(0, 0);
            log.anchorMax = new Vector2(1, 0);
            log.pivot = new Vector2(0.5f, 0);
            log.offsetMin = new Vector2(pad, logBottom);
            log.offsetMax = new Vector2(-pad, logBottom + 66);

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
            // На телефоне панель шире (доля ширины холста), а строки и кнопки в ней крупнее в s раз.
            const float margin = 20;
            float canvasW = 1080f * Screen.width / Mathf.Max(1, Screen.height);
            float panelW = L.PanelShare > 0 ? Mathf.Max(600, canvasW * L.PanelShare) : 600;
            float s = L.PanelScale;
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
            UIKit.TopBand(statsText.rectTransform, 18, 56 * s, 18);
            statsBar = statsText.rectTransform;
            AttachStatsRight(statsBar, 0);
            float y = 18 + 56 * s + 6;
            UIKit.TopBand(phaseText.rectTransform, y, 80 * s, 18);
            y += 80 * s + 8;
            UIKit.TopBand(oppRoot, y, 190 * s, 14);
            // Соперник в панели: подпись слева, рубашки в середине, колода справа.
            oppInfo.rectTransform.offsetMax = new Vector2(210 * s, -8);
            UIKit.Place(oppHandRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30 * s, 0), new Vector2(230 * s, 160));
            y += 190 * s + 12;

            float buttonH = 100 * s;
            var log = Backing(logText, new Color(0, 0, 0, 0.3f), Sprites.Soft, 12);
            log.anchorMin = Vector2.zero;
            log.anchorMax = Vector2.one;
            log.offsetMin = new Vector2(18, 24 + buttonH + 20);
            log.offsetMax = new Vector2(-18, -y);

            foreach (var b in new[] { rotateButton, nightButton }) b.transform.SetParent(panel, false);
            float buttonW = (panelW - 18 * 3) / 2;
            PlaceBottom(rotateButton, new Vector2(0, 0), new Vector2(18, 24), new Vector2(buttonW, buttonH));
            PlaceBottom(nightButton, new Vector2(1, 0), new Vector2(-18, 24), new Vector2(buttonW, buttonH));

            // Всё остальное — в области слева от панели (она растягивается на широких телефонах).
            var main = UIKit.Rect("Main", root);
            main.SetAsFirstSibling();
            main.anchorMin = Vector2.zero;
            main.anchorMax = Vector2.one;
            main.offsetMin = Vector2.zero;
            main.offsetMax = new Vector2(-(panelW + margin), 0);

            UIKit.Place((RectTransform)menuButton.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(100, 100));

            // Рука видна целиком: снизу запас под рамку, которая выступает за край карты (48/420 высоты).
            float handLift = L.CardH * 48f / 420f + 8;
            float handVisible = L.CardH + handLift + 16;
            float safeH = 1080f * Screen.safeArea.height / Mathf.Max(1, Screen.height);
            float fit = Mathf.Clamp((safeH - 20 - handVisible) / GridHeight, 0.4f, 1f);

            gridRoot.SetParent(main, false);
            gridRoot.anchorMin = gridRoot.anchorMax = new Vector2(0.5f, 1);
            gridRoot.pivot = new Vector2(0.5f, 1);
            gridRoot.anchoredPosition = new Vector2(0, -20);
            gridRoot.localScale = Vector3.one * fit;

            handRoot.SetParent(main, false);
            UIKit.Place(handRoot, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-70, handLift), handRoot.sizeDelta);
            pileRoot.SetParent(main, false);
            UIKit.Place(pileRoot, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 12), pileRoot.sizeDelta);
        }

        // Золото и сердца — справа в полосе статуса; ширину текста названия ужимаем под них в UpdateStats.
        void AttachStatsRight(RectTransform bar, float rightPad)
        {
            statsRight.SetParent(bar, false);
            statsRight.anchorMin = statsRight.anchorMax = new Vector2(1, 0.5f);
            statsRight.pivot = new Vector2(1, 0.5f);
            statsRight.anchoredPosition = new Vector2(-rightPad, 2);
            statsRight.sizeDelta = new Vector2(10, L.StatsFont * 1.3f);
            statsTextRight = statsText.rectTransform.offsetMax.x;
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

        // Размер шрифта не меньше читаемого на этом экране.
        int Fs(int size) => Mathf.Max(L.MinFont, size);
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

        void Refresh()
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
            if (phaseBar != null) phaseBar.gameObject.SetActive(!string.IsNullOrEmpty(phaseText.text));

            bool playing = state == Phase.Day || state == Phase.Night;
            rotateButton.gameObject.SetActive(playing);
            rotateButton.interactable = game.CanAct && (game.Selected >= 0 || game.MovingFrom.HasValue);
            nightButton.gameObject.SetActive(playing);
            nightButton.interactable = state == Phase.Day || game.Responding;
            bool letIn = state == Phase.Night;
            SetButton(nightButton, letIn ? "ВПУСТИТЬ" : "НОЧЬ", letIn ? LetInColor : NightColor);
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
            UIKit.Paint(b, color);
        }

        string PhaseHint()
        {
            switch (game.State)
            {
                case Phase.Day:
                {
                    string status = $"Ходов: {game.ActionsLeft} · Рука {game.Hand.Count}/{GameController.MaxHand}";
                    bool explain = game.IsTutorial || (!game.IsWander && game.Level.Index < 2);
                    if (game.ActionsLeft <= 0) return "Ходы закончились — жми «Ночь»";
                    if (!explain) return status;
                    if (game.MovingFrom.HasValue) return "Коснись зелёной клетки, чтобы перенести";
                    if (game.Selected >= 0 && game.Grid.AllPositions().Any(game.CanUpgradeAt)) return "Такая же комната в зелёной рамке — улучшить";
                    if (game.Grid.BrokenCount() > 0) return "Коснись сломанной комнаты — починить (1 ход)";
                    return "День: строй комнаты до ночи · " + status;
                }
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
            string title = game.IsTutorial ? "ОБУЧЕНИЕ · ДЕНЬ 0"
                : $"{game.Level.Title.ToUpper()} · НОЧЬ {Mathf.Min(game.Night, game.MaxNights)}/{game.MaxNights}";
            statsText.text = title;

            // Справа: сердца (полные и пустые до начального запаса) и золото рядом.
            int rep = Mathf.Max(0, game.Reputation);
            int maxRep = game.IsWander ? GameController.WanderMaxReputation : game.Level.StartReputation;
            statHearts.Set(rep, Mathf.Max(0, maxRep - rep));
            statGold.Set(gold.ToString());
            float gap = 22;
            var hr = statHearts.Root;
            hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(1, 0.5f);
            hr.anchoredPosition = Vector2.zero;
            var gr = statGold.Root;
            gr.anchorMin = gr.anchorMax = gr.pivot = new Vector2(1, 0.5f);
            gr.anchoredPosition = new Vector2(-(hr.sizeDelta.x + gap), 0);
            float rightW = hr.sizeDelta.x + gap + gr.sizeDelta.x + 16;
            statsRight.sizeDelta = new Vector2(rightW, statsRight.sizeDelta.y);
            var off = statsText.rectTransform.offsetMax;
            statsText.rectTransform.offsetMax = new Vector2(statsTextRight - rightW - 20, off.y);
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
            // Тап — выбор, долгое нажатие — крупный просмотр (тогда выбора нет).
            var hold = parts.Root.gameObject.AddComponent<LongPress>();
            hold.OnHold = () => InspectRoom(def, level, def.Doors, parts.Root);
            btn.onClick.AddListener(() =>
            {
                if (hold.Consume()) return;
                Sfx.Click();
                onClick();
            });
            parts.Root.gameObject.AddComponent<PressScale>().Pressed = 0.95f;
            return parts.Root;
        }

        // Внешний вид карты: цветная рамка, окно с иконкой комнаты, лента с названием, светлая плашка описания.
        // Без логики нажатий — её добавляют рука (HandCard) или экраны выбора (Button).
        // compact — карта в руке: на телефоне она слишком мала для описания, поэтому только арт,
        // крупное название и схема дверей; описание — в крупном просмотре карты.
        CardParts BuildCardVisual(Transform parent, RoomDef def, Doors doors, int level = 1, bool compact = false)
        {
            return BuildRoomCard(parent, def, doors, level, Icons.RoomArt(def.Type), compact);
        }

        // Карта комнаты из слоёв, в координатах лица 300×420 (от левого верхнего угла):
        // арт комнаты сверху, пергамент снизу, красная лента с названием на стыке, схема дверей в углу арта,
        // поверх всего — рамка круга улучшения (Art/Frames), та же, что у рубашек.
        CardParts BuildRoomCard(Transform parent, RoomDef def, Doors doors, int level, Sprite art, bool compact = false)
        {
            var card = UIKit.Rect("Card", parent);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0);
            card.pivot = new Vector2(0.5f, 0);
            card.sizeDelta = new Vector2(L.CardW, L.CardH);

            var area = UIKit.Rect("Face", card);
            UIKit.Stretch(area);
            var fit = area.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = FaceW / FaceH;
            // Сколько единиц интерфейса в одной единице лица — для шрифтов и рамок пергамента.
            float k = Mathf.Min(L.CardH, L.CardW * FaceH / FaceW) / FaceH;

            var glow = UIKit.Image("Glow", area, new Color(1, 0.85f, 0.2f, 0.95f), Sprites.Shadow);
            glow.raycastTarget = false;
            UIKit.Stretch(glow.rectTransform, -Sprites.ShadowPad - 30 * k);
            glow.enabled = false;
            UIKit.DropShadow(area, new Vector2(4, -12), 0.5f);

            // Подложка: у арта прозрачные углы, под ними не должно просвечивать поле.
            // Она же ловит нажатия по всей карте (остальные слои касаний не ловят).
            // Всё содержимое сжато на FaceInset внутрь: у рамок планки начинаются чуть внутри края карты,
            // и без отступа картинка выглядывала бы над рамкой.
            var bg = UIKit.Image("Bg", area, RoomDatabase.Hex("2a1f1a"), Sprites.Flat);
            FacePx(bg.rectTransform, FaceInset, FaceInset, FaceW - FaceInset, FaceH - FaceInset);

            var pic = UIKit.Image("Art", area, Color.white, art);
            pic.raycastTarget = false;
            FacePx(pic.rectTransform, FaceInset, FaceInset, FaceW - FaceInset, 300);

            var panelSprite = Icons.TextPanel;
            var panel = UIKit.Image("Panel", area, panelSprite != null ? Color.white : Cream, panelSprite != null ? panelSprite : Sprites.Flat);
            panel.raycastTarget = false;
            if (panelSprite != null) panel.pixelsPerUnitMultiplier = 1 / k; // края пергамента в масштабе карты
            FacePx(panel.rectTransform, FaceInset, 256, FaceW - FaceInset, FaceH - FaceInset);

            var ribbonEdge = UIKit.Image("RibbonEdge", area, RoomDatabase.Hex("d9a441"), Sprites.Flat);
            ribbonEdge.raycastTarget = false;
            // В руке лента шире и выше: название должно читаться крупно (не меньше L.MinFont).
            float rt0 = compact ? 220 : 237, rt1 = compact ? 292 : 273;
            FacePx(ribbonEdge.rectTransform, compact ? 6 : 10, rt0 - 3, compact ? 294 : 290, rt1 + 3);
            var ribbon = UIKit.Image("Ribbon", area, RoomDatabase.Hex("7a2418"), Sprites.Flat);
            ribbon.raycastTarget = false;
            FacePx(ribbon.rectTransform, compact ? 9 : 13, rt0, compact ? 291 : 287, rt1);
            string title = level > 1 ? $"{def.Name} {HouseDatabase.Roman(level)}" : def.Name;
            // В руке: крупно, но длинные названия («Сокровищница», «Библиотека») ужимаются, чтобы влезть в ленту.
            int nameMax = compact ? Mathf.RoundToInt(L.CardNameFont * 0.85f) : Mathf.RoundToInt(22 * k);
            var name = UIKit.Outlined(UIKit.Text(area, title, nameMax, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold), 2);
            FacePx(name.rectTransform, compact ? 14 : 22, rt0 - 1, compact ? 286 : 278, rt1 + 1);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = compact ? Mathf.RoundToInt(L.CardNameFont * 0.65f) : Mathf.Max(8, Mathf.RoundToInt(12 * k));
            name.resizeTextMaxSize = nameMax;

            DoorScheme scheme;
            if (compact)
            {
                // Описания нет — на пергаменте крупная схема дверей.
                scheme = new DoorScheme(area, 84 * k);
                scheme.Root.anchorMin = scheme.Root.anchorMax = new Vector2(0.5f, 1 - 348 / FaceH);
            }
            else
            {
                // Описание — в безопасной зоне пергамента: ~30 от боков и ~60 от низа (там камни богатых рамок).
                var desc = UIKit.Text(area, def.Description, Mathf.RoundToInt(17 * k), TextAnchor.MiddleCenter, InkBrown, FontStyle.Bold);
                FacePx(desc.rectTransform, 30, 280, 270, 360);
                desc.resizeTextForBestFit = true;
                desc.resizeTextMinSize = Mathf.Max(7, Mathf.RoundToInt(10 * k));
                desc.resizeTextMaxSize = Mathf.RoundToInt(17 * k);

                scheme = new DoorScheme(area, 48 * k);
                scheme.Root.anchorMin = scheme.Root.anchorMax = new Vector2(214 / FaceW, 1 - 94 / FaceH);
            }
            scheme.Root.anchoredPosition = Vector2.zero;
            scheme.Set(doors);

            CardBacks.AddFrame(area, Card.FrameTier(level));

            return new CardParts { Root = card, Frame = pic, Glow = glow, Def = def, Level = level, Painted = true, Scheme = scheme };
        }

        const float FaceW = 300, FaceH = 420, FaceInset = 8;

        // Прямоугольник на лице карты в «пикселях» 300×420 от левого верхнего угла.
        static void FacePx(RectTransform rt, float x0, float y0, float x1, float y1) =>
            Band(rt, x0 / FaceW, 1 - y1 / FaceH, x1 / FaceW, 1 - y0 / FaceH);

        // Растянуть элемент на прямоугольник бланка, заданный долями (лево, низ, право, верх).
        static void Band(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ───────────── Перетаскивание карты на поле ─────────────

        TileView dragGhost;
        Image dragRing, dragFx;
        Vector2Int? dragCell;

        // «Призрак» комнаты: показывает, куда и как встанет карта, пока её тянут над доской.
        void BuildDragGhost()
        {
            dragGhost = new TileView(gridRoot, L.Cell, L.TileFont, false);
            dragGhost.Root.gameObject.AddComponent<CanvasGroup>().alpha = 0.85f;
            dragGhost.Root.GetComponent<CanvasGroup>().blocksRaycasts = false;
            // Подсветка клетки (Art/Fx/TileOk|TileBad) — под призраком комнаты.
            dragFx = UIKit.Image("Highlight", dragGhost.Root, Color.white);
            dragFx.raycastTarget = false;
            dragFx.preserveAspect = true;
            UIKit.Place(dragFx.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * L.Cell * 1.45f);
            dragFx.transform.SetAsFirstSibling();
            dragFx.enabled = false;
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
            var highlight = FxArt(ok ? "TileOk" : "TileBad");
            dragFx.sprite = highlight;
            dragFx.enabled = highlight != null;

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
            // Улучшенная комната — с искрами.
            var room = game.Grid.Get(p);
            if (room != null && room.Level > 1) Burst(gridRoot, "Sparkles", CellPos(p), L.Cell * 1.2f, 0.25f, 1.1f);
        }

        void AnimateBroken(Vector2Int p)
        {
            tiles[p.x, p.y].PlayBreak();
            Burst(gridRoot, "Dust", CellPos(p), L.Cell * 1.2f, 0.15f);
            Sfx.Break();
        }
    }
}
