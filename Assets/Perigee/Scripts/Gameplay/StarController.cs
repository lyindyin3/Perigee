using UnityEngine;

namespace Perigee
{
    public class StarController : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] Visual visual;
        [SerializeField] Fx fx;
        [SerializeField] AudioManager sfx;

        // удар по звезде: вспышка, тряска и hit-stop
        public void Damage(float amount)
        {
            if (Immune) return;
            Drain(amount);
            visual.Flash(config.starHitFlashColor, config.starFlashTime);
            fx.Shake(config.starShakeAmp, config.starShakeTime);
            fx.HitStop(config.hitStopTime);
            sfx.Play(SfxId.StarHit);
        }

        // Паразит пьёт понемногу каждый шаг: урон без эффектов удара
        public void Drain(float amount)
        {
            if (Immune) return;
            loop.Run.hp = Mathf.Max(loop.Run.hp - amount, 0f);
        }

        // после возрождения звезда недолго не получает урон
        bool Immune => loop.Run.time < loop.Run.immuneUntil;

        public void Tick()
        {
            if (loop.Run.hp <= 0f) loop.Lose();
        }

        void Update()
        {
            // ниже порога звезда краснеет тем сильнее, чем меньше HP
            float hpFraction = loop.Run.hp / loop.Stats.starMaxHp;
            visual.Tint(config.starHitFlashColor, 1f - Mathf.Clamp01(hpFraction / config.starLowHpFraction));
        }
    }
}
