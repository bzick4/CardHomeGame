using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardHome
{
    // Части одной карты, чтобы менять её состояние без пересоздания.
    public class CardParts
    {
        public RectTransform Root { get; init; }
        public Image Frame { get; init; }
        public Image Glow { get; init; }
        public TileView Tile { get; init; }     // плитка с дверями (старые карты)
        public DoorScheme Scheme { get; init; } // схема дверей в углу (карта с артом RoomArt)
        public RoomDef Def { get; init; }
        public int Level { get; init; } = 1;
        public bool Painted { get; init; } // карта с артом (RoomArt) — её не красим цветом комнаты
    }

    // Рука карт веером: дуга с наклоном, раздача снизу по очереди,
    // карта под пальцем приподнимается, при нажатии — ещё выше и ровно, отпустил — пружинит обратно.
    // Повёл пальцем вбок — карты волной следуют за ним, отпустил — выбрана карта под пальцем;
    // повёл вверх — карта берётся и несётся на поле.
    // Без Update: всё на событиях указателя и DOTween.
    public class HandView
    {
        const float MaxRotDeg = 7f;
        const float AnimIn = 0.26f;
        const float AnimOut = 0.15f;
        const float Stagger = 0.04f;

        readonly RectTransform root;
        readonly float cardW, cardH, areaW;
        readonly Func<Card, Doors, CardParts> build;
        readonly List<HandCard> cards = new List<HandCard>();

        public event Action<int> CardTapped;

        // Откуда вылетают новые карты (обычно — из колоды справа). null — снизу экрана.
        public Func<Vector2?> DealOrigin { get; internal set; }

        // Перетаскивание карты на поле: начало, движение (экранная точка) и бросок.
        // Dropped возвращает true, если карту сыграли — тогда она не возвращается в руку.
        public Action<int> DragBegan { get; internal set; }
        public Action<int, Vector2> Dragging { get; internal set; }
        public Func<int, Vector2, bool> Dropped { get; internal set; }
        public bool IsDragging { get; internal set; }

        // Над доской карта прячется — вместо неё виден «призрак» комнаты.
        public void SetCardHidden(int index, bool hidden)
        {
            if (index < 0 || index >= cards.Count) return;
            var g = cards[index].Group;
            float target = hidden ? 0 : 1;
            if (Mathf.Approximately(g.alpha, target)) return;
            g.DOKill();
            g.DOFade(target, 0.1f).SetLink(g.gameObject);
        }

        public HandView(RectTransform root, float cardW, float cardH, float areaW, Func<Card, Doors, CardParts> build)
        {
            this.root = root;
            this.cardW = cardW;
            this.cardH = cardH;
            this.areaW = areaW;
            this.build = build;
        }

        // Привести руку к состоянию игры. Сравнивает с тем, что уже лежит на столе:
        // то же самое — только обновить выбор; одна карта пропала — она улетает, остальные съезжаются;
        // иначе — новая раздача.
        public void Sync(IReadOnlyList<Card> hand, int selected, Doors selectedDoors, bool interactable)
        {
            if (!SameTypes(hand))
            {
                int removed = FindRemoved(hand);
                if (removed >= 0)
                {
                    PlayAway(cards[removed]);
                    cards.RemoveAt(removed);
                    Layout(false);
                }
                else if (IsPrefix(hand))
                {
                    // Добор: старые карты остаются, новые прилетают из колоды.
                    int from = cards.Count;
                    for (int i = from; i < hand.Count; i++) AddCard(hand[i]);
                    Layout(true, from);
                }
                else
                {
                    DealOutAll();
                    for (int i = 0; i < hand.Count; i++) AddCard(hand[i]);
                    Layout(true);
                }
            }

            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                c.Index = i;
                bool isSelected = i == selected;
                c.SetState(isSelected, isSelected ? selectedDoors : c.Parts.Def.Doors, interactable);
            }
            SortSiblings();
        }

        internal void Tap(int index) => CardTapped?.Invoke(index);

        // ───── Просмотр руки пальцем ─────
        // Ведёшь пальцем вдоль руки — карты волной поднимаются за ним; отпустил — выбрана карта под пальцем;
        // увёл палец вверх над рукой — эту карту берёшь и несёшь на поле.

        // Дробный номер карты под пальцем (null — просмотра нет). По нему каждая карта считает свою «волну».
        internal float? Focus { get; private set; }
        int FocusIndex => Focus.HasValue ? Mathf.Clamp(Mathf.RoundToInt(Focus.Value), 0, cards.Count - 1) : -1;
        internal float CardWidth => cardW;

        // Палец в координатах руки.
        Vector2 Local(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, e.position, e.pressEventCamera, out var p);
            return p;
        }

        // Палец ушёл выше карт — пора брать карту на поле.
        internal bool AboveHand(PointerEventData e) => Local(e).y > -root.rect.height / 2 + cardH * 1.25f;

        // Обновить положение пальца: какая карта под ним (дробно — для плавной волны).
        internal void Browse(PointerEventData e)
        {
            if (cards.Count == 0) return;
            float x = Local(e).x;
            float f = 0;
            if (cards.Count > 1)
            {
                float step = cards[1].BaseX - cards[0].BaseX;
                f = step > 0.01f ? (x - cards[0].BaseX) / step : 0;
            }
            f = Mathf.Clamp(f, 0, cards.Count - 1);
            int before = FocusIndex;
            Focus = f;
            if (FocusIndex != before)
            {
                SortSiblings();
                Sfx.Click();
            }
            foreach (var c in cards) c.ApplyPose(0.12f, smooth: true);
        }

        // Палец отпущен над рукой — выбрать карту под ним, волна опадает.
        internal void EndBrowse(bool select)
        {
            int index = FocusIndex;
            Focus = null;
            SortSiblings();
            foreach (var c in cards) c.ApplyPose(0.2f);
            if (select && index >= 0 && cards[index].Interactable) Tap(index);
        }

        // Палец ушёл вверх — перетаскивание переходит к карте под пальцем.
        internal void TakeFromBrowse(PointerEventData e)
        {
            int index = FocusIndex;
            Focus = null;
            foreach (var c in cards) c.ApplyPose(0.15f);
            if (index < 0 || !cards[index].Interactable)
            {
                SortSiblings();
                return;
            }
            var card = cards[index];
            e.pointerDrag = card.gameObject; // дальше события перетаскивания получает она
            card.BeginTake(e);
        }

        // Карта в руке — для подсветки в обучении.
        public RectTransform CardRect(int index) => index >= 0 && index < cards.Count ? cards[index].Rt : null;

        // Порядок отрисовки: слева направо, поднятые и выбранные — поверх.
        internal void SortSiblings()
        {
            for (int i = 0; i < cards.Count; i++) cards[i].transform.SetSiblingIndex(i);
            foreach (var c in cards)
                if (c.IsUp) c.transform.SetAsLastSibling();
            int f = FocusIndex;
            if (f >= 0) cards[f].transform.SetAsLastSibling();
        }

        void AddCard(Card c)
        {
            var parts = build(c, c.Def.Doors);
            var card = parts.Root.gameObject.AddComponent<HandCard>();
            card.Init(this, parts, c, cardH * 0.16f);
            cards.Add(card);
        }

        bool IsPrefix(IReadOnlyList<Card> hand)
        {
            if (hand.Count <= cards.Count) return false;
            for (int i = 0; i < cards.Count; i++)
                if (cards[i].Type != hand[i]) return false;
            return true;
        }

        bool SameTypes(IReadOnlyList<Card> hand)
        {
            if (hand.Count != cards.Count) return false;
            for (int i = 0; i < hand.Count; i++)
                if (cards[i].Type != hand[i]) return false;
            return true;
        }

        int FindRemoved(IReadOnlyList<Card> hand)
        {
            if (cards.Count != hand.Count + 1) return -1;
            for (int skip = 0; skip < cards.Count; skip++)
            {
                bool ok = true;
                for (int i = 0, j = 0; i < cards.Count; i++)
                {
                    if (i == skip) continue;
                    if (cards[i].Type != hand[j++]) { ok = false; break; }
                }
                if (ok) return skip;
            }
            return -1;
        }

        // Раскладка веером. dealIn — выезд снизу по очереди, иначе плавный сдвиг на новые места.
        void Layout(bool dealIn, int dealFrom = 0)
        {
            int n = cards.Count;
            float step = n <= 1 ? 0 : Mathf.Min(cardW * 0.9f, (areaW - cardW) / (n - 1));
            float startX = -(n - 1) * step / 2f;
            float halfN = (n - 1) / 2f;
            float arcDrop = cardH * 0.06f;
            var origin = dealIn ? DealOrigin?.Invoke() : null;

            for (int i = 0; i < n; i++)
            {
                float t = halfN > 0 ? (i - halfN) / halfN : 0;
                var c = cards[i];
                c.SetBase(startX + i * step, 10 - arcDrop * t * t, -MaxRotDeg * t);
                if (dealIn && i >= dealFrom)
                {
                    int k = i - dealFrom;
                    c.Rt.anchoredPosition = origin ?? new Vector2(c.BaseX, -cardH - 50);
                    c.Rt.localEulerAngles = Vector3.zero;
                    if (origin.HasValue) c.Rt.localScale = Vector3.one * 0.6f;
                    float delay = k * (origin.HasValue ? 0.16f : Stagger);
                    c.ApplyPose(origin.HasValue ? AnimIn + 0.1f : AnimIn, delay, force: true);
                    if (origin.HasValue) DOVirtual.DelayedCall(delay, Sfx.Card).SetLink(c.gameObject);
                }
                else
                {
                    c.ApplyPose(0.2f, 0, force: true);
                }
            }
        }

        // Сыгранная карта взлетает к доске и тает.
        void PlayAway(HandCard c)
        {
            c.Kill();
            var rt = c.Rt;
            var group = c.Group;
            group.blocksRaycasts = false;
            DOTween.Sequence()
                .Append(rt.DOAnchorPosY(cardH * 0.9f, 0.28f).SetRelative().SetEase(Ease.OutCubic))
                .Join(rt.DOScale(0.6f, 0.28f))
                .Join(rt.DOLocalRotate(Vector3.zero, 0.2f))
                .Join(group.DOFade(0, 0.28f).SetEase(Ease.InQuad))
                .SetLink(rt.gameObject)
                .OnComplete(() => UnityEngine.Object.Destroy(rt.gameObject));
        }

        // Старая рука уезжает вниз по очереди.
        void DealOutAll()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                c.Kill();
                c.Group.blocksRaycasts = false;
                var rt = c.Rt;
                float delay = (cards.Count - 1 - i) * Stagger * 0.5f;
                DOTween.Sequence()
                    .AppendInterval(delay)
                    .Append(rt.DOAnchorPosY(-cardH - 60, AnimOut).SetEase(Ease.InCubic))
                    .SetLink(rt.gameObject)
                    .OnComplete(() => UnityEngine.Object.Destroy(rt.gameObject));
            }
            cards.Clear();
        }
    }

    // Поведение одной карты в руке. Реагирует только на события указателя.
    public class HandCard : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        static readonly Color GlowColor = new Color(1, 0.85f, 0.2f, 0.95f);

        public Card Type { get; private set; }
        public CardParts Parts { get; private set; }
        public RectTransform Rt { get; private set; }
        public CanvasGroup Group { get; private set; }
        public int Index { get; internal set; }
        public float BaseX { get; private set; }
        public float BaseY { get; private set; }
        public float BaseRot { get; private set; }
        public bool IsUp => hover || pressed || selected;
        public bool Interactable => interactable;

        HandView owner;
        float lift;
        bool hover, pressed, selected, dragging, browsing, interactable = true;
        Vector2 grabOffset;
        Sequence tween;
        Vector3 lastPose = new Vector3(float.NaN, 0, 0);

        public void Init(HandView owner, CardParts parts, Card type, float lift)
        {
            this.owner = owner;
            Parts = parts;
            Type = type;
            this.lift = lift;
            Rt = parts.Root;
            Group = gameObject.AddComponent<CanvasGroup>();

            // Невидимая зона касания вниз на высоту подъёма, чтобы поднятая карта не «убегала» из-под пальца.
            var hit = UIKit.Image("Hit", Rt, Color.clear);
            hit.rectTransform.SetAsFirstSibling();
            UIKit.Stretch(hit.rectTransform);
            hit.rectTransform.offsetMin = new Vector2(0, -lift * 1.5f);
        }

        public void SetBase(float x, float y, float rot)
        {
            BaseX = x;
            BaseY = y;
            BaseRot = rot;
        }

        public void SetState(bool isSelected, Doors doors, bool canInteract)
        {
            Parts.Tile?.SetRoom(Parts.Def, doors, false, Parts.Level);
            Parts.Scheme?.Set(doors);
            var baseColor = Parts.Painted ? Color.white : Parts.Def.Color;
            Parts.Frame.color = canInteract ? baseColor : baseColor * new Color(0.75f, 0.75f, 0.75f, 1);

            if (!canInteract)
            {
                hover = pressed = false;
            }
            interactable = canInteract;

            if (isSelected != selected)
            {
                selected = isSelected;
                var glow = Parts.Glow;
                glow.DOKill();
                glow.enabled = selected;
                if (selected)
                {
                    glow.color = GlowColor;
                    glow.DOFade(0.45f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(glow.gameObject);
                }
            }
            ApplyPose(0.15f);
        }

        // Куда должна встать карта в текущем состоянии.
        Vector3 Pose()
        {
            if (owner.Focus.HasValue)
            {
                // Волна: карта под пальцем выше всех и ровно, соседи — чуть ниже и расступаются.
                float d = Index - owner.Focus.Value;
                float w = Mathf.Clamp01(1 - Mathf.Abs(d) / 1.8f);
                w = w * w * (3 - 2 * w);
                float push = Mathf.Clamp(d, -1.5f, 1.5f) * owner.CardWidth * 0.12f * (1 - Mathf.Abs(Mathf.Clamp(d, -1, 1)) * 0.3f);
                return new Vector3(BaseX + push, BaseY + lift * 1.5f * w, BaseRot * (1 - w));
            }
            if (pressed) return new Vector3(BaseX, BaseY + lift * 1.4f, 0);
            if (hover || selected) return new Vector3(BaseX, BaseY + lift, BaseRot * 0.4f);
            return new Vector3(BaseX, BaseY, BaseRot);
        }

        // smooth — для частых обновлений (просмотр пальцем): без «пружины», иначе карты дрожат.
        public void ApplyPose(float duration, float delay = 0, bool force = false, bool smooth = false)
        {
            if (dragging) return; // карту держит палец
            var pose = Pose();
            if (!force && pose == lastPose) return;
            lastPose = pose;

            var ease = smooth ? Ease.OutQuad : Ease.OutBack;
            float focusScale = owner.Focus.HasValue ? 1 + 0.08f * Mathf.Clamp01(1 - Mathf.Abs(Index - owner.Focus.Value) / 1.8f) : 1;
            tween?.Kill();
            tween = DOTween.Sequence().SetLink(gameObject);
            if (delay > 0) tween.AppendInterval(delay);
            tween.Append(Rt.DOAnchorPos(new Vector2(pose.x, pose.y), duration).SetEase(ease))
                 .Join(Rt.DOLocalRotate(new Vector3(0, 0, pose.z), duration).SetEase(ease))
                 .Join(Rt.DOScale((selected ? 1.05f : 1f) * focusScale, duration).SetEase(ease));
        }

        public void Kill() => tween?.Kill();

        public void OnPointerEnter(PointerEventData e)
        {
            if (!interactable) return;
            hover = true;
            owner.SortSiblings();
            ApplyPose(0.12f);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (!hover && !pressed) return;
            hover = false;
            pressed = false;
            owner.SortSiblings();
            ApplyPose(0.12f);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!interactable) return;
            pressed = true;
            owner.SortSiblings();
            ApplyPose(0.07f);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!pressed) return;
            pressed = false;
            ApplyPose(0.2f);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (interactable && !e.dragging) owner.Tap(Index);
        }

        // ───── Перетаскивание на поле ─────

        // Куда повёл палец: вверх — берём карту на поле, вбок — просматриваем руку волной.
        public void OnBeginDrag(PointerEventData e)
        {
            var move = e.position - e.pressPosition;
            if (Mathf.Abs(move.x) > Mathf.Abs(move.y) || move.y < 0 || !interactable)
            {
                browsing = true;
                pressed = hover = false;
                owner.Browse(e);
                return;
            }
            BeginTake(e);
        }

        // Взять карту и нести её на поле.
        internal void BeginTake(PointerEventData e)
        {
            browsing = false;
            dragging = true;
            owner.IsDragging = true;
            pressed = hover = false;
            tween?.Kill();
            lastPose = new Vector3(float.NaN, 0, 0);
            transform.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rt.parent, e.position, e.pressEventCamera, out var local);
            grabOffset = Rt.anchoredPosition - local;
            tween = DOTween.Sequence().SetLink(gameObject)
                .Append(Rt.DOLocalRotate(Vector3.zero, 0.12f))
                .Join(Rt.DOScale(0.85f, 0.15f).SetEase(Ease.OutBack));
            Sfx.Card();
            owner.DragBegan?.Invoke(Index);
        }

        public void OnDrag(PointerEventData e)
        {
            if (browsing)
            {
                if (owner.AboveHand(e))
                {
                    browsing = false;
                    owner.TakeFromBrowse(e);
                }
                else owner.Browse(e);
                return;
            }
            if (!dragging) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rt.parent, e.position, e.pressEventCamera, out var local);
            // Карта догоняет палец, держась за то место, где её взяли (смещение плавно уходит).
            grabOffset = Vector2.Lerp(grabOffset, new Vector2(0, -Rt.rect.height * 0.35f), 0.15f);
            Rt.anchoredPosition = local + grabOffset;
            owner.Dragging?.Invoke(Index, e.position);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (browsing)
            {
                browsing = false;
                owner.EndBrowse(select: true);
                return;
            }
            if (!dragging) return;
            dragging = false;
            owner.IsDragging = false;
            bool played = owner.Dropped != null && owner.Dropped(Index, e.position);
            if (played) return; // карта ушла на поле, рука сама перестроится
            Group.DOKill();
            Group.alpha = 1;
            ApplyPose(0.25f, force: true);
            owner.SortSiblings();
        }
    }
}
