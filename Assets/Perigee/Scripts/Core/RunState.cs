namespace Perigee
{
    public class RunState
    {
        public int seed;
        public float hp;
        public float time;
        public int dust;
        public int xp;
        public int level;
        // стаки по индексу карточки в config.cards
        public int[] cardStacks;
        public Rng simRng;

        public void Reset(GameConfig config, int runSeed)
        {
            seed = runSeed;
            hp = config.starMaxHp;
            time = 0f;
            dust = 0;
            xp = 0;
            level = 0;
            cardStacks = new int[config.cards.Length];
            simRng = new Rng(runSeed + Rng.SimOffset);
        }
    }
}
