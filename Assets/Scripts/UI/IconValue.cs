using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Значок и число рядом (золото, ходы, цена). У UI.Text нет картинок внутри строки,
    // поэтому это пара из Image и Text; ширина подстраивается под текст при каждом Set.
    public class IconValue
    {
        public RectTransform Root { get; }
        readonly Image icon;
        readonly Text label;
        readonly float iconSize, gap;

        public IconValue(Transform parent, Sprite sprite, int fontSize, Color color, float iconSize = 0)
        {
            this.iconSize = iconSize > 0 ? iconSize : fontSize * 1.25f;
            gap = fontSize * 0.2f;
            Root = UIKit.Rect("IconValue", parent);
            Root.sizeDelta = new Vector2(this.iconSize, this.iconSize);

            icon = UIKit.Image("Icon", Root, Color.white, sprite);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            UIKit.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, Vector2.one * this.iconSize);

            label = UIKit.Outlined(UIKit.Text(Root, "", fontSize, TextAnchor.MiddleLeft, color, FontStyle.Bold));
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            UIKit.Place(label.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(this.iconSize + gap, 0), new Vector2(10, fontSize * 1.4f));
        }

        public void Set(string text)
        {
            if (label.text == text) return;
            label.text = text;
            float w = label.preferredWidth;
            label.rectTransform.sizeDelta = new Vector2(w + 4, label.rectTransform.sizeDelta.y);
            Root.sizeDelta = new Vector2(iconSize + gap + w + 4, Root.sizeDelta.y);
        }

        public Color Color
        {
            set => label.color = value;
        }
    }

    // Ряд сердец репутации: полные и пустые значки (Art/UI/Icons/Heart, HeartEmpty).
    public class HeartsRow
    {
        public RectTransform Root { get; }
        readonly float size;
        int shownFull = -1, shownEmpty = -1;

        public HeartsRow(Transform parent, float size)
        {
            this.size = size;
            Root = UIKit.Rect("Hearts", parent);
            Root.sizeDelta = new Vector2(size, size);
        }

        public void Set(int full, int empty)
        {
            full = Mathf.Max(0, full);
            empty = Mathf.Max(0, empty);
            if (full == shownFull && empty == shownEmpty) return;
            shownFull = full;
            shownEmpty = empty;
            for (int i = Root.childCount - 1; i >= 0; i--) Object.Destroy(Root.GetChild(i).gameObject);
            float step = size * 0.86f;
            int n = full + empty;
            for (int i = 0; i < n; i++)
            {
                var img = UIKit.Image("Heart", Root, Color.white, UiArt.Icon(i < full ? "Heart" : "HeartEmpty"));
                img.raycastTarget = false;
                img.preserveAspect = true;
                UIKit.Place(img.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(i * step, 0), Vector2.one * size);
            }
            Root.sizeDelta = new Vector2(Mathf.Max(size, (n - 1) * step + size), size);
        }
    }

    // Ценник: бумажная бирка (Panels/Tag) с монетой и числом. Нет денег — число красное.
    public class PriceTag
    {
        static readonly Color Ok = RoomDatabase.Hex("3b2a1e");
        static readonly Color NoMoney = RoomDatabase.Hex("c8302a");

        public RectTransform Root { get; }
        readonly IconValue value;

        public PriceTag(Transform parent, int fontSize)
        {
            var tag = UIKit.Image("Price", parent, Color.white, UiArt.Tag);
            tag.raycastTarget = false;
            if (tag.sprite != Sprites.Flat) tag.gameObject.AddComponent<SlicedFit>().Height = 108;
            Root = tag.rectTransform;
            value = new IconValue(Root, UiArt.Icon("Gold"), fontSize, Ok);
            var v = value.Root;
            v.anchorMin = v.anchorMax = new Vector2(0.5f, 0.5f);
            v.pivot = new Vector2(0.5f, 0.5f);
            Root.sizeDelta = new Vector2(fontSize * 3.6f, fontSize * 1.9f);
        }

        public void Set(string text, bool canAfford)
        {
            value.Set(text);
            value.Color = canAfford ? Ok : NoMoney;
            float w = value.Root.sizeDelta.x;
            value.Root.anchoredPosition = new Vector2(0, 0);
            Root.sizeDelta = new Vector2(Mathf.Max(Root.sizeDelta.y * 2f, w + Root.sizeDelta.y * 0.9f), Root.sizeDelta.y);
        }
    }
}
