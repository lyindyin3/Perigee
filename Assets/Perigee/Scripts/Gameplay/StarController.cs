using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;

namespace Perigee
{
    public class StarController : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] ComboSystem combo;
        [SerializeField] EnemySystem enemies;
        [SerializeField] BossSystem boss;
        [SerializeField] RelicSystem relics;
        [SerializeField] Visual visual;
        [SerializeField] SpriteRenderer rays;
        [SerializeField] Fx fx;
        [SerializeField] ScreenFx screenFx;
        [SerializeField] AudioManager sfx;
        // кольцо волны Нейтронной звезды: только рисунок
        [SerializeField] Visual waveRing;

        // цвет лучей из инспектора: эра его умножает
        Color raysColor;
        // накопленное лечение ФОТОСФЕРЫ, HP
        float regenBank;
        // аура класса, созданная в начале забега (у Жёлтого её нет)
        Visual aura;
        // секунды до волны Нейтронной
        float pulseTimer;
        // 0 = обычный радиус Цефеиды, 1 = раздутый в яркой фазе
        float breath;
        float wave;
        DOGetter<float> getWave;
        DOSetter<float> setWave;
        TweenCallback hideWave;

        void Awake()
        {
            raysColor = rays.color;
            getWave = () => wave;
            setWave = SetWave;
            hideWave = HideWave;
        }

        // класс звезды этого забега
        public StarClassDef Class => config.starClasses[(int)loop.Run.starClass];

        // радиус звезды сейчас: у Цефеиды он дышит
        public float Radius => Class.starRadius * Mathf.Lerp(1f, config.breathRadiusMul, breath);

        // яркая фаза дыхания Цефеиды (у других классов ни на что не влияет)
        public bool Bright => loop.Run.time % config.breathCycle < config.breathBright;

        // удар по звезде: вспышка, тряска и hit-stop
        public void Damage(float amount)
        {
            if (Immune) return;
            // ЩИТ ПРЕДКОВ гасит удар: звезда вспыхивает белым, комбо цело
            if (relics.Aegis())
            {
                visual.Flash(config.aegisFlashColor, config.starFlashTime);
                return;
            }
            Drain(amount);
            // удар по звезде обрывает комбо (Паразит, который пьёт, нет)
            combo.Break();

            visual.Flash(config.starHitFlashColor, config.starFlashTime);
            Punch(config.starHitPunch, config.starHitPunchTime);
            screenFx.StarHit();
            fx.Shake(config.starShakeAmp, config.starShakeTime);
            fx.HitStop(config.hitStopTime);
            sfx.Play(SfxId.StarHit);
            loop.Vibrate(config.vibrateStarHit);
        }

        // звезда на миг раздувается (удар, старт забега)
        public void Punch(float amount, float sec)
        {
            visual.Punch(amount, sec);
        }

        // Паразит пьёт понемногу каждый шаг: урон без эффектов удара
        public void Drain(float amount)
        {
            if (Immune) return;
            // КОРОНА ослабляет любой урон по звезде
            loop.Run.hp = Mathf.Max(loop.Run.hp - amount * loop.Stats.starDamageMul, 0f);
            loop.Run.starDamaged = true;
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
            pulseTimer = config.autoPulseInterval;
            breath = 0f;
            // облик звезды: картинки тела и лучей, выбранные в Мастерской; своя картинка эры важнее тела облика
            StarSkin skin = art.starSkins[loop.Shop.StarSkin];
            Sprite eraSprite = art.eraStarSprites[loop.EraNumber - 1].sprite;
            visual.Body.sprite = eraSprite != null ? eraSprite : skin.body;
            rays.sprite = skin.rays;
            // звезда в цвете эры и класса, лучи в цвете эры
            StarClassDef def = Class;
            visual.Multiply(loop.Era.starTint * def.tint);
            rays.color = raysColor * loop.Era.raysTint;
            visual.SetSizeMul(def.starRadius);

            // аура класса: декор, ребёнок звезды
            if (aura != null) Destroy(aura.gameObject);
            Visual prefab = art.starClasses[(int)def.id].aura;
            aura = prefab != null ? Instantiate(prefab, transform) : null;
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
                sfx.Play(SfxId.SecondSun);
            }

            Traits(dt);

            // лечение копится и приходит порциями по regenStep: так на полоске виден золотой «хвост»
            float max = loop.Stats.starMaxHp;
            regenBank = run.hp < max ? regenBank + loop.Stats.regen * dt : 0f;
            if (regenBank < config.regenStep) return;

            regenBank -= config.regenStep;
            run.hp = Mathf.Min(run.hp + config.regenStep, max);
        }

        // черты класса и ШИПЫ
        void Traits(float dt)
        {
            StarClassDef def = Class;
            // ШИПЫ (мантия Красного гиганта): поле жжёт врагов у поверхности со следующего шага
            float thorns = loop.Stats.Sum(StatId.Thorns);
            if (thorns > 0f) enemies.Field(Vector2.zero, Radius + config.thornsGap, 0f, 1f, config.thornsDps * thorns);

            if (def.trait == StarTrait.Breath)
            {
                breath = Mathf.MoveTowards(breath, Bright ? 1f : 0f, dt / config.breathGrowTime);
                visual.SetSizeMul(Radius);
            }

            if (def.trait != StarTrait.AutoPulse) return;
            pulseTimer -= dt;
            if (pulseTimer > 0f) return;

            // Автоимпульс: Рой и Стрелы гибнут, остальные и босс получают урон, никого не отбрасывает
            pulseTimer += config.autoPulseInterval;
            enemies.Pulse(config.autoPulseRadius, config.autoPulseDamage, 0f);
            boss.Blast(Vector2.zero, config.autoPulseRadius, config.autoPulseDamage);
            sfx.Play(SfxId.Pulse);

            waveRing.gameObject.SetActive(true);
            DOTween.Kill(waveRing);
            SetWave(0f);
            DOTween.To(getWave, setWave, 1f, config.pulseTime).SetTarget(waveRing).SetLink(waveRing.gameObject).OnComplete(hideWave);
        }

        void SetWave(float value)
        {
            wave = value;
            waveRing.SetSizeMul(value * config.autoPulseRadius);
            waveRing.SetAlpha(1f - value);
        }

        void HideWave()
        {
            waveRing.gameObject.SetActive(false);
        }

        void Update()
        {
            // ниже порога звезда краснеет тем сильнее, чем меньше HP
            float hpFraction = loop.Run.hp / loop.Stats.starMaxHp;
            visual.Tint(config.starHitFlashColor, 1f - Mathf.Clamp01(hpFraction / config.starLowHpFraction));
        }
    }
}
