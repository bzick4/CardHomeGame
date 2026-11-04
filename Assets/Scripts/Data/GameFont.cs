using UnityEngine;

namespace CardHome
{
    // Ссылка на шрифт игры. Сам шрифт лежит в Assets/Fonts (NotoSans),
    // а этот ассет в Resources нужен только чтобы найти его при запуске без сцены.
    // Сменить шрифт: выбери Assets/Resources/GameFont и перетащи другой .ttf в поле Font.
    public class GameFont : ScriptableObject
    {
        public Font Font;
    }
}
