using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Экран загрузки перед домом: ночное небо, название дома, полоска и совет.
    // Свой холст поверх всего; строится только из «вечных» элементов (градиент, плашки, шрифт),
    // поэтому сам ничего не загружает. Анимации — DOTween.
    public class LoadingScreen
    {
        static readonly string[] Tips =
        {
            "Ловушка на пути вора поймает его и принесёт золото.",
            "Карта на такую же комнату складывает уровни: I + I = II.",
            "Спальня рядом с подвалом — гостям снятся кошмары.",
            "Кухня рядом со столовой радует гостей вдвое.",
            "Камин греет соседние комнаты, но призраки не любят тепло.",
            "Коснись комнаты на доске, чтобы перенести или повернуть её.",
            "Пустая колода — усталость: каждая лишняя карта стоит репутации.",
            "Ночью у тебя есть ход в ответ на каждого гостя.",
        };

        GameObject root;
        string artPath;
        RectTransform fill;
        CanvasGroup group;

        public static LoadingScreen Show(string title)
        {
            var s = new LoadingScreen();
            s.root = new GameObject("Loading");
            var canvas = s.root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = s.root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            bool portrait = Screen.height >= Screen.width;
            scaler.referenceResolution = portrait ? new Vector2(1080, 1920) : new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = portrait ? 0 : 1;
            s.root.AddComponent<GraphicRaycaster>(); // ловит касания, пока идёт загрузка
            s.group = s.root.AddComponent<CanvasGroup>();

            var rt = (RectTransform)s.root.transform;
            // Картинка экрана загрузки (Art/UI/Loading); надпись и полоса — в спокойной части неба:
            // в вертикали — выше дома, в горизонтали — слева.
            var art = UiArt.Loading(!portrait);
            s.artPath = portrait ? "UI/Loading/Loading" : "UI/Loading/LoadingWide";
            var under = UIKit.Image("Under", rt, RoomDatabase.Hex("141b33"));
            UIKit.Stretch(under.rectTransform);
            var sky = UIKit.Image("Sky", rt, Color.white, art != null ? art : Sprites.NightBackground);
            if (art != null) UIKit.Cover(sky);
            else UIKit.Stretch(sky.rectTransform);

            var rnd = new System.Random(7);
            for (int i = 0; i < (art != null ? 0 : 30); i++)
            {
                var star = UIKit.Image("Star", rt, new Color(1, 1, 0.9f, 0.85f), Sprites.Light);
                star.raycastTarget = false;
                float size = 10 + (float)rnd.NextDouble() * 16;
                UIKit.Place(star.rectTransform, new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble()),
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
                star.DOFade(0.2f, 0.7f + (float)rnd.NextDouble()).SetLoops(-1, LoopType.Yoyo).SetLink(star.gameObject);
            }

            var center = UIKit.Rect("Center", rt);
            var at = art == null ? new Vector2(0.5f, 0.5f) : portrait ? new Vector2(0.5f, 0.6f) : new Vector2(0.3f, 0.56f);
            UIKit.Place(center, at, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 600));
            // Тёмная плашка под надписями — читается поверх картинки.
            var plate = UIKit.Image("Plate", center, new Color(1, 1, 1, 0.8f), UiArt.Plate);
            plate.raycastTarget = false;
            UIKit.Place(plate.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(940, 420));

            var t = UIKit.Outlined(UIKit.Text(center, title.ToUpper(), 64, TextAnchor.MiddleCenter, RoomDatabase.Hex("ffd34d"), FontStyle.Bold), 4);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(900, 120));
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 32;
            t.resizeTextMaxSize = 64;

            // Полоска: тёмная деревянная рамка и золотая заливка.
            var bar = UIKit.Image("Bar", center, RoomDatabase.Hex("4a2e20"), Sprites.Panel);
            UIKit.Place(bar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 48));
            var track = UIKit.Rect("Track", bar.rectTransform);
            UIKit.Stretch(track, 9);
            var fillImg = UIKit.Image("Fill", track, RoomDatabase.Hex("ffb834"), Sprites.Soft);
            s.fill = fillImg.rectTransform;
            s.fill.anchorMin = Vector2.zero;
            s.fill.anchorMax = new Vector2(0, 1);
            s.fill.offsetMin = s.fill.offsetMax = Vector2.zero;

            var tip = UIKit.Outlined(UIKit.Text(center, Tips[Random.Range(0, Tips.Length)], 36, TextAnchor.UpperCenter,
                RoomDatabase.Hex("f7ecd2"), FontStyle.Normal), 2);
            UIKit.Place(tip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(860, 160));

            s.group.alpha = 0;
            s.group.DOFade(1, 0.15f).SetLink(s.root);
            return s;
        }

        public void SetProgress(float p)
        {
            if (fill == null) return;
            fill.DOKill();
            fill.DOAnchorMax(new Vector2(Mathf.Clamp01(p), 1), 0.12f).SetLink(fill.gameObject);
        }

        // Плавно исчезнуть и удалиться.
        public void Hide()
        {
            if (root == null) return;
            var go = root;
            fill.DOKill();
            fill.anchorMax = Vector2.one;
            group.blocksRaycasts = false;
            string path = artPath;
            group.DOFade(0, 0.3f).SetDelay(0.1f).SetLink(go).OnComplete(() =>
            {
                Object.Destroy(go);
                ArtStore.Unload(path); // большой фон больше не нужен — освободить память
            });
            root = null;
        }
    }
}
