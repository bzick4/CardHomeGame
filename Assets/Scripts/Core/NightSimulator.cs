using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CardHome
{
    public enum Outcome { Satisfied, Unhappy, Caught, Robbed, Nothing }

    public class GuestStep
    {
        public Vector2Int Pos { get; init; }
        public int Delta { get; init; }
        public string Text { get; init; } // пусто = гость просто проходит мимо
        public bool Trap { get; init; }   // здесь гостя поймал капкан
    }

    public class GuestResult
    {
        public GuestDef Guest { get; init; }
        public List<GuestStep> Steps { get; } = new List<GuestStep>();
        public int Mood { get; internal set; }
        public Outcome Outcome { get; internal set; }
        public int Gold { get; internal set; }
        public int Reputation { get; internal set; }
        public string Summary { get; internal set; }
        // Последствия: сломанная комната и гость, который вернётся.
        public Vector2Int? Broke { get; internal set; }
        public string BreakText { get; internal set; }
        public bool BecomesRegular { get; internal set; }
    }

    // Чистая логика ночи: считает весь маршрут гостя заранее, без анимации.
    public static class NightSimulator
    {
        public const int BreakChance = 20;

        public static GuestResult Run(HouseGrid grid, GuestDef g, System.Random rng)
        {
            var res = new GuestResult { Guest = g };
            res.Steps.Add(new GuestStep { Pos = grid.Entrance, Text = $"{g.Name} стучится в дверь" });

            if (g.Behaviour == GuestBehaviour.Seeker) RunSeeker(grid, g, rng, res);
            else RunWanderer(grid, g, rng, res);
            return res;
        }

        // Сколько настроения даёт комната этому гостю и почему.
        static int RoomValue(HouseGrid grid, Vector2Int p, GuestDef g, List<string> notes)
        {
            var room = grid.Get(p);
            if (room.Broken)
            {
                notes.Add("сломано");
                return -1;
            }

            int v = g.Like(room.Type);
            if (g.EdgeHater && !room.Def.IsPassage)
            {
                v += 1;
                notes.Add("осмотр +1");
            }

            switch (room.Type)
            {
                case RoomType.Dining:
                    v += 1;
                    notes.Add("уют +1");
                    break;
                case RoomType.Lounge:
                    v += 2;
                    notes.Add("диван +2");
                    break;
                case RoomType.Kitchen:
                    if (v > 0 && grid.IsAdjacentTo(p, RoomType.Dining))
                    {
                        v *= 2;
                        notes.Add("рядом столовая ×2");
                    }
                    break;
                case RoomType.Bedroom:
                    if (grid.IsAdjacentTo(p, RoomType.Cellar))
                    {
                        v -= 3;
                        notes.Add("кошмары из подвала −3");
                    }
                    if (g.Romantic && grid.IsAdjacentTo(p, RoomType.Bath))
                    {
                        v += 2;
                        notes.Add("романтика +2");
                    }
                    break;
                case RoomType.Trap:
                    if (!g.Ethereal)
                    {
                        v -= 3;
                        notes.Add("наступил в капкан −3");
                    }
                    break;
            }

            if (room.Level >= 2 && v > 0)
            {
                v *= room.Level;
                notes.Add($"ур. {HouseDatabase.Roman(room.Level)} ×{room.Level}");
            }

            int warm = grid.AdjacentLevel(p, RoomType.Fireplace);
            if (warm > 0 && room.Type != RoomType.Fireplace && !room.Def.IsPassage)
            {
                int w = g.HatesWarmth ? -warm : warm;
                v += w;
                notes.Add(w > 0 ? $"тепло +{w}" : $"жарко {w}");
            }
            return v;
        }

        static void RunWanderer(HouseGrid grid, GuestDef g, System.Random rng, GuestResult res)
        {
            var visited = new HashSet<Vector2Int> { grid.Entrance };
            var rooms = new List<Vector2Int>(); // где гость побывал (для поломок)
            var cur = grid.Entrance;
            int stamina = g.Stamina;

            while (stamina > 0)
            {
                var frontier = FrontierPaths(grid, cur, visited);
                if (frontier.Count == 0)
                {
                    res.Steps.Add(new GuestStep { Pos = cur, Text = $"{g.Name}: больше некуда идти" });
                    break;
                }

                // Оцениваем все комнаты, куда гость может дойти.
                var options = frontier.Select(kv =>
                {
                    var notes = new List<string>();
                    int value = RoomValue(grid, kv.Key, g, notes);
                    return (target: kv.Key, path: kv.Value, value, notes);
                }).ToList();

                var pick = g.Behaviour == GuestBehaviour.Random
                    ? options[rng.Next(options.Count)]
                    : options.OrderByDescending(o => o.value).ThenBy(o => o.path.Count).First();

                if (g.Behaviour != GuestBehaviour.Random && pick.value < 0)
                {
                    res.Steps.Add(new GuestStep { Pos = cur, Text = $"{g.Name} не хочет идти дальше" });
                    break;
                }

                // Проходим через уже посещённые комнаты без эффекта.
                for (int i = 0; i < pick.path.Count - 1; i++)
                    res.Steps.Add(new GuestStep { Pos = pick.path[i] });

                var room = grid.Get(pick.target);
                string why = pick.notes.Count > 0 ? $" ({string.Join(", ", pick.notes)})" : "";
                string sign = pick.value > 0 ? "+" : "";
                res.Steps.Add(new GuestStep
                {
                    Pos = pick.target,
                    Delta = pick.value,
                    Text = $"{g.Name} → {room.Def.Name}: {sign}{pick.value}{why}",
                });
                res.Mood += pick.value;

                visited.Add(pick.target);
                cur = pick.target;
                if (!room.Def.IsPassage)
                {
                    stamina--;
                    if (room.Working) rooms.Add(pick.target);
                }
            }

            // Инспектор в конце пересчитывает двери, ведущие в стену.
            if (g.EdgeHater)
            {
                int edges = Mathf.Min(grid.DoorsToVoid(), 5);
                if (edges > 0)
                {
                    res.Mood -= edges;
                    res.Steps.Add(new GuestStep
                    {
                        Pos = cur,
                        Delta = -edges,
                        Text = $"{g.Name} нашёл двери в стену: −{edges}",
                    });
                }
            }

            if (res.Mood >= g.Need)
            {
                res.Outcome = Outcome.Satisfied;
                res.Gold = res.Mood * g.GoldMultiplier + g.RegularBonus;
                res.BecomesRegular = g.CanBecomeRegular;
                string bonus = g.RegularBonus > 0 ? $" (+{g.RegularBonus} как постоянный)" : "";
                res.Summary = $"<color=#8fd18f>{g.Name} доволен ({res.Mood}/{g.Need}) и платит {res.Gold} золота{bonus}</color>";
            }
            else
            {
                res.Outcome = Outcome.Unhappy;
                res.Reputation = -g.UnhappyPenalty;
                res.Summary = $"<color=#e08080>{g.Name} недоволен ({res.Mood}/{g.Need}). Репутация −{g.UnhappyPenalty}</color>";
            }

            // Недовольный гость с шансом 20% что-нибудь ломает. Пьяница — даже довольный.
            int chance = Mathf.Max(res.Outcome == Outcome.Unhappy ? BreakChance : 0, g.BreakChance);
            if (rooms.Count > 0 && rng.Next(100) < chance)
            {
                var p = rooms[rng.Next(rooms.Count)];
                res.Broke = p;
                res.BreakText = $"<color=#ff9a5e>{g.Name} сломал комнату «{grid.Get(p).Def.Name}»</color>";
            }
        }

        static void RunSeeker(HouseGrid grid, GuestDef g, System.Random rng, GuestResult res)
        {
            var path = SeekerRoute(grid, g, out int trapIndex);
            string target = RoomDatabase.Get(g.SeekTarget).Name;

            if (path == null)
            {
                res.Outcome = Outcome.Nothing;
                res.Summary = $"{g.Name} не нашёл, где {target}, и ушёл ни с чем";
                return;
            }

            for (int i = 0; i < path.Count; i++)
            {
                var p = path[i];
                var room = grid.Get(p);
                if (i == trapIndex)
                {
                    int reward = g.CatchReward * room.Level;
                    res.Steps.Add(new GuestStep { Pos = p, Text = $"{g.Name} попался в ловушку!", Trap = true });
                    res.Outcome = Outcome.Caught;
                    res.Gold = reward;
                    res.Summary = $"<color=#8fd18f>{g.Name} пойман: +{reward} золота</color>";
                    return;
                }
                res.Steps.Add(new GuestStep { Pos = p, Text = $"{g.Name} крадётся: {room.Def.Name}" });
            }

            res.Outcome = Outcome.Robbed;
            res.Reputation = -1;
            res.Gold = -g.StealAmount;
            res.Summary = g.StealAmount > 0
                ? $"<color=#e08080>{g.Name} добрался до комнаты «{target}»: −{g.StealAmount} золота, репутация −1</color>"
                : $"<color=#e08080>{g.Name} добрался до комнаты «{target}»: репутация −1</color>";

            // С шансом 20% угроза разоряет свою цель.
            if (rng.Next(100) < BreakChance)
            {
                res.Broke = path[path.Count - 1];
                res.BreakText = g.SeekTarget == RoomType.Kitchen
                    ? $"<color=#ff9a5e>{g.Name} разорил кухню</color>"
                    : $"<color=#ff9a5e>{g.Name} разгромил комнату «{target}»</color>";
            }
        }

        // Маршрут угрозы к цели (без прихожей) и где на нём сработает ловушка (−1 — нигде).
        // Нужен и для ночи, и для подсказки днём.
        public static List<Vector2Int> SeekerRoute(HouseGrid grid, GuestDef g, out int trapIndex)
        {
            trapIndex = -1;
            var path = ShortestPathTo(grid, grid.Entrance, g.SeekTarget);
            if (path == null) return null;
            if (!g.Trappable) return path;
            for (int i = 0; i < path.Count; i++)
            {
                var r = grid.Get(path[i]);
                if (r.Type == RoomType.Trap && r.Working)
                {
                    trapIndex = i;
                    break;
                }
            }
            return path;
        }

        // Все непосещённые комнаты, до которых можно дойти через посещённые.
        // Значение — путь от текущей комнаты (без неё) до цели включительно.
        static Dictionary<Vector2Int, List<Vector2Int>> FrontierPaths(HouseGrid grid, Vector2Int start, HashSet<Vector2Int> visited)
        {
            var result = new Dictionary<Vector2Int, List<Vector2Int>>();
            var parent = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var q in grid.Connected(p))
                {
                    if (parent.ContainsKey(q)) continue;
                    parent[q] = p;
                    if (visited.Contains(q)) queue.Enqueue(q);
                    else result[q] = BuildPath(parent, start, q);
                }
            }
            return result;
        }

        // Кратчайший путь до целой комнаты нужного типа.
        static List<Vector2Int> ShortestPathTo(HouseGrid grid, Vector2Int start, RoomType type)
        {
            var parent = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                var r = grid.Get(p);
                if (r.Type == type && r.Working) return BuildPath(parent, start, p);
                foreach (var q in grid.Connected(p))
                {
                    if (parent.ContainsKey(q)) continue;
                    parent[q] = p;
                    queue.Enqueue(q);
                }
            }
            return null;
        }

        static List<Vector2Int> BuildPath(Dictionary<Vector2Int, Vector2Int> parent, Vector2Int start, Vector2Int end)
        {
            var path = new List<Vector2Int>();
            for (var p = end; p != start; p = parent[p]) path.Add(p);
            path.Reverse();
            return path;
        }
    }
}
