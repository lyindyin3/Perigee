namespace Perigee
{
    public class Stats
    {
        public float radialSpeedMul;
        public float spinMul;
        public float powerMul;
        public float powerFarBonus;

        public void Reset(EraConfig era)
        {
            radialSpeedMul = 1f;
            spinMul = era.spinMul;
            powerMul = 1f;
            powerFarBonus = 0f;
        }
    }
}
