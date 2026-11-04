using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardHome
{
    // Прогресс между партиями: открытые дома, звёзды, наследство, настройки.
    [Serializable]
    public class ProgressData
    {
        public bool TutorialDone;
        public int Unlocked;                              // самый дальний открытый уровень
        public List<int> Stars = new List<int>();         // звёзды по уровням (0 — не пройден)
        public List<int> Heirlooms = new List<int>();     // карты-наследство (RoomType)
        public bool Muted;
        public List<int> Backs = new List<int>();         // полученные рубашки (коды CardBacks)
        public int EquippedBack;                          // 0 — обычная «Странник»

        public int StarsAt(int level) => level >= 0 && level < Stars.Count ? Stars[level] : 0;

        public void SetStars(int level, int stars)
        {
            while (Stars.Count <= level) Stars.Add(0);
            Stars[level] = Mathf.Max(Stars[level], stars);
        }
    }

    [Serializable]
    public class RoomSave
    {
        public int X, Y, Type, Doors, Level;
        public bool Broken;
    }

    // Партия, прерванная днём: всё, чтобы продолжить с того же места.
    [Serializable]
    public class RunSave
    {
        public int Level, Night, Gold, Reputation, Actions, Fatigue;
        public List<RoomSave> Rooms = new List<RoomSave>();
        public List<int> Deck = new List<int>(), Discard = new List<int>(), Hand = new List<int>();
        public List<string> OppDeck = new List<string>(), OppHand = new List<string>();
    }

    // Забег режима «Странствие»: карта (по зерну), где стоит игрок, пройденный путь, колода, золото, репутация.
    [Serializable]
    public class WanderSave
    {
        public int Seed;
        public int Row = -1, Col = -1;                    // −1 — ещё не вышли
        public List<int> Path = new List<int>();          // пройденные точки: ряд·10 + номер
        public List<int> Deck = new List<int>();          // коды карт
        public int Gold, Reputation;
        public List<int> SeenEvents = new List<int>();    // случаи, что уже были в этом забеге
    }

    // Хранит всё в виде JSON через Storage (в браузере — localStorage, переживает обновления на itch).
    public static class SaveSystem
    {
        const string ProgressKey = "cardhome_progress";
        const string RunKey = "cardhome_run";
        const string OldTutorialKey = "cardhome_tutorial_done";

        public static ProgressData LoadProgress()
        {
            ProgressData p = null;
            var json = Storage.GetString(ProgressKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { p = JsonUtility.FromJson<ProgressData>(json); }
                catch (Exception e) { Debug.LogWarning("CardHome: битое сохранение прогресса, начинаем заново. " + e.Message); }
            }
            if (p != null && p.Backs == null) p.Backs = new List<int>();
            if (p != null && p.Stars == null) p.Stars = new List<int>();
            if (p != null && p.Heirlooms == null) p.Heirlooms = new List<int>();
            if (p == null)
            {
                p = new ProgressData();
                // Кто прошёл обучение в прошлой версии — не заставляем снова.
                p.TutorialDone = Storage.GetInt(OldTutorialKey, 0) == 1;
            }
            return p;
        }

        public static void SaveProgress(ProgressData p)
        {
            Storage.SetString(ProgressKey, JsonUtility.ToJson(p));
            Storage.Save();
        }

        public static bool HasRun => Storage.HasKey(RunKey);

        const string WanderKey = "cardhome_wander";

        public static bool HasWander => Storage.HasKey(WanderKey);

        public static WanderSave LoadWander()
        {
            var json = Storage.GetString(WanderKey, "");
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<WanderSave>(json); }
            catch (Exception e)
            {
                Debug.LogWarning("CardHome: битое сохранение странствия. " + e.Message);
                ClearWander();
                return null;
            }
        }

        public static void SaveWander(WanderSave w)
        {
            Storage.SetString(WanderKey, JsonUtility.ToJson(w));
            Storage.Save();
        }

        public static void ClearWander()
        {
            Storage.DeleteKey(WanderKey);
            Storage.Save();
        }

        public static RunSave LoadRun()
        {
            var json = Storage.GetString(RunKey, "");
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<RunSave>(json); }
            catch (Exception e)
            {
                Debug.LogWarning("CardHome: битое сохранение партии. " + e.Message);
                ClearRun();
                return null;
            }
        }

        public static void SaveRun(RunSave r)
        {
            Storage.SetString(RunKey, JsonUtility.ToJson(r));
            Storage.Save();
        }

        public static void ClearRun()
        {
            Storage.DeleteKey(RunKey);
            Storage.Save();
        }

        // Полный сброс: прогресс, партия и показанные подсказки.
        public static void ResetAll()
        {
            Storage.DeleteAll();
            Storage.Save();
        }
    }
}
