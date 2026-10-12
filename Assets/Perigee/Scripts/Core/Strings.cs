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
        public static readonly string[] AbilityTitle = { "РЕВЕРС", "РОСТ", "ИМПУЛЬС", "СТАЗИС", "ЭХО", "ПРИТЯЖЕНИЕ", "КУПОЛ" };

        // строка эффекта на экране выбора, по порядку AbilityId;   = неразрывный пробел между числом и «с»
        public static readonly string[] AbilityText =
        {
            "Орбита разворачивается, 2 с удар ×1.5",
            "Спутники вдвое больше на 6 с",
            "Волна от звезды: мелочь гибнет, остальных отбрасывает",
            "Враги в 4 раза медленнее на 4 с",
            "У каждого спутника тень-двойник на 6 с",
            "Враги стягиваются на вашу орбиту на 3 с",
            "Звезда неуязвима 5 с, враги разбиваются о купол",
        };

        // на закрытой способности: цена в Мастерской
        public const string PriceFormat = "{0} пыли";

        // по порядку CardId
        public static readonly string[] CardTitle =
        {
            "ТЯЖЁЛЫЙ СПУТНИК", "ДАЛЬНИЙ УДАР", "ОСТРЫЕ ГРАНИ", "БРОНЕБОЙ", "РАСКОЛ", "НИТЬ-ПИЛА", "РАСКАЛЁННЫЙ", "ЦЕПНАЯ РЕАКЦИЯ",
            "НОВЫЙ СПУТНИК", "РЕАКТИВНЫЙ ДВИГАТЕЛЬ", "РАЗГОН", "КРУПНЫЙ СПУТНИК", "МАГНИТ", "ЖАДНОСТЬ", "ОХЛАДИТЕЛЬ",
            "БРОНЯ ЗВЕЗДЫ", "ФОТОСФЕРА", "КОРОНА", "МАЯК", "ПЕРЕЗАРЯДКА", "ДОЛГОЕ ДЕЙСТВИЕ",
        };

        // подпись на иконке взятой карточки, по порядку CardId
        public static readonly string[] CardShort =
        {
            "ТС", "ДУ", "ОГ", "ББ", "РК", "НП", "РЛ", "ЦР", "НС", "РД", "РЗ", "КС", "МГ", "ЖД", "ОХ", "БЗ", "ФС", "КР", "МК", "ПЗ", "ДД",
        };

        // плюс карточки, по порядку CardId;   = неразрывный пробел между числом и «с»
        public static readonly string[] CardPlus =
        {
            "Удар ×1.35",
            "Удар на дальней орбите +0.5",
            "Шанс крита +12% (крит бьёт ×2)",
            "Броня врагов −1",
            "Враг с HP ≤ 25% гибнет от любого удара",
            "Нить резонанса режет врагов",
            "Горячий спутник поджигает: 1 урон/с, 3 с",
            "Убитый враг взрывается: 1 урон рядом",
            "+1 спутник",
            "Выход и падение ×1.5",
            "Вращение ×1.25",
            "Спутник крупнее ×1.35",
            "Притяжение кристаллов +1.3",
            "Опыт за кристалл ×1.3",
            "Нагрев ×0.6",
            "Макс. HP +25 и лечение 25",
            "Звезда лечится 0.4 HP/с",
            "Урон по звезде ×0.7",
            "Волна от звезды каждые 8 с",
            "Перезарядка способностей ×0.75",
            "Способности действуют ×1.4 дольше",
        };

        // минус карточки, по порядку CardId
        public static readonly string[] CardMinus =
        {
            "Выход и падение медленнее ×0.75",
            "Нагрев ×1.15",
            "Удар ×0.92",
            "Вращение ×0.9",
            "Кристаллы живут ×0.7",
            "Удар ×0.9",
            "Нагрев ×1.15",
            "Опыт за кристалл ×0.9",
            "Нагрев ×1.2",
            "Нагрев ×1.2",
            "Нагрев ×1.15",
            "Вращение ×0.9",
            "Стрелы быстрее ×1.15",
            "Макс. HP звезды −15",
            "Удар ×0.9",
            "Вращение ×0.9",
            "Опыт за кристалл ×0.85",
            "Кристаллы живут ×0.8",
            "Кристаллы живут ×0.6",
            "Удар ×0.9",
            "Перезарядка ×1.15",
        };

        const char PipOn = '●';
        const char PipOff = '○';

        // точки стаков: закрашенных filled из max; при max 0 предела нет, видны только взятые
        public static string Pips(int filled, int max)
        {
            return new string(PipOn, filled) + new string(PipOff, System.Math.Max(max - filled, 0));
        }
    }
}
