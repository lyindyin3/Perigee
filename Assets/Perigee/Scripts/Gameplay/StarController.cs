using UnityEngine;

namespace Perigee
{
    public class StarController : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] Visual visual;

        public void Damage(float amount)
        {
            loop.Run.hp = Mathf.Max(loop.Run.hp - amount, 0f);
        }

        public void Tick()
        {
            if (loop.Run.hp <= 0f) loop.SetState(GameState.Defeat);
        }

        void Update()
        {
            // ниже порога звезда краснеет тем сильнее, чем меньше HP
            float hpFraction = loop.Run.hp / loop.Stats.starMaxHp;
            visual.Tint(config.starHitFlashColor, 1f - Mathf.Clamp01(hpFraction / config.starLowHpFraction));
        }
    }
}
