using System;

namespace Perigee
{
    // по номеру режим пишется в SaveData.lastMode; первые три идут как вкладки экрана эр
    public enum RunMode { Run, Endless, Daily, Challenge }

    // Модификаторы забега: один у Дня, свои у каждого испытания. По номеру бита идут строки в Strings.
    [Flags]
    public enum RunModifier
    {
        None = 0,
        SingleSat = 1 << 0,
        GlassStar = 1 << 1,
        HighOrbit = 1 << 2,
        NoAbilities = 1 << 3,
        SwarmOnly = 1 << 4,
        DoubleHeat = 1 << 5,
        AutoReverse = 1 << 6,
        Marathon = 1 << 7,
        FastEnemies = 1 << 8,
        DoubleArrows = 1 << 9,
    }

    public class RunState
    {
        public int seed;
        // режим, модификаторы и номер испытания ставит GameLoop после Reset
        public RunMode mode;
        public RunModifier mods;
        // номер в config.challenges; имеет смысл только в режиме Challenge
        public int challenge;
        // hp выставляет GameLoop после пересчёта Stats: максимум зависит от чертежей
        public float hp;
        public float time;
        public int dust;
        // дробный: карточки меняют опыт за кристалл
        public float xp;
        public int level;
        public int kills;
        // Бесконечный: номер цикла расписания с нуля и сколько боссов убито
        public int cycle;
        public int bossKills;
        // стаки по индексу карточки в config.cards
        public int[] cardStacks;
        public Rng simRng;
        // первый забег: вступление-обучение и подсказки; ставит GameLoop после Reset
        public bool tutorial;
        // возрождение за рекламу уже использовано
        public bool revived;
        // ВТОРОЕ СОЛНЦЕ уже вспыхнуло в этом забеге
        public bool secondSunUsed;
        // сколько бесплатных замен карточек (чертёж ВЫБОР) уже потрачено
        public int freeRerollsUsed;
        // до этой секунды забега звезда не получает урон
        public float immuneUntil;
        // лучшая серия комбо в забеге
        public int bestCombo;
        // подобрано кристаллов за забег (ПРИЛИВ)
        public int crystalsPicked;
        // бит на каждый CardId: эволюцию уже показывали, первое место ей больше не положено
        public long evoShown;

        // итоги: Progress заполняет их перед экраном Result
        public bool victory;
        // пыль по частям: кристаллы × множители, премия за победу, за первую победу в эре, за боссов Бесконечного, награда испытания
        public int dustCrystals;
        public float dustMul;
        public int dustVictory;
        public int dustFirstWin;
        public int dustBosses;
        public int dustReward;
        // первый за дату дневной забег: сумма удвоена
        public bool dailyDoubled;
        // «Пыль ×2» за рекламу уже взята
        public bool adDoubled;
        // вся пыль забега
        public int earned;
        public int score;
        // сколько секунд не хватило до рекорда эры; ноль и меньше = новый рекорд
        public float recordGap;
        // что открыл этот забег: номер эры в config.eras (0 = ничего, первая открыта всегда), режим Бесконечный, облики (бит на SkinId)
        public int unlockedEra;
        public bool unlockedEndless;
        public int unlockedSkins;

        public bool Has(RunModifier modifier)
        {
            return (mods & modifier) != 0;
        }

        public void Reset(GameConfig config, int runSeed)
        {
            seed = runSeed;
            time = 0f;
            dust = 0;
            xp = 0f;
            level = 0;
            kills = 0;
            cycle = 0;
            bossKills = 0;
            cardStacks = new int[config.cards.Length];
            simRng = new Rng(runSeed + Rng.SimOffset);
            tutorial = false;
            revived = false;
            secondSunUsed = false;
            freeRerollsUsed = 0;
            immuneUntil = 0f;
            bestCombo = 0;
            crystalsPicked = 0;
            evoShown = 0;
        }
    }
}
