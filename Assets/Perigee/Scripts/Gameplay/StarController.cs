using UnityEngine;

namespace Perigee
{
    public class StarController : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] Visual visual;
        [SerializeField] SpriteRenderer rays;
        [SerializeField] Fx fx;
        [SerializeField] AudioManager sfx;

        // цвет лучей из инспектора: эра его умножает
        Color raysColor;
        // накопленное лечение ФОТОСФЕРЫ, HP
        float regenBank;

        void Awake()
        {
            raysColor = rays.color;
        }

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

        // АККРЕЦИЯ: лечение сразу, не выше максимума; стеклянную звезду не лечит ничто
        public void Heal(float amount)
        {
            if (loop.Run.Has(RunModifier.GlassStar)) return;
            loop.Run.hp = Mathf.Min(loop.Run.hp + amount, loop.Stats.starMaxHp);
        }

        // после возрождения и под КУПОЛОМ звезда не получает урон ни от чего
        bool Immune => loop.Run.time < loop.Run.immuneUntil || abilities.Active(AbilityId.Dome);

        public void ResetRun()
        {
            regenBank = 0f;
            // облик звезды: картинки тела и лучей, выбранные в Мастерской; своя картинка эры важнее тела облика
            StarSkin skin = art.starSkins[loop.Shop.StarSkin];
            Sprite eraSprite = art.eraStarSprites[loop.EraNumber - 1];
            visual.Body.sprite = eraSprite != null ? eraSprite : skin.body;
            rays.sprite = skin.rays;
            // звезда и лучи в цвете эры
            visual.Multiply(loop.Era.starTint);
            rays.color = raysColor * loop.Era.raysTint;
        }

        public void Tick(float dt)
        {
            RunState run = loop.Run;
            if (run.hp <= 0f)
            {
                if (!loop.Stats.SecondSun || run.secondSunUsed)
                {
                    loop.Lose();
                    return;
                }

                // ВТОРОЕ СОЛНЦЕ: раз за забег звезда вспыхивает вместо гибели, раньше экрана поражения и возрождения за рекламу
                run.secondSunUsed = true;
                run.hp = loop.Stats.starMaxHp * config.secondSunHp;
                run.immuneUntil = run.time + config.reviveImmuneTime;
                abilities.PulseWave();
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
