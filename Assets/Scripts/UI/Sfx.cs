using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardHome
{
    // Звуки, синтезированные кодом: шаги, стук, скрип, капкан, монеты, поломка, сова.
    // Свои файлы: положи AudioClip в Assets/Resources/Audio/<Имя>.wav (Step, Knock, Creak, Trap,
    // Coin, Break, Owl, Click, Plop, Fanfare, Card) — он заменит синтезированный.
    public static class Sfx
    {
        const int Rate = 44100;
        static AudioSource source;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static readonly System.Random noise = new System.Random(7);

        public static bool Muted { get; set; }

        public static void Step() => Play("Step", 0.35f, 0.12f, MakeStep);
        public static void Knock() => Play("Knock", 0.7f, 0.05f, MakeKnock);
        public static void Creak() => Play("Creak", 0.3f, 0.15f, MakeCreak);
        public static void Trap() => Play("Trap", 0.9f, 0.05f, MakeTrap);
        public static void Coin() => Play("Coin", 0.45f, 0.08f, MakeCoin);
        public static void Break() => Play("Break", 0.8f, 0.1f, MakeBreak);
        public static void Owl() => Play("Owl", 0.5f, 0.03f, MakeOwl);
        public static void Click() => Play("Click", 0.35f, 0.1f, MakeClick);
        public static void Plop() => Play("Plop", 0.6f, 0.1f, MakePlop);
        public static void Fanfare() => Play("Fanfare", 0.6f, 0, MakeFanfare);
        public static void Card() => Play("Card", 0.5f, 0.12f, MakeCard);

        static void Play(string name, float volume, float pitchJitter, Func<float[]> make)
        {
            if (Muted) return;
            if (source == null)
            {
                var go = new GameObject("Sfx");
                UnityEngine.Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
            }
            if (!cache.TryGetValue(name, out var clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("Audio/" + name);
                if (clip == null)
                {
                    var data = make();
                    clip = AudioClip.Create(name, data.Length, 1, Rate, false);
                    clip.SetData(data, 0);
                }
                cache[name] = clip;
            }
            source.pitch = 1 + (float)(noise.NextDouble() * 2 - 1) * pitchJitter;
            source.PlayOneShot(clip, volume);
        }

        // ───── Синтез ─────

        static float[] Buffer(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];
        static float Noise() => (float)(noise.NextDouble() * 2 - 1);
        static float Sin(float freq, float t) => Mathf.Sin(2 * Mathf.PI * freq * t);

        // Глухой удар: низкий тон с падающей частотой и щелчок шума.
        static void Thump(float[] buf, float at, float freq, float decay, float amp)
        {
            int start = (int)(at * Rate);
            float phase = 0;
            for (int i = start; i < buf.Length; i++)
            {
                float t = (i - start) / (float)Rate;
                float env = Mathf.Exp(-t / decay);
                if (env < 0.001f) break;
                phase += 2 * Mathf.PI * freq * (1 + 1.5f * Mathf.Exp(-t * 40)) / Rate;
                buf[i] += (Mathf.Sin(phase) * 0.9f + Noise() * 0.25f * Mathf.Exp(-t * 120)) * env * amp;
            }
        }

        static void Tone(float[] buf, float at, float freq, float attack, float decay, float amp)
        {
            int start = (int)(at * Rate);
            for (int i = start; i < buf.Length; i++)
            {
                float t = (i - start) / (float)Rate;
                float env = Mathf.Min(1, t / Mathf.Max(attack, 1e-4f)) * Mathf.Exp(-t / decay);
                if (t > attack && env < 0.001f) break;
                buf[i] += Sin(freq, t) * env * amp;
            }
        }

        static float[] MakeStep()
        {
            var b = Buffer(0.15f);
            Thump(b, 0, 85, 0.035f, 0.9f);
            return b;
        }

        static float[] MakeKnock()
        {
            var b = Buffer(0.45f);
            Thump(b, 0, 170, 0.04f, 0.9f);
            Thump(b, 0.16f, 160, 0.04f, 0.9f);
            return b;
        }

        // Скрип половицы: пила с «плавающей» частотой и мягкой огибающей.
        static float[] MakeCreak()
        {
            var b = Buffer(0.6f);
            float phase = 0, lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float freq = 140 + 50 * Mathf.Sin(t * 9) + 25 * Mathf.Sin(t * 37);
                phase = (phase + freq / Rate) % 1f;
                float saw = phase * 2 - 1;
                lp += (saw - lp) * 0.18f;
                float env = Mathf.Sin(Mathf.PI * t / 0.6f);
                b[i] = lp * env * env * 0.6f;
            }
            return b;
        }

        // Капкан: резкий щелчок, металлический звон и глухой удар.
        static float[] MakeTrap()
        {
            var b = Buffer(0.6f);
            for (int i = 0; i < Rate * 0.012f; i++) b[i] += Noise() * (1 - i / (Rate * 0.012f));
            Tone(b, 0.005f, 1240, 0.001f, 0.18f, 0.35f);
            Tone(b, 0.005f, 1870, 0.001f, 0.12f, 0.25f);
            Tone(b, 0.005f, 2630, 0.001f, 0.08f, 0.15f);
            Thump(b, 0.01f, 110, 0.06f, 0.6f);
            return b;
        }

        static float[] MakeCoin()
        {
            var b = Buffer(0.35f);
            Tone(b, 0, 1318, 0.002f, 0.06f, 0.5f);
            Tone(b, 0.07f, 1976, 0.002f, 0.14f, 0.5f);
            return b;
        }

        // Поломка: треск шума с затуханием и пара ударов.
        static float[] MakeBreak()
        {
            var b = Buffer(0.6f);
            float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float crackle = noise.NextDouble() < 0.02 ? 1 : 0.3f;
                lp += (Noise() * crackle - lp) * 0.35f;
                b[i] += lp * Mathf.Exp(-t / 0.15f) * 0.9f;
            }
            Thump(b, 0, 70, 0.08f, 0.7f);
            Thump(b, 0.12f, 95, 0.05f, 0.5f);
            return b;
        }

        // Сова: «у-ху» с лёгким вибрато.
        static float[] MakeOwl()
        {
            var b = Buffer(1.1f);
            void Hoo(float at, float len, float freq)
            {
                int start = (int)(at * Rate);
                int n = (int)(len * Rate);
                for (int i = 0; i < n && start + i < b.Length; i++)
                {
                    float t = i / (float)Rate;
                    float env = Mathf.Sin(Mathf.PI * t / len);
                    float f = freq * (1 + 0.015f * Mathf.Sin(t * 30)) * (1 - 0.06f * t / len);
                    b[start + i] += Sin(f, t) * env * env * 0.5f;
                }
            }
            Hoo(0, 0.25f, 410);
            Hoo(0.38f, 0.55f, 390);
            return b;
        }

        static float[] MakeClick()
        {
            var b = Buffer(0.04f);
            Tone(b, 0, 1900, 0.001f, 0.008f, 0.6f);
            return b;
        }

        // «Плюх» комнаты на доску: тон с быстро падающей частотой.
        static float[] MakePlop()
        {
            var b = Buffer(0.2f);
            float phase = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                phase += 2 * Mathf.PI * Mathf.Lerp(620, 240, Mathf.Min(1, t / 0.12f)) / Rate;
                b[i] = Mathf.Sin(phase) * Mathf.Exp(-t / 0.05f) * 0.8f;
            }
            Thump(b, 0, 90, 0.04f, 0.5f);
            return b;
        }

        // Шорох карты: короткий всплеск шума с мягким фильтром.
        static float[] MakeCard()
        {
            var b = Buffer(0.14f);
            float lp = 0, prev = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                lp += (Noise() - lp) * 0.5f;
                float hp = lp - prev;
                prev = lp;
                float env = Mathf.Sin(Mathf.PI * Mathf.Min(1, t / 0.14f));
                b[i] = hp * env * 1.4f;
            }
            return b;
        }

        static float[] MakeFanfare()
        {
            var b = Buffer(1.2f);
            float[] notes = { 523, 659, 784, 1046 };
            for (int i = 0; i < notes.Length; i++)
            {
                Tone(b, i * 0.12f, notes[i], 0.01f, i == notes.Length - 1 ? 0.45f : 0.12f, 0.3f);
                Tone(b, i * 0.12f, notes[i] * 2, 0.01f, 0.08f, 0.08f);
            }
            return b;
        }
    }
}
