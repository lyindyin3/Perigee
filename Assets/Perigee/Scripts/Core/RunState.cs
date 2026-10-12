namespace Perigee
{
    public class RunState
    {
        public int seed;
        // hp выставляет GameLoop после пересчёта Stats: максимум зависит от чертежей
        public float hp;
        public float time;
        public int dust;
        public int xp;
        public int level;
        // стаки по индексу карточки в config.cards
        public int[] cardStacks;
        public Rng simRng;
        // первый забег: вступление-обучение и подсказки; ставит GameLoop после Reset
        public bool tutorial;
        // возрождение за рекламу уже использовано
        public bool revived;
        // до этой секунды забега звезда не получает урон
        public float immuneUntil;

        // итоги: GameLoop заполняет их перед экраном Result
        public bool victory;
        public int earned;
        // сколько секунд не хватило до рекорда; ноль и меньше = новый рекорд
        public float recordGap;
        // номер открытой этим забегом эры в config.eras; 0 = ничего, первая эра открыта всегда
        public int unlockedEra;

        public void Reset(GameConfig config, int runSeed)
        {
            seed = runSeed;
            time = 0f;
            dust = 0;
            xp = 0;
            level = 0;
            cardStacks = new int[config.cards.Length];
            simRng = new Rng(runSeed + Rng.SimOffset);
            tutorial = false;
            revived = false;
            immuneUntil = 0f;
        }
    }
}
