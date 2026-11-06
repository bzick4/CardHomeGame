using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Экраны «Странствия»: карта забега на пергаменте и ярмарка.
    public partial class GameView
    {
        static readonly Color FairColor = RoomDatabase.Hex("c9933a");
        static readonly Color BossColor = RoomDatabase.Hex("6b2b8f");

        RectTransform mapRoot, mapContent, mapNodes;
        bool abandonArmed;

        RectTransform fairRoot, fairContent, fairCards;
        Text fairGold, fairInfo;

        RectTransform restRoot, restContent;
        Text restInfo;
        Button restHeal;

        RectTransform eventRoot, eventContent, eventChoices, eventPortrait;
        Text eventTitle, eventText;
        string eventShown;

        void BuildWander()
        {
            BuildRunMap();
            BuildFair();
            BuildRest();
            BuildEvent();
        }

        void ShowWander(Phase phase, bool animate)
        {
            bool map = phase == Phase.Map, fair = phase == Phase.Fair;
            bool wasMap = mapRoot.gameObject.activeSelf, wasFair = fairRoot.gameObject.activeSelf;
            mapRoot.gameObject.SetActive(map);
            fairRoot.gameObject.SetActive(fair);
            if (animate && map && !wasMap) Pop(mapContent);
            if (animate && fair && !wasFair) Pop(fairContent);
            bool rest = phase == Phase.Rest, ev = phase == Phase.Event;
            bool wasRest = restRoot.gameObject.activeSelf, wasEvent = eventRoot.gameObject.activeSelf;
            restRoot.gameObject.SetActive(rest);
            eventRoot.gameObject.SetActive(ev);
            if (animate && rest && !wasRest) Pop(restContent);
            if (animate && ev && !wasEvent) Pop(eventContent);
            if (!ev) eventShown = null;
            abandonArmed = false;
        }

        void RefreshWander()
        {
            if (mapRoot.gameObject.activeSelf) RefreshRunMap();
            if (fairRoot.gameObject.activeSelf) RefreshFair();
            if (restRoot.gameObject.activeSelf) RefreshRest();
            if (eventRoot.gameObject.activeSelf) RefreshEvent();
            // Окна колоды открываются и с карты, и с ярмарки — там обычный Refresh до них не доходит.
            if (mapRoot.gameObject.activeSelf || fairRoot.gameObject.activeSelf || restRoot.gameObject.activeSelf || eventRoot.gameObject.activeSelf)
            {
                if (removeOpen) RefreshRemove();
                if (deckRoot.gameObject.activeSelf) RefreshDeckView();
            }
        }

        // ───────────── Карта забега ─────────────
        // В стиле игры: вывеска, лента с сердцами и золотом, свиток-карта с валиками,
        // точки — объёмные цветные кружки с обводкой, тропы — пунктир как на карте сокровищ.

        MenuBtn deckBtn, abandonBtn;
        Text mapStats;

        void BuildRunMap()
        {
            mapRoot = MenuScreen("RunMap", out mapContent);
            // Своя карта (Art/Wander/MapBackground или …Wide) — на весь экран, вписана целиком;
            // точки ставятся в долях картинки. Нет картинки — прежний пергамент с валиками.
            bool wide = !L.Portrait;
            var art = MenuArt.WanderArt(wide ? "MapBackgroundWide" : "MapBackground");
            if (art == null) { wide = !wide; art = MenuArt.WanderArt(wide ? "MapBackgroundWide" : "MapBackground"); }
            wanderFrac = art != null;
            wanderWide = wide;
            if (art != null)
            {
                var area = UIKit.Rect("MapArt", mapRoot);
                area.SetSiblingIndex(mapContent.GetSiblingIndex()); // над фоном меню, под интерфейсом
                UIKit.Stretch(area);
                var img = UIKit.Image("Map", area, Color.white, art);
                img.raycastTarget = false;
                UIKit.Stretch(img.rectTransform);
                var fit = img.gameObject.AddComponent<AspectRatioFitter>();
                // Карта заполняет весь экран (лишнее по краям обрезается — точки стоят далеко от краёв).
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = art.rect.width / art.rect.height;
                mapNodes = UIKit.Rect("Nodes", img.rectTransform);
                UIKit.Stretch(mapNodes);
            }
            else
            {
                var paperRoot = UIKit.Rect("Map", mapContent);
                UIKit.Place(paperRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(960, 1260));
                UIKit.DropShadow(paperRoot, new Vector2(0, -14), 0.5f);
                var paper = UIKit.Image("Paper", paperRoot, Color.white, GuestArt.MapPaper);
                paper.raycastTarget = false;
                UIKit.Stretch(paper.rectTransform);
                Roller(mapContent, 630);
                Roller(mapContent, -670);
                mapNodes = UIKit.Rect("Nodes", paperRoot);
                UIKit.Stretch(mapNodes);
            }

            // Шапка. На своей карте наверху крепость с боссом — вывеску не ставим, лента с сердцами
            // и золотом поднимается к кнопке «назад».
            // На своей карте шапка и кнопки прижаты к краям экрана (карта занимает его целиком).
            var top = wanderFrac ? EdgeFrame(mapRoot, true) : mapContent;
            var bottom = wanderFrac ? EdgeFrame(mapRoot, false) : mapContent;
            RoundButton(top, MenuArt.BackIcon, new Vector2(-445, 870), () => game?.ShowMenu(), 110);
            if (wanderFrac)
                mapStats = Ribbon(top, "", 870, 760, 60);
            else
            {
                WoodSign(mapContent, "СТРАНСТВИЕ", 850, 640, 130, L.StatsFont + 22);
                mapStats = Ribbon(mapContent, "", 735, 700);
            }

            var size = new Vector2(470, 130);
            deckBtn = MakeMenuButton(bottom, MenuWood, "Колода", new Vector2(-250, -820), size, OpenDeckView);
            abandonBtn = MakeMenuButton(bottom, SkipColor, "Сдаться", new Vector2(250, -820), size, OnAbandon);
        }

        // Рамка 1080×1920 в масштабе экранов меню, прижатая к верху или низу экрана:
        // координаты внутри — как в обычном Content, но край совпадает с краем экрана.
        RectTransform EdgeFrame(RectTransform screen, bool atTop)
        {
            var f = UIKit.Rect(atTop ? "Top" : "Bottom", screen);
            var a = new Vector2(0.5f, atTop ? 1 : 0);
            f.anchorMin = f.anchorMax = f.pivot = a;
            f.anchoredPosition = Vector2.zero;
            f.sizeDelta = new Vector2(1080, 1920);
            f.localScale = Vector3.one * OverlayScale;
            return f;
        }

        void OnAbandon()
        {
            if (!abandonArmed)
            {
                abandonArmed = true;
                RefreshRunMap();
                return;
            }
            abandonArmed = false;
            game?.AbandonWander();
        }

        bool wanderFrac, wanderWide; // точки в долях своей картинки карты (и какой — вертикальной или широкой)

        // Где стоит точка. На своей карте — доля картинки (x слева, y снизу): ряды идут снизу вверх
        // по дороге к крепости, босс — в её воротах. На пергаменте — смещение от центра в единицах интерфейса.
        Vector2 NodePos(MapNode n)
        {
            if (!wanderFrac)
            {
                float y = -540 + n.Row * (1110f / (RunMap.Rows - 1));
                return new Vector2(n.X * 360, y);
            }
            float t = n.Row / (float)(RunMap.Rows - 1);
            float fromTop = wanderWide ? Mathf.Lerp(0.84f, 0.19f, t) : Mathf.Lerp(0.80f, 0.135f, t);
            float x = n.Kind == NodeKind.Boss
                ? (wanderWide ? 0.512f : 0.555f)
                : 0.5f + n.X * (wanderWide ? 340f / 1920f : 360f / 1080f);
            return new Vector2(x, 1 - fromTop);
        }

        // Размер картинки карты в единицах интерфейса (для расстояний между точками тропы).
        Vector2 MapUnits => wanderWide ? new Vector2(1920, 1080) : new Vector2(1080, 1920);

        // Поставить элемент в точку карты.
        void PutOnMap(RectTransform rt, Vector2 pos, Vector2 size)
        {
            if (wanderFrac)
            {
                rt.anchorMin = rt.anchorMax = pos;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = size;
            }
            else UIKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        }

        void RefreshRunMap()
        {
            var map = game.Map;
            if (map == null) return;
            var w = game.Wander;
            mapStats.text = $"<color=#c8483c>{new string('♥', Mathf.Max(0, game.Reputation))}</color>   " +
                            $"<color=#a8741a>● {game.Gold}</color>   шаг {Mathf.Max(0, w.Row + 1)} из {RunMap.Rows}";
            StyleMenuButton(deckBtn, false, true, $"{game.RunDeckSize} карт");
            StyleMenuButton(abandonBtn, false, true, abandonArmed ? "нажми ещё раз" : "забег закончится");
            abandonBtn.Body.color = abandonArmed ? Bad : SkipColor;

            Clear(mapNodes);

            // Тропы: пунктир чернилами; пройденные — золотые, доступные — красные, как маршрут на карте сокровищ.
            foreach (var row in map.Grid)
            foreach (var a in row)
            foreach (int c in a.Next)
            {
                var b = map.Get(a.Row + 1, c);
                bool walked = game.Visited(a) && game.Visited(b);
                bool open = game.CurrentNode == a && game.CanEnter(b);
                var pa = NodePos(a);
                var pb = NodePos(b);
                var dist = wanderFrac ? Vector2.Scale(pb - pa, MapUnits) : pb - pa;
                int n = Mathf.Max(2, Mathf.RoundToInt(dist.magnitude / 32));
                // Светлый пунктир тропы; пройденная — золотая, куда можно пойти — ярче и крупнее.
                var color = walked ? RoomDatabase.Hex("ffcf3a") : open ? RoomDatabase.Hex("fff1cf") : RoomDatabase.Hex("e8d4a6");
                for (int k = 1; k < n; k++)
                {
                    var dot = UIKit.Image("Dot", mapNodes, open || walked ? color : new Color(color.r, color.g, color.b, 0.75f), Sprites.Circle);
                    dot.raycastTarget = false;
                    PutOnMap(dot.rectTransform, Vector2.Lerp(pa, pb, k / (float)n), Vector2.one * (open ? 20 : walked ? 18 : 14));
                    if (open)
                        dot.rectTransform.DOScale(1.25f, 0.45f).SetDelay(k * 0.08f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
                            .SetLink(dot.gameObject);
                }
            }

            foreach (var row in map.Grid)
            foreach (var node in row)
                RunNode(node);
        }

        // Точка карты: каменный постамент с золотой табличкой, на нём значок вида точки.
        // Доступные светятся и подпрыгивают, пройденные — потемнее с золотой галочкой, у текущей стоит Путник.
        void RunNode(MapNode n)
        {
            bool visited = game.Visited(n);
            bool reachable = game.CanEnter(n);
            bool current = game.CurrentNode == n;
            bool boss = n.Kind == NodeKind.Boss;
            float k = boss ? 1.3f : 1f;
            var size = new Vector2(150, 150) * k;

            var node = UIKit.Rect("Node", mapNodes);
            PutOnMap(node, NodePos(n), size);
            // На широкой карте ряды теснее — точки мельче.
            if (wanderFrac && wanderWide) node.localScale = Vector3.one * 0.72f;

            if (reachable || boss)
            {
                var glow = UIKit.Image("Glow", node, boss ? new Color(0.75f, 0.4f, 1f, 0.85f) : new Color(1, 0.85f, 0.3f, 0.9f), Sprites.Light);
                glow.raycastTarget = false;
                UIKit.Stretch(glow.rectTransform, -size.x * 0.35f);
                glow.DOFade(0.3f, boss ? 0.9f : 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(glow.gameObject);
            }

            // Пройденные — чуть темнее, далёкие — чуть бледнее (но не прозрачные).
            var tint = visited ? new Color(0.62f, 0.6f, 0.58f) : (!reachable && !current && !boss) ? new Color(0.85f, 0.85f, 0.85f) : Color.white;
            var whole = MenuArt.WanderArt("Node" + n.Kind);
            RectTransform iconRt;
            if (whole != null)
            {
                // Своя картинка точки (постамент + значок одним файлом).
                var img = UIKit.Image("Node", node, tint, whole);
                img.raycastTarget = false;
                img.preserveAspect = true;
                UIKit.Stretch(img.rectTransform);
                iconRt = img.rectTransform;
            }
            else
            {
                var ped = UIKit.Image("Pedestal", node, tint, MenuArt.Pedestal);
                ped.raycastTarget = false;
                UIKit.Place(ped.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(size.x, size.x * 0.75f));

                var holder = UIKit.Rect("Icon", node);
                UIKit.Place(holder, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, size.y * 0.33f), new Vector2(size.x * 0.66f, size.x * 0.66f));
                iconRt = holder;
                if (boss)
                {
                    var disc = UIKit.Image("Disc", holder, RoomDatabase.Hex("6b3fa0"), Sprites.Circle);
                    disc.raycastTarget = false;
                    UIKit.Stretch(disc.rectTransform, -6);
                }
                if (n.Kind == NodeKind.House)
                {
                    // Дом — маленькая круглая картинка этого дома на постаменте.
                    HouseDisc(holder, HouseDatabase.All[n.House], !visited, Vector2.zero, size.x * 0.66f);
                }
                else
                {
                    var icon = UIKit.Image("Sprite", holder, tint, n.Kind == NodeKind.Event ? MenuArt.EventBanner : NodeIcon(n));
                    icon.raycastTarget = false;
                    icon.preserveAspect = true;
                    UIKit.Stretch(icon.rectTransform);
                }
                if (n.Kind == NodeKind.Event)
                {
                    var q = UIKit.Outlined(UIKit.Text(holder, "?", Mathf.RoundToInt(size.x * 0.32f), TextAnchor.MiddleCenter, Color.white, FontStyle.Bold), 3);
                    UIKit.Stretch(q.rectTransform);
                    q.rectTransform.anchoredPosition = new Vector2(0, 2);
                }
                // Костёр мерцает.
                if (n.Kind == NodeKind.Rest && !visited)
                    holder.DOScale(new Vector3(1.05f, 1.1f, 1), 0.35f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(holder.gameObject);
            }

            // Подпись показываем у тех, куда можно пойти, и у босса — чтобы не загромождать карту.
            if (reachable || boss)
            {
                var plate = UIKit.Image("Label", node, Cream, Sprites.Flat);
                plate.raycastTarget = false;
                string label = NodeLabel(n);
                float lw = Mathf.Clamp(label.Length * 15 + 34, 130, 300);
                UIKit.Place(plate.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, 6), new Vector2(lw, 40));
                var t = UIKit.Text(plate.transform, label, L.BodyFont - 6, TextAnchor.MiddleCenter,
                    boss ? RoomDatabase.Hex("6b2b8f") : n.Kind == NodeKind.Elite ? RoomDatabase.Hex("8f2b2b") : InkBrown, FontStyle.Bold);
                UIKit.Stretch(t.rectTransform, 4);
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 10;
                t.resizeTextMaxSize = L.BodyFont - 6;
            }

            if (visited) Badge(node, "✓");

            if (current)
            {
                // Путник стоит у постамента, где он сейчас.
                var hero = UIKit.Image("Hero", node, Color.white, GuestArt.Character(GuestDatabase.Traveler));
                hero.raycastTarget = false;
                hero.preserveAspect = true;
                UIKit.Place(hero.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(size.x * 0.55f, 0), new Vector2(100, 100));
                hero.rectTransform.DOAnchorPosY(10, 0.5f).SetRelative().SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(hero.gameObject);
            }

            if (!reachable) return;
            // Значок подпрыгивает — «сюда можно».
            iconRt.DOAnchorPosY(10, 0.45f).SetRelative().SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(iconRt.gameObject);
            var hit = node.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var btn = node.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            node.gameObject.AddComponent<PressScale>().Pressed = 0.92f;
            int r = n.Row, c = n.Col;
            btn.onClick.AddListener(() =>
            {
                Sfx.Click();
                game?.EnterNode(r, c);
            });
        }

        static Sprite NodeIcon(MapNode n)
        {
            switch (n.Kind)
            {
                case NodeKind.Fair: return Icons.Coin;
                case NodeKind.Boss: return Icons.Guest(GuestDatabase.GhostLord);
                case NodeKind.Rest: return MenuArt.Campfire;
                case NodeKind.Elite: return MenuArt.Swords;
                default: return Icons.House(HouseDatabase.All[n.House]);
            }
        }

        static string NodeLabel(MapNode n)
        {
            switch (n.Kind)
            {
                case NodeKind.Fair: return "Ярмарка";
                case NodeKind.Boss: return "ХОЗЯИН";
                case NodeKind.Rest: return "Привал";
                case NodeKind.Event: return "Случай";
                case NodeKind.Elite: return "Опасно! " + HouseDatabase.All[n.House].Name;
                default: return HouseDatabase.All[n.House].Name;
            }
        }

        // ───────────── Привал ─────────────

        void BuildRest()
        {
            restRoot = MenuScreen("Rest", out restContent);
            Header(restContent, "ПРИВАЛ");

            // Костёр: камин с мерцающим светом.
            var fire = UIKit.Rect("Fire", restContent);
            UIKit.Place(fire, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 420), new Vector2(360, 360));
            var glow = UIKit.Image("Glow", fire, new Color(1, 0.6f, 0.2f, 0.7f), Sprites.Light);
            glow.raycastTarget = false;
            UIKit.Stretch(glow.rectTransform, -120);
            glow.DOFade(0.35f, 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(glow.gameObject);
            var icon = UIKit.Image("Icon", fire, Color.white, Icons.Room(RoomType.Fireplace));
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            UIKit.Stretch(icon.rectTransform, 40);
            icon.rectTransform.DOScale(1.05f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(icon.gameObject);

            restInfo = Label(restContent, "", L.StatsFont, Color.white, 150, 90, 1000);
            Label(restContent, "Выбери одно:", L.BodyFont + 4, Cream, 60, 60, 1000, FontStyle.Normal);
            restHeal = Place(UIKit.Button(restContent, "ОТДОХНУТЬ\n+1 ♥", RoomDatabase.Hex("d0473c"), () => game?.RestHeal(), L.ButtonFont), -240, -120, 440, 180);
            Place(UIKit.Button(restContent, "МАСТЕРСКАЯ\nулучшить карту даром", RoomDatabase.Hex("d4a62c"), () => OpenDeckShop(true, true), L.ButtonFont - 6),
                240, -120, 440, 180);
            Place(UIKit.Button(restContent, "КОЛОДА", MenuButton, OpenDeckView, L.ButtonFont - 6), 0, -320, 400, 100);
        }

        void RefreshRest()
        {
            restInfo.text = $"<color=#ff6b6b>{new string('♥', Mathf.Max(0, game.Reputation))}</color>    колода: {game.RunDeckSize}";
            restHeal.interactable = game.CanRestHeal;
            SetLabel(restHeal, game.CanRestHeal ? "ОТДОХНУТЬ\n+1 ♥" : $"ОТДОХНУТЬ\nмаксимум {GameController.WanderMaxReputation} ♥");
        }

        // ───────────── Случай ─────────────

        void BuildEvent()
        {
            eventRoot = MenuScreen("Event", out eventContent);
            eventTitle = Header(eventContent, "").GetComponentInChildren<Text>();

            // Портрет рассказчика: пейзаж и персонаж в деревянной рамке, как на картах гостей.
            eventPortrait = UIKit.Rect("Portrait", eventContent);
            UIKit.Place(eventPortrait, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 470), new Vector2(380, 420));

            var paper = UIKit.Image("Paper", eventContent, RoomDatabase.Hex("f3d79b"), Sprites.Flat);
            UIKit.Place(paper.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(960, 260));
            eventText = UIKit.Text(paper.transform, "", L.BodyFont + 6, TextAnchor.MiddleCenter, InkBrown, FontStyle.Bold);
            UIKit.Stretch(eventText.rectTransform, 30);
            eventText.resizeTextForBestFit = true;
            eventText.resizeTextMinSize = L.BodyFont - 4;
            eventText.resizeTextMaxSize = L.BodyFont + 6;

            eventChoices = UIKit.Rect("Choices", eventContent);
            UIKit.Place(eventChoices, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -330), new Vector2(960, 560));
        }

        void RefreshEvent()
        {
            var ev = game.CurrentEvent;
            if (ev == null) return;
            string key = ev.Title + "|" + game.EventResult + "|" + game.Gold + "|" + game.Reputation;
            if (key == eventShown) return;
            bool firstShow = eventShown == null || !eventShown.StartsWith(ev.Title + "|");
            eventShown = key;

            eventTitle.text = ev.Title.ToUpper();
            if (firstShow)
            {
                Clear(eventPortrait);
                UIKit.DropShadow(eventPortrait, new Vector2(4, -12), 0.5f);
                var frame = UIKit.Image("Frame", eventPortrait, RoomDatabase.Hex("8a5a32"), Sprites.Panel);
                frame.raycastTarget = false;
                UIKit.Stretch(frame.rectTransform);
                var window = UIKit.Rect("Window", eventPortrait);
                UIKit.Stretch(window, 16);
                window.gameObject.AddComponent<RectMask2D>();
                var sky = UIKit.Image("Sky", window, Color.white, GuestArt.Backdrop);
                sky.raycastTarget = false;
                UIKit.Cover(sky);
                var hero = UIKit.Image("Hero", window, Color.white, GuestArt.Character(GuestDatabase.ById(ev.GuestId)));
                hero.raycastTarget = false;
                hero.preserveAspect = true;
                UIKit.Place(hero.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -20), new Vector2(360, 360));
            }

            bool done = game.EventResult != null;
            eventText.text = done ? game.EventResult : ev.Text;

            Clear(eventChoices);
            if (done)
            {
                Place(UIKit.Button(eventChoices, "ДАЛЬШЕ ▶", RestartColor, () => game?.FinishEvent(), L.ButtonFont), 0, 180, 520, 120);
                return;
            }
            for (int i = 0; i < ev.Choices.Length; i++)
            {
                int index = i;
                bool can = game.CanChoose(i);
                var b = UIKit.Button(eventChoices, ev.Choices[i].Label, can ? MenuButton : SkipColor, () => game?.ChooseEvent(index), L.BodyFont + 4);
                Place(b, 0, 200 - i * 150, 900, 126);
                b.interactable = can;
            }
        }

        RectTransform Header(RectTransform content, string text)
        {
            var t = WoodSign(content, text.ToUpper(), 840, 700, 140, L.StatsFont + 22);
            return (RectTransform)t.transform.parent;
        }

        // ───────────── Ярмарка ─────────────

        void BuildFair()
        {
            fairRoot = MenuScreen("Fair", out fairContent);
            Header(fairContent, "Ярмарка");

            fairGold = Label(fairContent, "", L.StatsFont + 4, Gold, 740, 80);
            Label(fairContent, "Коснись карты, чтобы купить её в колоду странствия", L.BodyFont + 2, Color.white, 660, 60, 1000, FontStyle.Normal);

            fairCards = UIKit.Rect("Cards", fairContent);
            fairCards.localScale = new Vector3(1.2f, 1.2f, 1);
            UIKit.Place(fairCards, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 330),
                new Vector2(L.CardW * 3 + L.CardGap * 4, L.CardH + 60));

            fairInfo = Label(fairContent, "", L.BodyFont, Cream, -20, 70, 1000, FontStyle.Bold);
            Place(UIKit.Button(fairContent, "УЛУЧШИТЬ КАРТУ", RoomDatabase.Hex("d4a62c"), () => OpenDeckShop(true), L.ButtonFont - 6), -240, -170, 440, 120);
            Place(UIKit.Button(fairContent, "УБРАТЬ КАРТУ", ShopColor, () => OpenDeckShop(false), L.ButtonFont - 6), 240, -170, 440, 120);
            Place(UIKit.Button(fairContent, "КОЛОДА", MenuButton, OpenDeckView, L.ButtonFont - 6), -240, -320, 440, 110);
            Place(UIKit.Button(fairContent, "В ПУТЬ ▶", RestartColor, () => game?.LeaveFair(), L.ButtonFont), 240, -320, 440, 110);
        }

        void RefreshFair()
        {
            fairGold.text = $"У тебя ● {game.Gold}";
            fairInfo.text = $"Колода странствия: {game.RunDeckSize} карт";

            Clear(fairCards);
            var offers = game.FairOffers;
            for (int i = 0; i < offers.Count; i++)
            {
                int index = i;
                var (card, price) = offers[i];
                var rt = BuildCard(fairCards, card.Def, () => game?.BuyFairCard(index), card.Level);
                UIKit.Place(rt, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(L.CardGap + i * (L.CardW + L.CardGap) + L.CardW / 2, 30), new Vector2(L.CardW, L.CardH));
                bool can = game.Gold >= price;
                rt.GetComponent<Button>().interactable = can;
                var tag = UIKit.Outlined(UIKit.Text(fairCards, $"● {price}", L.StatsFont - 4, TextAnchor.MiddleCenter, can ? Gold : Bad, FontStyle.Bold), 2.5f);
                UIKit.Place(tag.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(L.CardGap + i * (L.CardW + L.CardGap) + L.CardW / 2, -L.CardH / 2 - 10), new Vector2(L.CardW, 50));
            }
            if (offers.Count == 0)
                Label(fairCards, "Всё раскупили!", L.StatsFont, Cream, 0, 100);
        }
    }
}
