using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Клетка дома: пустой «паз» или объёмная комната со стенами, полом и дверями.
    // Умеет показывать уровень, поломку, подсветку и ночной свет в окнах.
    public class TileView
    {
        static readonly Color SlotColor = new Color(0.20f, 0.13f, 0.09f, 0.55f);
        static readonly Color ValidColor = RoomDatabase.Hex("8be36f");
        static readonly Color DoorColor = RoomDatabase.Hex("fff1cf");
        static readonly Color PickColor = new Color(1, 0.85f, 0.2f, 1f);
        static readonly Color UpgradeColor = RoomDatabase.Hex("8be36f");
        static readonly Color LampColor = RoomDatabase.Hex("ffc861");

        enum Glow { None, Picked, Upgrade }

        public RectTransform Root;
        public Button Button;
        public bool IsValid { get; private set; }
        public bool HasRoom { get; private set; }

        readonly Image slot, glow, shadow, body, floor, icon, crack, shade, lamp, badge;
        readonly Text label, badgeText;
        readonly Image[] doors = new Image[4];
        Glow glowMode;
        bool night, broken;

        public TileView(Transform parent, float size, int fontSize, bool clickable, bool showLabel = true)
        {
            Root = UIKit.Rect("Tile", parent);
            Root.sizeDelta = new Vector2(size, size);

            slot = UIKit.Image("Slot", Root, SlotColor, Sprites.Soft);
            UIKit.Stretch(slot.rectTransform, size * 0.04f);

            // Свечение вокруг комнаты: жёлтое — взята для переноса, зелёное — можно улучшить.
            glow = UIKit.Image("Glow", Root, PickColor, Sprites.Shadow);
            glow.raycastTarget = false;
            UIKit.Stretch(glow.rectTransform, -Sprites.ShadowPad - size * 0.08f);
            glow.enabled = false;

            shadow = UIKit.DropShadow(Root, new Vector2(size * 0.03f, -size * 0.06f), 0.5f);

            body = UIKit.Image("Walls", Root, Color.white, Sprites.Panel);
            body.raycastTarget = false;
            UIKit.Stretch(body.rectTransform, 1);

            // Тёмный «пол» внутри — светлый край читается как стены, появляется глубина.
            floor = UIKit.Image("Floor", body.transform, new Color(0, 0, 0, 0.22f), Sprites.Soft);
            floor.raycastTarget = false;
            UIKit.Stretch(floor.rectTransform, size * 0.13f);
            floor.rectTransform.offsetMin += new Vector2(0, size * 0.03f);

            // Иконка комнаты на полу. С подписью — чуть выше, чтобы освободить место под название.
            icon = UIKit.Image("Icon", body.transform, Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            float iconSize = size * (showLabel ? 0.58f : 0.66f);
            UIKit.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, showLabel ? size * 0.08f : 0), new Vector2(iconSize, iconSize));

            // Трещины поверх сломанной комнаты.
            crack = UIKit.Image("Crack", body.transform, Color.white, Icons.Crack);
            crack.raycastTarget = false;
            UIKit.Stretch(crack.rectTransform, size * 0.06f);
            crack.enabled = false;

            // Ночью комната темнеет, а в окне горит тёплый свет.
            shade = UIKit.Image("Shade", Root, new Color(0.02f, 0.02f, 0.08f, 0), Sprites.Soft);
            shade.raycastTarget = false;
            UIKit.Stretch(shade.rectTransform, 1);
            lamp = UIKit.Image("Lamp", Root, new Color(LampColor.r, LampColor.g, LampColor.b, 0), Sprites.Light);
            lamp.raycastTarget = false;
            UIKit.Stretch(lamp.rectTransform, -size * 0.12f);

            for (int i = 0; i < 4; i++)
            {
                var d = DoorsUtil.All[i];
                var img = UIKit.Image("Door" + d, Root, DoorColor, Sprites.Pill);
                img.raycastTarget = false;
                var rt = img.rectTransform;
                bool horizontalEdge = d == Doors.N || d == Doors.S;
                Vector2 edge = d == Doors.N ? new Vector2(0.5f, 1) : d == Doors.S ? new Vector2(0.5f, 0)
                             : d == Doors.E ? new Vector2(1, 0.5f) : new Vector2(0, 0.5f);
                rt.anchorMin = rt.anchorMax = edge;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                float len = size * 0.34f, thick = Mathf.Max(10, size * 0.11f);
                rt.sizeDelta = horizontalEdge ? new Vector2(len, thick) : new Vector2(thick, len);
                doors[i] = img;
            }

            if (showLabel)
            {
                label = UIKit.Outlined(UIKit.Text(Root, "", fontSize, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold), 2);
                label.rectTransform.anchorMin = new Vector2(0, 0);
                label.rectTransform.anchorMax = new Vector2(1, 0);
                label.rectTransform.pivot = new Vector2(0.5f, 0);
                label.rectTransform.offsetMin = new Vector2(size * 0.06f, size * 0.07f);
                label.rectTransform.offsetMax = new Vector2(-size * 0.06f, size * 0.07f + fontSize + 6);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = Mathf.Max(8, fontSize / 2);
                label.resizeTextMaxSize = fontSize;
            }

            // Значок уровня в углу.
            float b = size * 0.3f;
            badge = UIKit.Image("Level", Root, RoomDatabase.Hex("ffd34d"), Sprites.Circle);
            badge.raycastTarget = false;
            UIKit.Place(badge.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-b * 0.28f, -b * 0.28f), new Vector2(b, b));
            badgeText = UIKit.Text(badge.transform, "II", Mathf.RoundToInt(b * 0.5f), TextAnchor.MiddleCenter, RoomDatabase.Hex("3b2a1e"), FontStyle.Bold);
            UIKit.Stretch(badgeText.rectTransform);
            badgeText.rectTransform.offsetMin = new Vector2(b * 0.08f, 0);
            badgeText.rectTransform.offsetMax = new Vector2(-b * 0.08f, 0);
            badgeText.resizeTextForBestFit = true;
            badgeText.resizeTextMinSize = 6;
            badgeText.resizeTextMaxSize = Mathf.RoundToInt(b * 0.5f);
            badge.gameObject.SetActive(false);

            if (clickable)
            {
                // Прозрачная область для касаний на всю клетку.
                var hit = Root.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                Button = Root.gameObject.AddComponent<Button>();
                Button.transition = Selectable.Transition.None;
                Button.targetGraphic = hit;
            }
        }

        public void SetRoom(RoomDef def, Doors d, bool picked = false, int level = 1, bool isBroken = false, bool upgradable = false)
        {
            slot.DOKill();
            IsValid = false;
            HasRoom = true;
            SetGlow(picked ? Glow.Picked : upgradable ? Glow.Upgrade : Glow.None);

            slot.enabled = false;
            shadow.enabled = body.enabled = true;
            broken = isBroken;
            body.color = broken ? def.Color * new Color(0.55f, 0.5f, 0.5f, 1) : def.Color;
            icon.sprite = Icons.Room(def.Type);
            icon.enabled = true;
            icon.color = broken ? new Color(0.6f, 0.6f, 0.6f, 1) : Color.white;
            crack.enabled = broken;
            if (label != null) label.text = broken ? "<color=#ff9a5e>Сломано</color>" : def.Name;
            for (int i = 0; i < 4; i++) doors[i].enabled = d.Has(DoorsUtil.All[i]);
            badge.gameObject.SetActive(level >= 2);
            if (level >= 2)
            {
                // Значок уровня в цвет окантовки: бронза, серебро, золото.
                badgeText.text = HouseDatabase.Roman(level);
                badge.color = CardBacks.Tiers[Card.FrameTier(level)].Main;
            }
            ApplyNight(0.3f);
        }

        public void SetEmpty(bool valid)
        {
            SetGlow(Glow.None);
            HasRoom = false;
            broken = false;
            slot.enabled = true;
            if (valid != IsValid || !valid)
            {
                IsValid = valid;
                slot.DOKill();
                slot.color = valid ? ValidColor : SlotColor;
                // Свободные клетки, куда можно строить, мягко пульсируют.
                if (valid)
                    slot.DOFade(0.45f, 0.45f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(slot.gameObject);
            }
            shadow.enabled = body.enabled = false;
            icon.enabled = false;
            crack.enabled = false;
            badge.gameObject.SetActive(false);
            if (label != null) label.text = "";
            for (int i = 0; i < 4; i++) doors[i].enabled = false;
            ApplyNight(0.3f);
        }

        // Ночь: комната темнеет, в окне загорается мерцающий свет. Сломанные стоят тёмными.
        public void SetNight(bool on, float delay = 0)
        {
            if (night == on) return;
            night = on;
            ApplyNight(0.6f, delay);
        }

        void ApplyNight(float duration, float delay = 0)
        {
            shade.DOKill();
            lamp.DOKill();
            bool dark = night && HasRoom;
            bool lit = dark && !broken;
            shade.DOFade(dark ? 0.5f : 0, duration).SetDelay(delay).SetLink(shade.gameObject);
            if (lit)
            {
                float target = Random.Range(0.45f, 0.6f);
                DOTween.Sequence()
                    .AppendInterval(delay)
                    .Append(lamp.DOFade(target, duration))
                    .Append(lamp.DOFade(target * 0.7f, Random.Range(0.18f, 0.45f)).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine))
                    .SetLink(lamp.gameObject)
                    .SetTarget(lamp);
            }
            else
            {
                lamp.DOFade(0, duration * 0.5f).SetLink(lamp.gameObject);
            }
        }

        // Короткая вспышка, когда комнату ломают.
        public void PlayBreak()
        {
            Root.DOKill(true);
            Root.DOShakeAnchorPos(0.4f, 14, 30).SetLink(Root.gameObject);
            crack.enabled = true;
            crack.transform.DOKill();
            crack.transform.DOScale(1, 0.25f).From(1.6f).SetEase(Ease.OutBack).SetLink(crack.gameObject);
        }

        void SetGlow(Glow mode)
        {
            if (mode == glowMode) return;
            glowMode = mode;
            glow.DOKill();
            glow.enabled = mode != Glow.None;
            Root.DOKill();
            if (mode == Glow.None)
            {
                Root.localScale = Vector3.one;
                return;
            }
            glow.color = mode == Glow.Picked ? PickColor : UpgradeColor;
            glow.DOFade(0.45f, 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(glow.gameObject);
            // Взятая комната приподнимается.
            Root.DOScale(mode == Glow.Picked ? 1.08f : 1f, 0.15f).SetEase(Ease.OutBack).SetLink(Root.gameObject);
        }
    }
}
