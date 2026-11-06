using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Соперник и ночь: рука рубашками вверх, вход перед домом, выкладывание карты гостя
    // с переворотом, пунктир его маршрута, фишка с фонариком, звуки шагов.
    public partial class GameView
    {
        static readonly Color ThiefRoute = RoomDatabase.Hex("ff6b5e");
        static readonly Color WolfRoute = RoomDatabase.Hex("ffb03a");
        static readonly Color WalkRoute = RoomDatabase.Hex("fff1a8");

        const float PorchScale = 0.5f;  // карта гостя на входе
        const float OppScale = 0.3f;    // рубашки в руке соперника

        bool nightAnim;                  // идёт анимация (карта летит или гость ходит)
        Sequence nightSeq;
        CanvasGroup nightGroup;

        RectTransform fxLayer, oppRoot, oppHandRoot, oppDeckRoot, porchSlot;
        readonly List<GuestCard> oppCards = new List<GuestCard>();
        GuestCard porchCard;
        int oppLaidOut = -1;
        Text oppInfo, oppDeckCount;

        RectTransform guestToken, guestVisual, routeLayer;
        Image guestImage, guestIcon, guestLight;
        Text guestMood;
        RectTransform moodBar, moodFill;
        Material guestDissolve; // фишка гостя растворяется, уходя
        Image moodFillImg;
        string routeKey;

        static float NightAlpha(Phase p) =>
            p == Phase.Night || p == Phase.Summary ? 1 : p == Phase.Reward ? 0.5f : 0;

        // Ночное небо: градиент, луна и мерцающие звёзды. Плавно проявляется целиком.
        // Если у дома свой фон (на нём уже ночь и луна) — вместо неба только тёмная дымка и звёзды.
        void BuildNightSky(bool overLocation = false, bool graded = false)
        {
            var layer = UIKit.Rect("Night", transform);
            UIKit.Stretch(layer);
            nightGroup = layer.gameObject.AddComponent<CanvasGroup>();
            nightGroup.blocksRaycasts = false;
            nightGroup.alpha = NightAlpha(game.State);

            if (overLocation)
            {
                // Если ночь уже делает шейдер фона — дымка лишь чуть-чуть, иначе погасила бы тёплые окна.
                var haze = UIKit.Image("Haze", layer, new Color(0.04f, 0.05f, 0.16f, graded ? 0.12f : 0.45f));
                haze.raycastTarget = false;
                UIKit.Stretch(haze.rectTransform);
            }
            else
            {
                var sky = UIKit.Image("Sky", layer, Color.white, Sprites.NightBackground);
                sky.raycastTarget = false;
                UIKit.Stretch(sky.rectTransform);

                var moonGlow = UIKit.Image("MoonGlow", layer, new Color(1, 0.95f, 0.75f, 0.25f), Sprites.Light);
                moonGlow.raycastTarget = false;
                UIKit.Place(moonGlow.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-170, -150), new Vector2(360, 360));
                var moon = UIKit.Image("Moon", layer, RoomDatabase.Hex("fff3c4"), Sprites.Circle);
                moon.raycastTarget = false;
                UIKit.Place(moon.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-170, -150), new Vector2(110, 110));
            }

            var rnd = new System.Random(42);
            for (int i = 0; i < 40; i++)
            {
                float s = 4 + (float)rnd.NextDouble() * 6;
                var star = UIKit.Image("Star", layer, new Color(1, 1, 0.9f, 0.9f), Sprites.Light);
                star.raycastTarget = false;
                var pos = new Vector2((float)rnd.NextDouble(), 0.35f + (float)rnd.NextDouble() * 0.65f);
                UIKit.Place(star.rectTransform, pos, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s * 3, s * 3));
                star.DOFade(0.2f, 0.8f + (float)rnd.NextDouble() * 1.6f)
                    .SetDelay((float)rnd.NextDouble() * 2).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
                    .SetLink(star.gameObject);
            }
        }

        void FadeNightSky(Phase phase)
        {
            nightGroup.DOKill();
            nightGroup.DOFade(NightAlpha(phase), 0.8f).SetLink(nightGroup.gameObject);
            if (locationMat != null)
            {
                DOTween.Kill(locationMat);
                locationMat.DOFloat(NightAlpha(phase), "_Night", 1.2f).SetEase(Ease.InOutSine).SetLink(gameObject);
            }
        }

        // ───────────── Соперник ─────────────

        void BuildOpponent()
        {
            oppRoot = UIKit.Rect("Opponent", root);
            var bg = UIKit.Image("Bg", oppRoot, PlateTint, UiArt.Plate);
            bg.raycastTarget = false;
            UIKit.Stretch(bg.rectTransform);

            oppInfo = UIKit.Text(oppRoot, "", Fs(L.BodyFont - 2), TextAnchor.MiddleLeft, Cream, FontStyle.Bold);
            oppInfo.rectTransform.anchorMin = new Vector2(0, 0);
            oppInfo.rectTransform.anchorMax = new Vector2(0, 1);
            oppInfo.rectTransform.pivot = new Vector2(0, 0.5f);
            oppInfo.rectTransform.offsetMin = new Vector2(20, 8);
            oppInfo.rectTransform.offsetMax = new Vector2(300, -8);
            oppInfo.resizeTextForBestFit = true;
            oppInfo.resizeTextMinSize = Fs(L.BodyFont - 10);
            oppInfo.resizeTextMaxSize = Fs(L.BodyFont - 2);

            oppHandRoot = UIKit.Rect("OppHand", oppRoot);
            UIKit.Place(oppHandRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60, 0), new Vector2(420, 160));

            // Колода гостей: стопка рубашек и число карт.
            oppDeckRoot = UIKit.Rect("OppDeck", oppRoot);
            UIKit.Place(oppDeckRoot, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 6), new Vector2(GuestCard.W * OppScale, GuestCard.H * OppScale));
            for (int i = 0; i < 3; i++)
            {
                var c = GuestCard.Create(oppDeckRoot, null, false, game.OpponentBack);
                c.Root.localScale = Vector3.one * OppScale;
                c.Root.anchoredPosition = new Vector2(-i * 3, i * 4);
            }
            oppDeckCount = UIKit.Outlined(UIKit.Text(oppDeckRoot, "", L.BodyFont, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold), 2);
            UIKit.Place(oppDeckCount.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, 2), new Vector2(160, 34));
            oppCards.Clear();
            oppLaidOut = -1;
        }

        // Рубашки в руке соперника: сколько карт — столько и рубашек, веером.
        void RefreshOpponent()
        {
            var opp = game.Opp;
            int count = opp != null ? opp.Hand.Count : 0;

            while (oppCards.Count > count)
            {
                var last = oppCards[oppCards.Count - 1];
                oppCards.RemoveAt(oppCards.Count - 1);
                Destroy(last.Root.gameObject);
            }
            bool added = false;
            while (oppCards.Count < count)
            {
                var c = GuestCard.Create(oppHandRoot, null, false, game.OpponentBack);
                c.Root.localScale = Vector3.one * OppScale;
                // Новая карта прилетает из колоды гостей.
                c.Root.position = oppDeckRoot.TransformPoint(oppDeckRoot.rect.center);
                oppCards.Add(c);
                added = true;
            }
            // Раскладываем заново, только когда число карт изменилось, — иначе сбили бы анимацию раздачи.
            if (oppCards.Count != oppLaidOut)
            {
                LayoutOpponent(added);
                oppLaidOut = oppCards.Count;
            }

            if (opp == null)
            {
                oppInfo.text = "";
                oppDeckCount.text = "";
                return;
            }
            oppDeckCount.text = opp.Deck.Count.ToString();
            string tonight = game.State == Phase.Night
                ? $"Ещё гостей: <color=#ffd34d>{game.GuestsLeftTonight}</color>"
                : $"Ночью гостей: <color=#ffd34d>{(game.IsTutorial ? 1 : game.Level.GuestsOnNight(game.Night))}</color>";
            oppInfo.text = $"<b>СОПЕРНИК</b>\nВ руке: {count}\n{tonight}";
        }

        void LayoutOpponent(bool animate)
        {
            int n = oppCards.Count;
            float cardW = GuestCard.W * OppScale;
            float step = n <= 1 ? 0 : Mathf.Min(cardW * 0.8f, (oppHandRoot.rect.width - cardW) / (n - 1));
            for (int i = 0; i < n; i++)
            {
                float t = n <= 1 ? 0 : i / (n - 1f) * 2 - 1;
                var rt = oppCards[i].Root;
                rt.SetSiblingIndex(i);
                var pos = new Vector2((i - (n - 1) / 2f) * step, -Mathf.Abs(t) * 8);
                rt.DOKill();
                rt.DOAnchorPos(pos, animate ? 0.35f : 0.2f).SetDelay(animate ? i * 0.16f + 0.08f : 0).SetEase(Ease.OutCubic).SetLink(rt.gameObject);
                rt.DOLocalRotate(new Vector3(0, 0, t * 8), 0.3f).SetLink(rt.gameObject);
            }
        }

        // ───────────── Вход ─────────────

        Vector2 PorchPos
        {
            get
            {
                return new Vector2(CellPos(new Vector2Int(2, 0)).x, BoardY - BoardBelow - 12 - GuestCard.H * PorchScale / 2);
            }
        }

        // Ячейка входа под прихожей: сюда соперник кладёт карту гостя.
        void BuildPorch()
        {
            porchSlot = UIKit.Rect("Porch", gridRoot);
            var size = new Vector2(GuestCard.W, GuestCard.H) * PorchScale;
            UIKit.Place(porchSlot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), PorchPos, size);

            var porchArt = BoardArt("Porch");
            if (porchArt != null)
            {
                // Нарисованное крыльцо: ступени от двери прихожей, коврик — под картой гостя.
                var porch = UIKit.Image("PorchArt", porchSlot, Color.white, porchArt);
                porch.raycastTarget = false;
                porch.preserveAspect = true;
                UIKit.Place(porch.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(size.x * 1.6f, size.y * 1.1f));
            }
            else
            {
                var mat = UIKit.Image("Mat", porchSlot, new Color(0.2f, 0.12f, 0.08f, 0.55f), Sprites.Soft);
                mat.raycastTarget = false;
                UIKit.Stretch(mat.rectTransform, -6);
            }
            var edge = UIKit.Image("Edge", porchSlot, new Color(1, 0.83f, 0.3f, 0.8f), Sprites.Frame);
            edge.raycastTarget = false;
            UIKit.Stretch(edge.rectTransform, -6);
            edge.DOFade(0.3f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(edge.gameObject);
            var label = UIKit.Outlined(UIKit.Text(porchSlot, "ВХОД", L.BodyFont + 2, TextAnchor.MiddleCenter, Gold, FontStyle.Bold), 2);
            UIKit.Stretch(label.rectTransform);
        }

        void PutOnPorch(GuestCard card)
        {
            card.Root.SetParent(porchSlot, false);
            card.Root.anchorMin = card.Root.anchorMax = new Vector2(0.5f, 0.5f);
            card.Root.anchoredPosition = Vector2.zero;
            card.Root.localRotation = Quaternion.identity;
            card.Root.localScale = Vector3.one * PorchScale;
            card.MakeClickable(() => Inspect(card.Guest, card.Root));
            porchCard = card;
        }

        void ClearPorch()
        {
            if (porchCard != null) Destroy(porchCard.Root.gameObject);
            porchCard = null;
        }

        // После перестройки экрана ночью — вернуть карту гостя на вход.
        void RestoreNightState()
        {
            porchCard = null;
            var g = game.CurrentGuest;
            if (game.State == Phase.Night && g != null && game.Step != NightStep.Reveal)
                PutOnPorch(GuestCard.Create(porchSlot, g, true, game.OpponentBack));
        }

        // ───────────── Ход соперника ─────────────

        // Соперник выкладывает карту: она вылетает из руки, крупно переворачивается в центре и ложится на вход.
        void OnGuestPlayed(GuestDef g, int index)
        {
            nightAnim = true;
            ClearPorch();

            GuestCard card;
            if (index >= 0 && index < oppCards.Count)
            {
                card = oppCards[index];
                oppCards.RemoveAt(index);
                card.Root.DOKill();
                card.SetGuest(g);
                card.Root.SetParent(fxLayer, true);
            }
            else
            {
                card = GuestCard.Create(fxLayer, g, false, game.OpponentBack);
                card.Root.position = oppDeckRoot.position;
                card.Root.localScale = Vector3.one * OppScale;
            }

            var center = fxLayer.TransformPoint(fxLayer.rect.center + new Vector2(0, 60));
            float revealScale = 1.3f * OverlayScale;
            float porchScale = gridRoot.lossyScale.x / fxLayer.lossyScale.x * PorchScale;

            nightSeq = DOTween.Sequence().SetLink(gameObject);
            nightSeq.AppendCallback(Sfx.Card)
                .Append(card.Root.DOMove(center, 0.45f).SetEase(Ease.OutCubic))
                .Join(card.Root.DOScale(revealScale, 0.45f).SetEase(Ease.OutBack))
                .Join(card.Root.DOLocalRotate(Vector3.zero, 0.3f))
                .Append(card.Flip(true))
                .AppendInterval(g.IsThreat || g.Special ? 1.6f : 1.1f)
                .Append(card.Root.DOMove(porchSlot.TransformPoint(porchSlot.rect.center), 0.42f).SetEase(Ease.InOutCubic))
                .Join(card.Root.DOScale(porchScale, 0.42f).SetEase(Ease.InOutCubic))
                .AppendCallback(Sfx.Plop)
                .OnComplete(() =>
                {
                    PutOnPorch(card);
                    porchCard.Root.DOPunchScale(Vector3.one * 0.06f, 0.25f, 6).SetLink(porchCard.Root.gameObject);
                    nightAnim = false;
                    nightSeq = null;
                    if (pendingRebuild) Rebuild();
                    game?.BeginResponse();
                });
        }

        // ───────────── Обход гостя ─────────────

        void OnGuestWalk(GuestResult res)
        {
            nightAnim = true;
            Vector2 outside = PorchPos;
            nightSeq = DOTween.Sequence().SetLink(gameObject);
            nightSeq.AppendInterval(0.35f); // дать плюхнуться ответной комнате

            // Карта «входит» в дом и превращается в фишку.
            if (porchCard != null)
            {
                var pc = porchCard;
                porchCard = null;
                // Сыгранная карта растворяется с золотым краем (UI_Dissolve).
                nightSeq.AppendCallback(() => UiShaders.Dissolve(pc.Root.gameObject, 0.5f))
                    .AppendInterval(0.5f)
                    .AppendCallback(() => Destroy(pc.Root.gameObject));
            }

            AppendGuest(nightSeq, res, outside);
            nightSeq.OnComplete(() =>
            {
                nightAnim = false;
                nightSeq = null;
                if (pendingRebuild) Rebuild();
                game?.GuestDone(res);
            });
        }

        // Фишка гостя: сама фишка едет по доске, а «тело» внутри прыгает над тенью. Вокруг — пятно фонарика.
        void BuildGuestToken()
        {
            guestToken = UIKit.Rect("Guest", gridRoot);
            float token = L.Cell * 0.5f;
            guestToken.sizeDelta = new Vector2(token, token);

            guestLight = UIKit.Image("Lantern", guestToken, new Color(1, 0.85f, 0.5f, 0.4f), Sprites.Light);
            guestLight.raycastTarget = false;
            UIKit.Place(guestLight.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * L.Cell * 2.4f);
            guestLight.DOFade(0.3f, 0.35f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(guestLight.gameObject);

            UIKit.DropShadow(guestToken, new Vector2(0, -6), 0.5f, Sprites.ShadowCircle);
            guestVisual = UIKit.Rect("Visual", guestToken);
            UIKit.Stretch(guestVisual);
            guestImage = UIKit.Image("Body", guestVisual, Color.white, Sprites.Circle);
            guestImage.raycastTarget = false;
            UIKit.Stretch(guestImage.rectTransform);
            guestIcon = UIKit.Image("Icon", guestVisual, Color.white);
            guestIcon.raycastTarget = false;
            if (guestDissolve != null) Destroy(guestDissolve);
            guestDissolve = UiShaders.NewDissolve();
            if (guestDissolve != null) guestIcon.material = guestDissolve;
            guestIcon.preserveAspect = true;
            UIKit.Stretch(guestIcon.rectTransform, token * 0.08f);
            // Шкала настроения над фишкой: рамка BoardOverlays/MoodBar, внутри — заливка кодом и число «3/4».
            var barArt = UiArt.MoodBar;
            var bar = UIKit.Image("MoodBar", guestVisual, barArt != null ? Color.white : new Color(0.1f, 0.07f, 0.06f, 0.9f), barArt != null ? barArt : Sprites.Flat);
            bar.raycastTarget = false;
            moodBar = bar.rectTransform;
            float barW = L.Cell * 1.05f, barH = Mathf.Max(34, L.Cell * 0.2f);
            UIKit.Place(moodBar, new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(barW, barH));
            if (barArt != null) bar.gameObject.AddComponent<SlicedFit>().Height = 56;
            var track = UIKit.Rect("Track", moodBar);
            UIKit.Stretch(track);
            track.offsetMin = new Vector2(barH * 0.55f, barH * 0.22f);
            track.offsetMax = new Vector2(-barH * 0.55f, -barH * 0.22f);
            moodFillImg = UIKit.Image("Fill", track, Good, Sprites.Soft);
            moodFillImg.raycastTarget = false;
            moodFill = moodFillImg.rectTransform;
            moodFill.anchorMin = Vector2.zero;
            moodFill.anchorMax = new Vector2(0, 1);
            moodFill.offsetMin = moodFill.offsetMax = Vector2.zero;
            moodBar.SetAsFirstSibling(); // под текстом
            guestMood = UIKit.Outlined(UIKit.Text(moodBar, "", Fs(L.BodyFont - 4), TextAnchor.MiddleCenter, Color.white, FontStyle.Bold));
            UIKit.Stretch(guestMood.rectTransform);
            guestMood.horizontalOverflow = HorizontalWrapMode.Overflow;
            guestToken.gameObject.SetActive(false);
        }

        void AppendGuest(Sequence seq, GuestResult res, Vector2 outside)
        {
            var g = res.Guest;
            int mood = 0;

            seq.AppendCallback(() =>
            {
                mood = 0;
                guestIcon.sprite = Icons.Guest(g);
                if (guestDissolve != null) guestDissolve.SetFloat("_Dissolve", 0);
                bool medallion = ArtStore.Get("Guests/" + g.Id, null) != null;
                guestImage.enabled = !medallion;
                guestImage.color = g.Color;
                UIKit.Stretch(guestIcon.rectTransform, medallion ? 0 : L.Cell * 0.5f * 0.08f);
                guestToken.SetAsLastSibling();
                guestToken.anchoredPosition = outside;
                guestVisual.anchoredPosition = Vector2.zero;
                guestToken.gameObject.SetActive(true);
                // У угроз фонарик красноватый.
                guestLight.color = g.IsThreat ? new Color(1, 0.45f, 0.35f, 0.4f) : new Color(1, 0.85f, 0.5f, 0.4f);
                UpdateMood(g, mood);
                Sfx.Knock();
                // Призраки появляются из дымки.
                if (IsGhost(g)) Burst(gridRoot, "GhostSmoke", outside, L.Cell * 1.1f, 0.3f, 1.1f);
            });
            seq.Append(guestVisual.DOScale(1, 0.25f).From(0.2f).SetEase(Ease.OutBack));
            seq.AppendInterval(0.2f);

            var rnd = new System.Random(res.Steps.Count * 31 + g.Id.Length);
            foreach (var step in res.Steps)
            {
                var s = step;
                AppendHop(seq, CellPos(s.Pos), 0.32f, rnd.Next(100) < 18);
                seq.AppendCallback(() =>
                {
                    mood += s.Delta;
                    UpdateMood(g, mood);
                    if (s.Delta != 0)
                    {
                        FloatingNumber(s.Delta);
                        BurstAtGuest(s.Delta > 0 ? "MoodUp" : "MoodDown", 0.75f);
                    }
                    if (s.Trap)
                    {
                        Sfx.Trap();
                        BurstAtGuest("Shield", 0.9f, 0.15f);
                        BurstAtGuest("Dust", 1.1f, 0.05f);
                        guestVisual.DOShakeAnchorPos(0.4f, 12, 30).SetLink(guestVisual.gameObject);
                    }
                    if (!string.IsNullOrEmpty(s.Text)) game?.AddLog(s.Text);
                });
                if (!string.IsNullOrEmpty(s.Text)) seq.AppendInterval(s.Trap ? 0.8f : 0.45f);
            }

            AppendHop(seq, outside, 0.3f, false);
            if (guestDissolve != null) seq.Append(guestDissolve.DOFloat(1, "_Dissolve", 0.4f).SetEase(Ease.InQuad));
            // Итог гостя у двери: золото — монеты, плюс к репутации — сердце.
            seq.AppendCallback(() =>
            {
                if (res.Gold > 0) Burst(gridRoot, "Coins", outside, L.Cell * 0.9f, 0.5f, 1f);
                if (res.Reputation > 0) Burst(gridRoot, "Heart", outside + new Vector2(L.Cell * 0.5f, 0), L.Cell * 0.9f, 0.5f, 1f);
            });
            seq.Append(guestVisual.DOScale(0.2f, 0.15f).SetEase(Ease.InBack));
            seq.AppendCallback(() => guestToken.gameObject.SetActive(false));
        }

        // Прыжок: фишка едет к цели, тело подлетает над тенью и сплющивается при приземлении.
        void AppendHop(Sequence seq, Vector2 to, float duration, bool creak)
        {
            float height = L.Cell * 0.25f;
            seq.AppendCallback(() =>
            {
                if (creak) Sfx.Creak();
            });
            seq.Append(guestToken.DOAnchorPos(to, duration).SetEase(Ease.InOutSine));
            seq.Join(guestVisual.DOAnchorPosY(height, duration / 2).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo));
            seq.AppendCallback(Sfx.Step);
            seq.Append(guestVisual.DOScale(new Vector3(1.18f, 0.82f, 1), 0.06f).SetLoops(2, LoopType.Yoyo));
        }

        void UpdateMood(GuestDef g, int mood)
        {
            // У угроз настроения нет — шкалы тоже нет.
            moodBar.gameObject.SetActive(!g.IsThreat);
            if (g.IsThreat) return;
            guestMood.text = $"{mood}/{g.Need}";
            float t = g.Need > 0 ? Mathf.Clamp01(mood / (float)g.Need) : 1;
            moodFill.DOKill();
            moodFill.DOAnchorMax(new Vector2(t, 1), 0.2f).SetLink(moodFill.gameObject);
            moodFillImg.color = mood >= g.Need ? Good : mood > 0 ? RoomDatabase.Hex("ffb834") : Bad;
        }

        // «+3» / «−2» всплывает над гостем и тает.
        void FloatingNumber(int delta)
        {
            var t = UIKit.Outlined(UIKit.Text(gridRoot, (delta > 0 ? "+" : "−") + Mathf.Abs(delta),
                L.StatsFont + 10, TextAnchor.MiddleCenter, delta > 0 ? Good : Bad, FontStyle.Bold), 3);
            var rt = t.rectTransform;
            rt.sizeDelta = new Vector2(160, 70);
            rt.anchoredPosition = guestToken.anchoredPosition + new Vector2(L.Cell * 0.3f, L.Cell * 0.3f);

            DOTween.Sequence()
                .Append(rt.DOScale(1.2f, 0.15f).From(0.4f).SetEase(Ease.OutBack))
                .Join(rt.DOAnchorPosY(L.Cell * 0.5f, 0.9f).SetRelative().SetEase(Ease.OutCubic))
                .Append(rt.DOScale(1f, 0.2f))
                .Insert(0.55f, t.DOFade(0, 0.35f))
                .SetLink(t.gameObject)
                .OnComplete(() => Destroy(t.gameObject));
        }

        // ───────────── Маршрут гостя на входе ─────────────

        // Пока игрок думает над ответом, пунктир показывает, куда пойдёт гость, если впустить его сейчас.
        // Перерисовывается, только если маршрут изменился (иначе анимация бы сбрасывалась).
        void RefreshRoutes()
        {
            var g = game.CurrentGuest;
            bool show = game.Responding && g != null;
            List<Vector2Int> path = null;
            int trap = -1;
            GuestResult predicted = null;
            string key = "";

            if (show)
            {
                if (g.IsThreat)
                {
                    path = NightSimulator.SeekerRoute(game.Grid, g, out trap);
                    key = "T" + (path == null ? "none" : string.Join(";", path)) + trap;
                }
                else if (g.Behaviour == GuestBehaviour.Random)
                {
                    key = "R";
                }
                else
                {
                    predicted = game.PredictCurrent();
                    path = new List<Vector2Int>();
                    foreach (var s in predicted.Steps)
                        if (path.Count == 0 || path[path.Count - 1] != s.Pos) path.Add(s.Pos);
                    if (path.Count > 0 && path[0] == game.Grid.Entrance) path.RemoveAt(0);
                    key = "W" + string.Join(";", path) + predicted.Mood;
                }
            }
            if (key == routeKey) return;
            routeKey = key;
            Clear(routeLayer);
            if (!show) return;

            var start = PorchPos + new Vector2(0, GuestCard.H * PorchScale / 2);
            if (g.Behaviour == GuestBehaviour.Random)
            {
                RouteLabel(CellPos(game.Grid.Entrance) + new Vector2(0, L.Cell * 0.62f), "бродит наугад", WalkRoute);
                return;
            }
            if (path == null)
            {
                RouteLabel(CellPos(game.Grid.Entrance) + new Vector2(0, L.Cell * 0.62f), "цели нет — уйдёт ни с чем", Good);
                return;
            }

            var points = new List<Vector2> { start, CellPos(game.Grid.Entrance) };
            int last = g.IsThreat && trap >= 0 ? trap : path.Count - 1;
            for (int i = 0; i <= last && i < path.Count; i++) points.Add(CellPos(path[i]));

            var color = g.IsThreat ? (g.SeekTarget == RoomType.Kitchen ? WolfRoute : ThiefRoute) : WalkRoute;
            int dot = 0;
            float spacing = L.Step / 5f;
            for (int s = 0; s < points.Count - 1; s++)
            {
                var a = points[s];
                var b = points[s + 1];
                int n = Mathf.Max(1, Mathf.RoundToInt((b - a).magnitude / spacing));
                for (int k = 0; k < n; k++) RouteDot(Vector2.Lerp(a, b, k / (float)n), color, dot++);
            }

            var end = points[points.Count - 1];
            if (g.IsThreat)
            {
                bool caught = trap >= 0;
                RouteMarker(end, g, caught ? Good : color, caught ? "ПОПАДЁТСЯ" : "ДОБЕРЁТСЯ!");
            }
            else
            {
                bool ok = predicted.Mood >= g.Need;
                RouteMarker(end, g, ok ? Good : Bad, $"~{predicted.Mood}/{g.Need} {(ok ? "доволен" : "недоволен")}");
            }
        }

        // Точка пунктира. Точки гаснут по очереди — кажется, что пунктир «бежит» к цели.
        void RouteDot(Vector2 pos, Color color, int index)
        {
            var img = UIKit.Image("Dot", routeLayer, color, Sprites.Circle);
            img.raycastTarget = false;
            float size = L.Cell * 0.13f;
            UIKit.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            img.DOFade(0.25f, 0.45f).SetDelay(index * 0.07f % 1.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
                .SetLink(img.gameObject);
        }

        // Конец маршрута: значок гостя и подпись — чем кончится.
        void RouteMarker(Vector2 pos, GuestDef g, Color color, string text)
        {
            var marker = UIKit.Rect("Marker", routeLayer);
            float size = L.Cell * 0.46f;
            UIKit.Place(marker, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));

            var ring = UIKit.Image("Ring", marker, color, Sprites.Circle);
            ring.raycastTarget = false;
            UIKit.Stretch(ring.rectTransform);
            var icon = UIKit.Image("Icon", marker, Color.white, Icons.Guest(g));
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            UIKit.Stretch(icon.rectTransform, size * 0.1f);

            var label = UIKit.Outlined(UIKit.Text(marker, text, L.TileFont, TextAnchor.UpperCenter, color, FontStyle.Bold), 2);
            UIKit.Place(label.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(L.Cell * 1.4f, L.TileFont + 8));
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            marker.DOScale(1.12f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(marker.gameObject);
        }

        void RouteLabel(Vector2 pos, string text, Color color)
        {
            var label = UIKit.Outlined(UIKit.Text(routeLayer, text, L.TileFont + 2, TextAnchor.MiddleCenter, color, FontStyle.Bold), 2);
            UIKit.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(L.Cell * 3, 40));
            label.rectTransform.DOScale(1.08f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetLink(label.gameObject);
        }
    }
}
