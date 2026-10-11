namespace Perigee
{
    public class RunState
    {
        public int seed;
        public float hp;
        public float time;

        public void Reset(GameConfig config, int runSeed)
        {
            seed = runSeed;
            hp = config.starMaxHp;
            time = 0f;
        }
    }
}
