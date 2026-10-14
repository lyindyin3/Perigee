using System;

namespace Perigee
{
    // Язык текстов. Strings отдаёт строки текущего языка; при смене языка Changed просит экраны переписать тексты.
    public static class Loc
    {
        public enum Lang { Ru, En }

        // языки браузера, для которых игра начинается по-русски
        static readonly string[] RussianCodes = { "ru", "be", "kk", "uk", "uz" };

        public static Lang Current { get; private set; }

        public static event Action Changed;

        public static bool En => Current == Lang.En;

        // смена языка с обновлением всех видимых текстов
        public static void Set(Lang lang)
        {
            Current = lang;
            Changed?.Invoke();
        }

        // смена без события: нужна подписям в инспекторе
        public static void Use(Lang lang)
        {
            Current = lang;
        }

        // язык браузера (две буквы) → язык игры
        public static Lang FromCode(string code)
        {
            return Array.IndexOf(RussianCodes, code) >= 0 ? Lang.Ru : Lang.En;
        }

        public static string Pick(string ru, string en)
        {
            return En ? en : ru;
        }

        public static string[] Pick(string[] ru, string[] en)
        {
            return En ? en : ru;
        }
    }
}
