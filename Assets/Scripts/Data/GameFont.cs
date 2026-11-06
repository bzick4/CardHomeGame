using UnityEngine;
using UnityEngine.Serialization;

namespace CardHome
{
    // Ссылка на шрифт игры. Сам шрифт лежит в Assets/Fonts (NotoSans),
    // а этот ассет в Resources нужен только чтобы найти его при запуске без сцены.
    // Сменить шрифт: выбери Assets/Resources/GameFont и перетащи другой .ttf в поле Font.
    public class GameFont : ScriptableObject
    {
        // Имя «Font» в ассете сохраняется, поэтому старая ссылка на шрифт не теряется.
        [SerializeField, FormerlySerializedAs("Font")] Font font = null;
        public Font Font => font;
    }
}
