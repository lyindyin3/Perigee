namespace Perigee
{
    // Все тексты игры на двух языках; каждое свойство отдаёт строку текущего языка (Loc).
    public static class Strings
    {
        const int SecondsPerMinute = 60;

        public static string Pause => Loc.Pick("ПАУЗА", "PAUSE");
        public static string Resume => Loc.Pick("ПРОДОЛЖИТЬ", "RESUME");
        public static string Restart => Loc.Pick("ЗАНОВО", "RESTART");
        public static string Menu => Loc.Pick("МЕНЮ", "MENU");

        public static string Settings => Loc.Pick("НАСТРОЙКИ", "SETTINGS");

        public static string GameTitle => Loc.Pick("ПЕРИГЕЙ", "PERIGEE");
        public static string Play => Loc.Pick("ИГРАТЬ", "PLAY");
        public static string Challenges => Loc.Pick("ИСПЫТАНИЯ", "CHALLENGES");
        // {0} = сколько забегов сыграно, {1} = лучшее время по всем эрам
        public static string StatsFormat => Loc.Pick("Забегов: {0} · Лучшее: {1}", "Runs: {0} · Best: {1}");
        public static string BestTimeFormat => Loc.Pick("Рекорд {0}", "Best {0}");
        public static string Back => Loc.Pick("НАЗАД", "BACK");
        public static string DustFormat => Loc.Pick("Пыль: {0}", "Stardust: {0}");

        // экран эр: вкладки по порядку RunMode
        public static string[] ModeTab => Loc.Pick(ModeTabRu, ModeTabEn);
        static readonly string[] ModeTabRu = { "ЗАБЕГ", "БЕСКОНЕЧНЫЙ", "ДЕНЬ" };
        static readonly string[] ModeTabEn = { "RUN", "ENDLESS", "DAILY" };
        public static string EndlessLocked => Loc.Pick("После победы в эре 1", "Win era 1 to unlock");
        public static string Start => Loc.Pick("СТАРТ", "START");
        public static string BossFormat => Loc.Pick("Босс: {0}", "Boss: {0}");
        // новые враги эры, по порядку config.eras
        public static string[] EraEnemies => Loc.Pick(EraEnemiesRu, EraEnemiesEn);
        static readonly string[] EraEnemiesRu =
        {
            "Первая звезда",
            "Новые враги: Дробитель, Факел",
            "Новые враги: Кружащий, Щитник",
            "Новые враги: Пожиратель, Призрак",
        };
        static readonly string[] EraEnemiesEn =
        {
            "The first star",
            "New enemies: Splitter, Torch",
            "New enemies: Orbiter, Shielder",
            "New enemies: Devourer, Phantom",
        };
        // {0} = номер эры, босса которой надо победить
        public static string EraLockedFormat => Loc.Pick("Победи босса эры {0}", "Beat the era {0} boss");
        public static string DailyTitle => Loc.Pick("ИСПЫТАНИЕ ДНЯ", "DAILY");
        public static string DailyBonusFormat => Loc.Pick("Сегодня: пыль ×{0}", "Today: stardust ×{0}");
        public static string DailyPlayed => Loc.Pick("Сегодня уже сыграно", "Already played today");
        public static string BestScoreFormat => Loc.Pick("Лучший счёт: {0}", "Best score: {0}");

        // испытания
        public static string EraNumberFormat => Loc.Pick("Эра {0}", "Era {0}");
        // награда испытания: пыль или облик (рядом его картинка)
        public static string RewardDustFormat => Loc.Pick("{0} пыли", "{0} stardust");
        public static string RewardSkinFormat => Loc.Pick("Облик «{0}»", "Skin «{0}»");
        public static string ChallengeDone => Loc.Pick("Пройдено", "Completed");
        public static string ChallengeLockedFormat => Loc.Pick("Откроется в эре {0}", "Unlocks in era {0}");
        public static string ChallengePlay => Loc.Pick("Играть", "Play");

        // модификаторы забега, по номеру бита RunModifier
        public static string[] ModifierTitle => Loc.Pick(ModifierTitleRu, ModifierTitleEn);
        static readonly string[] ModifierTitleRu =
        {
            "Одиночка", "Стеклянная звезда", "Высокая орбита", "Без способностей", "Рой", "Жара", "Встречный ветер", "Марафон", "Спешка", "Ливень стрел",
        };
        static readonly string[] ModifierTitleEn =
        {
            "Loner", "Glass Star", "High Orbit", "No Abilities", "Swarm", "Heatwave", "Headwind", "Marathon", "Rush", "Arrow Rain",
        };

        public static string[] ModifierText => Loc.Pick(ModifierTextRu, ModifierTextEn);
        static readonly string[] ModifierTextRu =
        {
            "Всегда один спутник",
            "HP звезды ×0.4, лечения нет",
            "Спутники не опускаются ниже 3.0",
            "Способностей нет",
            "Вместо крупных врагов — тройные стаи",
            "Нагрев ×2",
            "Орбита разворачивается каждые 8 с",
            "Босс на 5:00",
            "Враги быстрее ×1.2",
            "Стрел вдвое больше",
        };
        static readonly string[] ModifierTextEn =
        {
            "Always one satellite",
            "Star HP ×0.4, no healing",
            "Satellites stay above 3.0",
            "No abilities",
            "Triple packs instead of big enemies",
            "Heat ×2",
            "The orbit reverses every 8 s",
            "Boss at 5:00",
            "Enemies faster ×1.2",
            "Twice as many Arrows",
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
        public static string CycleFormat => Loc.Pick("Цикл {0}", "Cycle {0}");
        // баннер за босса в Бесконечном
        public static string BossDustFormat => Loc.Pick("+{0} пыли", "+{0} stardust");

        // настройки
        public static string SfxVolume => Loc.Pick("Звуки", "Sounds");
        public static string DroneVolume => Loc.Pick("Гул", "Drone");
        public static string MusicVolume => Loc.Pick("Музыка", "Music");
        public static string Shake => Loc.Pick("Тряска экрана", "Screen shake");
        public static string DamageNumbers => Loc.Pick("Числа урона", "Damage numbers");
        public static string HpRings => Loc.Pick("Полоски HP врагов", "Enemy HP rings");
        // по порядку HpRingMode
        public static string[] HpRingModes => Loc.Pick(HpRingModesRu, HpRingModesEn);
        static readonly string[] HpRingModesRu = { "После удара", "Всегда", "Никогда" };
        static readonly string[] HpRingModesEn = { "After a hit", "Always", "Never" };
        public static string Background => Loc.Pick("Фон", "Background");
        public static string BackgroundFull => Loc.Pick("Полный", "Full");
        public static string BackgroundSimple => Loc.Pick("Простой", "Simple");
        public static string Vibration => Loc.Pick("Вибрация", "Vibration");
        public static string Language => Loc.Pick("Язык", "Language");
        // по порядку Loc.Lang: каждый язык назван на самом себе
        public static readonly string[] LanguageNames = { "Русский", "English" };
        public static string ReplayTutorial => Loc.Pick("Пройти обучение снова", "Replay the tutorial");
        public static string ResetProgress => Loc.Pick("Сбросить прогресс", "Reset progress");
        public static string ConfirmTutorial => Loc.Pick("Пройти обучение снова?", "Replay the tutorial?");
        public static string ConfirmReset => Loc.Pick(
            "Сбросить всё? Пыль, чертежи, эры, способности и облик пропадут.",
            "Reset everything? Stardust, blueprints, eras, abilities and skins will be lost.");
        public static string VersionFormat => Loc.Pick("Версия {0}", "Version {0}");
        public static string Close => Loc.Pick("Закрыть", "Close");
        public static string Yes => Loc.Pick("Да", "Yes");
        public static string No => Loc.Pick("Нет", "No");

        public static string Workshop => Loc.Pick("МАСТЕРСКАЯ", "WORKSHOP");
        // вкладки Мастерской по порядку
        public static string[] WorkshopTab => Loc.Pick(WorkshopTabRu, WorkshopTabEn);
        static readonly string[] WorkshopTabRu = { "ЧЕРТЕЖИ", "СПОСОБНОСТИ", "ОБЛИК" };
        static readonly string[] WorkshopTabEn = { "BLUEPRINTS", "ABILITIES", "SKINS" };
        // по порядку BlueprintBranch
        public static string[] BranchTitle => Loc.Pick(BranchTitleRu, BranchTitleEn);
        static readonly string[] BranchTitleRu = { "ЗВЕЗДА", "УДАР", "ОРБИТА", "КРИСТАЛЛЫ" };
        static readonly string[] BranchTitleEn = { "STAR", "STRIKE", "ORBIT", "CRYSTALS" };
        // {0} = цена
        public static string BuyFormat => Loc.Pick("Купить · {0}", "Buy · {0}");
        public static string BlueprintMax => Loc.Pick("Макс.", "Max");
        // {0} = название узла ярусом ниже
        public static string NeedLevelFormat => Loc.Pick("Нужен уровень: {0}", "Needs a level of: {0}");
        public static string Owned => Loc.Pick("Есть", "Owned");
        // {0} и {1} = секунды;   = неразрывный пробел между числом и «с»
        public static string AbilityTimingFormat => Loc.Pick("Действует {0:0.#} с · перезарядка {1:0.#} с", "Lasts {0:0.#} s · cooldown {1:0.#} s");
        public static string AbilityInstantFormat => Loc.Pick("Мгновенно · перезарядка {0:0.#} с", "Instant · cooldown {0:0.#} s");
        // по порядку SkinKind
        public static string[] SkinKindTitle => Loc.Pick(SkinKindTitleRu, SkinKindTitleEn);
        static readonly string[] SkinKindTitleRu = { "Спутник", "Звезда" };
        static readonly string[] SkinKindTitleEn = { "Satellite", "Star" };
        public static string SkinSelected => Loc.Pick("Выбран", "Selected");
        public static string SkinSelect => Loc.Pick("Выбрать", "Select");
        public static string EraWinFormat => Loc.Pick("Победа в эре {0}", "Win era {0}");
        public static string ChallengeFormat => Loc.Pick("Испытание «{0}»", "Challenge «{0}»");

        public static string Victory => Loc.Pick("ПОБЕДА", "VICTORY");
        public static string Defeat => Loc.Pick("ПОРАЖЕНИЕ", "DEFEAT");
        public static string Retry => Loc.Pick("ЕЩЁ РАЗ", "RETRY");
        public static string TimeFormat => Loc.Pick("Время {0}", "Time {0}");
        public static string RecordGapFormat => Loc.Pick("До рекорда не хватило {0} с", "{0} s short of your best");
        public static string NewRecord => Loc.Pick("Новый рекорд!", "New best!");
        // пыль забега по частям: {0} = кристаллы, {1} = множитель эры и чертежей, {2} = пыль за них
        public static string DustCrystalsFormat => Loc.Pick("Кристаллы {0} ×{1:0.##}: +{2}", "Crystals {0} ×{1:0.##}: +{2}");
        public static string DustVictoryFormat => Loc.Pick("Победа: +{0}", "Victory: +{0}");
        public static string DustFirstWinFormat => Loc.Pick("Первая победа: +{0}", "First victory: +{0}");
        // Бесконечный: {0} = сколько боссов убито, {1} = пыль за них
        public static string DustBossesFormat => Loc.Pick("Боссы ×{0}: +{1}", "Bosses ×{0}: +{1}");
        public static string DustRewardFormat => Loc.Pick("Награда испытания: +{0}", "Challenge reward: +{0}");
        public static string DustDailyFormat => Loc.Pick("Первый забег дня: ×{0}", "First run of the day: ×{0}");
        public static string DustAdFormat => Loc.Pick("Реклама: ×{0}", "Ad: ×{0}");
        public static string DustTotalFormat => Loc.Pick("Итого пыли: +{0}", "Total stardust: +{0}");
        // итоги Бесконечного: {0} = номер цикла с единицы, {1} = убито боссов
        public static string EndlessResultFormat => Loc.Pick("Цикл {0} · Боссов убито: {1}", "Cycle {0} · Bosses killed: {1}");
        // что открыл забег
        public static string UnlockedEraFormat => Loc.Pick("Открыто: эра {0}", "Unlocked: era {0}");
        public static string UnlockedEndless => Loc.Pick("Открыто: режим БЕСКОНЕЧНЫЙ", "Unlocked: ENDLESS mode");
        public static string UnlockedSkinFormat => Loc.Pick("Открыто: облик «{0}»", "Unlocked: skin «{0}»");

        // кнопка с рекламой называет награду и говорит, что это реклама
        public static string ReviveTitle => Loc.Pick("ЗВЕЗДА ГАСНЕТ", "THE STAR IS FADING");
        public static string ReviveAd => Loc.Pick("ВОЗРОДИТЬСЯ (РЕКЛАМА)", "REVIVE (AD)");
        public static string GiveUp => Loc.Pick("СДАТЬСЯ", "GIVE UP");
        public static string DoubleDustAd => Loc.Pick("ПЫЛЬ ×2 (РЕКЛАМА)", "STARDUST ×2 (AD)");
        public static string NewCardsAd => Loc.Pick("НОВЫЕ КАРТОЧКИ (РЕКЛАМА)", "NEW CARDS (AD)");
        // бесплатная замена карточек: {0} = сколько осталось
        public static string FreeRerollFormat => Loc.Pick("ЗАМЕНИТЬ ({0})", "REROLL ({0})");

        // по порядку HintId
        public static string[] Hint => Loc.Pick(HintRu, HintEn);
        static readonly string[] HintRu =
        {
            "Держи — спутник уходит наружу",
            "Отпусти — спутник падает к звезде",
            "Перегрев! Держи, чтобы остыть",
            "Броня: бей внутри серебряного кольца",
            "Жми Q или кнопку: ИМПУЛЬС",
            "Резонанс: урон ×2",
        };
        static readonly string[] HintEn =
        {
            "Hold — the satellite moves out",
            "Release — the satellite falls to the star",
            "Overheat! Hold to cool down",
            "Armor: strike inside the silver ring",
            "Press Q or the button: PULSE",
            "Resonance: damage ×2",
        };

        // имя босса: над полосой HP и на баннере
        public static string BossTitle(EnemyType boss)
        {
            switch (boss)
            {
                case EnemyType.Twin: return Loc.Pick("БЛИЗНЕЦЫ", "TWINS");
                case EnemyType.Colossus: return Loc.Pick("КОЛОСС", "COLOSSUS");
                case EnemyType.Pulsar: return Loc.Pick("ПУЛЬСАР", "PULSAR");
                default: return Loc.Pick("КОМЕТА", "COMET");
            }
        }

        // по порядку config.eras
        public static string[] EraTitle => Loc.Pick(EraTitleRu, EraTitleEn);
        static readonly string[] EraTitleRu = { "СОЛНЦЕ", "КРАСНЫЙ КАРЛИК", "ТЯЖЁЛАЯ ЗВЕЗДА", "ЧЁРНАЯ ДЫРА" };
        static readonly string[] EraTitleEn = { "SUN", "RED DWARF", "HEAVY STAR", "BLACK HOLE" };

        // правило эры одной строкой, по порядку config.eras
        public static string[] EraRule => Loc.Pick(EraRuleRu, EraRuleEn);
        static readonly string[] EraRuleRu =
        {
            "Спокойная звезда. Здесь учатся держать орбиту.",
            "Вспышки: красный сектор греет спутники и жжёт врагов.",
            "Тяжесть: спутники взлетают медленнее, падают быстрее, кристаллы тонут в звезде.",
            "Воронка: враги летят к центру по спирали.",
        };
        static readonly string[] EraRuleEn =
        {
            "A calm star. Learn to hold your orbit here.",
            "Flares: a red sector heats satellites and burns enemies.",
            "Gravity: satellites rise slower, fall faster, crystals sink into the star.",
            "Vortex: enemies spiral toward the centre.",
        };

        // подсказка при первой встрече, по порядку EnemyType до Призрака; у Роя, Снаряда и Кометы её нет
        public static string[] EnemyTip => Loc.Pick(EnemyTipRu, EnemyTipEn);
        static readonly string[] EnemyTipRu =
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
        static readonly string[] EnemyTipEn =
        {
            "",
            "Arrow: fast and hits hard",
            "",
            "Bastion: armored — strike near the star or in resonance",
            "Parasite: latches onto the star — knock it off right at the star",
            "Shooter: stays far away and shoots",
            "Hive: bursts into a swarm near the star",
            "",
            "Splitter: breaks into a swarm when it dies",
            "Torch: heats your satellite on every hit",
            "Orbiter: circles against your satellites",
            "Shielder: the shield blocks one side. Hit the other or press REVERSE",
            "Devourer: eats crystals and grows",
            "Phantom: can't be hit while transparent",
        };

        // по порядку BlueprintId
        public static string[] BlueprintTitle => Loc.Pick(BlueprintTitleRu, BlueprintTitleEn);
        static readonly string[] BlueprintTitleRu =
        {
            "КОРПУС", "ЯДРО", "ОТРАЖАТЕЛЬ", "ВТОРОЕ СОЛНЦЕ", "ЗАКАЛКА", "КАМЕРТОН", "ОСТРИЁ", "ПАРА",
            "ТЯГА", "РАДИАТОР", "КОНДЕНСАТОР", "КВАДРИГА", "МАГНЕТИЗМ", "ОЗАРЕНИЕ", "АЛХИМИЯ", "ВЫБОР",
        };
        static readonly string[] BlueprintTitleEn =
        {
            "HULL", "CORE", "REFLECTOR", "SECOND SUN", "TEMPERING", "TUNING FORK", "EDGE", "PAIR",
            "THRUST", "RADIATOR", "CAPACITOR", "QUADRIGA", "MAGNETISM", "INSIGHT", "ALCHEMY", "CHOICE",
        };

        // текст узла, по порядку BlueprintId; {0} = что дают купленные уровни вместе (Blueprints.Total)
        public static string[] BlueprintText => Loc.Pick(BlueprintTextRu, BlueprintTextEn);
        static readonly string[] BlueprintTextRu =
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
        static readonly string[] BlueprintTextEn =
        {
            "Star: +8 max HP per level. Now +{0:0.##}",
            "The star heals +0.07 HP/s per level. Now +{0:0.##}",
            "Damage to the star −5% per level. Now −{0:0.#}%",
            "Once per run the star flares up instead of dying: 25% HP and a wave",
            "Strike +3% per level. Now +{0:0.#}%",
            "Resonance +0.1 per level. Now ×{0:0.##}",
            "Crit chance +3% per level. Now {0:0.#}%",
            "Runs start with two satellites",
            "Rise and fall +5% per level. Now +{0:0.#}%",
            "Heat −6% per level. Now −{0:0.#}%",
            "Ability cooldown −5% per level. Now −{0:0.#}%",
            "Allows a fourth satellite",
            "Crystal pull +0.3 per level. Now +{0:0.##}",
            "XP per level −5% per level. Now −{0:0.#}%",
            "Stardust per run +8% per level. Now +{0:0.#}%",
            "Lv 1: one free card reroll per run. Lv 2: choose from 4 cards",
        };

        // по порядку SkinId
        public static string[] SkinTitle => Loc.Pick(SkinTitleRu, SkinTitleEn);
        static readonly string[] SkinTitleRu =
        {
            "Ромб", "Спутник-1", "Шестерня", "Луна", "Звёздочка", "Глаз", "Солнце", "Око", "Подсолнух", "Мотор",
        };
        static readonly string[] SkinTitleEn =
        {
            "Rhomb", "Sputnik-1", "Gear", "Moon", "Red Star", "Eye", "Sun", "Oculus", "Sunflower", "Motor",
        };

        // по номеру испытания с нуля
        public static string[] ChallengeTitle => Loc.Pick(ChallengeTitleRu, ChallengeTitleEn);
        static readonly string[] ChallengeTitleRu =
        {
            "ОДИНОЧКА", "СТЕКЛЯННАЯ ЗВЕЗДА", "ВЫСОКАЯ ОРБИТА", "РОЙ", "БЕЗ СПОСОБНОСТЕЙ", "ЖАРА", "ВСТРЕЧНЫЙ ВЕТЕР", "МАРАФОН",
        };
        static readonly string[] ChallengeTitleEn =
        {
            "LONER", "GLASS STAR", "HIGH ORBIT", "SWARM", "NO ABILITIES", "HEATWAVE", "HEADWIND", "MARATHON",
        };

        // время вида мм:сс
        public static string Clock(float seconds)
        {
            int total = (int)seconds;
            return string.Format("{0:00}:{1:00}", total / SecondsPerMinute, total % SecondsPerMinute);
        }

        public static string CardPickTitle => Loc.Pick("ВЫБЕРИ КАРТОЧКУ", "CHOOSE A CARD");
        public const string MulFormat = "×{0:0.#}";
        public static string AbilityPickTitle => Loc.Pick("ВЫБЕРИ СПОСОБНОСТЬ", "CHOOSE AN ABILITY");
        //   = неразрывный пробел: число и «с» не разъезжаются по строкам
        public static string CooldownFormat => Loc.Pick("Перезарядка {0:0.#} с", "Cooldown {0:0.#} s");

        // по порядку AbilityId
        public static string[] AbilityTitle => Loc.Pick(AbilityTitleRu, AbilityTitleEn);
        static readonly string[] AbilityTitleRu = { "РЕВЕРС", "РОСТ", "ИМПУЛЬС", "СТАЗИС", "ЭХО", "ПРИТЯЖЕНИЕ", "КУПОЛ" };
        static readonly string[] AbilityTitleEn = { "REVERSE", "GROW", "PULSE", "STASIS", "ECHO", "PULL", "DOME" };

        // строка эффекта на экране выбора, по порядку AbilityId;   = неразрывный пробел между числом и «с»
        public static string[] AbilityText => Loc.Pick(AbilityTextRu, AbilityTextEn);
        static readonly string[] AbilityTextRu =
        {
            "Орбита разворачивается, 2 с удар ×1.5",
            "Спутники вдвое больше на 6 с",
            "Волна от звезды: мелочь гибнет, остальных отбрасывает",
            "Враги в 4 раза медленнее на 4 с",
            "У каждого спутника тень-двойник на 6 с",
            "Враги стягиваются на вашу орбиту на 3 с",
            "Звезда неуязвима 5 с, враги разбиваются о купол",
        };
        static readonly string[] AbilityTextEn =
        {
            "The orbit reverses, strike ×1.5 for 2 s",
            "Satellites twice as big for 6 s",
            "A wave from the star: small fry die, the rest are pushed back",
            "Enemies 4 times slower for 4 s",
            "Each satellite gets a shadow twin for 6 s",
            "Enemies are pulled onto your orbit for 3 s",
            "The star is invulnerable for 5 s, enemies shatter on the dome",
        };

        // на закрытой способности: цена в Мастерской
        public static string PriceFormat => Loc.Pick("{0} пыли", "{0} stardust");

        // по порядку CardId
        public static string[] CardTitle => Loc.Pick(CardTitleRu, CardTitleEn);
        static readonly string[] CardTitleRu =
        {
            "ТЯЖЁЛЫЙ СПУТНИК", "ДАЛЬНИЙ УДАР", "ОСТРЫЕ ГРАНИ", "БРОНЕБОЙ", "РАСКОЛ", "НИТЬ-ПИЛА", "РАСКАЛЁННЫЙ", "ЦЕПНАЯ РЕАКЦИЯ",
            "НОВЫЙ СПУТНИК", "РЕАКТИВНЫЙ ДВИГАТЕЛЬ", "РАЗГОН", "КРУПНЫЙ СПУТНИК", "МАГНИТ", "ЖАДНОСТЬ", "ОХЛАДИТЕЛЬ",
            "БРОНЯ ЗВЕЗДЫ", "ФОТОСФЕРА", "КОРОНА", "МАЯК", "ПЕРЕЗАРЯДКА", "ДОЛГОЕ ДЕЙСТВИЕ",
            "ЖАРОСТОЙКОСТЬ", "СОЛНЕЧНЫЙ ПАРУС", "ЖАР-ПТИЦА", "ПРАЩА", "ГРАВИЗАХВАТ", "ЛОМ", "ГОРИЗОНТ СОБЫТИЙ", "ТЁМНАЯ МАТЕРИЯ", "АККРЕЦИЯ",
        };
        static readonly string[] CardTitleEn =
        {
            "HEAVY SATELLITE", "FAR STRIKE", "SHARP EDGES", "ARMOR PIERCER", "SHATTER", "SAW THREAD", "RED-HOT", "CHAIN REACTION",
            "NEW SATELLITE", "JET ENGINE", "SPIN-UP", "BIG SATELLITE", "MAGNET", "GREED", "COOLANT",
            "STAR PLATING", "PHOTOSPHERE", "CORONA", "BEACON", "QUICK RECHARGE", "LONG EFFECT",
            "HEATPROOF", "SOLAR SAIL", "FIREBIRD", "SLINGSHOT", "GRAVITY GRIP", "CROWBAR", "EVENT HORIZON", "DARK MATTER", "ACCRETION",
        };

        // подпись на иконке взятой карточки, по порядку CardId
        public static string[] CardShort => Loc.Pick(CardShortRu, CardShortEn);
        static readonly string[] CardShortRu =
        {
            "ТС", "ДУ", "ОГ", "ББ", "РК", "НП", "РЛ", "ЦР", "НС", "РД", "РЗ", "КС", "МГ", "ЖД", "ОХ", "БЗ", "ФС", "КР", "МК", "ПЗ", "ДД",
            "ЖС", "СП", "ЖП", "ПР", "ГЗ", "ЛМ", "ГС", "ТМ", "АК",
        };
        static readonly string[] CardShortEn =
        {
            "HS", "FS", "SE", "AP", "SH", "ST", "RH", "CR", "NS", "JE", "SU", "BS", "MG", "GR", "CL", "PL", "PH", "CO", "BC", "QR", "LE",
            "HP", "SS", "FB", "SL", "GG", "CB", "EH", "DM", "AC",
        };

        // плюс карточки, по порядку CardId;   = неразрывный пробел между числом и «с»
        public static string[] CardPlus => Loc.Pick(CardPlusRu, CardPlusEn);
        static readonly string[] CardPlusRu =
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
        static readonly string[] CardPlusEn =
        {
            "Strike ×1.35",
            "Strike on a far orbit +0.5",
            "Crit chance +12% (crits hit ×2)",
            "Enemy armor −1",
            "An enemy at ≤ 25% HP dies to any hit",
            "The resonance thread cuts enemies",
            "A hot satellite sets on fire: 1 dmg/s, 3 s",
            "A killed enemy explodes: 1 dmg nearby",
            "+1 satellite",
            "Rise and fall ×1.5",
            "Spin ×1.25",
            "Satellite bigger ×1.35",
            "Crystal pull +1.3",
            "XP per crystal ×1.3",
            "Heat ×0.6",
            "Max HP +25 and heal 25",
            "The star heals 0.4 HP/s",
            "Damage to the star ×0.7",
            "A wave from the star every 8 s",
            "Ability cooldown ×0.75",
            "Abilities last ×1.4 longer",
            "Flares and Torches don't heat",
            "During a flare and 3 s after: strike ×2",
            "A satellite at 70%+ heat hits ×1.5",
            "1 s after release: strike ×1.5",
            "Crystals don't sink into the star",
            "Shielder shields break in 1 hit, Orbiters take ×2",
            "Enemies within 1.8 of the centre lose 2 HP/s",
            "Phantoms don't phase out",
            "A kill within 3.0 of the centre heals 1 HP",
        };

        // минус карточки, по порядку CardId
        public static string[] CardMinus => Loc.Pick(CardMinusRu, CardMinusEn);
        static readonly string[] CardMinusRu =
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
        static readonly string[] CardMinusEn =
        {
            "Rise and fall slower ×0.75",
            "Heat ×1.15",
            "Strike ×0.92",
            "Spin ×0.9",
            "Crystals last ×0.7",
            "Strike ×0.9",
            "Heat ×1.15",
            "XP per crystal ×0.9",
            "Heat ×1.2",
            "Heat ×1.2",
            "Heat ×1.15",
            "Spin ×0.9",
            "Arrows faster ×1.15",
            "Star max HP −15",
            "Strike ×0.9",
            "Spin ×0.9",
            "XP per crystal ×0.85",
            "Crystals last ×0.8",
            "Crystals last ×0.6",
            "Strike ×0.9",
            "Cooldown ×1.15",
            "Spin ×0.95",
            "Heat ×1.1",
            "Heat ×1.2",
            "Rise ×0.9",
            "Strike ×0.95",
            "Spin ×0.9",
            "Star max HP −10",
            "Strike ×0.95",
            "Crystals last ×0.8",
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
