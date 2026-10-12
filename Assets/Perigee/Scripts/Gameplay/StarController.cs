using UnityEngine;

namespace Perigee
{
    public class StarController : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] Visual visual;
        [SerializeField] Fx fx;
        [SerializeField] AudioManager sfx;

        // накопленное лечение ФОТОСФЕРЫ, HP
        float regenBank;

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
            // КОРОНА ослабляет любой урон по звезде
            loop.Run.hp = Mathf.Max(loop.Run.hp - amount * loop.Stats.starDamageMul, 0f);
        }

        // после возрождения и под КУПОЛОМ звезда не получает урон ни от чего
        bool Immune => loop.Run.time < loop.Run.immuneUntil || abilities.Active(AbilityId.Dome);

        public void ResetRun()
        {
            regenBank = 0f;
        }

        public void Tick(float dt)
        {
            RunState run = loop.Run;
            if (run.hp <= 0f)
            {
                loop.Lose();
                return;
            }

            // лечение копится и приходит порциями по regenStep: так на полоске виден золотой «хвост»
            float max = loop.Stats.starMaxHp;
            regenBank = run.hp < max ? regenBank + loop.Stats.regen * dt : 0f;
            if (regenBank < config.regenStep) return;

            regenBank -= config.regenStep;
            run.hp = Mathf.Min(run.hp + config.regenStep, max);
        }

        void Update()
        {
            // ниже порога звезда краснеет тем сильнее, чем меньше HP
            float hpFraction = loop.Run.hp / loop.Stats.starMaxHp;
            visual.Tint(config.starHitFlashColor, 1f - Mathf.Clamp01(hpFraction / config.starLowHpFraction));
        }
    }
}
