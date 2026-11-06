using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardHome
{
    // Обучение «день 0». Затемняет весь экран, оставляя «окно» только над нужным элементом,
    // и ведёт игрока по шагам. Нажать можно только то, что разрешает текущий шаг
    // (затемнение ловит касания, а контроллер дополнительно проверяет TutorialGate).
    // Без Update: шаги переключаются по событиям контроллера.
    public class TutorialView : MonoBehaviour
    {
        static readonly Color DimColor = new Color(0.02f, 0.01f, 0.06f, 0.72f);
        static readonly Color Gold = RoomDatabase.Hex("ffd34d");
        static readonly Color Paper = RoomDatabase.Hex("f7ecd2");
        static readonly Color Ink = RoomDatabase.Hex("3b2a1e");
        static readonly Color Wood = RoomDatabase.Hex("4a2e20");

        class Step
        {
            public string Text { get; init; }
            public Func<RectTransform> Target { get; init; }
            public Action<TutorialGate> Gate { get; init; }
            public Func<bool> Done { get; init; }     // шаг ждёт действия игрока
            public string Button { get; init; }       // или кнопку «Далее»
            public Action OnButton { get; init; }
            public bool Hidden { get; init; }         // ничего не показывать (идёт ночь)
        }

        GameView view;
        GameController game;
        Layout L;
        readonly List<Step> steps = new List<Step>();
        int index = -1;
        bool attached;

        RectTransform root, frame, bubble, arrow, skipRoot;
        CanvasGroup group;
        readonly Image[] dims = new Image[4];
        Text text, arrowText;
        Button nextButton;

        public static TutorialView Create(GameView view, GameController game, Transform parent, int startStep)
        {
            var rt = UIKit.Rect("Tutorial", parent);
            UIKit.Stretch(rt);
            var t = rt.gameObject.AddComponent<TutorialView>();
            t.Init(view, game, startStep);
            return t;
        }

        void Init(GameView view, GameController game, int startStep)
        {
            this.view = view;
            this.game = game;
            L = view.CurrentLayout;
            root = (RectTransform)transform;
            group = gameObject.AddComponent<CanvasGroup>();
            BuildVisuals();
            BuildSteps();

            game.Changed += Check;
            game.PhaseChanged += OnPhase;
            attached = true;
            Show(Mathf.Clamp(startStep, 0, steps.Count - 1));
        }

        public void Detach()
        {
            if (skipRoot != null) Destroy(skipRoot.gameObject);
            if (!attached) return;
            attached = false;
            game.Changed -= Check;
            game.PhaseChanged -= OnPhase;
        }

        void OnDestroy() => Detach();

        // Обучение и кнопка «Пропустить» — поверх всех окон.
        public void BringToFront()
        {
            transform.SetAsLastSibling();
            if (skipRoot != null) skipRoot.SetAsLastSibling();
        }

        // ───────────── Шаги ─────────────

        int CardIndex(RoomType t)
        {
            for (int i = 0; i < game.Hand.Count; i++)
                if (game.Hand[i].Type == t) return i;
            return -1;
        }

        // Размер шрифта не меньше читаемого на этом экране.
        int Fs(int size) => Mathf.Max(L.MinFont, size);

        void BuildSteps()
        {
            var kitchenCell = new Vector2Int(1, 0);
            var bedroomCell = new Vector2Int(3, 0);

            steps.Add(new Step
            {
                Text = "<b>Добро пожаловать в «Дом на ночь»!</b>\n\nДнём ты строишь дом из карт-комнат. Ночью соперник выкладывает " +
                       "на вход карты гостей — а ты отвечаешь ходом. Довольные гости платят, недовольные портят репутацию.",
                Button = "ДАЛЕЕ",
            });
            steps.Add(new Step
            {
                Text = "Здесь видно дом, ночь, <color=#c9933a>золото</color> и <color=#d0473c>репутацию</color>.\n" +
                       "Если репутация упадёт до нуля — дом закроется.",
                Target = () => view.StatsRect,
                Button = "ДАЛЕЕ",
            });
            steps.Add(new Step
            {
                Text = "Это <b>соперник</b>. У него своя колода гостей, и ты видишь только рубашки. " +
                       "Ночью он сам решает, кого прислать, — и старается выбрать неудобного гостя.",
                Target = () => view.OpponentRect,
                Button = "ДАЛЕЕ",
            });
            steps.Add(new Step
            {
                Text = "А это твоя рука. Гости любят поесть и выспаться. Коснись карты <b>«Кухня»</b>.",
                Target = () => view.CardRect(CardIndex(RoomType.Kitchen)),
                Gate = g => g.Card = CardIndex(RoomType.Kitchen),
                Done = () => game.Selected >= 0 && game.Selected == CardIndex(RoomType.Kitchen),
            });
            steps.Add(new Step
            {
                Text = "Светлые полоски по краям — <b>двери</b>. Комнату можно поставить, только если её дверь " +
                       "совпала с дверью соседа.\n\nПоставь кухню <b>слева от прихожей</b>.",
                Target = () => view.TileRect(kitchenCell),
                Gate = g => g.Cell = kitchenCell,
                Done = () => game.Grid.Get(kitchenCell) != null,
            });
            steps.Add(new Step
            {
                Text = "Каждая постройка тратит <b>ход</b>, днём их 2.\n\nТеперь коснись карты <b>«Спальня»</b>.",
                Target = () => view.CardRect(CardIndex(RoomType.Bedroom)),
                Gate = g => g.Card = CardIndex(RoomType.Bedroom),
                Done = () => game.Selected >= 0 && game.Selected == CardIndex(RoomType.Bedroom),
            });
            steps.Add(new Step
            {
                Text = "У каждой комнаты три двери и одна глухая стена. У спальни глухая — слева, а справа от прихожей " +
                       "ей нужна дверь именно <b>слева</b>.\n\n" +
                       "Нажми <b>«Повернуть»</b> (или коснись карты ещё раз).",
                Target = () => view.RotateRect,
                Gate = g =>
                {
                    g.Rotate = true;
                    g.Card = CardIndex(RoomType.Bedroom);
                },
                Done = () => game.Selected >= 0 && (game.SelectedDoors & Doors.W) != 0,
            });
            steps.Add(new Step
            {
                Text = "Отлично! Поставь спальню <b>справа от прихожей</b>.",
                Target = () => view.TileRect(bedroomCell),
                Gate = g => g.Cell = bedroomCell,
                Done = () => game.Grid.Get(bedroomCell) != null,
            });
            steps.Add(new Step
            {
                Text = "Справа от руки — <b>твоя колода</b>. Каждый день из неё приходит новая рука. " +
                       "Коснись её в игре, чтобы посмотреть, какие карты ещё остались.",
                Target = () => view.PileRect,
                Button = "ДАЛЕЕ",
            });
            steps.Add(new Step
            {
                Text = "Ещё пара правил:\n\n• Коснись комнаты на доске — её можно <b>перенести или повернуть</b> за 1 ход.\n" +
                       "• Положи карту на такую же комнату — она <b>улучшится до II</b> и даст вдвое больше.\n" +
                       "• Сломанную комнату <b>чинят</b> касанием за 1 ход.",
                Target = () => view.GridRect,
                Button = "ДАЛЕЕ",
            });
            steps.Add(new Step
            {
                Text = "Готово! Жми <b>«Ночь»</b> — и соперник сделает свой ход.",
                Target = () => view.NightRect,
                Gate = g => g.Night = true,
                Done = () => game.State != Phase.Day,
            });
            steps.Add(new Step
            {
                Hidden = true,
                Done = () => game.Responding,
            });
            steps.Add(new Step
            {
                Text = "Соперник выложил гостя на <b>ВХОД</b>. Пунктир показывает, куда он пойдёт и чем всё кончится.\n\n" +
                       "Теперь твой ответ: <b>один ход</b> — построить, улучшить, перенести или повернуть. " +
                       "Вор и волк? Самое время поставить ловушку на их пути!",
                Target = () => view.PorchRect,
                Button = "ДАЛЕЕ",
            });
            steps.Add(new Step
            {
                Text = "Путник и так будет доволен — отвечать не нужно. Нажми <b>«Впустить»</b>.",
                Target = () => view.NightRect,
                Gate = g => g.LetIn = true,
                Done = () => game.Step == NightStep.Walk || game.State != Phase.Night,
            });
            steps.Add(new Step
            {
                Hidden = true,
                Done = () => game.State == Phase.Summary,
            });
            steps.Add(new Step
            {
                Text = "Это <b>итоги ночи</b>: кто доволен, сколько золота и репутации. " +
                       "Дальше — награды, лавка и десять домов, которые открываются по очереди. Удачи!",
                Target = () => view.SummaryRect,
                Button = "НАЧАТЬ ИГРУ ▶",
                OnButton = () => game.ContinueAfterSummary(),
            });
        }

        void OnPhase(Phase _) => Check();

        void Check()
        {
            if (!attached || index < 0 || index >= steps.Count) return;
            var s = steps[index];
            if (s.Done != null && s.Done()) Show(index + 1);
            else Relayout();
        }

        void Show(int i)
        {
            if (i >= steps.Count) return;
            index = i;
            view.SetTutorialStep(i);
            var s = steps[i];

            // Разрешаем только то, что нужно на этом шаге.
            var gate = new TutorialGate();
            s.Gate?.Invoke(gate);
            game.Gate = gate;

            group.DOKill();
            if (s.Hidden)
            {
                group.blocksRaycasts = false;
                group.DOFade(0, 0.25f).SetLink(gameObject);
                return;
            }
            group.blocksRaycasts = true;
            group.DOFade(1, 0.25f).SetLink(gameObject);

            text.text = s.Text;
            nextButton.gameObject.SetActive(s.Button != null);
            if (s.Button != null) nextButton.GetComponentInChildren<Text>().text = s.Button;

            Relayout();
            bubble.DOKill();
            bubble.localScale = Vector3.one;
            bubble.DOScale(1, 0.3f).From(0.85f).SetEase(Ease.OutBack).SetLink(bubble.gameObject);
            // Цели могут ещё ехать (карты раздаются, комнаты плюхаются) — подровняемся, когда всё встанет.
            DOVirtual.DelayedCall(0.5f, Relayout).SetLink(gameObject);

            // Шаг мог выполниться сразу (например, после перестройки экрана).
            if (s.Done != null && s.Done()) Show(i + 1);
        }

        void OnNext()
        {
            if (index < 0 || index >= steps.Count) return;
            var s = steps[index];
            if (s.OnButton != null) s.OnButton();
            else Show(index + 1);
        }

        // ───────────── Отрисовка ─────────────

        void BuildVisuals()
        {
            for (int i = 0; i < 4; i++)
            {
                dims[i] = UIKit.Image("Dim", root, DimColor);
                dims[i].raycastTarget = true; // ловит все касания мимо «окна»
            }

            frame = UIKit.Image("Frame", root, Gold, Sprites.Frame).rectTransform;
            frame.GetComponent<Image>().raycastTarget = false;
            frame.GetComponent<Image>().DOFade(0.35f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(frame.gameObject);

            arrow = UIKit.Rect("Arrow", root);
            arrow.sizeDelta = new Vector2(120, 90);
            arrowText = UIKit.Outlined(UIKit.Text(arrow, "▼", 72, TextAnchor.MiddleCenter, Gold, FontStyle.Bold), 3);
            UIKit.Stretch(arrowText.rectTransform);

            bubble = UIKit.Rect("Bubble", root);
            UIKit.DropShadow(bubble, new Vector2(0, -12), 0.55f);
            var border = UIKit.Image("Border", bubble, Wood, Sprites.Panel);
            UIKit.Stretch(border.rectTransform);
            var paper = UIKit.Image("Paper", bubble, Paper, Sprites.Flat);
            UIKit.Stretch(paper.rectTransform, 16);

            text = UIKit.Text(paper.transform, "", L.BodyFont + 6, TextAnchor.MiddleLeft, Ink, FontStyle.Normal);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(28, 20);
            text.rectTransform.offsetMax = new Vector2(-28, -20);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Fs(L.BodyFont - 4);
            text.resizeTextMaxSize = L.BodyFont + 6;

            nextButton = UIKit.Button(bubble, "ДАЛЕЕ", RoomDatabase.Hex("4cbb5a"), OnNext, Fs(L.ButtonFont - 6));
            UIKit.Place((RectTransform)nextButton.transform, new Vector2(1, 0), new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(320, 90));

            // Пропустить обучение можно в любой момент — кнопка всегда поверх затемнения.
            skipRoot = UIKit.Rect("Skip", (RectTransform)transform.parent);
            UIKit.Stretch(skipRoot);
            var skip = UIKit.Button(skipRoot, "ПРОПУСТИТЬ ▶▶", RoomDatabase.Hex("8a7f72"), () => game?.SkipTutorial(), Fs(L.ButtonFont - 10));
            var srt = (RectTransform)skip.transform;
            UIKit.Place(srt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(330, 80));

        }

        // Вырезает «окно» над целью, ставит рамку, стрелку и пузырь с текстом.
        void Relayout()
        {
            if (!attached || index < 0 || index >= steps.Count || steps[index].Hidden) return;
            var s = steps[index];
            var area = root.rect;
            var targetRt = s.Target?.Invoke();

            Rect hole;
            bool hasHole = targetRt != null && targetRt.gameObject.activeInHierarchy;
            if (hasHole)
            {
                var corners = new Vector3[4];
                targetRt.GetWorldCorners(corners);
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
                foreach (var c in corners)
                {
                    Vector2 p = root.InverseTransformPoint(c);
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
                const float pad = 14;
                hole = Rect.MinMaxRect(min.x - pad, min.y - pad, max.x + pad, max.y + pad);
            }
            else
            {
                hole = new Rect(area.center, Vector2.zero);
            }

            // Четыре полосы затемнения вокруг окна.
            SetRect(dims[0], new Rect(area.xMin, hole.yMax, area.width, Mathf.Max(0, area.yMax - hole.yMax)));
            SetRect(dims[1], new Rect(area.xMin, area.yMin, area.width, Mathf.Max(0, hole.yMin - area.yMin)));
            SetRect(dims[2], new Rect(area.xMin, hole.yMin, Mathf.Max(0, hole.xMin - area.xMin), hole.height));
            SetRect(dims[3], new Rect(hole.xMax, hole.yMin, Mathf.Max(0, area.xMax - hole.xMax), hole.height));

            frame.gameObject.SetActive(hasHole);
            arrow.gameObject.SetActive(hasHole);
            if (hasHole) SetRect(frame, new Rect(hole.xMin - 6, hole.yMin - 6, hole.width + 12, hole.height + 12));

            // Пузырь — там, где больше места: над окном или под ним.
            float w = Mathf.Min(area.width - 60, 1000);
            float h = s.Button != null ? 400 : 300;
            bool above = !hasHole || hole.center.y < area.center.y;
            float y;
            if (!hasHole) y = area.center.y - h / 2;
            else if (above) y = Mathf.Min(hole.yMax + 130, area.yMax - h - 40);
            else y = Mathf.Max(hole.yMin - 130 - h, area.yMin + 40);
            SetRect(bubble, new Rect(area.center.x - w / 2, y, w, h));
            var text0 = text.rectTransform;
            text0.offsetMin = new Vector2(28, s.Button != null ? 110 : 20);

            if (hasHole)
            {
                arrowText.text = above ? "▼" : "▲";
                float ay = above ? hole.yMax + 60 : hole.yMin - 60;
                arrow.DOKill();
                SetRect(arrow, new Rect(hole.center.x - 60, ay - 45, 120, 90));
                arrow.DOAnchorPosY(above ? 18 : -18, 0.45f).SetRelative().SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
                    .SetLink(arrow.gameObject);
            }
        }

        static void SetRect(Graphic g, Rect r) => SetRect(g.rectTransform, r);

        // Позиция в координатах корня (его центр — начало координат).
        static void SetRect(RectTransform rt, Rect r)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = r.position;
            rt.sizeDelta = r.size;
        }
    }
}
