namespace Perigee
{
    // по номеру режим пишется в SaveData.lastMode
    public enum RunMode { Run, Endless, Daily, Challenge }

    public class RunState
    {
        public int seed;
        // режим ставит GameLoop после Reset
        public RunMode mode;
        // hp выставляет GameLoop после пересчёта Stats: максимум зависит от чертежей
        public float hp;
        public float time;
        public int dust;
        // дробный: карточки меняют опыт за кристалл
        public float xp;
        public int level;
        public int kills;
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

        // итоги: Progress заполняет их перед экраном Result
        public bool victory;
        // пыль по частям: кристаллы × множители, премия за победу, премия за первую победу в эре
        public int dustCrystals;
        public float dustMul;
        public int dustVictory;
        public int dustFirstWin;
        // первый за дату дневной забег: сумма удвоена
        public bool dailyDoubled;
        // «Пыль ×2» за рекламу уже взята
        public bool adDoubled;
        // вся пыль забега
        public int earned;
        public int score;
        // сколько секунд не хватило до рекорда эры; ноль и меньше = новый рекорд
        public float recordGap;
        // номер открытой этим забегом эры в config.eras; 0 = ничего, первая эра открыта всегда
        public int unlockedEra;

        public void Reset(GameConfig config, int runSeed)
        {
            seed = runSeed;
            time = 0f;
            dust = 0;
            xp = 0f;
            level = 0;
            kills = 0;
            cardStacks = new int[config.cards.Length];
            simRng = new Rng(runSeed + Rng.SimOffset);
            tutorial = false;
            revived = false;
            secondSunUsed = false;
            freeRerollsUsed = 0;
            immuneUntil = 0f;
        }
    }
}
