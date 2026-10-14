namespace Perigee
{
    public static class Strings
    {
        const int SecondsPerMinute = 60;

        public const string Pause = "ПАУЗА";
        public const string Resume = "ПРОДОЛЖИТЬ";
        public const string Restart = "ЗАНОВО";
        public const string Menu = "МЕНЮ";

        public const string Settings = "НАСТРОЙКИ";

        public const string GameTitle = "ПЕРИГЕЙ";
        public const string Play = "ИГРАТЬ";
        public const string Challenges = "ИСПЫТАНИЯ";
        // {0} = сколько забегов сыграно, {1} = лучшее время по всем эрам
        public const string StatsFormat = "Забегов: {0} · Лучшее: {1}";
        public const string BestTimeFormat = "Рекорд {0}";
        public const string Back = "НАЗАД";
        public const string DustFormat = "Пыль: {0}";

        // экран эр: вкладки по порядку RunMode
        public static readonly string[] ModeTab = { "ЗАБЕГ", "БЕСКОНЕЧНЫЙ", "ДЕНЬ" };
        public const string EndlessLocked = "После победы в эре 1";
        public const string Start = "СТАРТ";
        public const string BossFormat = "Босс: {0}";
        // новые враги эры, по порядку config.eras
        public static readonly string[] EraEnemies =
        {
            "Первая звезда",
            "Новые враги: Дробитель, Факел",
            "Новые враги: Кружащий, Щитник",
            "Новые враги: Пожиратель, Призрак",
        };
        // {0} = номер эры, босса которой надо победить
        public const string EraLockedFormat = "Победи босса эры {0}";
        public const string DailyTitle = "ИСПЫТАНИЕ ДНЯ";
        public const string DailyBonusFormat = "Сегодня: пыль ×{0}";
        public const string DailyPlayed = "Сегодня уже сыграно";
        public const string BestScoreFormat = "Лучший счёт: {0}";

        // испытания
        public const string EraNumberFormat = "Эра {0}";
        // награда испытания: пыль или облик (рядом его картинка)
        public const string RewardDustFormat = "{0} пыли";
        public const string RewardSkinFormat = "Облик «{0}»";
        public const string ChallengeDone = "Пройдено";
        public const string ChallengeLockedFormat = "Откроется в эре {0}";
        public const string ChallengePlay = "Играть";

        // модификаторы забега, по номеру бита RunModifier
        public static readonly string[] ModifierTitle =
        {
            "Одиночка", "Стеклянная звезда", "Высокая орбита", "Без способностей", "Рой", "Жара", "Встречный ветер", "Марафон", "Спешка", "Ливень стрел",
        };

        public static readonly string[] ModifierText =
        {
            "Всегда один спутник",
            "HP звезды ×0.4, лечения нет",
            "Спутники не опускаются ниже 3.0",
            "Способностей нет",
            "Вместо крупных врагов — тройные стаи",
            "Нагрев ×2",
            "Орбита разворачивается каждые 8 с",
            "Босс на 5:00",
            "Враги быстрее ×1.2",
            "Стрел вдвое больше",
        };

        const string ModifierSeparator = " · ";

        // строки включённых модификаторов из таблицы table (ModifierTitle или ModifierText) в одну строку
        public static string Modifiers(RunModifier mods, string[] table)
        {
            string line = string.Empty;
            for (int i = 0; i < table.Length; i++)
            {
                if (((int)mods & 1 << i) == 0) continue;
                line = line.Length == 0 ? table[i] : line + ModifierSeparator + table[i];
            }
            return line;
        }

        // под таймером в Бесконечном: {0} = номер цикла с единицы
        public const string CycleFormat = "Цикл {0}";
        // баннер за босса в Бесконечном
        public const string BossDustFormat = "+{0} пыли";

        // настройки
        public const string SfxVolume = "Звуки";
        public const string DroneVolume = "Гул";
        public const string Shake = "Тряска экрана";
        public const string DamageNumbers = "Числа урона";
        public const string HpRings = "Полоски HP врагов";
        // по порядку HpRingMode
        public static readonly string[] HpRingModes = { "После удара", "Всегда", "Никогда" };
        public const string Background = "Фон";
        public const string BackgroundFull = "Полный";
        public const string BackgroundSimple = "Простой";
        public const string ReplayTutorial = "Пройти обучение снова";
        public const string ResetProgress = "Сбросить прогресс";
        public const string ConfirmTutorial = "Пройти обучение снова?";
        public const string ConfirmReset = "Сбросить всё? Пыль, чертежи, эры, способности и облик пропадут.";
        public const string VersionFormat = "Версия {0}";
        public const string Close = "Закрыть";
        public const string Yes = "Да";
        public const string No = "Нет";

        public const string Workshop = "МАСТЕРСКАЯ";
        // вкладки Мастерской по порядку
        public static readonly string[] WorkshopTab = { "ЧЕРТЕЖИ", "СПОСОБНОСТИ", "ОБЛИК" };
        // по порядку BlueprintBranch
        public static readonly string[] BranchTitle = { "ЗВЕЗДА", "УДАР", "ОРБИТА", "КРИСТАЛЛЫ" };
        // {0} = цена
        public const string BuyFormat = "Купить · {0}";
        public const string BlueprintMax = "Макс.";
        // {0} = название узла ярусом ниже
        public const string NeedLevelFormat = "Нужен уровень: {0}";
        public const string Owned = "Есть";
        // {0} и {1} = секунды;   = неразрывный пробел между числом и «с»
        public const string AbilityTimingFormat = "Действует {0:0.#} с · перезарядка {1:0.#} с";
        public const string AbilityInstantFormat = "Мгновенно · перезарядка {0:0.#} с";
        // по порядку SkinKind
        public static readonly string[] SkinKindTitle = { "Спутник", "Звезда" };
        public const string SkinSelected = "Выбран";
        public const string SkinSelect = "Выбрать";
        public const string EraWinFormat = "Победа в эре {0}";
        public const string ChallengeFormat = "Испытание «{0}»";

        public const string Victory = "ПОБЕДА";
        public const string Defeat = "ПОРАЖЕНИЕ";
        public const string Retry = "ЕЩЁ РАЗ";
        public const string TimeFormat = "Время {0}";
        public const string RecordGapFormat = "До рекорда не хватило {0} с";
        public const string NewRecord = "Новый рекорд!";
        // пыль забега по частям: {0} = кристаллы, {1} = множитель эры и чертежей, {2} = пыль за них
        public const string DustCrystalsFormat = "Кристаллы {0} ×{1:0.##}: +{2}";
        public const string DustVictoryFormat = "Победа: +{0}";
        public const string DustFirstWinFormat = "Первая победа: +{0}";
        // Бесконечный: {0} = сколько боссов убито, {1} = пыль за них
        public const string DustBossesFormat = "Боссы ×{0}: +{1}";
        public const string DustRewardFormat = "Награда испытания: +{0}";
        public const string DustDailyFormat = "Первый забег дня: ×{0}";
        public const string DustAdFormat = "Реклама: ×{0}";
        public const string DustTotalFormat = "Итого пыли: +{0}";
        // итоги Бесконечного: {0} = номер цикла с единицы, {1} = убито боссов
        public const string EndlessResultFormat = "Цикл {0} · Боссов убито: {1}";
        // что открыл забег
        public const string UnlockedEraFormat = "Открыто: эра {0}";
        public const string UnlockedEndless = "Открыто: режим БЕСКОНЕЧНЫЙ";
        public const string UnlockedSkinFormat = "Открыто: облик «{0}»";

        // кнопка с рекламой называет награду и говорит, что это реклама
        public const string ReviveTitle = "ЗВЕЗДА ГАСНЕТ";
        public const string ReviveAd = "ВОЗРОДИТЬСЯ (РЕКЛАМА)";
        public const string GiveUp = "СДАТЬСЯ";
        public const string DoubleDustAd = "ПЫЛЬ ×2 (РЕКЛАМА)";
        public const string NewCardsAd = "НОВЫЕ КАРТОЧКИ (РЕКЛАМА)";
        // бесплатная замена карточек: {0} = сколько осталось
        public const string FreeRerollFormat = "ЗАМЕНИТЬ ({0})";

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

        // имя босса: над полосой HP и на баннере
        public static string BossTitle(EnemyType boss)
        {
            switch (boss)
            {
                case EnemyType.Twin: return "БЛИЗНЕЦЫ";
                case EnemyType.Colossus: return "КОЛОСС";
                case EnemyType.Pulsar: return "ПУЛЬСАР";
                default: return "КОМЕТА";
            }
        }

        // по порядку config.eras
        public static readonly string[] EraTitle = { "СОЛНЦЕ", "КРАСНЫЙ КАРЛИК", "ТЯЖЁЛАЯ ЗВЕЗДА", "ЧЁРНАЯ ДЫРА" };

        // правило эры одной строкой, по порядку config.eras
        public static readonly string[] EraRule =
        {
            "Спокойная звезда. Здесь учатся держать орбиту.",
            "Вспышки: красный сектор греет спутники и жжёт врагов.",
            "Тяжесть: спутники взлетают медленнее, падают быстрее, кристаллы тонут в звезде.",
            "Воронка: враги летят к центру по спирали.",
        };

        // подсказка при первой встрече, по порядку EnemyType до Призрака; у Роя, Снаряда и Кометы её нет
        public static readonly string[] EnemyTip =
        {
            "",
            "Стрела: быстрая и больно бьёт",
            "",
            "Бастион: в броне — бей у звезды или в резонансе",
            "Паразит: присасывается к звезде — сбей его у самой звезды",
            "Стрелок: стоит вдали и стреляет",
            "Улей: у звезды лопается роем",
            "",
            "Дробитель: после гибели распадается на рой",
            "Факел: греет спутник при каждом ударе",
            "Кружащий: кружит навстречу спутникам",
            "Щитник: щит держит удар с одной стороны. Бей с другой или жми РЕВЕРС",
            "Пожиратель: ест кристаллы и растёт",
            "Призрак: когда прозрачный, его не задеть",
        };

        // по порядку BlueprintId
        public static readonly string[] BlueprintTitle =
        {
            "КОРПУС", "ЯДРО", "ОТРАЖАТЕЛЬ", "ВТОРОЕ СОЛНЦЕ", "ЗАКАЛКА", "КАМЕРТОН", "ОСТРИЁ", "ПАРА",
            "ТЯГА", "РАДИАТОР", "КОНДЕНСАТОР", "КВАДРИГА", "МАГНЕТИЗМ", "ОЗАРЕНИЕ", "АЛХИМИЯ", "ВЫБОР",
        };

        // текст узла, по порядку BlueprintId; {0} = что дают купленные уровни вместе (Blueprints.Total)
        public static readonly string[] BlueprintText =
        {
            "Звезда: +8 к макс. HP за уровень. Сейчас +{0:0.##}",
            "Звезда лечится +0.07 HP/с за уровень. Сейчас +{0:0.##}",
            "Урон по звезде −5% за уровень. Сейчас −{0:0.#}%",
            "Раз за забег звезда вспыхивает вместо гибели: 25% HP и волна",
            "Удар +3% за уровень. Сейчас +{0:0.#}%",
            "Резонанс +0.1 за уровень. Сейчас ×{0:0.##}",
            "Шанс крита +3% за уровень. Сейчас {0:0.#}%",
            "Забег начинается с двумя спутниками",
            "Выход и падение +5% за уровень. Сейчас +{0:0.#}%",
            "Нагрев −6% за уровень. Сейчас −{0:0.#}%",
            "Перезарядка способностей −5% за уровень. Сейчас −{0:0.#}%",
            "Можно взять четвёртый спутник",
            "Притяжение кристаллов +0.3 за уровень. Сейчас +{0:0.##}",
            "Опыта на уровень −5% за уровень. Сейчас −{0:0.#}%",
            "Пыль за забег +8% за уровень. Сейчас +{0:0.#}%",
            "Ур. 1: бесплатная замена карточек раз в забег. Ур. 2: на выбор 4 карточки",
        };

        // по порядку SkinId
        public static readonly string[] SkinTitle =
        {
            "Ромб", "Спутник-1", "Шестерня", "Луна", "Звёздочка", "Глаз", "Солнце", "Око", "Подсолнух", "Мотор",
        };

        // по номеру испытания с нуля
        public static readonly string[] ChallengeTitle =
        {
            "ОДИНОЧКА", "СТЕКЛЯННАЯ ЗВЕЗДА", "ВЫСОКАЯ ОРБИТА", "РОЙ", "БЕЗ СПОСОБНОСТЕЙ", "ЖАРА", "ВСТРЕЧНЫЙ ВЕТЕР", "МАРАФОН",
        };

        // время вида мм:сс
        public static string Clock(float seconds)
        {
            int total = (int)seconds;
            return string.Format("{0:00}:{1:00}", total / SecondsPerMinute, total % SecondsPerMinute);
        }

        public const string CardPickTitle = "ВЫБЕРИ КАРТОЧКУ";
        public const string MulFormat = "×{0:0.#}";
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
            "ЖАРОСТОЙКОСТЬ", "СОЛНЕЧНЫЙ ПАРУС", "ЖАР-ПТИЦА", "ПРАЩА", "ГРАВИЗАХВАТ", "ЛОМ", "ГОРИЗОНТ СОБЫТИЙ", "ТЁМНАЯ МАТЕРИЯ", "АККРЕЦИЯ",
        };

        // подпись на иконке взятой карточки, по порядку CardId
        public static readonly string[] CardShort =
        {
            "ТС", "ДУ", "ОГ", "ББ", "РК", "НП", "РЛ", "ЦР", "НС", "РД", "РЗ", "КС", "МГ", "ЖД", "ОХ", "БЗ", "ФС", "КР", "МК", "ПЗ", "ДД",
            "ЖС", "СП", "ЖП", "ПР", "ГЗ", "ЛМ", "ГС", "ТМ", "АК",
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
            "Вспышки и Факелы не греют",
            "Во время вспышки и 3 с после: удар ×2",
            "Спутник с нагревом от 70% бьёт ×1.5",
            "1 с после отпускания: удар ×1.5",
            "Кристаллы не тонут в звезде",
            "Щит Щитника ломается с 1 удара, Кружащие получают ×2",
            "Враги ближе 1.8 к центру теряют 2 HP/с",
            "Призраки не уходят в фазу",
            "Убийство ближе 3.0 к центру лечит 1 HP",
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
            "Вращение ×0.95",
            "Нагрев ×1.1",
            "Нагрев ×1.2",
            "Выход ×0.9",
            "Удар ×0.95",
            "Вращение ×0.9",
            "Макс. HP звезды −10",
            "Удар ×0.95",
            "Кристаллы живут ×0.8",
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
