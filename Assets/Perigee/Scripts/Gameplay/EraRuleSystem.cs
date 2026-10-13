using UnityEngine;

namespace Perigee
{
    // Правило эры. Вспышки Красного карлика идут здесь; тяжесть и воронку читают по Era.rule
    // сами системы (SatelliteSystem, CrystalSystem, EnemySystem).
    public class EraRuleSystem : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] EnemySystem enemies;
        [SerializeField] FlareSector sector;
        [SerializeField] BannerView banner;
        [SerializeField] Fx fx;

        // правило эры уже показано строкой сверху
        bool announced;
        // секунда забега, когда начнётся следующая вспышка
        float nextFlare;
        // вспышка идёт: сначала предупреждение, потом удар
        bool flaring;
        bool burst;
        // секунды с начала предупреждения
        float age;
        // середина сектора, градусы; у первой вспышки прошлого сектора нет
        float centre;
        bool hasCentre;
        // секунды с начала последнего удара
        float sinceBurst = float.PositiveInfinity;

        // СОЛНЕЧНЫЙ ПАРУС: окно идёт весь удар вспышки и ещё solarSailAfter после него
        public bool Sailing => sinceBurst <= config.flareTime + config.solarSailAfter;

        public void ResetRun()
        {
            announced = false;
            nextFlare = config.flareFirst;
            flaring = false;
            burst = false;
            hasCentre = false;
            sinceBurst = float.PositiveInfinity;
            sector.Hide();
        }

        // вспышки идут по обычному времени: СТАЗИС их не замедляет
        public void Tick(float dt)
        {
            EraDef era = loop.Era;
            if (!announced)
            {
                // первый шаг забега: правило эры строкой сверху (у первой эры правила нет)
                announced = true;
                if (era.rule != EraRule.None) banner.Show(Strings.EraRule[loop.EraNumber - 1], null, Color.white, config.bannerRuleHold);
            }
            if (era.rule != EraRule.Flares) return;

            sinceBurst += dt;
            if (!flaring)
            {
                if (loop.Run.time < nextFlare) return;
                Begin();
            }

            age += dt;
            if (!burst)
            {
                if (age < config.flareWarn)
                {
                    sector.Warn(centre, age);
                    return;
                }

                // удар: враги в секторе получают урон сразу, спутники греются, пока он идёт
                burst = true;
                sinceBurst = 0f;
                sector.Burst();
                enemies.Flare(centre, config.flareArc * 0.5f, config.flareDamage);
                fx.Shake(config.flareShakeAmp, config.flareShakeTime);
            }
            if (age >= config.flareWarn + config.flareTime)
            {
                flaring = false;
                sector.Hide();
                return;
            }
            if (loop.Stats.Heatproof) return;

            float heat = config.flareHeatRate * loop.Stats.heatMul * dt;
            for (int i = 0; i < satellites.Count; i++)
                if (Mathf.Abs(Mathf.DeltaAngle(satellites.Angle(i), centre)) <= config.flareArc * 0.5f) satellites.Warm(i, heat);
        }

        void Begin()
        {
            flaring = true;
            burst = false;
            age = 0f;
            nextFlare += config.flareInterval;

            // новый сектор не ближе flareMinGap к прошлому
            Rng rng = loop.Run.simRng;
            centre = hasCentre ? Mathf.Repeat(centre + rng.Range(config.flareMinGap, 360f - config.flareMinGap), 360f) : rng.Range(0f, 360f);
            hasCentre = true;
        }
    }
}
