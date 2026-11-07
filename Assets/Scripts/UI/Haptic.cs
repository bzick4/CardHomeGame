using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace CardHome
{
    // Вибрация трёх уровней: лёгкая (карта легла, кнопка), средняя (поимка, недовольный гость),
    // сильная (поломка, кража). WebGL — через navigator.vibrate (CardHomeHaptic.jslib),
    // телефонная сборка — Handheld.Vibrate (только средняя и сильная: она длинная), в редакторе — ничего.
    public static class Haptic
    {
        // Настройка «Вибрация» (ProgressData.NoVibration).
        public static bool Enabled { get; set; } = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CardHome_Vibrate(int ms);
        [DllImport("__Internal")] static extern void CardHome_VibratePattern(int a, int b, int c);
#endif

        public static void Light() => Play(10);
        public static void Medium() => Play(25);

        public static void Heavy()
        {
            if (!Enabled) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            CardHome_VibratePattern(30, 40, 30);
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        static void Play(int ms)
        {
            if (!Enabled) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            CardHome_Vibrate(ms);
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (ms >= 20) Handheld.Vibrate();
#endif
        }
    }
}
