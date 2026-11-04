using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Оформление экранов вне партии — теми же элементами, что и сама игра:
    // ночное небо партии, деревянная доска с настоящими комнатами, кнопки как «ПОВЕРНУТЬ» и «НОЧЬ».
    public partial class GameView
    {
        static readonly Color MenuWood = RoomDatabase.Hex("9a6438");
        static readonly Color Rope = RoomDatabase.Hex("3b2a1e");

        // Фон: ночное небо как в партии — градиент, луна, мерцающие звёзды. dim — притемнение под интерфейс.
        void NightBackdrop(RectTransform parent, float dim)
        {
            var sky = UIKit.Image("Sky", parent, Color.white, Sprites.NightBackground);
            sky.raycastTarget = false;
            UIKit.Stretch(sky.rectTransform);

            float k = OverlayScale;
            var moonGlow = UIKit.Image("MoonGlow", parent, new Color(1, 0.95f, 0.75f, 0.25f), Sprites.Light);
            moonGlow.raycastTarget = false;
            UIKit.Place(moonGlow.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-170, -170) * k, Vector2.one * 380 * k);
            var moon = UIKit.Image("Moon", parent, RoomDatabase.Hex("fff3c4"), Sprites.Circle);
            moon.raycastTarget = false;
            UIKit.Place(moon.rectTransform, new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-170, -170) * k, Vector2.one * 120 * k);

            var rnd = new System.Random(42);
            for (int i = 0; i < 46; i++)
            {
                float s = (4 + (float)rnd.NextDouble() * 6) * 3 * k;
                var star = UIKit.Image("Star", parent, new Color(1, 1, 0.9f, 0.9f), Sprites.Light);
                star.raycastTarget = false;
                UIKit.Place(star.rectTransform, new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble()),
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(s, s));
                star.DOFade(0.2f, 0.8f + (float)rnd.NextDouble() * 1.6f).SetDelay((float)rnd.NextDouble() * 2)
                    .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(star.gameObject);
            }

            if (dim > 0)
            {
                var shade = UIKit.Image("Dim", parent, new Color(0.02f, 0.02f, 0.08f, dim));
                shade.raycastTarget = false;
                UIKit.Stretch(shade.rectTransform);
            }
        }

        // Фон экранов вне партии — иллюстрация (дом ночью, Путник на тропинке).
        // Всегда покрывает экран целиком, медленно «дышит». Если картинки нет — ночное небо из партии.
        void ArtBackdrop(RectTransform parent, float dim)
        {
            var art = MenuArt.BackgroundFor(L.Portrait);
            if (art == null)
            {
                NightBackdrop(parent, dim);
                return;
            }
            var under = UIKit.Image("Under", parent, RoomDatabase.Hex("141b33"));
            under.raycastTarget = false;
            UIKit.Stretch(under.rectTransform);

            var img = UIKit.Image("Art", parent, Color.white, art);
            img.raycastTarget = false;
            UIKit.Stretch(img.rectTransform);
            var fit = img.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = art.rect.width / art.rect.height;
            float sx = MenuArt.MirrorBackground ? -1 : 1;
            img.rectTransform.localScale = new Vector3(sx, 1, 1);
            img.rectTransform.DOScale(new Vector3(sx * 1.04f, 1.04f, 1), 14f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
                .SetLink(img.gameObject);

            if (dim > 0)
            {
                var shade = UIKit.Image("Dim", parent, new Color(0.02f, 0.02f, 0.08f, dim));
                shade.raycastTarget = false;
                UIKit.Stretch(shade.rectTransform);
            }
        }

        // Деревянная вывеска на двух верёвках с крупной надписью (как заголовки в игре).
        Text WoodSign(RectTransform parent, string text, float y, float width = 880, float height = 190, int size = 0, float posX = 0)
        {
            var sign = UIKit.Rect("Sign", parent);
            UIKit.Place(sign, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(posX, y), new Vector2(width, height));
            foreach (float x in new[] { -width * 0.35f, width * 0.35f })
            {
                var rope = UIKit.Image("Rope", sign, Rope, Sprites.Pill);
                rope.raycastTarget = false;
                UIKit.Place(rope.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(x, -12), new Vector2(12, 300));
            }
            UIKit.DropShadow(sign, new Vector2(0, -14), 0.5f);
            var plank = UIKit.Image("Plank", sign, DarkWood, Sprites.Panel);
            UIKit.Stretch(plank.rectTransform);
            var inner = UIKit.Image("Inner", sign, Wood, Sprites.Flat);
            UIKit.Stretch(inner.rectTransform, 14);
            foreach (float x in new[] { -width * 0.35f, width * 0.35f })
            {
                var nail = UIKit.Image("Nail", sign, RoomDatabase.Hex("9aa6ad"), Sprites.Circle);
                nail.raycastTarget = false;
                UIKit.Place(nail.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(x, -26), new Vector2(26, 26));
            }
            var t = UIKit.Outlined(UIKit.Text(sign, text, size > 0 ? size : L.StatsFont + 44, TextAnchor.MiddleCenter, Gold, FontStyle.Bold), 4);
            UIKit.Stretch(t.rectTransform, 18);
            t.rectTransform.anchoredPosition = new Vector2(0, -4);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = L.StatsFont;
            t.resizeTextMaxSize = size > 0 ? size : L.StatsFont + 44;
            sign.DOLocalRotate(new Vector3(0, 0, 1f), 2.4f).From(new Vector3(0, 0, -1f)).SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine).SetLink(sign.gameObject);
            return t;
        }

        // Пергаментная лента-стрелка с подписью (как имя на картах гостей).
        Text Ribbon(RectTransform parent, string text, float y, float width = 760, float x = 0)
        {
            var banner = UIKit.Image("Ribbon", parent, Color.white, GuestArt.Banner);
            banner.raycastTarget = false;
            UIKit.Place(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(width, width * 0.19f));
            var t = UIKit.Text(banner.transform, text, L.BodyFont + 4, TextAnchor.MiddleCenter, InkBrown, FontStyle.Bold);
            UIKit.Stretch(t.rectTransform, 14);
            t.rectTransform.offsetMax = new Vector2(-width * 0.1f, -10);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = L.BodyFont - 8;
            t.resizeTextMaxSize = L.BodyFont + 4;
            return t;
        }

        // Кнопка меню — та же, что в партии («ПОВЕРНУТЬ», «НОЧЬ»): цветная объёмная плашка с обводкой,
        // белый текст; пояснение — второй строкой помельче.
        class MenuBtn
        {
            public Button Button;
            public Image Body;
            public Text Label;
            public string Title;
            public Image Glow;
        }

        MenuBtn MakeMenuButton(RectTransform parent, Color color, string title, Vector2 pos, Vector2 size, System.Action onClick)
        {
            var b = new MenuBtn { Title = title.ToUpper() };
            b.Button = UIKit.Button(parent, b.Title, color, onClick, L.ButtonFont + 6);
            var rt = (RectTransform)b.Button.transform;
            UIKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            b.Body = (Image)b.Button.targetGraphic;
            b.Label = b.Button.GetComponentInChildren<Text>();
            b.Label.lineSpacing = 0.9f;

            // Подсветка главной кнопки — как у выбранной карты.
            b.Glow = UIKit.Image("Glow", rt, new Color(1, 0.85f, 0.2f, 0.9f), Sprites.Shadow);
            b.Glow.raycastTarget = false;
            b.Glow.transform.SetAsFirstSibling();
            UIKit.Stretch(b.Glow.rectTransform, -Sprites.ShadowPad - 12);
            b.Glow.DOFade(0.3f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(b.Glow.gameObject);
            b.Glow.enabled = false;
            return b;
        }

        // Главную кнопку подсвечиваем; пояснение — второй строкой.
        void StyleMenuButton(MenuBtn b, bool primary, bool enabled, string hint)
        {
            b.Button.interactable = enabled;
            b.Glow.enabled = primary && enabled;
            b.Label.text = string.IsNullOrEmpty(hint)
                ? b.Title
                : $"{b.Title}\n<size={L.BodyFont - 2}><color=#fff1cf>{hint}</color></size>";
        }

        // Квадратная кнопка-значок, как кнопка меню «≡» в партии.
        Button RoundButton(RectTransform parent, Sprite icon, Vector2 pos, System.Action onClick, float size = 120)
        {
            var b = UIKit.Button(parent, "", DarkWood, onClick, L.ButtonFont);
            var rt = (RectTransform)b.transform;
            UIKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var img = UIKit.Image("Icon", rt, Color.white, icon);
            img.raycastTarget = false;
            img.preserveAspect = true;
            UIKit.Stretch(img.rectTransform, size * 0.2f);
            return b;
        }

        // Окно: деревянная рама и пергамент внутри (как подсказки в партии).
        RectTransform WoodWindow(RectTransform parent, Vector2 size, string title)
        {
            var box = UIKit.Rect("Window", parent);
            UIKit.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            UIKit.DropShadow(box, new Vector2(0, -16), 0.55f);
            var body = UIKit.Image("Body", box, DarkWood, Sprites.Panel);
            UIKit.Stretch(body.rectTransform);
            var paper = UIKit.Image("Paper", box, Cream, Sprites.Flat);
            paper.raycastTarget = false;
            paper.rectTransform.anchorMin = Vector2.zero;
            paper.rectTransform.anchorMax = Vector2.one;
            paper.rectTransform.offsetMin = new Vector2(24, 24);
            paper.rectTransform.offsetMax = new Vector2(-24, -120);
            var t = UIKit.Outlined(UIKit.Text(box, title, L.StatsFont + 10, TextAnchor.MiddleCenter, Gold, FontStyle.Bold), 3);
            UIKit.TopBand(t.rectTransform, 18, 96);
            return box;
        }
    }
}
