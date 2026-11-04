using System.Runtime.InteropServices;
using UnityEngine;

namespace CardHome
{
    // Хранилище сохранений.
    // В браузере (WebGL, itch.io) — localStorage под ключами «cardhome.v1.…»: оно привязано к домену,
    // а не к конкретной сборке, поэтому прогресс переживает загрузку новой версии на itch.
    // В редакторе и на телефонах — обычные PlayerPrefs.
    // Старые сохранения (из PlayerPrefs) при первом чтении переносятся в localStorage сами.
    public static class Storage
    {
        // Не меняй префикс — иначе игроки потеряют сохранения.
        const string Prefix = "cardhome.v1.";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern string CardHome_StorageGet(string key);
        [DllImport("__Internal")] static extern void CardHome_StorageSet(string key, string value);
        [DllImport("__Internal")] static extern void CardHome_StorageDelete(string key);
        [DllImport("__Internal")] static extern void CardHome_StorageClear(string prefix);
        static bool Web => true;
#else
        static string CardHome_StorageGet(string key) => null;
        static void CardHome_StorageSet(string key, string value) { }
        static void CardHome_StorageDelete(string key) { }
        static void CardHome_StorageClear(string prefix) { }
        static bool Web => false;
#endif

        public static string GetString(string key, string fallback = "")
        {
            if (!Web) return PlayerPrefs.GetString(key, fallback);
            var value = CardHome_StorageGet(Prefix + key);
            if (value != null) return value;
            // Перенос из старого хранилища (сохранения, сделанные до этой версии).
            if (PlayerPrefs.HasKey(key))
            {
                var old = PlayerPrefs.GetString(key, fallback);
                CardHome_StorageSet(Prefix + key, old);
                return old;
            }
            return fallback;
        }

        public static void SetString(string key, string value)
        {
            if (Web) CardHome_StorageSet(Prefix + key, value);
            else PlayerPrefs.SetString(key, value);
        }

        public static int GetInt(string key, int fallback = 0)
        {
            if (!Web) return PlayerPrefs.GetInt(key, fallback);
            var value = CardHome_StorageGet(Prefix + key);
            if (value != null) return int.TryParse(value, out var i) ? i : fallback;
            if (PlayerPrefs.HasKey(key))
            {
                int old = PlayerPrefs.GetInt(key, fallback);
                CardHome_StorageSet(Prefix + key, old.ToString());
                return old;
            }
            return fallback;
        }

        public static void SetInt(string key, int value)
        {
            if (Web) CardHome_StorageSet(Prefix + key, value.ToString());
            else PlayerPrefs.SetInt(key, value);
        }

        public static bool HasKey(string key)
        {
            if (!Web) return PlayerPrefs.HasKey(key);
            return CardHome_StorageGet(Prefix + key) != null || PlayerPrefs.HasKey(key);
        }

        public static void DeleteKey(string key)
        {
            if (Web) CardHome_StorageDelete(Prefix + key);
            PlayerPrefs.DeleteKey(key);
        }

        // Стереть все сохранения игры (новая игра, сброс прогресса).
        public static void DeleteAll()
        {
            if (Web) CardHome_StorageClear(Prefix);
            PlayerPrefs.DeleteAll();
        }

        // localStorage пишется сразу; PlayerPrefs — по Save().
        public static void Save() => PlayerPrefs.Save();
    }
}
