using System;
using System.Linq;

namespace CardHome
{
    // Вариант ответа в случае: подпись, условие (можно ли выбрать) и последствие (возвращает текст итога).
    public class EventChoice
    {
        public string Label { get; init; }
        public Func<GameController, bool> Can { get; init; }
        public Func<GameController, string> Apply { get; init; }
    }

    // Случай на карте странствия: короткая история с выбором.
    public class WanderEvent
    {
        public string Title { get; init; }
        public string Text { get; init; }
        public string GuestId { get; init; }   // чей портрет показать (Id гостя)
        public EventChoice[] Choices { get; init; }
    }

    public static class WanderEvents
    {
        static EventChoice C(string label, Func<GameController, string> apply, Func<GameController, bool> can = null) =>
            new EventChoice { Label = label, Apply = apply, Can = can };

        static string Leave(GameController g) => "Ты идёшь дальше.";

        public static readonly WanderEvent[] All =
        {
            new WanderEvent
            {
                Title = "Странник у ворот",
                GuestId = "Traveler",
                Text = "Промокший путник просит ночлег, но платить ему нечем. «Я отплачу иначе», — обещает он.",
                Choices = new[]
                {
                    C("Впустить даром (−1 ♥, бронзовая карта)", g =>
                    {
                        g.ChangeReputation(-1);
                        var card = g.AddRandomRunCard(2);
                        return $"Утром путник оставил подарок: «{card.Title}». Но соседи ворчат: −1 ♥.";
                    }),
                    C("Взять плату вещами (+5 ●)", g =>
                    {
                        g.ChangeGold(5);
                        return "Путник отдал старый подсвечник. Ты продал его: +5 ●.";
                    }),
                },
            },
            new WanderEvent
            {
                Title = "Заброшенный погреб",
                GuestId = "Ghost",
                Text = "Под старым домом — погреб. Пахнет сыростью, но в углу блестит бочонок.",
                Choices = new[]
                {
                    C("Обыскать (50%: +10 ●, иначе −1 ♥)", g =>
                    {
                        if (g.Chance(50))
                        {
                            g.ChangeGold(10);
                            return "В бочонке — старые монеты! +10 ●.";
                        }
                        g.ChangeReputation(-1);
                        return "Крысы! Ты еле унёс ноги, слух разошёлся по округе: −1 ♥.";
                    }),
                    C("Пройти мимо", Leave),
                },
            },
            new WanderEvent
            {
                Title = "Бродячий плотник",
                GuestId = "Inspector",
                Text = "Плотник с ящиком инструментов предлагает подновить твои комнаты — за плату.",
                Choices = new[]
                {
                    C("Заплатить 6 ● (улучшить 2 случайные карты)", g =>
                    {
                        g.ChangeGold(-6);
                        var up = g.UpgradeRandomRunCards(2);
                        return up.Length > 0 ? $"Улучшены: {string.Join(", ", up.Select(c => c.Title))}." : "Улучшать оказалось нечего.";
                    }, g => g.Gold >= 6),
                    C("Отказаться", Leave),
                },
            },
            new WanderEvent
            {
                Title = "Фокусник на площади",
                GuestId = "Drunkard",
                Text = "«Угадай, под каким напёрстком монета! Ставка — пять золотых, выигрыш — двенадцать!»",
                Choices = new[]
                {
                    C("Сыграть: 5 ● (50%: +12 ●)", g =>
                    {
                        g.ChangeGold(-5);
                        if (g.Chance(50))
                        {
                            g.ChangeGold(12);
                            return "Угадал! Фокусник нехотя отсчитывает 12 ●.";
                        }
                        return "Мимо. Фокусник улыбается и прячет твои 5 ●.";
                    }, g => g.Gold >= 5),
                    C("Пройти мимо", Leave),
                },
            },
            new WanderEvent
            {
                Title = "Ведьма у колодца",
                GuestId = "GhostLord",
                Text = "Старуха у колодца протягивает тебе свёрток: «Дам вещь редкую, но заберу кусочек твоего доброго имени».",
                Choices = new[]
                {
                    C("Согласиться (−1 ♥, серебряная карта)", g =>
                    {
                        g.ChangeReputation(-1);
                        var card = g.AddRandomRunCard(3);
                        return $"В свёртке — «{card.Title}». По деревне ползут слухи: −1 ♥.";
                    }),
                    C("Вежливо отказаться", Leave),
                },
            },
            new WanderEvent
            {
                Title = "Сгоревший дом",
                GuestId = "Thief",
                Text = "От дома остались угли. В завалах можно что-нибудь найти — или избавиться от лишнего хлама.",
                Choices = new[]
                {
                    C("Разобрать завалы (убрать карту, +3 ●)", g =>
                    {
                        var removed = g.RemoveWeakestRunCard();
                        g.ChangeGold(3);
                        return removed.HasValue
                            ? $"Ты выбросил «{removed.Value.Title}» и нашёл 3 ●."
                            : "Выбрасывать нечего, но нашлось 3 ●.";
                    }, g => g.RunDeckSize > GameController.MinDeck),
                    C("Уйти", Leave),
                },
            },
            new WanderEvent
            {
                Title = "Гостеприимный трактирщик",
                GuestId = "Traveler",
                Text = "Трактирщик зовёт к огню: «Отдохни с дороги! А ещё могу продать рецепт — гости пальчики оближут».",
                Choices = new[]
                {
                    C("Отдохнуть (+1 ♥)", g =>
                    {
                        g.ChangeReputation(1);
                        return "Ты выспался, о тебе говорят только хорошее: +1 ♥.";
                    }, g => g.Reputation < GameController.WanderMaxReputation),
                    C("Купить рецепт: 4 ● (Кухня серебро)", g =>
                    {
                        g.ChangeGold(-4);
                        var card = g.AddRunCard(new Card(RoomType.Kitchen, 3));
                        return $"В колоде новая карта: «{card.Title}».";
                    }, g => g.Gold >= 4),
                    C("Уйти", Leave),
                },
            },
            new WanderEvent
            {
                Title = "Призрак в зеркале",
                GuestId = "Ghost",
                Text = "Из старого зеркала смотрит призрак: «Отдам клад, если пустишь меня жить в твои подвалы»",
                Choices = new[]
                {
                    C("Принять (+15 ●, 2 карты «Подвал»)", g =>
                    {
                        g.ChangeGold(15);
                        g.AddRunCard(new Card(RoomType.Cellar));
                        g.AddRunCard(new Card(RoomType.Cellar));
                        return "Клад твой: +15 ●. В колоде теперь два подвала — спальням будут сниться кошмары.";
                    }),
                    C("Разбить зеркало (−1 ♥)", g =>
                    {
                        g.ChangeReputation(-1);
                        return "Звон стекла разнёсся по округе. Говорят, это к беде: −1 ♥.";
                    }),
                },
            },
        };
    }
}
