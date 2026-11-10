using System;

namespace CardHome
{
    // Мини-обучения: при первой встрече с новым гостем, погодой или картой контроллер просит интерфейс
    // показать короткое пошаговое объяснение. Пока оно идёт, интерфейс ставит Gate и блокирует остальное.
    // Каждое показывается один раз на устройстве (как подсказки).
    public enum IntroKind { Guest, Weather, Room }

    public class IntroInfo
    {
        public IntroKind Kind { get; init; }
        public GuestDef Guest { get; init; }
        public NightWeather Weather { get; init; }
        public RoomType Room { get; init; }
    }

    public partial class GameController
    {
        public event Action<IntroInfo> IntroRequested;

        static string IntroKey(string id) => "cardhome_intro_" + id;

        void Introduce(string id, IntroInfo info)
        {
            if (IsTutorial || IntroRequested == null) return;
            if (Storage.GetInt(IntroKey(id), 0) == 1) return;
            Storage.SetInt(IntroKey(id), 1);
            Storage.Save();
            IntroRequested.Invoke(info);
        }

        // Гость на пороге: все, кроме путника (его объясняет обучение «день 0»).
        void IntroduceGuest(GuestDef g)
        {
            if (g == null || g.Id == GuestDatabase.Traveler.Id) return;
            Introduce("guest_" + g.Id, new IntroInfo { Kind = IntroKind.Guest, Guest = g });
        }

        void IntroduceWeather(NightWeather w)
        {
            if (w == NightWeather.Clear) return;
            Introduce("weather_" + w, new IntroInfo { Kind = IntroKind.Weather, Weather = w });
        }

        // Новые карты (те, что открываются не с первого уровня) — когда впервые оказались в руке.
        void IntroduceNewCards()
        {
            foreach (var c in handCards)
                if (c.Def.MinStage > 1)
                    Introduce("room_" + c.Type, new IntroInfo { Kind = IntroKind.Room, Room = c.Type });
        }

        // Показать все мини-обучения заново (кнопка «Обучение» в настройках).
        public static void ResetIntros()
        {
            foreach (var g in GuestDatabase.All) Storage.DeleteKey(IntroKey("guest_" + g.Id));
            foreach (NightWeather w in Enum.GetValues(typeof(NightWeather))) Storage.DeleteKey(IntroKey("weather_" + w));
            foreach (RoomType t in Enum.GetValues(typeof(RoomType))) Storage.DeleteKey(IntroKey("room_" + t));
            Storage.Save();
        }
    }
}
