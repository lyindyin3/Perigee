namespace Perigee
{
    public class Stats
    {
        public float radialSpeedMul;
        public float spinMul;

        public void Reset(EraConfig era)
        {
            radialSpeedMul = 1f;
            spinMul = era.spinMul;
        }
    }
}
