namespace Perigee
{
    public class Rng
    {
        // сдвиги зерна забега: spawnRng = seed, cardRng = seed + 1, simRng = seed + 2
        public const int CardOffset = 1;
        public const int SimOffset = 2;

        readonly System.Random random;

        public Rng(int seed)
        {
            random = new System.Random(seed);
        }

        public float Range(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        // max не включается
        public int Range(int min, int max)
        {
            return random.Next(min, max);
        }
    }
}
