using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace CardHome
{
    // Запускает игру при нажатии Play в любой сцене: ничего не нужно настраивать руками.
    // Когда появится своя сцена с GameController, автозапуск просто ничего не будет делать.
    public static class Bootstrap
    {
        // Игра работает в обеих ориентациях: вертикально удобно одной рукой, горизонтально — просторнее.
        // Интерфейс сам выбирает раскладку по размеру экрана (в браузере — по размеру окна/холста).
        public const bool AllowLandscape = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            Application.targetFrameRate = 60;
            // Анимаций много (рубашки, звёзды, карты) — сразу даём DOTween запас, без предупреждений.
            DG.Tweening.DOTween.Init(recycleAllByDefault: false, useSafeMode: true, logBehaviour: DG.Tweening.LogBehaviour.ErrorsOnly)
                .SetCapacity(2000, 500);
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = AllowLandscape;
            Screen.autorotateToLandscapeRight = AllowLandscape;
            Screen.orientation = AllowLandscape ? ScreenOrientation.AutoRotation : ScreenOrientation.Portrait;

            if (Object.FindFirstObjectByType<GameController>() != null) return;

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.11f, 0.10f, 0.09f);
            }

            new GameObject("CardHome").AddComponent<GameController>();
        }
    }
}
