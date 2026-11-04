using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Карта гостя в стиле мультяшной ККИ: деревянная рамка, пейзаж, персонаж в полный рост,
    // лента с именем, пергамент с описанием, солнце (сколько нужно настроения) и луна (выносливость).
    // Рисуется в размере 300×420 и масштабируется целиком. Умеет переворачиваться рубашкой вверх.
    public class GuestCard
    {
        public const float W = 300, H = 420;

        static readonly Color Wood = RoomDatabase.Hex("8a5a32");
        static readonly Color WindowEdge = RoomDatabase.Hex("3b2416");
        static readonly Color Parchment = RoomDatabase.Hex("f3d79b");
        static readonly Color Ink = RoomDatabase.Hex("3b2a1e");

        public RectTransform Root;   // позиция и масштаб
        RectTransform flipper;       // переворот (по X)
        RectTransform front, back;
        public GuestDef Guest { get; private set; }
        public bool FaceUp { get; private set; }

        public static GuestCard Create(Transform parent, GuestDef guest, bool faceUp, int backCode = CardBacks.DefaultCode)
        {
            var c = new GuestCard();
            c.Root = UIKit.Rect("GuestCard", parent);
            c.Root.sizeDelta = new Vector2(W, H);
            c.flipper = UIKit.Rect("Flip", c.Root);
            UIKit.Stretch(c.flipper);
            UIKit.DropShadow(c.flipper, new Vector2(4, -12), 0.5f);
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
            front = BuildFront(flipper, g);
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

        static RectTransform BuildFront(Transform parent, GuestDef g)
        {
            var front = UIKit.Rect("Front", parent);
            UIKit.Stretch(front);

            var frame = UIKit.Image("Frame", front, Wood, Sprites.Panel);
            frame.raycastTarget = false;
            UIKit.Stretch(frame.rectTransform);

            // Окно с пейзажем и персонажем; всё, что вылезает за окно, обрезается.
            var window = UIKit.Rect("Window", front);
            window.anchorMin = Vector2.zero;
            window.anchorMax = Vector2.one;
            window.offsetMin = new Vector2(16, 128);
            window.offsetMax = new Vector2(-16, -16);
            window.gameObject.AddComponent<RectMask2D>();
            var sky = UIKit.Image("Backdrop", window, Color.white, GuestArt.Backdrop);
            sky.raycastTarget = false;
            UIKit.Cover(sky);
            var hero = UIKit.Image("Hero", window, Color.white, GuestArt.Character(g));
            hero.raycastTarget = false;
            hero.preserveAspect = true;
            UIKit.Place(hero.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -14), new Vector2(262, 262));
            var edge = UIKit.Image("Edge", front, WindowEdge, Sprites.Frame);
            edge.raycastTarget = false;
            edge.rectTransform.anchorMin = Vector2.zero;
            edge.rectTransform.anchorMax = Vector2.one;
            edge.rectTransform.offsetMin = new Vector2(12, 124);
            edge.rectTransform.offsetMax = new Vector2(-12, -12);

            // Лента-стрелка с именем.
            var banner = UIKit.Image("Banner", front, Color.white, GuestArt.Banner);
            banner.raycastTarget = false;
            UIKit.Place(banner.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -20), new Vector2(240, 56));
            var name = UIKit.Text(banner.transform, g.Name, 24, TextAnchor.MiddleLeft, Ink, FontStyle.Bold);
            name.rectTransform.anchorMin = Vector2.zero;
            name.rectTransform.anchorMax = Vector2.one;
            name.rectTransform.offsetMin = new Vector2(26, 6);
            name.rectTransform.offsetMax = new Vector2(-30, -6);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 12;
            name.resizeTextMaxSize = 24;

            // Значок роли в углу: друг, угроза, особый, босс.
            var badge = UIKit.Image("Role", front, RoleColor(g.Role), Sprites.Circle);
            badge.raycastTarget = false;
            UIKit.Place(badge.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(36, -42), new Vector2(72, 72));
            var roleIcon = UIKit.Image("Icon", badge.transform, Color.white, GuestArt.Role(g.Role));
            roleIcon.raycastTarget = false;
            UIKit.Stretch(roleIcon.rectTransform, 12);

            // Пергамент с описанием.
            var paper = UIKit.Image("Paper", front, Parchment, Sprites.Flat);
            paper.raycastTarget = false;
            paper.rectTransform.anchorMin = new Vector2(0, 0);
            paper.rectTransform.anchorMax = new Vector2(1, 0);
            paper.rectTransform.pivot = new Vector2(0.5f, 0);
            paper.rectTransform.offsetMin = new Vector2(30, 14);
            paper.rectTransform.offsetMax = new Vector2(-30, 124);
            var desc = UIKit.Text(paper.transform, g.Description, 17, TextAnchor.MiddleCenter, Ink, FontStyle.Bold);
            desc.rectTransform.anchorMin = Vector2.zero;
            desc.rectTransform.anchorMax = Vector2.one;
            desc.rectTransform.offsetMin = new Vector2(30, 6);
            desc.rectTransform.offsetMax = new Vector2(-30, -6);
            desc.resizeTextForBestFit = true;
            desc.resizeTextMinSize = 10;
            desc.resizeTextMaxSize = 17;

            // Солнце и луна в нижних углах.
            string sun = g.IsThreat ? "+" + g.CatchReward : g.Need.ToString();
            string moon = g.IsThreat ? "♥" : g.Stamina.ToString();
            Corner(front, GuestArt.Sun, sun, new Vector2(0, 0), new Vector2(30, 34));
            Corner(front, GuestArt.Moon, moon, new Vector2(1, 0), new Vector2(-30, 34));
            return front;
        }

        static void Corner(Transform parent, Sprite sprite, string value, Vector2 anchor, Vector2 pos)
        {
            var img = UIKit.Image("Corner", parent, Color.white, sprite);
            img.raycastTarget = false;
            UIKit.Place(img.rectTransform, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(78, 78));
            var t = UIKit.Outlined(UIKit.Text(img.transform, value, 30, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold), 2.5f);
            UIKit.Stretch(t.rectTransform);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        public static Color RoleColor(GuestRole r) =>
            r == GuestRole.Threat ? RoomDatabase.Hex("8f2b2b")
            : r == GuestRole.Special ? RoomDatabase.Hex("c9933a")
            : r == GuestRole.Boss ? RoomDatabase.Hex("6b2b8f")
            : RoomDatabase.Hex("3f7a4a");
    }
}
