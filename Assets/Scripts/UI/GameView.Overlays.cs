using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Экраны поверх доски: выбор дома, итоги ночи, награда с лавкой, наследство, конец игры, подсказки.
    // Все координаты внутри — в «вертикальных» единицах 1080×1920; на других экранах контент масштабируется.
    public partial class GameView
    {
        static readonly Color ShopColor = RoomDatabase.Hex("c9933a");
        static readonly Color SkipColor = RoomDatabase.Hex("8a7f72");

        TutorialView tutorial;
        int tutorialStep;

        RectTransform deckRoot, deckContent, deckGrid;
        Text deckInfo;

        RectTransform inspectRoot, inspectContent;

        RectTransform summaryRoot, summaryContent, summaryPanel;

        RectTransform rewardRoot, rewardContent, rewardCards;
        Text rewardNext, shopGold;
        RectTransform rewardShop;
        Button rerollButton, actionButton, removeButton, repairButton, upgradeButton;

        RectTransform removeRoot, removeContent, removeGrid;
        Text removeInfo, removeTitle;
        bool upgradeMode; // окно колоды в лавке: убрать карту или улучшить
        bool freeUpgrade; // мастерская на привале: улучшение бесплатно
        bool removeOpen;

        RectTransform inheritRoot, inheritContent, inheritCards, inheritStars, inheritBack;
        Text inheritInfo, inheritTitle;

        RectTransform gameOverRoot, gameOverContent;
        Text gameOverTitle, gameOverBody;
        Image gameOverIcon;

        RectTransform hintRoot, hintContent;
        Text hintTitle, hintBody;
        readonly Queue<(string title, string text)> hints = new Queue<(string, string)>();
        (string title, string text)? currentHint;

        // Контент оверлеев рисуется в размере 1080×1920 и ужимается, если экран ниже.
        float OverlayScale
        {
            get
            {
                if (!L.Portrait) return 1080f / 1920f;
                float h = Screen.height * 1080f / Mathf.Max(1, Screen.width);
                return Mathf.Min(1, h / 1920f);
            }
        }

        RectTransform Overlay(string name, float dim, out RectTransform content)
        {
            var o = UIKit.Rect(name, transform);
            UIKit.Stretch(o);
            var img = UIKit.Image("Dim", o, new Color(0.03f, 0.02f, 0.08f, dim));
            UIKit.Stretch(img.rectTransform);
            content = UIKit.Rect("Content", o);
            UIKit.Place(content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080, 1920));
            content.localScale = Vector3.one * OverlayScale;
            o.gameObject.SetActive(false);
            return o;
        }

        Text Label(Transform parent, string text, int size, Color color, float y, float h, float w = 1000, FontStyle style = FontStyle.Bold)
        {
            var t = UIKit.Outlined(UIKit.Text(parent, text, size, TextAnchor.MiddleCenter, color, style), size > 40 ? 3.5f : 2);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(w, h));
            return t;
        }

        static Button Place(Button b, float x, float y, float w, float h)
        {
            UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));
            return b;
        }

        void Pop(RectTransform content)
        {
            float s = OverlayScale;
            content.DOKill();
            content.localScale = Vector3.one * s;
            content.DOScale(s, 0.4f).From(s * 0.6f).SetEase(Ease.OutBack).SetLink(content.gameObject);
        }

        static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
        }

        void BuildOverlays()
        {
            BuildSummaryOverlay();
            BuildRewardOverlay();
            BuildRemoveOverlay();
            BuildInheritOverlay();
            BuildGameOverOverlay();
            BuildDeckOverlay();
            BuildInspectOverlay();
            BuildHintOverlay();
            ShowOverlays(game.State, false);
            if (currentHint.HasValue) ShowHint(currentHint.Value, false);
        }

        void ShowOverlays(Phase phase, bool animate)
        {
            void Toggle(RectTransform r, RectTransform content, bool on)
            {
                bool was = r.gameObject.activeSelf;
                r.gameObject.SetActive(on);
                if (on && !was && animate) Pop(content);
            }

            Toggle(rewardRoot, rewardContent, phase == Phase.Reward);
            Toggle(inheritRoot, inheritContent, phase == Phase.LevelDone);
            Toggle(gameOverRoot, gameOverContent, phase == Phase.GameOver);
            if (phase != Phase.Reward && phase != Phase.Fair && phase != Phase.Rest) CloseRemove();
            if (phase != Phase.Day && phase != Phase.Night && phase != Phase.Map && phase != Phase.Fair
                && phase != Phase.Rest && phase != Phase.Event) deckRoot.gameObject.SetActive(false);
            CloseInspectOnPhase(phase);
            if (phase == Phase.LevelDone) AnimateStars();

            bool summary = phase == Phase.Summary;
            bool wasSummary = summaryRoot.gameObject.activeSelf;
            summaryRoot.gameObject.SetActive(summary);
            if (summary && (!wasSummary || !animate)) BuildSummary(animate);
        }

        void RefreshOverlays()
        {
            switch (game.State)
            {
                case Phase.Reward: RefreshReward(); break;
                case Phase.LevelDone: RefreshInherit(); break;
                case Phase.GameOver: RefreshGameOver(); break;
            }
            if (removeOpen) RefreshRemove();
            if (deckRoot.gameObject.activeSelf) RefreshDeckView();
        }

        // ───────────── Итоги ночи ─────────────

        void BuildSummaryOverlay()
        {
            summaryRoot = Overlay("Summary", 0.55f, out summaryContent);
        }

        void BuildSummary(bool animate)
        {
            Clear(summaryContent);
            var report = game.Report;
            if (report == null) return;

            int rows = report.Results.Count;
            int notes = report.Notes.Count + (report.TreasuryGold > 0 ? 1 : 0);
            float rowH = 112, noteH = 58;
            float height = 170 + rows * rowH + notes * noteH + 130 + 170;

            summaryPanel = UIKit.Rect("Panel", summaryContent);
            UIKit.Place(summaryPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(980, height));
            UIKit.DropShadow(summaryPanel, new Vector2(0, -16), 0.55f);
            var body = UIKit.Image("Body", summaryPanel, DarkWood, Sprites.Panel);
            UIKit.Stretch(body.rectTransform);

            string title = game.IsTutorial ? "ИТОГИ НОЧИ 0" : $"ИТОГИ НОЧИ {game.Night}";
            var t = UIKit.Outlined(UIKit.Text(summaryPanel, title, C.StatsFont + 16, TextAnchor.MiddleCenter, Gold, FontStyle.Bold), 3);
            UIKit.TopBand(t.rectTransform, 30, 100);

            var items = new List<RectTransform>();
            float top = 160;
            foreach (var res in report.Results)
            {
                var row = UIKit.Rect("Row", summaryPanel);
                UIKit.TopBand(row, top, rowH - 12, 30);
                var bg = UIKit.Image("Bg", row, new Color(0, 0, 0, 0.25f), Sprites.Soft);
                bg.raycastTarget = false;
                UIKit.Stretch(bg.rectTransform);
                var disc = UIKit.Image("Disc", row, res.Guest.Color, Sprites.Circle);
                disc.raycastTarget = false;
                UIKit.Place(disc.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(84, 84));
                var icon = UIKit.Image("Icon", disc.transform, Color.white, Icons.Guest(res.Guest));
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                UIKit.Stretch(icon.rectTransform, 6);
                var text = UIKit.Text(row, res.Summary, C.BodyFont + 2, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
                text.rectTransform.anchorMin = Vector2.zero;
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = new Vector2(112, 4);
                text.rectTransform.offsetMax = new Vector2(-14, -4);
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = Fc(C.BodyFont - 8);
                text.resizeTextMaxSize = C.BodyFont + 2;
                items.Add(row);
                top += rowH;
            }

            var noteLines = new List<string>(report.Notes);
            if (report.TreasuryGold > 0) noteLines.Add($"<color=#ffd34d>Сокровищница приносит +{report.TreasuryGold} золота</color>");
            foreach (var line in noteLines)
            {
                var n = UIKit.Text(summaryPanel, line, C.BodyFont, TextAnchor.MiddleCenter, Cream, FontStyle.Bold);
                UIKit.TopBand(n.rectTransform, top, noteH, 30);
                n.resizeTextForBestFit = true;
                n.resizeTextMinSize = Fc(C.BodyFont - 8);
                n.resizeTextMaxSize = C.BodyFont;
                items.Add(n.rectTransform);
                top += noteH;
            }

            int goldDelta = game.Gold - report.GoldBefore;
            int repDelta = game.Reputation - report.ReputationBefore;
            string g = goldDelta >= 0 ? $"<color=#ffd34d>● +{goldDelta}</color>" : $"<color=#ff6b5e>● {goldDelta}</color>";
            string r = repDelta == 0 ? "<color=#ff6b6b>♥ без потерь</color>" : $"<color=#ff6b5e>♥ {repDelta}</color>";
            var totals = UIKit.Outlined(UIKit.Text(summaryPanel, $"{g}        {r}", C.StatsFont + 12, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold), 3);
            UIKit.TopBand(totals.rectTransform, top + 10, 110, 30);
            items.Add(totals.rectTransform);

            var next = UIKit.Button(summaryPanel, game.IsTutorial ? "НАЧАТЬ ИГРУ" : "ДАЛЬШЕ", RestartColor,
                () => game?.ContinueAfterSummary(), C.ButtonFont);
            UIKit.Place((RectTransform)next.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(520, 110));

            if (!animate)
            {
                goldHeld = false;
                UpdateStats();
                return;
            }

            // Строки появляются по очереди, потом монеты летят в счётчик.
            for (int i = 0; i < items.Count; i++)
            {
                var group = items[i].gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0;
                group.DOFade(1, 0.3f).SetDelay(0.25f + i * 0.22f).SetLink(group.gameObject);
                items[i].DOAnchorPosX(-60, 0.35f).From(isRelative: true).SetDelay(0.25f + i * 0.22f).SetEase(Ease.OutCubic)
                    .SetLink(items[i].gameObject);
            }
            FlyCoins(totals.rectTransform, report.GoldBefore, game.Gold, 0.4f + items.Count * 0.22f);
        }

        // Монеты летят из итогов в счётчик золота, и счётчик растёт с каждой.
        void FlyCoins(RectTransform from, int before, int after, float delay)
        {
            int delta = after - before;
            if (delta <= 0)
            {
                DOVirtual.DelayedCall(delay, () =>
                {
                    goldHeld = false;
                    UpdateStats();
                }).SetLink(gameObject);
                return;
            }

            int count = Mathf.Clamp(delta, 1, 12);
            var start = from.position;
            var end = statsBar.position;
            float size = 64 * OverlayScale;
            for (int i = 0; i < count; i++)
            {
                int k = i;
                var coin = UIKit.Image("Coin", summaryRoot, Color.white, Icons.Coin);
                coin.raycastTarget = false;
                coin.rectTransform.sizeDelta = new Vector2(size, size);
                coin.rectTransform.position = start + (Vector3)(Random.insideUnitCircle * 40 * OverlayScale);
                coin.rectTransform.localScale = Vector3.zero;

                DOTween.Sequence()
                    .AppendInterval(delay + i * 0.08f)
                    .Append(coin.rectTransform.DOScale(1, 0.15f).SetEase(Ease.OutBack))
                    .Append(coin.rectTransform.DOMove(end, 0.55f).SetEase(Ease.InCubic))
                    .Join(coin.rectTransform.DOScale(0.6f, 0.55f))
                    .SetLink(coin.gameObject)
                    .OnComplete(() =>
                    {
                        Destroy(coin.gameObject);
                        shownGold = before + Mathf.RoundToInt(delta * (k + 1f) / count);
                        if (k == count - 1) goldHeld = false;
                        UpdateStats();
                        Sfx.Coin();
                        statsBar.DOKill(true);
                        statsBar.DOPunchScale(Vector3.one * 0.08f, 0.2f, 6).SetLink(statsBar.gameObject);
                    });
            }
        }

        // ───────────── Награда и лавка ─────────────

        void BuildRewardOverlay()
        {
            rewardRoot = Overlay("Reward", 0.75f, out rewardContent);
            Label(rewardContent, "НАГРАДА!", C.StatsFont + 30, Gold, 790, 100);
            Label(rewardContent, "Выбери карту — она придёт в руку уже завтра", C.BodyFont + 2, Color.white, 715, 60, 1000, FontStyle.Normal);

            rewardCards = UIKit.Rect("Cards", rewardContent);
            float scale = 1.2f;
            rewardCards.localScale = new Vector3(scale, scale, 1);
            UIKit.Place(rewardCards, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 430),
                new Vector2(L.CardW * 3 + L.CardGap * 4, L.CardH));

            rewardNext = Label(rewardContent, "", C.BodyFont, Color.white, 180, 70, 1000, FontStyle.Normal);
            Place(UIKit.Button(rewardContent, "ПРОПУСТИТЬ", SkipColor, () => game?.PickReward(-1), Fc(C.ButtonFont - 6)), 0, 80, 360, 90);

            // Лавка: деревянная плашка с четырьмя покупками.
            var shop = rewardShop = UIKit.Rect("Shop", rewardContent);
            UIKit.Place(shop, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -330), new Vector2(1000, 540));
            UIKit.DropShadow(shop, new Vector2(0, -14), 0.5f);
            var body = UIKit.Image("Body", shop, DarkWood, Sprites.Panel);
            UIKit.Stretch(body.rectTransform);
            shopGold = UIKit.Outlined(UIKit.Text(shop, "", C.StatsFont, TextAnchor.MiddleCenter, Gold, FontStyle.Bold), 2.5f);
            UIKit.TopBand(shopGold.rectTransform, 20, 70);

            int f = Fc(C.ButtonFont - 10);
            rerollButton = ShopButton(shop, "Reroll", () => game?.RerollReward(), f, -320, 60);
            actionButton = ShopButton(shop, "ExtraMove", () => game?.BuyExtraAction(), f, 0, 60);
            repairButton = ShopButton(shop, "Repair", () => game?.RepairAll(), f, 320, 60);
            upgradeButton = ShopButton(shop, "Upgrade", () => OpenDeckShop(true), f, -160, -130);
            removeButton = ShopButton(shop, "Remove", () => OpenDeckShop(false), f, 160, -130);
        }

        // Кнопка лавки: значок слева, название справа, ценник в правом верхнем углу.
        readonly Dictionary<Button, PriceTag> shopTags = new Dictionary<Button, PriceTag>();

        Button ShopButton(RectTransform parent, string icon, System.Action onClick, int font, float x, float y)
        {
            var b = Place(UIKit.Button(parent, "", ShopColor, onClick, font), x, y, 300, 150);
            var rt = (RectTransform)b.transform;
            var img = UIKit.Image("Icon", b.targetGraphic.transform, Color.white, UiArt.Icon(icon));
            img.raycastTarget = false;
            img.preserveAspect = true;
            UIKit.Place(img.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 6), new Vector2(84, 84));
            var label = b.GetComponentInChildren<Text>();
            label.rectTransform.offsetMin = new Vector2(104, label.rectTransform.offsetMin.y);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = C.MinFont;
            label.resizeTextMaxSize = font;
            var tag = new PriceTag(rt, Fc(C.BodyFont - 4));
            tag.Root.anchorMin = tag.Root.anchorMax = tag.Root.pivot = new Vector2(1, 1);
            tag.Root.anchoredPosition = new Vector2(16, 22);
            shopTags[b] = tag;
            return b;
        }

        void SetPrice(Button b, int price, bool show, string prefix = "")
        {
            var tag = shopTags[b];
            tag.Root.gameObject.SetActive(show);
            if (show) tag.Set(prefix + price, game.Gold >= price);
        }

        static void SetLabel(Button b, string text) => b.GetComponentInChildren<Text>().text = text;

        void RefreshReward()
        {
            Clear(rewardCards);
            for (int i = 0; i < game.RewardOptions.Count; i++)
            {
                int index = i;
                var def = RoomDatabase.Get(game.RewardOptions[i]);
                var card = BuildCard(rewardCards, def, () => game?.PickReward(index), game.RewardLevel);
                UIKit.Place(card, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(L.CardGap + i * (L.CardW + L.CardGap) + L.CardW / 2, 0), new Vector2(L.CardW, L.CardH));
            }

            // В странствии лавки между ночами нет — она на ярмарках.
            rewardShop.gameObject.SetActive(!game.IsWander);
            rewardNext.text = game.IsWander && game.HouseFinished
                ? "<color=#ffd34d>Дом пройден!</color> Карта уйдёт в колоду странствия"
                : $"Следующей ночью соперник пришлёт гостей: <color=#ffd34d>{game.Level.GuestsOnNight(game.Night)}</color>";

            int gold = game.Gold;
            shopGold.text = $"ЛАВКА · у тебя {gold} зол.";
            SetLabel(rerollButton, "Новые награды");
            SetPrice(rerollButton, GameController.RerollPrice, true);
            rerollButton.interactable = gold >= GameController.RerollPrice;
            SetLabel(actionButton, game.ExtraActionBought ? "+1 ход завтра\nкуплено" : "+1 ход завтра");
            SetPrice(actionButton, GameController.ExtraActionPrice, !game.ExtraActionBought);
            actionButton.interactable = !game.ExtraActionBought && gold >= GameController.ExtraActionPrice;
            SetLabel(upgradeButton, "Улучшить карту");
            SetPrice(upgradeButton, GameController.UpgradePrice(2), true, "от ");
            upgradeButton.interactable = gold >= GameController.UpgradePrice(2);
            SetLabel(removeButton, "Убрать карту");
            SetPrice(removeButton, GameController.RemovePrice, true);
            removeButton.interactable = game.CanRemoveCards && gold >= GameController.RemovePrice;
            int broken = game.Grid.BrokenCount();
            SetLabel(repairButton, broken > 0 ? $"Починить всё ({broken})" : "Починить всё\nвсё цело");
            SetPrice(repairButton, GameController.RepairPrice, broken > 0);
            repairButton.interactable = broken > 0 && gold >= GameController.RepairPrice;
        }

        // ───────────── Колода в лавке: убрать или улучшить карту ─────────────

        void BuildRemoveOverlay()
        {
            removeRoot = Overlay("DeckShop", 0.8f, out removeContent);
            removeTitle = Label(removeContent, "", C.StatsFont + 10, Gold, 700, 90);
            removeInfo = Label(removeContent, "", C.BodyFont, Color.white, 610, 90, 1000, FontStyle.Normal);
            removeGrid = UIKit.Rect("Cards", removeContent);
            UIKit.Place(removeGrid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1020, 1000));
            Place(UIKit.Button(removeContent, "ГОТОВО", RestartColor, CloseRemove, Fc(C.ButtonFont - 4)), 0, -620, 380, 100);
        }

        void OpenDeckShop(bool upgrade) => OpenDeckShop(upgrade, false);

        void OpenDeckShop(bool upgrade, bool free)
        {
            upgradeMode = upgrade;
            freeUpgrade = free;
            removeOpen = true;
            removeRoot.gameObject.SetActive(true);
            removeRoot.SetAsLastSibling();
            Pop(removeContent);
            RefreshRemove();
        }

        void CloseRemove()
        {
            removeOpen = false;
            if (removeRoot != null) removeRoot.gameObject.SetActive(false);
        }

        void RefreshRemove()
        {
            Clear(removeGrid);
            removeTitle.text = freeUpgrade ? "МАСТЕРСКАЯ" : upgradeMode ? "УЛУЧШИТЬ КАРТУ" : "УБРАТЬ КАРТУ ИЗ КОЛОДЫ";
            removeInfo.text = freeUpgrade ? "Выбери одну карту — улучшу бесплатно.\nПосле этого привал закончится."
                : upgradeMode
                ? $"Обычная → бронза ● {GameController.UpgradePrice(2)} → серебро ● {GameController.UpgradePrice(3)} → золото ● {GameController.UpgradePrice(4)} → … без предела.\n" +
                  $"Улучшенная карта ставит комнату сразу своего уровня. У тебя ● {game.Gold}."
                : $"Каждая — ● {GameController.RemovePrice}. У тебя ● {game.Gold}. В колоде {game.DeckSize} карт (минимум {GameController.MinDeck}).";

            var counts = game.DeckCounts();
            for (int i = 0; i < counts.Count; i++)
            {
                var (card, count) = counts[i];
                int col = i % 3, row = i / 3;
                string label;
                bool can;
                if (upgradeMode)
                {
                    int price = GameController.UpgradePrice(card.Level + 1);
                    can = card.CanLevelUp && (freeUpgrade || game.Gold >= price);
                    label = card.CanLevelUp
                        ? $"{card.Title} ×{count}\n→ {Card.LevelName(card.Level + 1).ToLower()} {(freeUpgrade ? "даром" : "● " + price)}"
                        : $"{card.Title} ×{count}\n{(card.Def.CanUpgrade ? "максимум" : "не улучшается")}";
                }
                else
                {
                    can = game.CanRemoveCards && game.Gold >= GameController.RemovePrice;
                    label = $"{card.Title} ×{count}";
                }
                var c = card;
                var b = UIKit.Button(removeGrid, label, upgradeMode || freeUpgrade ? ShopColor : Bad,
                    () =>
                    {
                        if (freeUpgrade) game?.RestUpgrade(c);
                        else if (upgradeMode) game?.UpgradeCard(c);
                        else game?.RemoveCard(c);
                    }, Fc(C.BodyFont - 2));
                Place(b, (col - 1) * 335, 420 - row * 130, 315, 116);
                b.interactable = can;
                MetalEdge(b.transform, card.Level);
            }
        }

        // Тонкая металлическая окантовка у улучшенной карты в списках.
        void MetalEdge(Transform parent, int level)
        {
            if (level < 2) return;
            var edge = UIKit.Image("Metal", parent, CardBacks.Tiers[Card.FrameTier(level)].Main, Sprites.Frame);
            edge.raycastTarget = false;
            UIKit.Stretch(edge.rectTransform, -4);
        }

        // ───────────── Наследство ─────────────

        void BuildInheritOverlay()
        {
            inheritRoot = Overlay("LevelDone", 0.75f, out inheritContent);
            var trophy = UIKit.Image("Trophy", inheritContent, Color.white, UiArt.Icon("Trophy"));
            trophy.raycastTarget = false;
            trophy.preserveAspect = true;
            trophy.enabled = trophy.sprite != null;
            UIKit.Place(trophy.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 840), new Vector2(170, 170));
            inheritTitle = Label(inheritContent, "", C.StatsFont + 30, Gold, 700, 120);
            inheritStars = UIKit.Rect("Stars", inheritContent);
            UIKit.Place(inheritStars, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 560), new Vector2(480, 150));
            Label(inheritContent, "Выбери карту-наследство: она будет в колоде каждого следующего дома", C.BodyFont + 2, Color.white, 420, 90, 960, FontStyle.Normal);

            inheritCards = UIKit.Rect("Cards", inheritContent);
            inheritCards.localScale = new Vector3(1.2f, 1.2f, 1);
            UIKit.Place(inheritCards, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60),
                new Vector2(L.CardW * 3 + L.CardGap * 4, L.CardH));

            Place(UIKit.Button(inheritContent, "БЕЗ НАСЛЕДСТВА", SkipColor, () => game?.PickInherit(-1), Fc(C.ButtonFont - 6)), 0, -320, 440, 96);
            inheritInfo = Label(inheritContent, "", C.BodyFont, Cream, -460, 140, 1000, FontStyle.Normal);

            // Новая рубашка за дом — справа от звёзд.
            inheritBack = UIKit.Rect("NewBack", inheritContent);
            UIKit.Place(inheritBack, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(390, 560), new Vector2(110, 154));
        }

        void RefreshInherit()
        {
            inheritTitle.text = $"«{game.Level.Title.ToUpper()}» ВЫСТОЯЛ!";
            Clear(inheritBack);
            if (game.NewBack != 0)
            {
                var back = CardBacks.Create(inheritBack, game.NewBack, new Vector2(110, 154));
                UIKit.Stretch(back);
                var label = UIKit.Outlined(UIKit.Text(inheritBack, "Новая рубашка!", Fc(C.BodyFont - 4), TextAnchor.UpperCenter, Gold, FontStyle.Bold), 2);
                UIKit.Place(label.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(220, 40));
                inheritBack.DOKill(true);
                inheritBack.DOScale(1, 0.5f).From(0).SetDelay(1.2f).SetEase(Ease.OutBack).SetLink(inheritBack.gameObject);
            }
            Clear(inheritCards);
            for (int i = 0; i < game.InheritOptions.Count; i++)
            {
                int index = i;
                var card = BuildCard(inheritCards, RoomDatabase.Get(game.InheritOptions[i]), () => game?.PickInherit(index));
                UIKit.Place(card, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(L.CardGap + i * (L.CardW + L.CardGap) + L.CardW / 2, 0), new Vector2(L.CardW, L.CardH));
            }
            var next = HouseDatabase.Level(game.Level.Index + 1);
            string heir = game.Progress.Heirlooms.Count > 0
                ? string.Join(", ", game.Progress.Heirlooms.Select(t => RoomDatabase.Get((RoomType)t).Name))
                : "пока пусто";
            inheritInfo.text = $"Открыт дом: <color=#ffd34d>{next.Title}</color>\n" +
                               $"Наследство ({game.Progress.Heirlooms.Count}/{GameController.MaxHeirlooms}): {heir}";
        }

        // Звёзды за дом: 3 — репутация не пострадала, 2 — осталось хотя бы 2 сердца, 1 — выстоял.
        void AnimateStars()
        {
            Clear(inheritStars);
            for (int i = 0; i < 3; i++)
            {
                bool on = i < game.LastStars;
                var star = UIKit.Image("Star", inheritStars, Color.white, on ? GuestArt.Star : GuestArt.StarEmpty);
                star.raycastTarget = false;
                UIKit.Place(star.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 150, i == 1 ? 20 : 0), new Vector2(130, 130));
                star.rectTransform.localScale = Vector3.zero;
                star.rectTransform.DOScale(1, 0.4f).SetDelay(0.3f + i * 0.25f).SetEase(Ease.OutBack, 3)
                    .OnStart(() => { if (on) Sfx.Coin(); })
                    .SetLink(star.gameObject);
            }
        }

        // ───────────── Колода игрока ─────────────

        void BuildDeckOverlay()
        {
            deckRoot = Overlay("Deck", 0.8f, out deckContent);
            Label(deckContent, "ТВОЯ КОЛОДА", C.StatsFont + 14, Gold, 700, 100);
            deckInfo = Label(deckContent, "", C.BodyFont + 2, Color.white, 610, 80, 1000, FontStyle.Normal);
            deckGrid = UIKit.Rect("Cards", deckContent);
            UIKit.Place(deckGrid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1020, 1000));
            Place(UIKit.Button(deckContent, "ЗАКРЫТЬ", RestartColor, () => deckRoot.gameObject.SetActive(false), Fc(C.ButtonFont - 4)), 0, -640, 380, 100);
        }

        void OpenDeckView()
        {
            deckRoot.gameObject.SetActive(true);
            deckRoot.SetAsLastSibling();
            if (tutorial != null) tutorial.BringToFront();
            Pop(deckContent);
            RefreshDeckView();
        }

        // Что ещё можно вытянуть: карты колоды добора по типам.
        void RefreshDeckView()
        {
            Clear(deckGrid);
            deckInfo.text = $"В колоде: {game.DeckCount} · в руке: {game.Hand.Count}/{GameController.MaxHand}\n" +
                            "<size=" + (Fc(C.BodyFont - 4)) + ">Утром +2 карты, перед ночью +1. Пустая колода — усталость: −1 ♥ за каждую карту</size>";
            var counts = game.DrawPileCounts();
            for (int i = 0; i < counts.Count; i++)
            {
                var (card, count) = counts[i];
                var def = card.Def;
                var type = card.Type;
                int col = i % 3, row = i / 3;
                var chip = UIKit.Rect("Chip", deckGrid);
                UIKit.Place(chip, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((col - 1) * 335, 420 - row * 130), new Vector2(315, 110));
                UIKit.DropShadow(chip, new Vector2(0, -6), 0.45f);
                var body = UIKit.Image("Body", chip, def.Color, Sprites.Panel);
                body.raycastTarget = false;
                UIKit.Stretch(body.rectTransform);
                var icon = UIKit.Image("Icon", chip, Color.white, Icons.Room(type));
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                UIKit.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 2), new Vector2(84, 84));
                var t = UIKit.Outlined(UIKit.Text(chip, $"{card.Title}\n×{count}", C.BodyFont + 2, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold), 2);
                t.rectTransform.anchorMin = Vector2.zero;
                t.rectTransform.anchorMax = Vector2.one;
                t.rectTransform.offsetMin = new Vector2(104, 4);
                t.rectTransform.offsetMax = new Vector2(-8, -4);
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = Fc(C.BodyFont - 8);
                t.resizeTextMaxSize = C.BodyFont + 2;
                MetalEdge(chip, card.Level);
            }
            if (counts.Count == 0)
                Label(deckGrid, "Колода пуста", C.StatsFont, Cream, 0, 100);
        }

        // ───────────── Карта гостя крупно ─────────────

        // ───────────── Конец игры ─────────────

        void BuildGameOverOverlay()
        {
            gameOverRoot = Overlay("GameOver", 0.7f, out gameOverContent);
            // Над заголовком — кубок (победа) или череп (дом закрылся).
            gameOverIcon = UIKit.Image("Icon", gameOverContent, Color.white);
            gameOverIcon.raycastTarget = false;
            gameOverIcon.preserveAspect = true;
            UIKit.Place(gameOverIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 470), new Vector2(240, 240));
            gameOverTitle = Label(gameOverContent, "", C.StatsFont + 40, Gold, 260, 140);
            gameOverBody = Label(gameOverContent, "", C.StatsFont, Color.white, 60, 260, 1000, FontStyle.Bold);
            Place(UIKit.Button(gameOverContent, "ЕЩЁ РАЗ", RestartColor, () => game?.RetryLevel(), C.ButtonFont), 0, -180, 520, 120);
            Place(UIKit.Button(gameOverContent, "В МЕНЮ", SkipColor, () => game?.ShowMenu(), Fc(C.ButtonFont - 4)), 0, -330, 420, 100);
        }

        void RefreshGameOver()
        {
            gameOverTitle.text = game.Won ? "ПОБЕДА!" : "ДОМ ЗАКРЫЛСЯ";
            gameOverTitle.color = game.Won ? Gold : Bad;
            gameOverIcon.sprite = UiArt.Icon(game.Won ? "Trophy" : "Skull");
            gameOverIcon.enabled = gameOverIcon.sprite != null;
            gameOverBody.text = game.GameOverText;
        }

        // ───────────── Подсказки ─────────────

        void BuildHintOverlay()
        {
            hintRoot = Overlay("Hint", 0.65f, out hintContent);
            var panel = UIKit.Rect("Panel", hintContent);
            UIKit.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, 820));
            UIKit.DropShadow(panel, new Vector2(0, -16), 0.55f);
            var body = UIKit.Image("Body", panel, DarkWood, Sprites.Panel);
            UIKit.Stretch(body.rectTransform);
            var paper = UIKit.Image("Paper", panel, Cream, Sprites.Flat);
            UIKit.Stretch(paper.rectTransform, 26);
            paper.rectTransform.offsetMax = new Vector2(-26, -130);
            paper.rectTransform.offsetMin = new Vector2(26, 160);

            hintTitle = UIKit.Outlined(UIKit.Text(panel, "", C.StatsFont + 10, TextAnchor.MiddleCenter, Gold, FontStyle.Bold), 3);
            UIKit.TopBand(hintTitle.rectTransform, 24, 96);
            hintBody = UIKit.Text(paper.transform, "", C.BodyFont + 6, TextAnchor.MiddleCenter, InkBrown, FontStyle.Bold);
            UIKit.Stretch(hintBody.rectTransform, 26);
            hintBody.resizeTextForBestFit = true;
            hintBody.resizeTextMinSize = Fc(C.BodyFont - 4);
            hintBody.resizeTextMaxSize = C.BodyFont + 6;

            var ok = UIKit.Button(panel, "ПОНЯТНО", RestartColor, NextHint, C.ButtonFont);
            UIKit.Place((RectTransform)ok.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(420, 110));
        }

        void OnHint(string title, string text)
        {
            if (currentHint.HasValue)
            {
                hints.Enqueue((title, text));
                return;
            }
            ShowHint((title, text), true);
        }

        void ShowHint((string title, string text) hint, bool animate)
        {
            currentHint = hint;
            hintTitle.text = hint.title;
            hintBody.text = hint.text;
            hintRoot.gameObject.SetActive(true);
            hintRoot.SetAsLastSibling();
            if (tutorial != null) tutorial.BringToFront();
            if (animate) Pop(hintContent);
            // Ночь ждёт, пока игрок читает.
            if (nightSeq != null && nightSeq.IsActive()) nightSeq.Pause();
        }

        void NextHint()
        {
            if (hints.Count > 0)
            {
                ShowHint(hints.Dequeue(), true);
                return;
            }
            currentHint = null;
            hintRoot.gameObject.SetActive(false);
            if (nightSeq != null && nightSeq.IsActive()) nightSeq.Play();
        }
    }
}
