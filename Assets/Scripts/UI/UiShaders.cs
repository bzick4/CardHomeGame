using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Материалы UI-шейдеров из Assets/Resources/Shaders (лежат в Resources, чтобы попасть в сборку:
    // материалы создаются из кода, и иначе Unity выбросил бы шейдеры как неиспользуемые).
    // Общие материалы — один на все объекты; индивидуальные — только там, где параметр анимируется отдельно.
    // Нет шейдера — возвращается null, и всё выглядит как раньше, без эффекта.
    public static class UiShaders
    {
        static readonly Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        static readonly Dictionary<int, Material> foil = new Dictionary<int, Material>();

        static Shader Load(string name)
        {
            if (!shaders.TryGetValue(name, out var s))
            {
                s = Resources.Load<Shader>("Shaders/" + name);
                if (s != null && !s.isSupported) s = null;
                shaders[name] = s;
            }
            return s;
        }

        // Блик по рамке круга: 0–3 — без блика, 4–6 — белый, 7 — чаще и ярче, 8 — радужная голография.
        public static Material Foil(int tier)
        {
            if (tier < 4) return null;
            if (foil.TryGetValue(tier, out var m)) return m;
            var shader = Load("UI_Foil");
            if (shader != null)
            {
                m = new Material(shader) { name = "Foil" + tier };
                bool epic = tier == 7, legend = tier >= 8;
                m.SetFloat("_Period", legend ? 3f : epic ? 2.6f : 4.5f);
                m.SetFloat("_Width", legend ? 0.16f : 0.11f);
                m.SetFloat("_Intensity", legend ? 0.75f : epic ? 0.95f : 0.55f);
                m.SetFloat("_Rainbow", legend ? 1f : 0f);
            }
            foil[tier] = m;
            return m;
        }

        // День → ночь для фона локации (свой экземпляр: _Night анимируется).
        public static Material NewNightGrade()
        {
            var shader = Load("UI_NightGrade");
            return shader != null ? new Material(shader) { name = "NightGrade" } : null;
        }

        // Растворение с золотым краем (свой экземпляр на каждый растворяемый объект).
        public static Material NewDissolve()
        {
            var shader = Load("UI_Dissolve");
            return shader != null ? new Material(shader) { name = "Dissolve" } : null;
        }

        // Растворить все картинки объекта одним узором; по окончании — onDone.
        // Нет шейдера — объект просто тает по прозрачности.
        public static Tween Dissolve(GameObject target, float duration, TweenCallback onDone = null)
        {
            var mat = NewDissolve();
            if (mat == null)
            {
                var group = target.GetComponent<CanvasGroup>();
                if (group == null) group = target.AddComponent<CanvasGroup>();
                return group.DOFade(0, duration).SetLink(target).OnComplete(onDone);
            }
            foreach (var g in target.GetComponentsInChildren<Graphic>()) g.material = mat; // и картинки, и надписи
            mat.SetFloat("_Dissolve", 0);
            return mat.DOFloat(1, "_Dissolve", duration).SetEase(Ease.InQuad).SetLink(target)
                .OnComplete(onDone)
                .OnKill(() => Object.Destroy(mat)); // и после завершения, и при досрочной остановке
        }
    }
}
