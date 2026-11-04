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
        public RectTransform Root;
        public Image Frame;
        public Image Glow;
        public TileView Tile;
        public RoomDef Def;
        public int Level = 1;
        public bool Painted; // карта из нарисованного бланка (Art/RoomCards) — её не красим цветом комнаты
    }

    // Рука карт веером, как в cityIdle: дуга с наклоном, раздача снизу по очереди,
    // карта под пальцем приподнимается, при нажатии — ещё выше и ровно, отпустил — пружинит обратно.
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
        public Func<Vector2?> DealOrigin;

        // Перетаскивание карты на поле: начало, движение (экранная точка) и бросок.
        // Dropped возвращает true, если карту сыграли — тогда она не возвращается в руку.
        public Action<int> DragBegan;
        public Action<int, Vector2> Dragging;
        public Func<int, Vector2, bool> Dropped;
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
        public void Sync(IList<Card> hand, int selected, Doors selectedDoors, bool interactable)
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

        // Карта в руке — для подсветки в обучении.
        public RectTransform CardRect(int index) => index >= 0 && index < cards.Count ? cards[index].Rt : null;

        // Порядок отрисовки: слева направо, поднятые и выбранные — поверх.
        internal void SortSiblings()
        {
            for (int i = 0; i < cards.Count; i++) cards[i].transform.SetSiblingIndex(i);
            foreach (var c in cards)
                if (c.IsUp) c.transform.SetAsLastSibling();
        }

        void AddCard(Card c)
        {
            var parts = build(c, c.Def.Doors);
            var card = parts.Root.gameObject.AddComponent<HandCard>();
            card.Init(this, parts, c, cardH * 0.16f);
            cards.Add(card);
        }

        bool IsPrefix(IList<Card> hand)
        {
            if (hand.Count <= cards.Count) return false;
            for (int i = 0; i < cards.Count; i++)
                if (cards[i].Type != hand[i]) return false;
            return true;
        }

        bool SameTypes(IList<Card> hand)
        {
            if (hand.Count != cards.Count) return false;
            for (int i = 0; i < hand.Count; i++)
                if (cards[i].Type != hand[i]) return false;
            return true;
        }

        int FindRemoved(IList<Card> hand)
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
        public int Index;
        public float BaseX { get; private set; }
        public float BaseY { get; private set; }
        public float BaseRot { get; private set; }
        public bool IsUp => hover || pressed || selected;

        HandView owner;
        float lift;
        bool hover, pressed, selected, dragging, interactable = true;
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
            Parts.Tile.SetRoom(Parts.Def, doors, false, Parts.Level);
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
            if (pressed) return new Vector3(BaseX, BaseY + lift * 1.4f, 0);
            if (hover || selected) return new Vector3(BaseX, BaseY + lift, BaseRot * 0.4f);
            return new Vector3(BaseX, BaseY, BaseRot);
        }

        public void ApplyPose(float duration, float delay = 0, bool force = false)
        {
            if (dragging) return; // карту держит палец
            var pose = Pose();
            if (!force && pose == lastPose) return;
            lastPose = pose;

            tween?.Kill();
            tween = DOTween.Sequence().SetLink(gameObject);
            if (delay > 0) tween.AppendInterval(delay);
            tween.Append(Rt.DOAnchorPos(new Vector2(pose.x, pose.y), duration).SetEase(Ease.OutBack))
                 .Join(Rt.DOLocalRotate(new Vector3(0, 0, pose.z), duration).SetEase(Ease.OutBack))
                 .Join(Rt.DOScale(selected ? 1.05f : 1f, duration).SetEase(Ease.OutBack));
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

        public void OnBeginDrag(PointerEventData e)
        {
            if (!interactable)
            {
                e.pointerDrag = null;
                return;
            }
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
            if (!dragging) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rt.parent, e.position, e.pressEventCamera, out var local);
            // Карта догоняет палец, держась за то место, где её взяли (смещение плавно уходит).
            grabOffset = Vector2.Lerp(grabOffset, new Vector2(0, -Rt.rect.height * 0.35f), 0.15f);
            Rt.anchoredPosition = local + grabOffset;
            owner.Dragging?.Invoke(Index, e.position);
        }

        public void OnEndDrag(PointerEventData e)
        {
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
