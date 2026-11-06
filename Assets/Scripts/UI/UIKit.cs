using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardHome
{
    // Мелкие помощники, чтобы собирать uGUI из кода в мультяшном стиле.
    public static class UIKit
    {
        public static readonly Color Ink = new Color(0.10f, 0.07f, 0.06f, 1);

        static Font font;

        // Шрифт игры — строго Assets/Fonts/NotoSans (ссылка на него — Assets/Resources/GameFont).
        public static Font Font
        {
            get
            {
                if (font != null) return font;
                var reference = Resources.Load<GameFont>("GameFont");
                if (reference != null) font = reference.Font;
                if (font == null)
                {
                    Debug.LogError("CardHome: не найден шрифт игры. Проверь Assets/Resources/GameFont → поле Font (Assets/Fonts/NotoSans).");
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return font;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Image(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sprite.border.sqrMagnitude > 0 ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            }
            return img;
        }

        public static Text Text(Transform parent, string text, int size, TextAnchor anchor, Color color, FontStyle style = FontStyle.Normal)
        {
            var t = Rect("Text", parent).gameObject.AddComponent<SafeText>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = color;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        // Белый текст с толстой тёмной обводкой — как в мультяшных ККИ.
        public static Text Outlined(Text t, float thickness = 3f)
        {
            thickness = Mathf.Max(3f, thickness); // тоньше 3 на телефоне обводка теряется
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = Ink;
            o.effectDistance = new Vector2(thickness, -thickness);
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = Ink;
            s.effectDistance = new Vector2(0, -thickness * 1.6f);
            return t;
        }

        // Тень под объектом. Добавлять первой, чтобы она была позади.
        public static Image DropShadow(Transform parent, Vector2 offset, float alpha = 0.45f, Sprite sprite = null)
        {
            var img = Image("Shadow", parent, new Color(0, 0, 0, alpha), sprite ?? Sprites.Shadow);
            img.raycastTarget = false;
            Stretch(img.rectTransform, -Sprites.ShadowPad);
            img.rectTransform.anchoredPosition += offset;
            return img;
        }

        // Пухлая кнопка: тень, объёмная плашка, текст с обводкой, проседает при нажатии.
        public static Button Button(Transform parent, string label, Color color, Action onClick, int fontSize = 22)
        {
            var root = Rect("Button", parent);
            DropShadow(root, new Vector2(0, -8));
            var body = Image("Body", root, color, Sprites.Panel);
            Stretch(body.rectTransform);

            var b = root.gameObject.AddComponent<Button>();
            b.targetGraphic = body;
            var colors = b.colors;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            b.colors = colors;
            b.onClick.AddListener(() => { Sfx.Click(); onClick(); });
            root.gameObject.AddComponent<PressScale>();

            var t = Outlined(Text(body.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold));
            Stretch(t.rectTransform, 6);
            t.rectTransform.anchoredPosition = new Vector2(0, 3);
            return b;
        }

        // Картинка заполняет родителя целиком без искажений (лишнее по краям обрезает маска родителя).
        public static void Cover(Image img)
        {
            Stretch(img.rectTransform);
            if (img.sprite == null) return;
            var fit = img.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = img.sprite.rect.width / img.sprite.rect.height;
        }

        public static void Stretch(RectTransform rt, float pad = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // Полоса на всю ширину родителя: отступ сверху и высота.
        public static void TopBand(RectTransform rt, float top, float height, float pad = 24)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(pad, -top - height);
            rt.offsetMax = new Vector2(-pad, -top);
        }
    }

    // Кнопка чуть сжимается под пальцем и пружинит обратно.
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float Pressed { get; set; } = 0.92f;
        Selectable selectable;
        Vector3 baseScale;
        bool down;

        void Awake() => selectable = GetComponent<Selectable>();

        public void OnPointerDown(PointerEventData e)
        {
            if (selectable != null && !selectable.interactable) return;
            if (!down) baseScale = transform.localScale;
            down = true;
            transform.DOKill();
            transform.DOScale(baseScale * Pressed, 0.08f).SetEase(Ease.OutQuad).SetLink(gameObject);
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        void Release()
        {
            if (!down) return;
            down = false;
            transform.DOKill();
            transform.DOScale(baseScale, 0.25f).SetEase(Ease.OutBack).SetLink(gameObject);
        }
    }
}
