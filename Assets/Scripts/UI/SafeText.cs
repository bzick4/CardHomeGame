using System.Text.RegularExpressions;
using UnityEngine.UI;

namespace CardHome
{
    // Текст игры. Шрифт Noto Sans не содержит значков вроде ● ♥ ★ ✓ ▶, а в WebGL подставить их неоткуда —
    // вместо них были бы пустые квадраты. Поэтому любой текст при записи проходит через Glyphs.Clean.
    // Там, где важна картинка (золото, сердца, отметки), интерфейс ставит иконки Art/UI/Icons отдельно.
    public class SafeText : Text
    {
        public override string text
        {
            get => base.text;
            set => base.text = Glyphs.Clean(value);
        }
    }

    // Замена символов, которых нет в шрифте, на слова или похожие символы из шрифта.
    public static class Glyphs
    {
        static readonly Regex GoldAfter = new Regex(@"●\s*([+−\-]?\d+)");
        static readonly Regex GoldBefore = new Regex(@"([+−\-]?\d+)\s*●");
        static readonly Regex HeartAfter = new Regex(@"♥\s*([+−\-]?\d+)");
        static readonly Regex HeartBefore = new Regex(@"([+−\-]?\d+)\s*♥");

        public static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s) || !NeedsCleaning(s)) return s;
            s = GoldBefore.Replace(s, "$1 зол.");
            s = GoldAfter.Replace(s, "$1 зол.");
            s = HeartBefore.Replace(s, "$1 реп.");
            s = HeartAfter.Replace(s, "$1 реп.");
            return s
                .Replace("●", "зол.").Replace("♥", "реп.")
                .Replace("★", "*").Replace("✦", "•")
                .Replace("✓", "+").Replace("✗", "×")
                .Replace("▶", "›").Replace("◀", "‹").Replace("▲", "^").Replace("▼", "v")
                .Replace("→", "»").Replace("≈", "~")
                .Replace("☾", "").Replace("⟳", "").Replace("≡", "").Replace("🔒", "");
        }

        static bool NeedsCleaning(string s)
        {
            foreach (char c in s)
                if (c >= '←' && c != '−') return true; // стрелки, фигуры, значки, эмодзи (кроме знака минус)
            return false;
        }
    }
}
