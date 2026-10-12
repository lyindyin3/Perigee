using UnityEngine;

namespace Perigee
{
    // Серебряное кольцо на радиусе, где внутренний спутник бьёт ровно на броню Бастиона (armorBase минус БРОНЕБОЙ):
    // ближе к звезде удар пробивает броню, дальше нет.
    public class ArmorRing : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] Visual visual;
        // декор на кольце, в сцене выключен
        [SerializeField] Behaviour tutorialPulse;

        // обучение: кольцо мигает, пока на поле первый Бастион
        public void SetPulse(bool on)
        {
            if (tutorialPulse.enabled == on) return;
            tutorialPulse.enabled = on;
            // декор менял прозрачность кольца: возвращаем цвет из инспектора
            if (!on) visual.Untint();
        }

        // вызывает GameLoop после каждого пересчёта Stats
        public void Refresh()
        {
            float armor = config.armorBase - loop.Stats.armorPierce;
            // если брони не осталось или удар пробивает её и вдали, кольцо не нужно
            gameObject.SetActive(armor > 0f && satellites.Power(config.powerFarRadius) < armor);

            float far = config.powerFar + loop.Stats.powerFarBonus;
            float t = (config.powerNear - armor / loop.Stats.powerMul) / (config.powerNear - far);
            visual.SetSizeMul(config.rMin + Mathf.Clamp01(t) * (config.powerFarRadius - config.rMin));
        }
    }
}
