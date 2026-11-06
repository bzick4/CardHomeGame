using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Крупный просмотр карты (комнаты или гостя) с оборотом.
    // Открытие: копия карты появляется на месте исходной, летит в центр, растёт и делает полный оборот
    // (на 90° видна рубашка, на 270° — снова лицо), за ней вспышка; пока открыта — лёгкое «дыхание».
    // Под картой — крупное название и полное описание. Касание где угодно — тот же путь обратно.
    // Только DOTween: ни Update, ни корутин.
    public partial class GameView
    {
        const float InspectOpen = 0.6f, InspectClose = 0.45f;
        const float InspectHeight = 800; // высота карты в просмотре (единицы экрана 1080×1920)

        Image inspectDim;
        RectTransform inspectCard, inspectSource, inspectInfo;
        CanvasGroup inspectSourceGroup, inspectInfoGroup;
        float inspectSourceAlpha;
        Vector3 inspectStartPos;
        float inspectStartScale;
        Action<bool> inspectShowFace;
        Sequence inspectSeq;
        Tween inspectBreath, inspectBob;
        Phase inspectPhase;
        bool inspectClosing;

        void BuildInspectOverlay()
        {
            inspectRoot = Overlay("Inspect", 0, out inspectContent);
            inspectDim = inspectRoot.Find("Dim").GetComponent<Image>();
            // Касание в любом месте закрывает.
            var close = inspectRoot.gameObject.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.targetGraphic = inspectDim;
            close.onClick.AddListener(CloseInspect);
        }

        // Гость: лицо его карты, на обороте — рубашка соперника.
        void Inspect(GuestDef g, RectTransform source = null)
        {
            CloseInspectNow();
            var card = GuestCard.Create(inspectContent, g, true, game.OpponentBack);
            string stats = g.IsThreat
                ? $"Награда за поимку: {g.CatchReward} зол."
                : $"Нужно настроения: {g.Need} · выносливость: {g.Stamina}";
            OpenInspect(card.Root, new Vector2(GuestCard.W, GuestCard.H), card.SetFace, source,
                g.Name, $"{g.Description}\n<color=#ffd34d>{stats}</color>", CardBacks.TierOf(game.OpponentBack));
        }

        // Комната: полная карта с описанием, на обороте — рубашка игрока.
        void InspectRoom(RoomDef def, int level, Doors doors, RectTransform source)
        {
            CloseInspectNow();
            var holder = UIKit.Rect("InspectCard", inspectContent);
            holder.sizeDelta = new Vector2(L.CardW, L.CardH);
            var parts = BuildCardVisual(holder, def, doors, level);
            var face = parts.Root;
            face.anchorMin = face.anchorMax = face.pivot = new Vector2(0.5f, 0.5f);
            face.anchoredPosition = Vector2.zero;
            // Рубашка — размером с лицо карты (оно вписано в карту с пропорцией 300:420).
            var backSize = new Vector2(L.CardH * 300f / 420f, L.CardH);
            var back = CardBacks.Create(holder, game.Progress != null ? game.Progress.EquippedBack : CardBacks.DefaultCode, backSize);
            back.gameObject.SetActive(false);

            string title = level > 1 ? $"{def.Name} {HouseDatabase.Roman(level)}" : def.Name;
            string text = def.Description + (level > 1 ? $"\n<color=#ffd34d>Уровень: {Card.LevelName(level)}</color>" : "")
                          + (def.IsPassage ? "\n<color=#c9e7ff>Проход: не тратит выносливость гостя</color>" : "");
            OpenInspect(holder, holder.sizeDelta, up =>
            {
                face.gameObject.SetActive(up);
                back.gameObject.SetActive(!up);
            }, source, title, text, Card.FrameTier(level));
        }

        void OpenInspect(RectTransform card, Vector2 baseSize, Action<bool> showFace, RectTransform source,
            string title, string text, int tier)
        {
            inspectCard = card;
            inspectShowFace = showFace;
            inspectPhase = game.State;
            inspectClosing = false;
            inspectRoot.gameObject.SetActive(true);
            inspectRoot.SetAsLastSibling();
            inspectContent.localScale = Vector3.one * OverlayScale;
            // Поверх обучения: его затемнение ловит касания мимо цели, и под ним просмотр нельзя было бы закрыть.
            // Само обучение не трогаем — после закрытия оно на месте.

            // Откуда летит: центр исходной карты в координатах просмотра, тот же видимый размер.
            inspectSource = source != null && source.gameObject.activeInHierarchy ? source : null;
            if (inspectSource != null)
            {
                inspectStartPos = inspectContent.InverseTransformPoint(inspectSource.TransformPoint(inspectSource.rect.center));
                inspectStartScale = inspectSource.lossyScale.x / inspectContent.lossyScale.x * (inspectSource.rect.height / baseSize.y);
                inspectSourceGroup = inspectSource.GetComponent<CanvasGroup>();
                if (inspectSourceGroup == null) inspectSourceGroup = inspectSource.gameObject.AddComponent<CanvasGroup>();
                inspectSourceAlpha = inspectSourceGroup.alpha;
                inspectSourceGroup.alpha = 0;
            }
            else
            {
                inspectStartPos = new Vector3(0, 230, 0);
                inspectStartScale = 0.3f;
                inspectSourceGroup = null;
            }

            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.localPosition = inspectStartPos;
            card.localScale = Vector3.one * inspectStartScale;
            card.localEulerAngles = Vector3.zero;
            showFace(true);

            BuildInspectInfo(title, text);
            var target = new Vector3(0, 230, 0);
            float targetScale = InspectHeight / baseSize.y;

            inspectDim.DOKill();
            inspectDim.color = new Color(inspectDim.color.r, inspectDim.color.g, inspectDim.color.b, 0);
            inspectSeq = DOTween.Sequence().SetLink(inspectRoot.gameObject)
                .Append(inspectDim.DOFade(0.7f, 0.25f))
                .Insert(0, card.DOLocalMove(target, InspectOpen).SetEase(Ease.OutCubic))
                .Insert(0, card.DOScale(targetScale, InspectOpen).SetEase(Ease.OutBack))
                .Insert(0, card.DOLocalRotate(new Vector3(0, 360, 0), InspectOpen, RotateMode.FastBeyond360)
                    .SetEase(Ease.InOutSine).OnUpdate(UpdateInspectSide))
                .AppendCallback(() =>
                {
                    card.localEulerAngles = Vector3.zero;
                    showFace(true);
                    Sfx.Card();
                    // Вспышка за картой; у самых редких окантовок — ещё и золотой луч.
                    float glow = InspectHeight * 1.15f;
                    BehindCard(Burst(inspectContent, "Sparkles", target, glow, 0, 1.1f));
                    if (tier >= 7) BehindCard(Burst(inspectContent, "BeamGold", target + new Vector3(0, -InspectHeight * 0.2f), glow, 0.05f, 1.4f));
                    StartBreathing(card, target);
                })
                .Append(inspectInfoGroup.DOFade(1, 0.25f));
        }

        void BehindCard(Image fx)
        {
            if (fx != null && inspectCard != null) fx.transform.SetSiblingIndex(inspectCard.GetSiblingIndex());
        }

        // Пока карта открыта — едва заметно покачивается и «дышит».
        void StartBreathing(RectTransform card, Vector3 center)
        {
            inspectBreath = DOTween.Sequence()
                .Append(card.DOLocalRotate(new Vector3(0, 0, 2), 0.9f).SetEase(Ease.InOutSine))
                .Append(card.DOLocalRotate(new Vector3(0, 0, -2), 1.8f).SetEase(Ease.InOutSine))
                .Append(card.DOLocalRotate(Vector3.zero, 0.9f).SetEase(Ease.InOutSine))
                .SetLoops(-1)
                .SetLink(card.gameObject);
            inspectBob = card.DOLocalMoveY(center.y + 6, 1.2f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(card.gameObject);
        }

        // Ребром к зрителю (90°…270°) карта показывает рубашку.
        void UpdateInspectSide()
        {
            if (inspectCard == null) return;
            float a = Mathf.Repeat(inspectCard.localEulerAngles.y, 360);
            inspectShowFace?.Invoke(a < 90 || a > 270);
        }

        // Под картой: крупное название и описание на тёмной плашке, внизу — как закрыть.
        void BuildInspectInfo(string title, string text)
        {
            var plate = UIKit.Image("Info", inspectContent, new Color(1, 1, 1, 0.9f), UiArt.Plate);
            plate.raycastTarget = false;
            inspectInfo = plate.rectTransform;
            UIKit.Place(inspectInfo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, -210), new Vector2(980, 470));
            inspectInfoGroup = plate.gameObject.AddComponent<CanvasGroup>();
            inspectInfoGroup.alpha = 0;
            inspectInfoGroup.blocksRaycasts = false;

            var name = UIKit.Outlined(UIKit.Text(inspectInfo, title, Mathf.Max(48, C.StatsFont + 4), TextAnchor.MiddleCenter, Gold, FontStyle.Bold));
            UIKit.TopBand(name.rectTransform, 22, 70, 30);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 40;
            name.resizeTextMaxSize = Mathf.Max(48, C.StatsFont + 4);

            var body = UIKit.Text(inspectInfo, text, Mathf.Max(36, C.BodyFont + 2), TextAnchor.UpperCenter, Cream, FontStyle.Bold);
            body.rectTransform.anchorMin = Vector2.zero;
            body.rectTransform.anchorMax = Vector2.one;
            body.rectTransform.offsetMin = new Vector2(36, 70);
            body.rectTransform.offsetMax = new Vector2(-36, -104);
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 34;
            body.resizeTextMaxSize = Mathf.Max(36, C.BodyFont + 2);

            var hint = UIKit.Text(inspectInfo, "Коснись, чтобы закрыть", Mathf.Max(32, C.BodyFont - 2), TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.6f));
            UIKit.Place(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(900, 52));
        }

        // Закрыть с анимацией: карта тем же оборотом возвращается на своё место, исходная снова видна.
        void CloseInspect()
        {
            if (inspectClosing || inspectCard == null) return;
            inspectClosing = true;
            inspectSeq?.Kill();
            inspectBreath?.Kill();
            inspectBob?.Kill();
            var card = inspectCard;

            // Исходная карта могла сдвинуться (рука перестроилась) — летим туда, где она сейчас.
            Vector3 to = inspectStartPos;
            float scale = inspectStartScale;
            if (inspectSource != null && inspectSource.gameObject.activeInHierarchy)
                to = inspectContent.InverseTransformPoint(inspectSource.TransformPoint(inspectSource.rect.center));
            else if (inspectSource != null)
                scale = 0.2f;

            card.localEulerAngles = Vector3.zero;
            inspectSeq = DOTween.Sequence().SetLink(inspectRoot.gameObject)
                .Append(inspectInfoGroup.DOFade(0, 0.15f))
                .Insert(0, inspectDim.DOFade(0, InspectClose))
                .Insert(0, card.DOLocalMove(to, InspectClose).SetEase(Ease.InOutCubic))
                .Insert(0, card.DOScale(scale, InspectClose).SetEase(Ease.InCubic))
                .Insert(0, card.DOLocalRotate(new Vector3(0, 360, 0), InspectClose, RotateMode.FastBeyond360)
                    .SetEase(Ease.InOutSine).OnUpdate(UpdateInspectSide))
                .OnComplete(CloseInspectNow);
        }

        // Закрыть сразу (смена фазы, новый просмотр): убрать копию и вернуть исходную карту.
        void CloseInspectNow()
        {
            inspectSeq?.Kill();
            inspectBreath?.Kill();
            inspectBob?.Kill();
            inspectSeq = null;
            if (inspectSourceGroup != null) inspectSourceGroup.alpha = inspectSourceAlpha;
            inspectSourceGroup = null;
            inspectSource = null;
            inspectCard = null;
            inspectShowFace = null;
            inspectClosing = false;
            if (inspectContent != null) Clear(inspectContent);
            if (inspectRoot != null) inspectRoot.gameObject.SetActive(false);
        }

        // Если фаза сменилась, пока карта открыта, — закрыть без анимации.
        void CloseInspectOnPhase(Phase phase)
        {
            if (inspectCard != null && phase != inspectPhase) CloseInspectNow();
        }
    }
}
