using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Карта гостя в стиле мультяшной ККИ: пейзаж, персонаж в полный рост, лента с именем, пергамент с описанием,
    // солнце (сколько нужно настроения) и луна (выносливость), сверху рамка круга из Art/Frames.
    // Рисуется в размере 300×420 и масштабируется целиком. Умеет переворачиваться рубашкой вверх.
    public class GuestCard
    {
        public const float W = 300, H = 420;
        const float Inset = 8;

        static readonly Color Parchment = RoomDatabase.Hex("f3d79b");
        static readonly Color Ink = RoomDatabase.Hex("3b2a1e");

        public RectTransform Root { get; private set; }   // позиция и масштаб
        RectTransform flipper;       // переворот (по X)
        RectTransform front, back;
        public GuestDef Guest { get; private set; }
        int frameTier; // окантовка круга — одна и та же для лица и рубашки
        bool FaceUp { get; set; }

        public static GuestCard Create(Transform parent, GuestDef guest, bool faceUp, int backCode = CardBacks.DefaultCode)
        {
            var c = new GuestCard();
            c.Root = UIKit.Rect("GuestCard", parent);
            c.Root.sizeDelta = new Vector2(W, H);
            c.flipper = UIKit.Rect("Flip", c.Root);
            UIKit.Stretch(c.flipper);
            UIKit.DropShadow(c.flipper, new Vector2(4, -12), 0.5f);
            c.frameTier = CardBacks.TierOf(backCode);
            c.back = CardBacks.Create(c.flipper, backCode, new Vector2(W, H));
            UIKit.Stretch(c.back);
            if (guest != null) c.SetGuest(guest);
            c.Show(faceUp && guest != null);
            return c;
        }

        public void SetGuest(GuestDef g)
        {
            Guest = g;
            if (front != null) Object.Destroy(front.gameObject);
            front = BuildFront(flipper, g, frameTier);
            Show(FaceUp);
        }

        void Show(bool up)
        {
            FaceUp = up;
            if (front != null) front.gameObject.SetActive(up);
            back.gameObject.SetActive(!up);
        }

        // Переворот: карта сжимается по ширине, меняет сторону и разжимается обратно.
        public Sequence Flip(bool toFront, float duration = 0.36f)
        {
            return DOTween.Sequence()
                .Append(flipper.DOScaleX(0, duration / 2).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    Show(toFront);
                    Sfx.Card();
                })
                .Append(flipper.DOScaleX(1, duration / 2).SetEase(Ease.OutQuad));
        }

        // Сделать карту кнопкой (например, чтобы рассмотреть её крупно).
        public void MakeClickable(System.Action onClick)
        {
            var hit = Root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var b = Root.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = hit;
            b.onClick.AddListener(() => onClick());
        }

        // Лицо карты в координатах 300×420 от левого верхнего угла (как у карт комнат):
        // пейзаж и персонаж в полный рост сверху (ноги уходят под ленту), значок роли слева вверху,
        // солнце и луна у нижнего края картинки, красная лента с именем, пергамент с описанием,
        // поверх всего — рамка круга (та же, что у рубашки этой карты).
        static RectTransform BuildFront(Transform parent, GuestDef g, int tier)
        {
            var front = UIKit.Rect("Front", parent);
            UIKit.Stretch(front);

            // Содержимое сжато на Inset внутрь — чтобы не выглядывать из-под планок рамки.
            var bg = UIKit.Image("Bg", front, RoomDatabase.Hex("2a1f1a"), Sprites.Flat);
            bg.raycastTarget = false;
            Px(bg.rectTransform, Inset, Inset, W - Inset, H - Inset);

            // Окно картинки: всё, что вылезает за него, обрезается.
            var window = UIKit.Rect("Window", front);
            Px(window, Inset, Inset, W - Inset, 272);
            window.gameObject.AddComponent<RectMask2D>();
            var sky = UIKit.Image("Backdrop", window, Color.white, GuestArt.Backdrop);
            sky.raycastTarget = false;
            UIKit.Cover(sky);
            var hero = UIKit.Image("Hero", window, Color.white, GuestArt.Character(g));
            hero.raycastTarget = false;
            hero.preserveAspect = true;
            // Координаты лица, пересчитанные в окно (оно начинается с отступом Inset):
            // голова ниже верхней планки рамки (у богатых рамок там камни и корона), ноги над лентой.
            InWindow(hero.rectTransform, 40, 60, 260, 238, 272);

            // Значок роли: цветной круг с картинкой.
            var badge = UIKit.Image("Role", front, RoleColor(g.Role), Sprites.Circle);
            badge.raycastTarget = false;
            Px(badge.rectTransform, 45, 65, 101, 121);
            var roleIcon = UIKit.Image("Icon", badge.transform, Color.white, GuestArt.Role(g.Role));
            roleIcon.raycastTarget = false;
            roleIcon.preserveAspect = true;
            UIKit.Stretch(roleIcon.rectTransform, 9);

            // Солнце: сколько настроения нужно (у угроз — награда за поимку).
            // Луна: выносливость; у угроз вместо неё сердце — сколько репутации стоит упустить.
            Stat(front, GuestArt.Sun, g.IsThreat ? "+" + g.CatchReward : g.Need.ToString(), 64, 214);
            var heart = ArtStore.Get("UI/Heart", null);
            if (g.IsThreat) Stat(front, heart != null ? heart : GuestArt.Moon, "−" + g.UnhappyPenalty, 236, 214);
            else Stat(front, GuestArt.Moon, g.Stamina.ToString(), 236, 214);

            var panelSprite = Icons.TextPanel;
            var panel = UIKit.Image("Panel", front, panelSprite != null ? Color.white : Parchment, panelSprite != null ? panelSprite : Sprites.Flat);
            panel.raycastTarget = false;
            Px(panel.rectTransform, Inset, 256, W - Inset, H - Inset);

            var ribbonEdge = UIKit.Image("RibbonEdge", front, RoomDatabase.Hex("d9a441"), Sprites.Flat);
            ribbonEdge.raycastTarget = false;
            Px(ribbonEdge.rectTransform, 10, 234, 290, 276);
            var ribbon = UIKit.Image("Ribbon", front, RoomDatabase.Hex("7a2418"), Sprites.Flat);
            ribbon.raycastTarget = false;
            Px(ribbon.rectTransform, 13, 237, 287, 273);
            var name = UIKit.Outlined(UIKit.Text(front, g.Name, 22, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold), 2);
            Px(name.rectTransform, 22, 236, 278, 274);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 12;
            name.resizeTextMaxSize = 22;

            // Описание — в безопасной зоне пергамента (~30 от боков, ~60 от низа).
            var desc = UIKit.Text(front, g.Description, 17, TextAnchor.MiddleCenter, Ink, FontStyle.Bold);
            Px(desc.rectTransform, 30, 280, 270, 360);
            desc.resizeTextForBestFit = true;
            desc.resizeTextMinSize = 10;
            desc.resizeTextMaxSize = 17;

            CardBacks.AddFrame(front, tier);
            return front;
        }

        // Значок-показатель (солнце, луна, сердце) с числом поверх; центр в точке (x, y) лица.
        static void Stat(Transform parent, Sprite sprite, string value, float x, float y)
        {
            var img = UIKit.Image("Stat", parent, Color.white, sprite);
            img.raycastTarget = false;
            img.preserveAspect = true;
            Px(img.rectTransform, x - 30, y - 30, x + 30, y + 30);
            var t = UIKit.Outlined(UIKit.Text(img.transform, value, 26, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold), 2.5f);
            UIKit.Stretch(t.rectTransform);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        // То же, но для элемента внутри окна картинки (окно: Inset…W−Inset по ширине, Inset…windowBottom по высоте).
        static void InWindow(RectTransform rt, float x0, float y0, float x1, float y1, float windowBottom)
        {
            float w = W - Inset * 2, h = windowBottom - Inset;
            rt.anchorMin = new Vector2((x0 - Inset) / w, 1 - (y1 - Inset) / h);
            rt.anchorMax = new Vector2((x1 - Inset) / w, 1 - (y0 - Inset) / h);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // Прямоугольник на лице карты в «пикселях» 300×420 от левого верхнего угла.
        static void Px(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0 / W, 1 - y1 / H);
            rt.anchorMax = new Vector2(x1 / W, 1 - y0 / H);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static Color RoleColor(GuestRole r) =>
            r == GuestRole.Threat ? RoomDatabase.Hex("8f2b2b")
            : r == GuestRole.Special ? RoomDatabase.Hex("c9933a")
            : r == GuestRole.Boss ? RoomDatabase.Hex("6b2b8f")
            : RoomDatabase.Hex("3f7a4a");
    }
}
