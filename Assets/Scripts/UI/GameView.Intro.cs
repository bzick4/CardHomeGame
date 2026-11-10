using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CardHome
{
    // Мини-обучения: первая встреча с новым гостем, погодой или картой.
    // Показываются по очереди тем же экраном, что и обучение «день 0» (затемнение, подсветка, облачко).
    // Пока идёт мини-обучение, остальной экран и действия контроллера заблокированы.
    public partial class GameView
    {
        readonly Queue<IntroInfo> intros = new Queue<IntroInfo>();
        IntroInfo currentIntro;
        int introStep;
        TutorialView introView;

        void OnIntro(IntroInfo info)
        {
            intros.Enqueue(info);
            if (currentIntro == null) NextIntro();
        }

        void NextIntro()
        {
            introView = null;
            if (intros.Count == 0 || game == null)
            {
                currentIntro = null;
                return;
            }
            currentIntro = intros.Dequeue();
            introStep = 0;
            // Сразу блокируем ввод, а показываем чуть позже — пусть карты долетят и экран встанет.
            game.Gate = new TutorialGate();
            DOVirtual.DelayedCall(0.6f, StartIntro).SetLink(gameObject);
        }

        // Создать экран текущего мини-обучения (и заново — после перестройки экрана при повороте).
        void StartIntro()
        {
            if (currentIntro == null || game == null || introView != null) return;
            introView = TutorialView.CreateIntro(this, game, transform, IntroSteps(currentIntro), introStep,
                s => introStep = s, NextIntro);
        }

        // Экран перестроили (поворот): старый экран мини-обучения удалён вместе со всем остальным.
        void RestoreIntro()
        {
            introView = null;
            if (currentIntro != null) StartIntro();
        }

        List<TutorialView.Step> IntroSteps(IntroInfo info)
        {
            switch (info.Kind)
            {
                case IntroKind.Guest: return GuestIntro(info.Guest);
                case IntroKind.Weather: return WeatherIntro(info.Weather);
                default: return RoomIntro(info.Room);
            }
        }

        List<TutorialView.Step> GuestIntro(GuestDef g)
        {
            string kind = g.Boss ? "Босс" : g.IsThreat ? "Угроза" : g.Special ? "Особый гость" : "Новый гость";
            var steps = new List<TutorialView.Step>
            {
                new TutorialView.Step
                {
                    Text = $"<b>{kind}: {g.Name}</b>\n<i>{g.Tagline}</i>\n\n{g.Description}",
                    Target = () => PorchRect,
                    Button = "ДАЛЕЕ",
                },
            };

            string how;
            if (g.IsThreat)
            {
                string target = RoomDatabase.Get(g.SeekTarget).Name.ToLower();
                how = $"Угроза идёт кратчайшим путём к цели — сюда это <b>{target}</b>. Пунктир показывает маршрут.\n\n" +
                      "Поставь ответом <b>ловушку</b> на пунктир — она поймает гостя и принесёт золото.";
                if (g.FearsCat) how += "\nА если в доме живёт кошка — крысы даже не сунутся.";
                if (g.Arsonist) how += "\nНе дай ему дойти: сгорит не только камин, но и всё вокруг.";
            }
            else if (g.Behaviour == GuestBehaviour.Random)
                how = $"{g.Name} бродит где хочет — маршрут заранее не угадать.\n\n" +
                      "Ставь комнаты, которые нравятся гостю, поближе ко входу.";
            else
                how = "Пунктир показывает, куда пойдёт гость и сколько настроения наберёт.\n\n" +
                      "Ответь одним ходом: построй или улучши то, что он любит, — или сразу впусти.";

            if (g.Snob) how += "\n\nОбычные комнаты ему безразличны — нужны улучшенные (ур. II и выше).";
            if (g.HouseCat) how += "\n\nУгоди ей — и она останется жить в доме, а крысы больше не придут.";
            if (g.Curser) how += "\n\nНе разочаруй её: комната, где она закончит путь, станет подвалом.";
            if (g.Boss) how += "\n\nХозяин дома приходит только в последние ночи.";

            steps.Add(new TutorialView.Step
            {
                Text = how,
                Target = () => GridRect,
                Button = "ПОНЯТНО",
            });
            return steps;
        }

        List<TutorialView.Step> WeatherIntro(NightWeather w)
        {
            string name = w == NightWeather.Storm ? "Гроза" : "Полнолуние";
            string rules = w == NightWeather.Storm
                ? "Все гости устают на одну комнату раньше — обходят меньше.\nЗато <b>камин греет вдвое</b>."
                : "<b>Волки</b> вдвое опаснее, но и в ловушке приносят вдвое больше.\n<b>Призраки</b> тревожатся: −2 к настроению.";
            string tip = w == NightWeather.Storm
                ? "Ставь лучшие комнаты ближе ко входу и рядом с камином."
                : "Перекрой ловушкой путь к кухне. Призракам этой ночью угодить труднее.";
            return new List<TutorialView.Step>
            {
                new TutorialView.Step
                {
                    Text = $"<b>Погода: {name}</b>\n\nПрогноз на ночь виден здесь с утра. {rules}",
                    Target = () => StatsRect,
                    Button = "ДАЛЕЕ",
                },
                new TutorialView.Step
                {
                    Text = $"У тебя целый день, чтобы подготовиться.\n\n{tip}",
                    Target = () => GridRect,
                    Button = "ПОНЯТНО",
                },
            };
        }

        List<TutorialView.Step> RoomIntro(RoomType t)
        {
            var def = RoomDatabase.Get(t);
            return new List<TutorialView.Step>
            {
                new TutorialView.Step
                {
                    Text = $"<b>Новая карта: {def.Name}</b>\n<i>{def.Tagline}</i>\n\n{def.Description}",
                    Target = () => CardRect(HandIndex(t)),
                    Button = "ПОНЯТНО",
                },
            };
        }

        int HandIndex(RoomType t)
        {
            for (int i = 0; i < game.Hand.Count; i++)
                if (game.Hand[i].Type == t) return i;
            return -1;
        }
    }
}
