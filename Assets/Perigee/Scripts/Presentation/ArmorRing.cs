using UnityEngine;

namespace Perigee
{
    // Серебряное кольцо на радиусе, где внутренний спутник бьёт ровно на armorBase:
    // ближе к звезде удар пробивает броню, дальше нет.
    public class ArmorRing : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] Visual visual;

        // вызывает GameLoop после каждого пересчёта Stats
        public void Refresh()
        {
            // если удар пробивает броню и вдали, кольцо не нужно
            gameObject.SetActive(satellites.Power(config.powerFarRadius) < config.armorBase);

            float far = config.powerFar + loop.Stats.powerFarBonus;
            float t = (config.powerNear - config.armorBase / loop.Stats.powerMul) / (config.powerNear - far);
            visual.SetSizeMul(config.rMin + Mathf.Clamp01(t) * (config.powerFarRadius - config.rMin));
        }
    }
}
