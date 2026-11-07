using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // «Сочность» (game feel) на DOTween: переиспользуемые отклики — подскок, сплющивание, тряска, вспышка,
    // всплывающие числа, полёт монет, докрутка счётчика, стоп-кадр и замедление.
    // Правила: без Update и корутин; каждый твин с SetLink; повторный вызов того же эффекта на той же цели
    // сначала завершает предыдущий (DOKill с complete по id) — эффекты не копятся, элементы возвращаются на место.
    // «Меньше эффектов» (Reduced): без тряски экрана, стоп-кадров, замедлений и мигания, остальное — вполсилы.
    public static class Juice
    {
        // Все силы и длительности — здесь, чтобы крутить в одном месте.
        public static class Tuning
        {
            public static float PunchStrength = 0.12f, PunchTime = 0.25f;
            public static float SquashAmount = 0.15f, SquashTime = 0.3f;
            public static float ShakeStrength = 18f, ShakeTime = 0.35f;
            public static int ShakeVibrato = 22;
            public static float FlashTime = 0.18f;
            public static float FlickerTime = 0.3f;
            public static float PulseScale = 1.06f, PulsePeriod = 0.9f;
            public static float PopTime = 0.3f;
            public static float FloatRise = 90f, FloatTime = 1.0f;
            public static float FlyArc = 160f;
            public static float HitStopScale = 0.05f, HitStopTime = 0.08f;
            public static float SlowMoScale = 0.35f, SlowMoTime = 0.6f;
            public static float ReducedFactor = 0.5f; // сила эффектов при «Меньше эффектов»
        }

        // «Меньше эффектов» (настройка игрока, сохраняется в ProgressData).
        public static bool Reduced { get; set; }

        // Корень игрового экрана для тряски (не Canvas: тогда тряслись бы и оверлеи).
        public static RectTransform ScreenRoot { get; set; }

        static float K => Reduced ? Tuning.ReducedFactor : 1f;

        // Сила эффекта с учётом «Меньше эффектов» — для твинов, собранных на месте.
        public static float Strength(float v) => v * K;
        static string Id(string effect, UnityEngine.Object target) => effect + target.GetInstanceID();

        static void KillSame(string id) => DOTween.Kill(id, true);

        // ───────── Масштаб ─────────

        // Подскок вокруг текущего масштаба.
        public static Tween Punch(RectTransform t, float strength = -1, float dur = -1)
        {
            if (t == null) return null;
            string id = Id("punch", t);
            KillSame(id);
            float s = (strength < 0 ? Tuning.PunchStrength : strength) * K;
            return t.DOPunchScale(Vector3.one * s, dur < 0 ? Tuning.PunchTime : dur, 6, 0.6f).SetId(id).SetLink(t.gameObject);
        }

        // Сплющивание: ниже и шире, затем пружиной обратно.
        public static Tween Squash(RectTransform t, float amount = -1, float dur = -1)
        {
            if (t == null) return null;
            string id = Id("squash", t);
            KillSame(id);
            float a = (amount < 0 ? Tuning.SquashAmount : amount) * K;
            float d = dur < 0 ? Tuning.SquashTime : dur;
            var baseScale = t.localScale;
            return DOTween.Sequence().SetId(id).SetLink(t.gameObject)
                .Append(t.DOScale(new Vector3(baseScale.x * (1 + a), baseScale.y * (1 - a), baseScale.z), d * 0.25f).SetEase(Ease.OutQuad))
                .Append(t.DOScale(baseScale, d * 0.75f).SetEase(Ease.OutElastic))
                .OnKill(() => { if (t != null) t.localScale = baseScale; });
        }

        // Пульс («дыхание») до остановки: StopPulse вернёт масштаб.
        public static Tween Pulse(RectTransform t, float scale = -1, float period = -1)
        {
            if (t == null) return null;
            string id = Id("pulse", t);
            if (DOTween.IsTweening(id)) return null; // уже пульсирует
            float s = 1 + ((scale < 0 ? Tuning.PulseScale : scale) - 1) * K;
            var baseScale = t.localScale;
            return t.DOScale(baseScale * s, (period < 0 ? Tuning.PulsePeriod : period) / 2).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetId(id).SetLink(t.gameObject)
                .OnKill(() => { if (t != null) t.localScale = baseScale; });
        }

        public static void StopPulse(RectTransform t)
        {
            if (t != null) DOTween.Kill(Id("pulse", t));
        }

        // Появление и исчезновение «с пружинкой».
        public static Tween PopIn(RectTransform t, float dur = -1, float to = 1)
        {
            if (t == null) return null;
            string id = Id("pop", t);
            KillSame(id);
            t.localScale = Vector3.one * to * 0.6f;
            return t.DOScale(to, dur < 0 ? Tuning.PopTime : dur).SetEase(Ease.OutBack).SetId(id).SetLink(t.gameObject);
        }

        public static Tween PopOut(RectTransform t, float dur = -1, Action done = null)
        {
            if (t == null) return null;
            string id = Id("pop", t);
            KillSame(id);
            return t.DOScale(0.6f * t.localScale.x, (dur < 0 ? Tuning.PopTime : dur) * 0.7f).SetEase(Ease.InBack).SetId(id).SetLink(t.gameObject)
                .OnComplete(() => done?.Invoke());
        }

        // ───────── Положение ─────────

        // Тряска с точным возвратом на исходное место.
        public static Tween Shake(RectTransform t, float strength = -1, float dur = -1, int vibrato = -1)
        {
            if (t == null) return null;
            string id = Id("shake", t);
            KillSame(id);
            var basePos = t.anchoredPosition;
            return t.DOShakeAnchorPos(dur < 0 ? Tuning.ShakeTime : dur, (strength < 0 ? Tuning.ShakeStrength : strength) * K,
                    vibrato < 0 ? Tuning.ShakeVibrato : vibrato, 90, false, true)
                .SetId(id).SetLink(t.gameObject)
                .OnKill(() => { if (t != null) t.anchoredPosition = basePos; });
        }

        // Тряска всего игрового экрана — только для сильных событий; при «Меньше эффектов» её нет.
        public static Tween ScreenShake(float strength, float dur)
        {
            if (Reduced || ScreenRoot == null) return null;
            return Shake(ScreenRoot, strength, dur);
        }

        // Полёт по дуге к цели с уменьшением; на прибытии — подскок цели и onArrive. destroy — убрать летящий элемент.
        public static Tween FlyTo(RectTransform item, RectTransform target, float dur, Action onArrive = null, bool destroy = true)
        {
            if (item == null || target == null) return null;
            Vector3 from = item.position;
            Vector3 to = target.TransformPoint(target.rect.center);
            var canvas = item.GetComponentInParent<Canvas>();
            float unit = canvas != null ? canvas.transform.lossyScale.y : 1f;
            Vector3 mid = Vector3.Lerp(from, to, 0.45f) + Vector3.up * Tuning.FlyArc * unit;
            return DOTween.Sequence().SetLink(item.gameObject)
                .Append(item.DOPath(new[] { mid, to }, dur, PathType.CatmullRom).SetEase(Ease.InOutSine))
                .Join(item.DOScale(item.localScale * 0.6f, dur).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    Punch(target, 0.15f, 0.2f);
                    onArrive?.Invoke();
                    if (destroy && item != null) UnityEngine.Object.Destroy(item.gameObject);
                });
        }

        // ───────── Цвет ─────────

        // Вспышка цветом и возврат к исходному.
        public static Tween Flash(Graphic g, Color c, float dur = -1)
        {
            if (g == null) return null;
            string id = Id("flash", g);
            KillSame(id);
            var baseColor = g.color;
            float d = dur < 0 ? Tuning.FlashTime : dur;
            var to = Color.Lerp(baseColor, c, K);
            return DOTween.Sequence().SetId(id).SetLink(g.gameObject)
                .Append(g.DOColor(to, d * 0.35f))
                .Append(g.DOColor(baseColor, d * 0.65f))
                .OnKill(() => { if (g != null) g.color = baseColor; });
        }

        // Мигание альфой (предупреждение). При «Меньше эффектов» — нет.
        public static Tween Flicker(Graphic g, int times = 3, float dur = -1)
        {
            if (g == null || Reduced) return null;
            string id = Id("flicker", g);
            KillSame(id);
            float a = g.color.a;
            return g.DOFade(a * 0.2f, (dur < 0 ? Tuning.FlickerTime : dur) / (times * 2)).SetLoops(times * 2, LoopType.Yoyo)
                .SetId(id).SetLink(g.gameObject)
                .OnKill(() => { if (g != null) { var c = g.color; c.a = a; g.color = c; } });
        }

        // ───────── Числа ─────────

        // Значок + текст вылетает вверх с подскоком и тает (например, «+5» с монетой).
        public static RectTransform FloatText(RectTransform parent, Vector2 pos, string text, Color c, Sprite icon = null, int fontSize = 40)
        {
            if (parent == null) return null;
            var root = UIKit.Rect("Float", parent);
            root.anchoredPosition = pos;
            root.sizeDelta = new Vector2(10, fontSize * 1.4f);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var value = new IconValue(root, icon, fontSize, c, icon != null ? fontSize * 1.2f : 0.01f);
            value.Set(text);
            var v = value.Root;
            v.anchorMin = v.anchorMax = v.pivot = new Vector2(0.5f, 0.5f);
            v.anchoredPosition = Vector2.zero;
            root.SetAsLastSibling();

            float rise = Tuning.FloatRise, life = Tuning.FloatTime;
            DOTween.Sequence().SetLink(root.gameObject)
                .Append(root.DOScale(1.15f, 0.15f).From(0.4f).SetEase(Ease.OutBack))
                .Join(root.DOAnchorPosY(pos.y + rise, life).SetEase(Ease.OutCubic))
                .Append(root.DOScale(1f, 0.2f))
                .Insert(life * 0.6f, group.DOFade(0, life * 0.4f))
                .OnComplete(() => UnityEngine.Object.Destroy(root.gameObject));
            return root;
        }

        // Число «докручивается» от from до to, в конце — подскок.
        public static Tween CountTo(Text label, int from, int to, float dur, string format = "{0}")
        {
            if (label == null) return null;
            string id = Id("count", label);
            KillSame(id);
            int v = from;
            label.text = string.Format(format, from);
            return DOTween.To(() => v, x => { v = x; label.text = string.Format(format, v); }, to, dur)
                .SetEase(Ease.OutQuad).SetId(id).SetLink(label.gameObject)
                .OnComplete(() => Punch(label.rectTransform, 0.18f, 0.22f));
        }

        // ───────── Время ─────────
        // Меняют только DOTween.timeScale (не Time.timeScale). Возврат отсчитывается по реальному времени
        // (Time.realtimeSinceStartup) в обратном вызове твина — так длительность точна при любом timeScale.
        // Логика ночи не зависит от скорости: исходы гостей считаются заранее, анимация лишь показывает их.

        static Tween clock;
        static float hitEnd, slowStart, slowEnd, slowScale = 1;

        public static void HitStop(float dur = -1)
        {
            if (Reduced) return;
            hitEnd = Mathf.Max(hitEnd, Time.realtimeSinceStartup + (dur < 0 ? Tuning.HitStopTime : dur));
            RunClock();
        }

        public static void SlowMo(float scale = -1, float dur = -1)
        {
            if (Reduced) return;
            slowScale = scale < 0 ? Tuning.SlowMoScale : scale;
            slowStart = Time.realtimeSinceStartup;
            slowEnd = slowStart + (dur < 0 ? Tuning.SlowMoTime : dur);
            RunClock();
        }

        // Вернуть нормальную скорость сразу (смена экрана, выход).
        public static void ResetTime()
        {
            hitEnd = slowEnd = 0;
            clock?.Kill();
            clock = null;
            DOTween.timeScale = 1;
        }

        static void RunClock()
        {
            if (clock != null && clock.IsActive()) { Tick(); return; }
            clock = DOVirtual.Float(0, 1, 3600f, _ => { }).SetUpdate(true).OnUpdate(Tick);
            Tick();
        }

        static void Tick()
        {
            float now = Time.realtimeSinceStartup;
            float s = 1;
            if (now < slowEnd)
            {
                // Быстро вниз (0.1 с), держим, плавно вверх (последние 0.25 с).
                float t0 = now - slowStart, left = slowEnd - now;
                float k = Mathf.Min(Mathf.Clamp01(t0 / 0.1f), Mathf.Clamp01(left / 0.25f));
                s = Mathf.Lerp(1, slowScale, k);
            }
            if (now < hitEnd) s = Mathf.Min(s, Tuning.HitStopScale);
            DOTween.timeScale = s;
            if (now >= hitEnd && now >= slowEnd)
            {
                DOTween.timeScale = 1;
                clock?.Kill();
                clock = null;
            }
        }
    }
}
