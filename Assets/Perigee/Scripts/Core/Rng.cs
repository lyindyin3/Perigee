namespace Perigee
{
    public class Rng
    {
        readonly System.Random random;

        public Rng(int seed)
        {
            random = new System.Random(seed);
        }

        public float Range(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
