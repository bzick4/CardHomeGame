using System.Collections.Generic;
using UnityEngine;

namespace CardHome
{
    public enum NodeKind { House, Fair, Boss, Rest, Event, Elite }

    // Точка на карте странствия.
    public class MapNode
    {
        public int Row, Col;
        public NodeKind Kind;
        public int House;   // какой дом (индекс в HouseDatabase.All) — для домов и босса
        public float X;     // положение по горизонтали: −1 … 1
        public readonly List<int> Next = new List<int>(); // в какие точки следующего ряда ведут тропы
    }

    // Карта одного забега: ряды точек снизу вверх, тропы только в следующий ряд, наверху — босс.
    // Строится из числа-зерна, поэтому для сохранения хватает одного seed.
    public class RunMap
    {
        public const int Rows = 8;           // 7 обычных рядов + босс
        // Доли видов точек в обычных рядах, %: дом, случай, ярмарка, опасный дом, привал.
        // Опасные дома и привалы — со 2-го ряда.
        static NodeKind Roll(System.Random rng, int row)
        {
            int roll = rng.Next(100);
            if (row < 2) return roll < 65 ? NodeKind.House : roll < 85 ? NodeKind.Event : NodeKind.Fair;
            if (roll < 42) return NodeKind.House;
            if (roll < 62) return NodeKind.Event;
            if (roll < 76) return NodeKind.Fair;
            if (roll < 90) return NodeKind.Elite;
            return NodeKind.Rest;
        }

        public readonly List<List<MapNode>> Grid = new List<List<MapNode>>();
        public int Seed { get; private set; }

        public MapNode Get(int row, int col) =>
            row >= 0 && row < Grid.Count && col >= 0 && col < Grid[row].Count ? Grid[row][col] : null;

        public static RunMap Generate(int seed)
        {
            var rng = new System.Random(seed);
            var map = new RunMap { Seed = seed };

            for (int r = 0; r < Rows; r++)
            {
                bool boss = r == Rows - 1;
                int count = boss ? 1 : r == 0 ? 3 : rng.Next(2, 5);
                var row = new List<MapNode>();
                for (int c = 0; c < count; c++)
                {
                    var n = new MapNode { Row = r, Col = c };
                    n.X = count == 1 ? 0 : (-1 + 2f * c / (count - 1)) * 0.8f;
                    if (!boss && count > 1) n.X += (float)(rng.NextDouble() * 0.16 - 0.08);

                    if (boss) n.Kind = NodeKind.Boss;
                    else if (r == 0) n.Kind = NodeKind.House;
                    else if (r == Rows - 2) n.Kind = rng.Next(2) == 0 ? NodeKind.Rest : NodeKind.Fair; // перед боссом — отдых или ярмарка
                    else n.Kind = Roll(rng, r);

                    n.House = boss ? HouseDatabase.Count - 1 : rng.Next(HouseDatabase.Count - 1);
                    row.Add(n);
                }
                // В середине пути хотя бы одна ярмарка.
                if (r == 3 && !row.Exists(n => n.Kind == NodeKind.Fair)) row[rng.Next(row.Count)].Kind = NodeKind.Fair;
                // Перед боссом обязательно есть привал.
                if (r == Rows - 2 && !row.Exists(n => n.Kind == NodeKind.Rest)) row[rng.Next(row.Count)].Kind = NodeKind.Rest;
                map.Grid.Add(row);
            }

            // Тропы: каждая точка ведёт к ближайшей в следующем ряду, иногда — ещё к соседней.
            for (int r = 0; r < Rows - 1; r++)
            {
                var cur = map.Grid[r];
                var next = map.Grid[r + 1];
                foreach (var a in cur)
                {
                    var order = new List<MapNode>(next);
                    order.Sort((p, q) => Mathf.Abs(p.X - a.X).CompareTo(Mathf.Abs(q.X - a.X)));
                    Link(a, order[0]);
                    if (order.Count > 1 && Mathf.Abs(order[1].X - a.X) < 0.75f && rng.Next(100) < 45) Link(a, order[1]);
                }
                // В каждую точку должна вести хотя бы одна тропа.
                foreach (var b in next)
                {
                    if (cur.Exists(a => a.Next.Contains(b.Col))) continue;
                    var from = cur[0];
                    foreach (var a in cur)
                        if (Mathf.Abs(a.X - b.X) < Mathf.Abs(from.X - b.X)) from = a;
                    Link(from, b);
                }
            }
            return map;
        }

        static void Link(MapNode a, MapNode b)
        {
            if (!a.Next.Contains(b.Col)) a.Next.Add(b.Col);
        }
    }
}
