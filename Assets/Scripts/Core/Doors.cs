using System;
using UnityEngine;

namespace CardHome
{
    // Двери комнаты. Можно комбинировать: Doors.N | Doors.S.
    [Flags]
    public enum Doors
    {
        None = 0,
        N = 1,
        E = 2,
        S = 4,
        W = 8,
    }

    public static class DoorsUtil
    {
        public static readonly Doors[] All = { Doors.N, Doors.E, Doors.S, Doors.W };

        public static Vector2Int Offset(Doors d)
        {
            switch (d)
            {
                case Doors.N: return Vector2Int.up;
                case Doors.E: return Vector2Int.right;
                case Doors.S: return Vector2Int.down;
                case Doors.W: return Vector2Int.left;
                default: return Vector2Int.zero;
            }
        }

        // Поворот по часовой стрелке: N→E→S→W→N.
        public static Doors RotateCW(Doors d)
        {
            int v = (int)d;
            return (Doors)(((v << 1) | (v >> 3)) & 15);
        }

        public static Doors Opposite(Doors d) => RotateCW(RotateCW(d));

        public static bool Has(this Doors doors, Doors d) => (doors & d) != 0;
    }
}
