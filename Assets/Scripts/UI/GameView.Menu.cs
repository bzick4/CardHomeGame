using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Главное меню и выбор домов. Фон — тот же пейзаж, что на картах гостей.
    public partial class GameView
    {
        static readonly Color MenuButton = RoomDatabase.Hex("c9933a");

        RectTransform menuRoot, menuContent, levelsRoot, levelsContent, levelsGrid;
        Text levelsInfo;
        int levelsTier = -1;

        // Экран вне партии: та же ночная сцена с домом, притемнённая — поверх много интерфейса.
        RectTransform MenuScreen(string name, out RectTransform content)
        {
            var o = Overlay(name, 0.0f, out content);
            var bg = UIKit.Rect("Scene", o);
            bg.SetSiblingIndex(0);
            UIKit.Stretch(bg);
            ArtBackdrop(bg, 0.55f);
            return o;
        }

        // Экраны вне партии строятся, только когда открыты: во время партии их нет,
        // и их фоны не держатся в памяти (меню и партия никогда не живут в одном интерфейсе одновременно).
        static bool MenuPhase(Phase p) =>
            p == Phase.Menu || p == Phase.Levels || p == Phase.Map || p == Phase.Fair || p == Phase.Rest || p == Phase.Event;

        void BuildMenus()
        {
            if (!MenuPhase(game.State)) return;
            EnsureMenus();
            ShowMenus(game.State, false);
        }

        void EnsureMenus()
        {
            if (menuRoot != null) return;
            BuildMainMenu();
            BuildLevelSelect();
            BuildBacks();
            BuildWander();
        }

        void ShowMenus(Phase phase, bool animate)
        {
            if (MenuPhase(phase)) EnsureMenus();
            if (menuRoot == null) return;
            bool menu = phase == Phase.Menu, levels = phase == Phase.Levels;
            bool wasMenu = menuRoot.gameObject.activeSelf, wasLevels = levelsRoot.gameObject.activeSelf;
            menuRoot.gameObject.SetActive(menu);
            if (!menu) backsRoot.gameObject.SetActive(false);
            levelsRoot.gameObject.SetActive(levels);
            if (levels && !wasLevels) levelsTier = game.Progress.Unlocked / HouseDatabase.Count;
            if (animate && menu && !wasMenu) Pop(menuContent);
            if (animate && levels && !wasLevels) Pop(levelsContent);
            ShowWander(phase, animate);
            resetArmed = false;
            if (!menu && settingsRoot != null) settingsRoot.gameObject.SetActive(false);
            if (!menu && confirmRoot != null) confirmRoot.gameObject.SetActive(false);
        }

        void RefreshMenus()
        {
            if (menuRoot == null) return;
            if (menuRoot.gameObject.activeSelf) RefreshMainMenu();
            if (levelsRoot.gameObject.activeSelf) RefreshLevels();
            if (backsRoot.gameObject.activeSelf) RefreshBacks();
            RefreshWander();
        }

        // ───────────── Главное меню ─────────────
        // Ночь, наш дом с горящими окнами, к нему по тропинке идёт путник.
        // Наверху вывеска с названием и лента-подпись, внизу деревянные кнопки с иконкой и пояснением.

        MenuBtn continueBtn, newGameBtn, wanderBtn, backsBtn;
        RectTransform confirmRoot, settingsRoot, settingsContent;
        MenuBtn soundBtn, tutorialBtn, resetBtn;
        bool resetArmed;

        void BuildMainMenu()
        {
            menuRoot = Overlay("Menu", 0, out menuContent);
            var bg = UIKit.Rect("Scene", menuRoot);
            bg.SetSiblingIndex(0);
            UIKit.Stretch(bg);
            ArtBackdrop(bg, 0);

            if (L.Portrait) BuildMenuPortrait();
            else BuildMenuLandscape();

            BuildConfirm();
            BuildSettings();
        }

        // Вертикальный фон: название слева вверху на небе, кнопки колонкой по левой стороне внизу
        // (как в горизонтальном режиме), настройки в правом нижнем углу. Зеркальный фон — всё наоборот.
        void BuildMenuPortrait()
        {
            float side = MenuArt.MirrorBackground ? -1 : 1;
            WoodSign(menuContent, "ДОМ НА НОЧЬ", 820, 620, 150, L.StatsFont + 30, -side * 175);
            Ribbon(menuContent, "карточная стратегия о гостеприимстве", 700, 600, -side * 175);

            var size = new Vector2(480, 124);
            float bx = -side * 268;
            continueBtn = MakeMenuButton(menuContent, RestartColor, "Продолжить", new Vector2(bx, -330), size, () => game?.ContinueGame());
            newGameBtn = MakeMenuButton(menuContent, RotateColor, "Новая игра", new Vector2(bx, -470), size, OnNewGame);
            wanderBtn = MakeMenuButton(menuContent, NightColor, "Странствие", new Vector2(bx, -610), size, () => game?.StartWander());
            backsBtn = MakeMenuButton(menuContent, MenuWood, "Рубашки", new Vector2(bx, -750), size, OpenBacks);

            RoundButton(menuContent, MenuArt.Gear, new Vector2(side * 445, -860), OpenSettings, 110);
        }

        // Горизонтальный фон: луна слева вверху, Путник в центре, дом и вор справа.
        // Название — наверху на небе между луной и домом, кнопки — слева на ёлках и заборе, настройки — справа вверху.
        // Раскладка по всему экрану (а не по вертикальной колонке), чтобы попадать в свободные места картинки.
        void BuildMenuLandscape()
        {
            bool mirror = MenuArt.MirrorBackground;
            RectTransform At(float x, float y)
            {
                var r = UIKit.Rect("Spot", menuRoot);
                var a = new Vector2(mirror ? 1 - x : x, y);
                r.anchorMin = r.anchorMax = a;
                r.sizeDelta = Vector2.zero;
                return r;
            }

            WoodSign(At(0.47f, 0.9f), "ДОМ НА НОЧЬ", 0, 600, 130, L.StatsFont + 26);
            Ribbon(At(0.47f, 0.78f), "карточная стратегия о гостеприимстве", 0, 560);

            var column = At(0.165f, 0.4f);
            var size = new Vector2(440, 110);
            continueBtn = MakeMenuButton(column, RestartColor, "Продолжить", new Vector2(0, 190), size, () => game?.ContinueGame());
            newGameBtn = MakeMenuButton(column, RotateColor, "Новая игра", new Vector2(0, 65), size, OnNewGame);
            wanderBtn = MakeMenuButton(column, NightColor, "Странствие", new Vector2(0, -60), size, () => game?.StartWander());
            backsBtn = MakeMenuButton(column, MenuWood, "Рубашки", new Vector2(0, -185), size, OpenBacks);

            RoundButton(At(0.965f, 0.9f), MenuArt.Gear, Vector2.zero, OpenSettings, 100);
        }

        // Новая игра стирает прогресс — если он есть, спрашиваем.
        void OnNewGame()
        {
            if (game.HasProgress)
            {
                confirmRoot.gameObject.SetActive(true);
                confirmRoot.SetAsLastSibling();
                Pop(confirmRoot.GetChild(1) as RectTransform);
                return;
            }
            game?.NewGame();
        }

        void BuildConfirm()
        {
            confirmRoot = Overlay("Confirm", 0.6f, out var content);
            var box = WoodWindow(content, new Vector2(880, 560), "НОВАЯ ИГРА?");
            var text = UIKit.Text(box, "Весь прогресс будет удалён:\nдома, звёзды, рубашки и забег.", L.BodyFont + 8, TextAnchor.MiddleCenter, InkBrown, FontStyle.Bold);
            UIKit.Place(text.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(780, 150));

            var yes = MakeMenuButton(box, RestartColor, "Начать", new Vector2(-200, -150), new Vector2(380, 120), () =>
            {
                confirmRoot.gameObject.SetActive(false);
                game?.NewGame();
            });
            StyleMenuButton(yes, false, true, null);
            var no = MakeMenuButton(box, SkipColor, "Отмена", new Vector2(200, -150), new Vector2(380, 120),
                () => confirmRoot.gameObject.SetActive(false));
            StyleMenuButton(no, false, true, null);
        }

        void RefreshMainMenu()
        {
            SetMenuIcon(continueBtn, UiArt.Icon("Campaign"));
            SetMenuIcon(newGameBtn, UiArt.Icon("Home"));
            SetMenuIcon(wanderBtn, UiArt.Icon("Wander"));
            SetMenuIcon(backsBtn, UiArt.Icon("Deck"));
            bool has = game.HasProgress;
            int saved = game.SavedLevel;
            string contHint = saved >= 0 ? $"{HouseDatabase.Level(saved).Title} · день {SaveSystem.LoadRun()?.Night}"
                : has ? $"открыто домов: {game.Progress.Unlocked + 1}" : "нет сохранённой игры";
            StyleMenuButton(continueBtn, has, has, contHint);
            StyleMenuButton(newGameBtn, !has, true, "с обучением");
            StyleMenuButton(wanderBtn, false, true, game.HasWanderSave ? "забег идёт" : "рогалик");
            StyleMenuButton(backsBtn, false, true, $"{game.Progress.Backs.Count} из {CardBacks.ThemeCount * CardBacks.Tiers.Length}");
            RefreshSettings();
        }

        // ───────────── Настройки ─────────────

        void BuildSettings()
        {
            settingsRoot = Overlay("Settings", 0.6f, out settingsContent);
            var box = WoodWindow(settingsContent, new Vector2(880, 900), "НАСТРОЙКИ");
            var size = new Vector2(760, 136);
            soundBtn = MakeMenuButton(box, NightColor, "Звук", new Vector2(0, 230), size, () => game?.ToggleSound());
            tutorialBtn = MakeMenuButton(box, RotateColor, "Обучение", new Vector2(0, 75), size, () => game?.StartTutorial());
            resetBtn = MakeMenuButton(box, RoomDatabase.Hex("c8483c"), "Сбросить прогресс", new Vector2(0, -80), size, OnReset);
            var close = MakeMenuButton(box, RestartColor, "Закрыть", new Vector2(0, -280), new Vector2(460, 110),
                () => settingsRoot.gameObject.SetActive(false));
            StyleMenuButton(close, false, true, null);
        }

        void OpenSettings()
        {
            resetArmed = false;
            settingsRoot.gameObject.SetActive(true);
            settingsRoot.SetAsLastSibling();
            Pop(settingsContent);
            RefreshSettings();
        }

        // Сброс прогресса — с подтверждением вторым нажатием.
        void OnReset()
        {
            if (!resetArmed)
            {
                resetArmed = true;
                RefreshSettings();
                return;
            }
            resetArmed = false;
            game?.ResetProgress();
        }

        void RefreshSettings()
        {
            if (soundBtn == null) return;
            StyleMenuButton(soundBtn, false, true, Sfx.Muted ? "выключен" : "включён");
            SetMenuIcon(soundBtn, UiArt.Icon(Sfx.Muted ? "SoundOff" : "SoundOn"));
            SetMenuIcon(tutorialBtn, UiArt.Icon("Info"));
            StyleMenuButton(tutorialBtn, false, true, "пройти ещё раз");
            StyleMenuButton(resetBtn, false, true, resetArmed ? "нажми ещё раз — всё удалится" : "дома, звёзды, рубашки, забег");
            resetBtn.Glow.enabled = resetArmed;
        }

        // ───────────── Выбор дома: карта на пергаменте ─────────────

        RectTransform levelsMap, levelsTabs;
        Text levelsTitle;
        Vector2[] campaignPoints; // точки домов на картинке кампании (доли)

        void BuildLevelSelect()
        {
            levelsRoot = MenuScreen("Levels", out levelsContent);

            // Деревянная вывеска с кнопкой «назад».
            var header = UIKit.Rect("Header", levelsContent);
            UIKit.Place(header, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40, 850), new Vector2(860, 110));
            UIKit.DropShadow(header, new Vector2(0, -10), 0.5f);
            var plank = UIKit.Image("Plank", header, DarkWood, Sprites.Panel);
            UIKit.Stretch(plank.rectTransform);
            levelsTitle = UIKit.Outlined(UIKit.Text(header, "", L.StatsFont + 6, TextAnchor.MiddleCenter, Cream, FontStyle.Bold), 2.5f);
            UIKit.Stretch(levelsTitle.rectTransform, 10);
            levelsTitle.rectTransform.offsetMin = new Vector2(70, 10);
            RoundButton(levelsContent, MenuArt.BackIcon, new Vector2(-440, 850), () => game?.ShowMenu(), 120);

            levelsTabs = UIKit.Rect("Tabs", levelsContent);
            UIKit.Place(levelsTabs, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 745), new Vector2(900, 80));

            // Карта королевства (Art/Map/Campaign или CampaignWide) — на весь экран, вписана целиком;
            // дома стоят в её точках (CampaignPoints).
            bool wide = !L.Portrait;
            var campaign = MenuArt.Campaign(wide);
            if (campaign == null) { wide = !wide; campaign = MenuArt.Campaign(wide); }
            if (campaign == null) { wide = false; campaign = ArtStore.Require("Map/Campaign"); }
            campaignPoints = CampaignPoints.For(!wide);
            var area = UIKit.Rect("Campaign", levelsRoot);
            area.SetSiblingIndex(levelsContent.GetSiblingIndex()); // над фоном меню, под интерфейсом
            UIKit.Stretch(area);
            var art = UIKit.Image("Art", area, Color.white, campaign);
            art.raycastTarget = false;
            UIKit.Stretch(art.rectTransform);
            var fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = campaign.rect.width / campaign.rect.height;
            levelsMap = art.rectTransform;
            levelsGrid = UIKit.Rect("Nodes", levelsMap);
            UIKit.Stretch(levelsGrid);

            levelsInfo = Label(levelsContent, "", L.BodyFont, Cream, -770, 90, 1000, FontStyle.Bold);
        }

        void SwitchTier(int tier)
        {
            int maxTier = game.Progress.Unlocked / HouseDatabase.Count;
            levelsTier = Mathf.Clamp(tier, 0, maxTier);
            RefreshLevels();
            levelsMap.DOKill(true);
            levelsMap.DOPunchScale(Vector3.one * 0.03f, 0.25f, 6).SetLink(levelsMap.gameObject);
        }

        void RefreshLevels()
        {
            var progress = game.Progress;
            int maxTier = progress.Unlocked / HouseDatabase.Count;
            if (levelsTier < 0 || levelsTier > maxTier) levelsTier = maxTier;
            levelsTitle.text = $"ВЫБОР ДОМА · КРУГ {HouseDatabase.Roman(levelsTier + 1)}";

            // Вкладки кругов: открытые, текущая выделена, следующий — тёмный, пока закрыт.
            Clear(levelsTabs);
            int first = Mathf.Max(0, Mathf.Min(levelsTier - 1, maxTier - 1));
            for (int i = 0; i < 3; i++)
            {
                int tier = first + i;
                bool open = tier <= maxTier;
                bool on = tier == levelsTier;
                var color = on ? RoomDatabase.Hex("d0473c") : open ? RoomDatabase.Hex("8a5a32") : RoomDatabase.Hex("4a4038");
                var tab = Place(UIKit.Button(levelsTabs, (open ? "" : "   ") + $"КРУГ {HouseDatabase.Roman(tier + 1)}",
                    color, () => SwitchTier(tier), Fs(L.ButtonFont - 8)), (i - 1) * 300, 0, 280, 76);
                tab.interactable = open && !on;
                if (!open)
                {
                    var lockIcon = UIKit.Image("Lock", tab.transform, Color.white, GuestArt.Lock);
                    lockIcon.raycastTarget = false;
                    UIKit.Place(lockIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34, 2), new Vector2(46, 46));
                }
            }

            Clear(levelsGrid);
            // Дорога нарисована на карте — ставим только дома в её точках.
            for (int i = 0; i < HouseDatabase.Count; i++)
                CampaignNode(levelsGrid, HouseDatabase.Level(levelsTier * HouseDatabase.Count + i), campaignPoints[i]);

            string heir = progress.Heirlooms.Count > 0
                ? string.Join(", ", progress.Heirlooms.Select(t => RoomDatabase.Get((RoomType)t).Name))
                : "нет — получишь, пройдя дом";
            levelsInfo.text = $"Наследство: <color=#ffd34d>{heir}</color>\nКаждый дом — 7 ночей. Пройди дом, чтобы открыть следующий.";
        }

        // Картинка дома в круглом окне внутри цветного кольца: заполняет круг, лишнее обрезается.
        void HouseDisc(Transform parent, HouseDef house, bool unlocked, Vector2 pos, float size)
        {
            var disc = UIKit.Image("Base", parent, unlocked ? house.Color : new Color(0.5f, 0.45f, 0.4f), Sprites.Circle);
            disc.raycastTarget = false;
            UIKit.Place(disc.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var window = UIKit.Image("Window", disc.transform, Color.white, Sprites.Circle);
            window.raycastTarget = false;
            UIKit.Stretch(window.rectTransform, size * 0.075f);
            window.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var icon = UIKit.Image("Icon", window.transform, unlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f), Icons.House(house));
            icon.raycastTarget = false;
            UIKit.Cover(icon);
        }

        // Дом на карте кампании: платформа в точке дороги, на ней круглая картинка дома, ниже табличка с именем.
        // Закрытый — платформа в цепях с замком; текущий — платформа с флагом, сияние и путник рядом.
        void CampaignNode(Transform parent, LevelInfo level, Vector2 point)
        {
            var progress = game.Progress;
            bool unlocked = game.IsUnlocked(level.Index);
            bool current = level.Index == progress.Unlocked;
            int stars = progress.StarsAt(level.Index);

            // Узел стоит центром платформы точно в точке карты; в горизонтальной карте точки теснее — узлы мельче.
            var node = UIKit.Rect("House", parent);
            node.anchorMin = node.anchorMax = point;
            node.anchoredPosition = Vector2.zero;
            node.sizeDelta = new Vector2(190, 210);
            node.localScale = Vector3.one * (L.Portrait ? 1f : 0.72f);

            if (current)
            {
                var glow = UIKit.Image("Glow", node, new Color(1, 0.85f, 0.3f, 0.85f), Sprites.Light);
                glow.raycastTarget = false;
                UIKit.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(300, 300));
                glow.DOFade(0.3f, 0.7f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(glow.gameObject);
            }

            if (current)
            {
                // Золотой луч из платформы — «сюда».
                var beamSprite = ArtStore.Get("Fx/BeamGold", null);
                if (beamSprite != null)
                {
                    var beam = UIKit.Image("Beam", node, Color.white, beamSprite);
                    beam.raycastTarget = false;
                    beam.preserveAspect = true;
                    UIKit.Place(beam.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(240, 200));
                    beam.DOFade(0.45f, 0.9f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(beam.gameObject);
                }
            }

            var platform = MenuArt.Platform(!unlocked ? "Locked" : current ? "Current" : "Open");
            var pl = UIKit.Image("Platform", node, platform != null ? Color.white : new Color(0.45f, 0.4f, 0.35f), platform != null ? platform : Sprites.Circle);
            pl.raycastTarget = false;
            pl.preserveAspect = true;
            UIKit.Place(pl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 170));

            if (unlocked)
            {
                // Дом «стоит» на платформе: тень под ним, сам круг чуть выше центра.
                var shadow = UIKit.Image("Shadow", node, new Color(0, 0, 0, 0.35f), Sprites.ShadowCircle);
                shadow.raycastTarget = false;
                UIKit.Place(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(110, 40));
                HouseDisc(node, level.House, true, new Vector2(0, 62), 112);
            }

            var plaque = UIKit.Image("Plaque", node, DarkWood, Sprites.Panel);
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, -58), new Vector2(250, 56));
            var name = UIKit.Outlined(UIKit.Text(plaque.transform, $"{level.Index + 1} · {level.Title}", Fs(L.BodyFont - 2), TextAnchor.MiddleCenter, Cream, FontStyle.Bold), 2);
            UIKit.Stretch(name.rectTransform, 6);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 22; // длинные названия («Постоялый двор») ужимаются, чтобы влезть в табличку
            name.resizeTextMaxSize = Fs(L.BodyFont - 2);

            if (stars > 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    var star = UIKit.Image("Star", node, Color.white, i < stars ? GuestArt.Star : GuestArt.StarEmpty);
                    star.raycastTarget = false;
                    UIKit.Place(star.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 36, i == 1 ? 134 : 126), new Vector2(36, 36));
                }
            }

            if (!unlocked)
            {
                // Подсказку показываем только у ближайшего закрытого дома.
                if (level.Index == progress.Unlocked + 1)
                {
                    var prev = HouseDatabase.Level(level.Index - 1);
                    var pill = UIKit.Image("Hint", node, new Color(0.15f, 0.12f, 0.12f, 0.85f), Sprites.Soft);
                    pill.raycastTarget = false;
                    UIKit.Place(pill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(290, 44));
                    var t = UIKit.Text(pill.transform, $"Пройди «{prev.Title}»", Fs(L.BodyFont - 4), TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
                    UIKit.Stretch(t.rectTransform, 6);
                    t.resizeTextForBestFit = true;
                    t.resizeTextMinSize = 12;
                    t.resizeTextMaxSize = Fs(L.BodyFont - 4);
                }
                return;
            }

            if (current)
            {
                // Путник стоит у дома, который пора проходить.
                var hero = UIKit.Image("Hero", node, Color.white, GuestArt.Character(GuestDatabase.Traveler));
                hero.raycastTarget = false;
                hero.preserveAspect = true;
                UIKit.Place(hero.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0), new Vector2(92, -10), new Vector2(100, 110));
                hero.rectTransform.DOAnchorPosY(12, 0.5f).SetRelative().SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
                    .SetLink(hero.gameObject);
            }

            if (game.SavedLevel == level.Index)
            {
                // Здесь есть начатый день — его можно продолжить.
                var pill = UIKit.Image("Saved", node, RoomDatabase.Hex("2fb5c4"), Sprites.Panel);
                pill.raycastTarget = false;
                UIKit.Place(pill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, -118), new Vector2(250, 50));
                var t = UIKit.Outlined(UIKit.Text(pill.transform, $"Продолжить · день {SaveSystem.LoadRun()?.Night}", Fs(L.BodyFont - 4), TextAnchor.MiddleCenter, Color.white, FontStyle.Bold), 2);
                UIKit.Stretch(t.rectTransform, 4);
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 20;
                t.resizeTextMaxSize = Fs(L.BodyFont - 4);
            }

            var hit = node.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var b = node.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = hit;
            int index = level.Index;
            b.onClick.AddListener(() =>
            {
                Sfx.Click();
                if (game.SavedLevel == index) game?.ContinueRun();
                else game?.StartLevel(index);
            });
            node.gameObject.AddComponent<PressScale>().Pressed = 0.93f;
        }

        // ───────────── Коллекция рубашек ─────────────

        RectTransform backsRoot, backsContent, backsGrid, backsPreview;
        Text backsTierText, backsInfo;
        Button backsEquip, backsTest;
        int backsTier, previewCode;

        void BuildBacks()
        {
            backsRoot = MenuScreen("Backs", out backsContent);

            var header = UIKit.Rect("Header", backsContent);
            UIKit.Place(header, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40, 850), new Vector2(860, 110));
            UIKit.DropShadow(header, new Vector2(0, -10), 0.5f);
            var plank = UIKit.Image("Plank", header, DarkWood, Sprites.Panel);
            UIKit.Stretch(plank.rectTransform);
            var title = UIKit.Outlined(UIKit.Text(header, "РУБАШКИ", L.StatsFont + 10, TextAnchor.MiddleCenter, Cream, FontStyle.Bold), 2.5f);
            UIKit.Stretch(title.rectTransform, 10);
            RoundButton(backsContent, MenuArt.BackIcon, new Vector2(-440, 850), () => backsRoot.gameObject.SetActive(false), 120);

            // Переключатель окантовки (круга).
            Place(UIKit.Button(backsContent, "‹", MenuButton, () => SwitchBackTier(-1), L.ButtonFont + 10), -400, 745, 110, 90);
            Place(UIKit.Button(backsContent, "›", MenuButton, () => SwitchBackTier(1), L.ButtonFont + 10), 400, 745, 110, 90);
            backsTierText = Label(backsContent, "", L.StatsFont + 2, Color.white, 745, 90, 640);

            backsGrid = UIKit.Rect("Grid", backsContent);
            UIKit.Place(backsGrid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(1000, 700));

            // Крупный просмотр выбранной рубашки и действия.
            backsPreview = UIKit.Rect("Preview", backsContent);
            UIKit.Place(backsPreview, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-250, -360), new Vector2(300, 420));
            backsInfo = UIKit.Outlined(UIKit.Text(backsContent, "", L.BodyFont + 4, TextAnchor.UpperLeft, Color.white, FontStyle.Bold), 2);
            UIKit.Place(backsInfo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(-60, -150), new Vector2(540, 240));
            backsEquip = Place(UIKit.Button(backsContent, "ВЫБРАТЬ", RestartColor, () => game?.EquipBack(previewCode), L.ButtonFont), 210, -440, 440, 110);
            Place(UIKit.Button(backsContent, "СТРАННИК", SkipColor, () => PreviewBack(CardBacks.DefaultCode), Fs(L.ButtonFont - 6)), 210, -570, 440, 96);

            backsTest = Place(UIKit.Button(backsContent, "", RoomDatabase.Hex("5a4632"), () => game?.ToggleTestBacks(), Fs(L.ButtonFont - 8)), 0, -800, 620, 96);
        }

        void OpenBacks()
        {
            backsTier = 0;
            previewCode = game.Progress.EquippedBack;
            if (previewCode > 0) backsTier = CardBacks.TierOf(previewCode);
            backsRoot.gameObject.SetActive(true);
            backsRoot.SetAsLastSibling();
            Pop(backsContent);
            RefreshBacks();
        }

        void SwitchBackTier(int delta)
        {
            backsTier = (backsTier + delta + CardBacks.Tiers.Length) % CardBacks.Tiers.Length;
            RefreshBacks();
            backsGrid.DOKill(true);
            backsGrid.DOPunchScale(Vector3.one * 0.04f, 0.25f, 6).SetLink(backsGrid.gameObject);
        }

        void PreviewBack(int code)
        {
            previewCode = code;
            RefreshBacks();
            backsPreview.DOKill(true);
            backsPreview.DOPunchScale(Vector3.one * 0.06f, 0.25f, 6).SetLink(backsPreview.gameObject);
            Sfx.Card();
        }

        void RefreshBacks()
        {
            var tier = CardBacks.Tiers[backsTier];
            backsTierText.text = $"КРУГ {HouseDatabase.Roman(backsTier + 1)} · {tier.Name.ToUpper()}" +
                                 (tier.Shimmer ? " <size=" + (Fs(L.BodyFont - 2)) + ">✦ анимация</size>" : "");

            // Десять домов этого круга: 5 × 2.
            Clear(backsGrid);
            var size = new Vector2(150, 210);
            int owned = 0;
            for (int theme = 0; theme < CardBacks.ThemeCount; theme++)
            {
                int code = CardBacks.Code(theme, backsTier);
                bool has = game.OwnsBack(code);
                if (game.Progress.Backs.Contains(code)) owned++;
                int col = theme % 5, row = theme / 5;
                var pos = new Vector2((col - 2) * 196, 175 - row * 340);

                var slot = UIKit.Rect("Slot", backsGrid);
                UIKit.Place(slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
                if (code == previewCode)
                {
                    var glow = UIKit.Image("Glow", slot, new Color(1, 0.85f, 0.2f, 0.95f), Sprites.Shadow);
                    glow.raycastTarget = false;
                    UIKit.Stretch(glow.rectTransform, -Sprites.ShadowPad - 16);
                }
                var back = CardBacks.Create(slot, code, size);
                UIKit.Stretch(back);
                if (!has)
                {
                    var shade = UIKit.Image("Locked", slot, new Color(0, 0, 0, 0.55f), Sprites.Soft);
                    shade.raycastTarget = false;
                    UIKit.Stretch(shade.rectTransform, 6);
                    var lockIcon = UIKit.Image("Lock", slot, Color.white, GuestArt.Lock);
                    lockIcon.raycastTarget = false;
                    UIKit.Place(lockIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70));
                }
                if (code == game.Progress.EquippedBack) CheckBadge(slot);

                var name = UIKit.Outlined(UIKit.Text(slot, HouseDatabase.All[theme].Name, Fs(L.BodyFont - 4), TextAnchor.UpperCenter, has ? Color.white : Cream, FontStyle.Bold), 2);
                UIKit.Place(name.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(190, 60));

                var hit = slot.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                var b = slot.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.targetGraphic = hit;
                int c = code;
                b.onClick.AddListener(() => PreviewBack(c));
                slot.gameObject.AddComponent<PressScale>().Pressed = 0.94f;
            }

            // Крупно — выбранная рубашка.
            Clear(backsPreview);
            var big = CardBacks.Create(backsPreview, previewCode, new Vector2(300, 420));
            UIKit.Stretch(big);
            bool canUse = game.OwnsBack(previewCode);
            bool equipped = previewCode == game.Progress.EquippedBack;
            if (!canUse)
            {
                var shade = UIKit.Image("Locked", backsPreview, new Color(0, 0, 0, 0.45f), Sprites.Soft);
                shade.raycastTarget = false;
                UIKit.Stretch(shade.rectTransform, 10);
            }

            string status = equipped ? "<color=#8be36f>Сейчас на твоей колоде</color>"
                : canUse ? (game.Progress.Backs.Contains(previewCode) || previewCode == CardBacks.DefaultCode
                    ? "<color=#8be36f>Получена</color>" : "<color=#ffb03a>Открыта в тест-режиме</color>")
                : $"<color=#ff9a5e>🔒 {CardBacks.Condition(previewCode)}</color>";
            backsInfo.text = $"<color=#ffd34d>{CardBacks.Name(previewCode)}</color>\n{status}\n\n" +
                             $"<size={Fs(L.BodyFont - 2)}>Собрано на этом круге: {owned}/{CardBacks.ThemeCount}\n" +
                             $"Соперник в этом доме играет такой рубашкой.</size>";
            backsInfo.text = backsInfo.text.Replace("🔒 ", "");
            SetLabel(backsEquip, equipped ? "ВЫБРАНА" : "ВЫБРАТЬ");
            backsEquip.interactable = canUse && !equipped;

            SetLabel(backsTest, game.TestAllBacks ? "ТЕСТ: ВСЕ ОТКРЫТЫ — ВКЛ" : "ТЕСТ: ВСЕ ОТКРЫТЫ — ВЫКЛ");
            UIKit.Paint(backsTest, game.TestAllBacks ? RoomDatabase.Hex("f59a2c") : SkipColor);
        }

        // Зелёная галочка в углу (пройдено, выбрано) — значок Icons/Check.
        void CheckBadge(Transform parent)
        {
            var check = UiArt.Icon("Check");
            if (check != null)
            {
                var img = UIKit.Image("Check", parent, Color.white, check);
                img.raycastTarget = false;
                img.preserveAspect = true;
                UIKit.Place(img.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-8, -8), new Vector2(64, 64));
                return;
            }
            var badge = UIKit.Image("Badge", parent, Good, Sprites.Circle);
            badge.raycastTarget = false;
            UIKit.Place(badge.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-8, -8), new Vector2(54, 54));
            var t = UIKit.Outlined(UIKit.Text(badge.transform, "+", 34, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold));
            UIKit.Stretch(t.rectTransform);
        }
    }
}
