using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Схема дверей в углу карты: тёмная плитка, из центра к каждой двери идёт золотой проход.
    // Поворот карты меняет только набор дверей — плитка перерисовывает проходы.
    public class DoorScheme
    {
        static readonly Doors[] Sides = { Doors.N, Doors.E, Doors.S, Doors.W };
        static readonly Color Gold = RoomDatabase.Hex("f2c14e");

        public RectTransform Root { get; }
        readonly Image[] passages = new Image[4];

        public DoorScheme(RectTransform parent, float size)
        {
            Root = UIKit.Rect("Doors", parent);
            Root.sizeDelta = new Vector2(size, size);

            var bg = UIKit.Image("Bg", Root, new Color(0.13f, 0.09f, 0.08f, 0.92f), Sprites.Flat);
            bg.raycastTarget = false;
            bg.pixelsPerUnitMultiplier = 3; // скругление поменьше: плитка маленькая
            UIKit.Stretch(bg.rectTransform);

            float w = size * 0.22f;
            for (int i = 0; i < 4; i++)
            {
                // Проход растёт из центра (pivot внизу) и поворачивается к своей стороне.
                var p = UIKit.Image("Passage", Root, Gold);
                p.raycastTarget = false;
                var rt = p.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(w, size * 0.5f);
                rt.localEulerAngles = new Vector3(0, 0, -90 * i);
                passages[i] = p;
            }

            var hub = UIKit.Image("Hub", Root, Gold, Sprites.Circle);
            hub.raycastTarget = false;
            UIKit.Place(hub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w * 1.7f, w * 1.7f));
        }

        public void Set(Doors doors)
        {
            for (int i = 0; i < 4; i++)
                passages[i].enabled = (doors & Sides[i]) != 0;
        }
    }
}
