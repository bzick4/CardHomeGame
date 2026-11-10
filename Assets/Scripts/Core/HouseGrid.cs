using System.Collections.Generic;
using UnityEngine;

namespace CardHome
{
    // Погода на ближайшую ночь: меняет правила для всех гостей.
    public enum NightWeather { Clear, Storm, FullMoon }

    public class PlacedRoom
    {
        public RoomType Type { get; init; }
        public Doors Doors { get; internal set; } // уже с учётом поворота
        public int Level { get; internal set; } = 1; // 2 — улучшена такой же картой
        public bool Broken { get; internal set; }   // сломана гостем: не работает, пока не починят
        public RoomDef Def => RoomDatabase.Get(Type);
        public bool Working => !Broken;
    }

    // Сетка дома. (0,0) — левый нижний угол, прихожая внизу по центру.
    public class HouseGrid
    {
        public const int Width = 5;
        public const int Height = 5;
        public const int MaxLevel = Card.MaxLevel;

        public Vector2Int Entrance { get; } = new Vector2Int(2, 0);

        readonly PlacedRoom[,] cells = new PlacedRoom[Width, Height];

        // Состояние дома на эту ночь: погода и кошка, которая поселилась в доме.
        public NightWeather Weather { get; set; }
        public bool Cat { get; set; }

        public HouseGrid()
        {
            Place(Entrance, RoomType.Hall, RoomDatabase.Get(RoomType.Hall).Doors);
        }

        public bool InBounds(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < Width && p.y < Height;

        public PlacedRoom Get(Vector2Int p) => InBounds(p) ? cells[p.x, p.y] : null;

        public void Place(Vector2Int p, RoomType type, Doors doors, int level = 1)
        {
            cells[p.x, p.y] = new PlacedRoom { Type = type, Doors = doors, Level = Mathf.Clamp(level, 1, MaxLevel) };
        }

        // Перенос сохраняет уровень комнаты.
        public void Move(Vector2Int from, Vector2Int to, Doors doors)
        {
            var room = cells[from.x, from.y];
            cells[from.x, from.y] = null;
            room.Doors = doors;
            cells[to.x, to.y] = room;
        }

        public void Remove(Vector2Int p) => cells[p.x, p.y] = null;

        // Ставить можно в пустую клетку, если хотя бы одна дверь совпала с дверью соседа.
        // ignore — клетка, которую считаем пустой (откуда переносим комнату).
        public bool CanPlace(Vector2Int p, Doors doors, Vector2Int? ignore = null)
        {
            if (!InBounds(p)) return false;
            if (Get(p) != null && p != ignore) return false;
            foreach (var d in DoorsUtil.All)
            {
                if (!doors.Has(d)) continue;
                var q = p + DoorsUtil.Offset(d);
                if (q == ignore) continue;
                var n = Get(q);
                if (n != null && n.Doors.Has(DoorsUtil.Opposite(d))) return true;
            }
            return false;
        }

        // Карту такого же типа можно положить на целую комнату, пока та не достигла максимума.
        // Уровни складываются: комната I + карта II = комната III.
        public bool CanUpgrade(Vector2Int p, Card card)
        {
            var r = Get(p);
            return r != null && r.Type == card.Type && r.Def.CanUpgrade && r.Level < MaxLevel && r.Working;
        }

        // Соседи, в которые можно пройти через дверь (двери с обеих сторон).
        public IEnumerable<Vector2Int> Connected(Vector2Int p)
        {
            var room = Get(p);
            if (room == null) yield break;
            foreach (var d in DoorsUtil.All)
            {
                if (!room.Doors.Has(d)) continue;
                var q = p + DoorsUtil.Offset(d);
                var n = Get(q);
                if (n != null && n.Doors.Has(DoorsUtil.Opposite(d))) yield return q;
            }
        }

        // "Рядом" = соприкасаются стенами, двери не важны. Сломанные комнаты не считаются.
        public bool IsAdjacentTo(Vector2Int p, RoomType type) => AdjacentLevel(p, type) > 0;

        // Наибольший уровень целой соседней комнаты этого типа (0 — нет такой).
        public int AdjacentLevel(Vector2Int p, RoomType type)
        {
            int best = 0;
            foreach (var d in DoorsUtil.All)
            {
                var n = Get(p + DoorsUtil.Offset(d));
                if (n != null && n.Type == type && n.Working) best = Mathf.Max(best, n.Level);
            }
            return best;
        }

        // Двери, которые смотрят за край дома, — «двери в стену».
        public int DoorsToVoid()
        {
            int c = 0;
            foreach (var p in AllPositions())
            {
                var r = Get(p);
                foreach (var d in DoorsUtil.All)
                    if (r.Doors.Has(d) && !InBounds(p + DoorsUtil.Offset(d))) c++;
            }
            return c;
        }

        // Сумма уровней целых комнат этого типа.
        public int Count(RoomType type)
        {
            int c = 0;
            foreach (var r in cells)
                if (r != null && r.Type == type && r.Working) c += r.Level;
            return c;
        }

        public IEnumerable<Vector2Int> AllPositions()
        {
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (cells[x, y] != null) yield return new Vector2Int(x, y);
        }

        public int BrokenCount()
        {
            int c = 0;
            foreach (var r in cells)
                if (r != null && r.Broken) c++;
            return c;
        }
    }
}
