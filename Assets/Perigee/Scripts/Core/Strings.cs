namespace Perigee
{
    public static class Strings
    {
        const int SecondsPerMinute = 60;

        public const string Pause = "ПАУЗА";
        public const string Resume = "ПРОДОЛЖИТЬ";
        public const string Restart = "ЗАНОВО";
        public const string Menu = "МЕНЮ";

        public const string GameTitle = "ПЕРИГЕЙ";
        public const string Play = "ИГРАТЬ";
        public const string Daily = "ДНЕВНОЙ ЗАБЕГ";
        public const string EraFormat = "ЭРА: {0}";
        public const string BestTimeFormat = "Рекорд {0}";
        public const string Blueprints = "ЧЕРТЕЖИ";
        public const string Back = "НАЗАД";
        public const string DustFormat = "Пыль: {0}";
        // {0} = название, {1} = купленный уровень, {2} = всего уровней
        public const string BlueprintLevelFormat = "{0}  {1}/{2}";
        public const string BlueprintMax = "МАКС";

        public const string Victory = "ПОБЕДА";
        public const string Defeat = "ПОРАЖЕНИЕ";
        public const string Retry = "ЕЩЁ РАЗ";
        public const string TimeFormat = "Время {0}";
        public const string RecordGapFormat = "До рекорда не хватило {0} с";
        public const string NewRecord = "Новый рекорд!";
        public const string DustEarnedFormat = "Пыль +{0}";
        public const string EraUnlockedFormat = "Открыта эра: {0}";

        // кнопка с рекламой называет награду и говорит, что это реклама
        public const string ReviveTitle = "ЗВЕЗДА ГАСНЕТ";
        public const string ReviveAd = "ВОЗРОДИТЬСЯ (РЕКЛАМА)";
        public const string GiveUp = "СДАТЬСЯ";
        public const string DoubleDustAd = "ПЫЛЬ ×2 (РЕКЛАМА)";
        public const string NewCardsAd = "НОВЫЕ КАРТОЧКИ (РЕКЛАМА)";

        // по порядку HintId
        public static readonly string[] Hint =
        {
            "Держи — спутник уходит наружу",
            "Отпусти — спутник падает к звезде",
            "Перегрев! Держи, чтобы остыть",
            "Броня: бей внутри серебряного кольца",
            "Жми Q или кнопку: ИМПУЛЬС",
            "Резонанс: урон ×2",
        };

        // имя над полосой HP босса
        public const string BossTitle = "КОМЕТА";

        // по порядку config.eras
        public static readonly string[] EraTitle = { "СОЛНЦЕ", "КРАСНЫЙ КАРЛИК", "ТЯЖЁЛАЯ ЗВЕЗДА" };

        // по порядку BlueprintId
        public static readonly string[] BlueprintTitle = { "КОРПУС", "КОНДЕНСАТОР", "ОХЛАДИТЕЛЬ", "МАГНЕТИЗМ" };

        // строка эффекта, по порядку BlueprintId; {0} = значение за уровень
        public static readonly string[] BlueprintEffect =
        {
            "HP звезды +{0} за уровень",
            "Перезарядка способностей ×{0} за уровень",
            "Нагрев ×{0} за уровень",
            "Радиус магнита +{0} за уровень",
        };

        // время вида мм:сс
        public static string Clock(float seconds)
        {
            int total = (int)seconds;
            return string.Format("{0:00}:{1:00}", total / SecondsPerMinute, total % SecondsPerMinute);
        }

        public const string CardPickTitle = "ВЫБЕРИ КАРТОЧКУ";
        public const string MulFormat = "×{0}";
        public const string AbilityPickTitle = "ВЫБЕРИ СПОСОБНОСТЬ";
        //   = неразрывный пробел: число и «с» не разъезжаются по строкам
        public const string CooldownFormat = "Перезарядка {0:0.#} с";

        // по порядку AbilityId
        public static readonly string[] AbilityTitle = { "ЯКОРЬ", "РОСТ", "ИМПУЛЬС", "ХОЛОД", "ВСПЛЕСК" };

        // строка эффекта, по порядку AbilityId; {0} = длительность, {1} = число способности
        public static readonly string[] AbilityEffect =
        {
            "Орбита замирает на {0:0.#} с",
            "Спутники ×{1} на {0:0.#} с",
            "Волна от звезды, урон {1}",
            "Нагрев сброшен, {0:0.#} с без перегрева",
            "Сила удара ×{1} сквозь броню, {0:0.#} с",
        };

        // по порядку CardId
        public static readonly string[] CardTitle =
        {
            "ТЯЖЁЛЫЙ", "БЫСТРЫЙ", "СПУТНИК", "МАГНИТ", "ОХЛАДИТЕЛЬ", "БРОНЯ ЗВЕЗДЫ",
            "РЕЗОНАТОР", "АПОГЕЙ", "БОЛЬШОЙ СПУТНИК", "МАЯК", "РАЗГОН", "ЗАТЯЖКА",
        };

        // подпись на иконке взятой карточки, по порядку CardId
        public static readonly string[] CardShort =
        {
            "ТЖ", "БС", "СП", "МГ", "ОХ", "БЗ", "РЗ", "АП", "БЛ", "МК", "РГ", "ЗТ",
        };

        // строка эффекта, по порядку StatId; {0} = значение из карточки (у МАЯКА период волны)
        public static readonly string[] StatFormat =
        {
            "Сила удара ×{0}",
            "Скорость по радиусу ×{0}",
            "Нагрев ×{0}",
            "Вращение ×{0}",
            "Угол резонанса ×{0}",
            "Размер спутника ×{0}",
            "Жизнь кристаллов ×{0}",
            "Скорость Стрел ×{0}",
            "Перезарядка способностей ×{0}",
            "Длительность способностей ×{0}",
            "Радиус магнита +{0}",
            "Сила вдали +{0}",
            "HP звезды +{0} и лечение {0}",
            "+{0} спутник",
            "Волна от звезды раз в {0} с",
        };
    }
}
