using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Оформление экранов вне партии — теми же элементами, что и сама игра:
    // ночное небо партии, деревянная доска с настоящими комнатами, кнопки как «ПОВЕРНУТЬ» и «НОЧЬ».
    public partial class GameView
    {
        // Шрифты помощников меню. Обычно — вертикальный макет (окна на холсте 1080×1920);
        // горизонтальное главное меню расставлено прямо по экрану и на время постройки ставит сюда L.
        Layout menuFonts;
        Layout M => menuFonts ?? Layout.PortraitLayout;
        int Fm(int size) => Mathf.Max(M.MinFont, size);

        static readonly Color MenuWood = RoomDatabase.Hex("9a6438");
        static readonly Color Rope = RoomDatabase.Hex("3b2a1e");

        // Фон экранов вне партии — иллюстрация Menu/Background(Wide) (дом ночью, Путник на тропинке).
        // Всегда покрывает экран целиком, медленно «дышит». dim — притемнение под интерфейс.
        void ArtBackdrop(RectTransform parent, float dim)
        {
            var art = MenuArt.BackgroundFor(L.Portrait) ?? ArtStore.Require("Menu/Background");
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
            var t = UIKit.Outlined(UIKit.Text(sign, text, size > 0 ? size : M.StatsFont + 44, TextAnchor.MiddleCenter, Gold, FontStyle.Bold), 4);
            UIKit.Stretch(t.rectTransform, 18);
            t.rectTransform.anchoredPosition = new Vector2(0, -4);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = M.StatsFont;
            t.resizeTextMaxSize = size > 0 ? size : M.StatsFont + 44;
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
            var t = UIKit.Text(banner.transform, text, M.BodyFont + 4, TextAnchor.MiddleCenter, InkBrown, FontStyle.Bold);
            UIKit.Stretch(t.rectTransform, 14);
            t.rectTransform.offsetMax = new Vector2(-width * 0.1f, -10);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 20; // подпись на ленте — лучше мельче, но в одну строку
            t.resizeTextMaxSize = M.BodyFont + 4;
            return t;
        }

        // Кнопка меню — та же, что в партии («ПОВЕРНУТЬ», «НОЧЬ»): цветная объёмная плашка с обводкой,
        // белый текст; пояснение — второй строкой помельче.
        class MenuBtn
        {
            public Button Button { get; set; }
            public Image Body { get; set; }
            public Text Label { get; set; }
            public string Title { get; init; }
            public Image Glow { get; set; }
            public Image Icon { get; set; }
            public int HintSize { get; init; } // размер второй строки (пояснения) — из макета, где кнопку создали
        }

        // Значок слева в кнопке меню (Art/UI/Icons); текст сдвигается вправо. null — убрать значок.
        void SetMenuIcon(MenuBtn b, Sprite icon)
        {
            var rt = (RectTransform)b.Button.transform;
            float h = rt.rect.height > 1 ? rt.rect.height : rt.sizeDelta.y;
            if (b.Icon == null)
            {
                b.Icon = UIKit.Image("Icon", b.Body.transform, Color.white);
                b.Icon.raycastTarget = false;
                b.Icon.preserveAspect = true;
                UIKit.Place(b.Icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(h * 0.28f, 4), Vector2.one * h * 0.68f);
            }
            b.Icon.sprite = icon;
            b.Icon.enabled = icon != null;
            b.Label.rectTransform.offsetMin = new Vector2(icon != null ? h * 0.95f : 6, b.Label.rectTransform.offsetMin.y);
        }

        MenuBtn MakeMenuButton(RectTransform parent, Color color, string title, Vector2 pos, Vector2 size, System.Action onClick)
        {
            var b = new MenuBtn { Title = title.ToUpper(), HintSize = Fm(M.BodyFont - 2) };
            b.Button = UIKit.Button(parent, b.Title, color, onClick, M.ButtonFont + 6);
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
                : $"{b.Title}\n<size={b.HintSize}><color=#fff1cf>{hint}</color></size>";
        }

        // Квадратная кнопка-значок, как кнопка меню «≡» в партии.
        Button RoundButton(RectTransform parent, Sprite icon, Vector2 pos, System.Action onClick, float size = 120)
        {
            var round = UiArt.RoundButton;
            bool art = round != Sprites.Circle;
            var b = UIKit.Button(parent, "", art ? Color.white : DarkWood, onClick, M.ButtonFont);
            var rt = (RectTransform)b.transform;
            UIKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            var body = (Image)b.targetGraphic;
            body.sprite = round;
            body.type = Image.Type.Simple;
            body.preserveAspect = true;
            var img = UIKit.Image("Icon", rt, Color.white, icon);
            img.raycastTarget = false;
            img.preserveAspect = true;
            UIKit.Stretch(img.rectTransform, size * 0.19f); // иконка ~62% кнопки
            return b;
        }

        // Окно: деревянная рама и пергамент внутри (как подсказки в партии).
        RectTransform WoodWindow(RectTransform parent, Vector2 size, string title)
        {
            var box = UIKit.Rect("Window", parent);
            UIKit.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            UIKit.DropShadow(box, new Vector2(0, -16), 0.55f);
            var window = UiArt.Window;
            if (window != Sprites.Panel)
            {
                // Нарисованное окно: деревянная рама с пергаментом; заголовок — на красной ленте над верхним краем.
                var body = UIKit.Image("Body", box, Color.white, window);
                UIKit.Stretch(body.rectTransform);
                float w = Mathf.Min(size.x * 0.86f, 700), h = w * 192f / 713f;
                var ribbon = UIKit.Image("Title", box, Color.white, UiArt.WindowTitle);
                ribbon.raycastTarget = false;
                UIKit.Place(ribbon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, h * 0.12f), new Vector2(w, h));
                var t = UIKit.Outlined(UIKit.Text(ribbon.transform, title, M.StatsFont + 6, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold));
                UIKit.Stretch(t.rectTransform, 12);
                t.rectTransform.offsetMin = new Vector2(w * 0.2f, h * 0.22f);
                t.rectTransform.offsetMax = new Vector2(-w * 0.2f, -h * 0.12f);
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = Fm(M.StatsFont - 10);
                t.resizeTextMaxSize = M.StatsFont + 6;
            }
            else
            {
                var body = UIKit.Image("Body", box, DarkWood, Sprites.Panel);
                UIKit.Stretch(body.rectTransform);
                var paper = UIKit.Image("Paper", box, Cream, Sprites.Flat);
                paper.raycastTarget = false;
                paper.rectTransform.anchorMin = Vector2.zero;
                paper.rectTransform.anchorMax = Vector2.one;
                paper.rectTransform.offsetMin = new Vector2(24, 24);
                paper.rectTransform.offsetMax = new Vector2(-24, -120);
                var t = UIKit.Outlined(UIKit.Text(box, title, M.StatsFont + 10, TextAnchor.MiddleCenter, Gold, FontStyle.Bold), 3);
                UIKit.TopBand(t.rectTransform, 18, 96);
            }
            return box;
        }
    }
}
