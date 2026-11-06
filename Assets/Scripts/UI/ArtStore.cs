using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace CardHome
{
    // Набор, к которому относится картинка, — от него зависит, когда её выгрузить.
    public enum ArtScope
    {
        Global, // значки и интерфейс — живут всю игру
        Level,  // персонажи, бланки карт, рубашки, фоны домов — выгружаются при выходе из дома
        Screen, // фоны меню и карты странствия — выгружаются, когда начинается дом
    }

    // Единое хранилище картинок игры.
    // Картинка берётся из Assets/Resources/Art/<путь>.png, а если файла нет — рисуется кодом.
    // Всё запоминается до выгрузки своего набора; нарисованные кодом текстуры при выгрузке уничтожаются,
    // загруженные из файлов — отпускаются (Unity освобождает память после Resources.UnloadUnusedAssets).
    // Без Update: предзагрузка идёт шагами через DOTween, по несколько картинок за кадр.
    public static class ArtStore
    {
        class Entry
        {
            public Sprite Sprite { get; set; }
            public bool OwnsSprite { get; set; }
            public bool OwnsTexture { get; set; }
            public ArtScope Scope { get; init; }
        }

        static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

        // path — путь внутри Resources/Art без расширения, например "Rooms/Kitchen".
        // paint — как нарисовать, если файла нет; null — картинка необязательная (вернётся null).
        public static Sprite Get(string path, Func<Sprite> paint)
        {
            if (entries.TryGetValue(path, out var e))
            {
                if (e.Sprite != null || paint == null) return e.Sprite;
            }

            var entry = new Entry { Scope = ScopeOf(path) };
            entry.Sprite = Resources.Load<Sprite>("Art/" + path);
            if (entry.Sprite == null)
            {
                // Картинку импортировали как обычную текстуру — делаем спрайт сами.
                var tex = Resources.Load<Texture2D>("Art/" + path);
                if (tex != null)
                {
                    entry.Sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
                    entry.OwnsSprite = true;
                }
            }
            if (entry.Sprite == null && paint != null)
            {
                entry.Sprite = paint();
                entry.OwnsSprite = entry.OwnsTexture = entry.Sprite != null;
            }
            entries[path] = entry;
            return entry.Sprite;
        }

        // К какому набору относится картинка — по папке.
        static ArtScope ScopeOf(string path)
        {
            if (path.StartsWith("Menu/") || path.StartsWith("Wander/") || path.StartsWith("Map/Campaign")) return ArtScope.Screen;
            if (path.StartsWith("Locations/") || path.StartsWith("RoomArt/")) return ArtScope.Level;
            if (path.StartsWith("Backs/Theme") || path.StartsWith("Backs/Frame")) return ArtScope.Level;
            if (path.StartsWith("Cards/"))
            {
                // Персонажи (крупные) — уровень; мелкие детали карт (солнце, значки, звёзды) — всегда.
                string name = path.Substring("Cards/".Length);
                if (GuestDatabase.All.Any(g => g.Id == name)) return ArtScope.Level;
            }
            return ArtScope.Global;
        }

        // Выгрузить одну картинку (например, большой фон экрана загрузки, который больше не нужен).
        public static void Unload(string path)
        {
            if (!entries.TryGetValue(path, out var e)) return;
            if (e.Sprite != null)
            {
                if (e.OwnsTexture && e.Sprite.texture != null) UnityEngine.Object.Destroy(e.Sprite.texture);
                if (e.OwnsSprite) UnityEngine.Object.Destroy(e.Sprite);
            }
            entries.Remove(path);
            DOVirtual.DelayedCall(0.2f, () => Resources.UnloadUnusedAssets(), ignoreTimeScale: true);
        }

        // Выгрузить набор. Звать, когда то, что его показывало, уже уничтожено (или будет уничтожено в этом кадре).
        public static void Release(ArtScope scope)
        {
            var keys = entries.Where(kv => kv.Value.Scope == scope).Select(kv => kv.Key).ToList();
            if (keys.Count == 0) return;
            foreach (var key in keys)
            {
                var e = entries[key];
                if (e.Sprite != null)
                {
                    if (e.OwnsTexture && e.Sprite.texture != null) UnityEngine.Object.Destroy(e.Sprite.texture);
                    if (e.OwnsSprite) UnityEngine.Object.Destroy(e.Sprite);
                }
                entries.Remove(key);
            }
            // Файловые текстуры Unity отпустит, когда на них не останется ссылок:
            // уничтоженные объекты исчезают в конце кадра, поэтому чуть подождём.
            DOVirtual.DelayedCall(0.2f, () => Resources.UnloadUnusedAssets(), ignoreTimeScale: true);
        }

        // Предзагрузка: выполнить шаги по несколько за кадр, сообщая прогресс 0…1. Без Update — цепочкой DOTween.
        public static void Preload(IReadOnlyList<Action> steps, Action<float> progress, Action done, int perFrame = 2)
        {
            int i = 0;
            void Next()
            {
                for (int k = 0; k < perFrame && i < steps.Count; k++, i++)
                {
                    try { steps[i](); }
                    catch (Exception ex) { Debug.LogWarning("CardHome: не удалось подготовить картинку. " + ex.Message); }
                }
                progress?.Invoke(steps.Count == 0 ? 1 : i / (float)steps.Count);
                if (i < steps.Count) DOVirtual.DelayedCall(0.001f, Next, ignoreTimeScale: true);
                else done?.Invoke();
            }
            Next();
        }

        // Что подготовить перед домом: фон дома, персонажи, комнаты и бланки карт, детали карт, рубашки.
        public static List<Action> ForLevel(LevelInfo level, int playerBack)
        {
            var steps = new List<Action>();
            string id = level.House.Id;
            steps.Add(() => Get("Locations/" + id, null));
            steps.Add(() => Get("Locations/" + id + "Wide", null));

            foreach (var g in GuestDatabase.All)
            {
                var guest = g;
                steps.Add(() => GuestArt.Character(guest));
                steps.Add(() => Icons.Guest(guest));
            }
            foreach (RoomType t in Enum.GetValues(typeof(RoomType)))
            {
                var type = t;
                steps.Add(() => Icons.Room(type));
                steps.Add(() => Icons.RoomArt(type));
            }

            steps.Add(() => { var _ = GuestArt.Backdrop; });
            steps.Add(() => { var _ = GuestArt.Banner; });
            steps.Add(() => { var _ = GuestArt.Sun; });
            steps.Add(() => { var _ = GuestArt.Moon; });
            foreach (GuestRole r in Enum.GetValues(typeof(GuestRole)))
            {
                var role = r;
                steps.Add(() => GuestArt.Role(role));
            }
            steps.Add(() => { var _ = Icons.Coin; });
            steps.Add(() => { var _ = Icons.TextPanel; });
            // Эффекты партии (Art/Fx): подсветка клеток, ночь, ловушки, улучшение.
            foreach (var fx in new[] { "TileOk", "TileBad", "MoodUp", "MoodDown", "Shield", "Dust", "Coins", "Heart", "GhostSmoke", "Sparkles" })
            {
                string name = fx;
                steps.Add(() => Get("Fx/" + name, null));
            }
            steps.Add(() => { var _ = Icons.Crack; });

            // Рубашки: соперника этого дома и игрока; окантовки первых уровней карт.
            foreach (int code in new[] { CardBacks.CodeForLevel(level.Index), playerBack })
            {
                int c = code;
                steps.Add(() => CardBacks.ThemeSprite(CardBacks.ThemeOf(c)));
                steps.Add(() => CardBacks.FrameSprite(CardBacks.TierOf(c)));
            }
            for (int tier = 0; tier <= 3; tier++)
            {
                int t = tier;
                steps.Add(() => CardBacks.FrameSprite(t));
                steps.Add(() => CardBacks.FrameArt(t));
            }
            return steps;
        }
    }
}
