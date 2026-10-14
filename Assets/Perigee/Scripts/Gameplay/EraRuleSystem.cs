using UnityEngine;

namespace Perigee
{
    // Правило эры. Здесь идут вспышки Красного карлика, облака Туманности, луны Газового гиганта,
    // аномалии и тьма; тяжесть и воронку читают по Era.rule сами системы (SatelliteSystem, CrystalSystem, EnemySystem).
    // Объекты правил живут по обычному времени: СТАЗИС их не замедляет.
    public class EraRuleSystem : MonoBehaviour
    {
        // аномалия: сначала проявляется, потом действует, потом гаснет
        class Anomaly
        {
            public Visual well;
            public Visual repulsor;
            public Visual visual;
            public bool open;
            public bool isWell;
            public Vector2 centre;
            public float age;
        }

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] EnemySystem enemies;
        [SerializeField] BossSystem boss;
        [SerializeField] CrystalSystem crystals;
        [SerializeField] FlareSector sector;
        // облака Туманности и луны Газового гиганта: объекты сцены, по порядку чисел в конфиге
        [SerializeField] Transform[] clouds;
        [SerializeField] Transform[] moons;
        // родитель аномалий: их пул собирается в начале забега
        [SerializeField] Transform anomalyRoot;
        [SerializeField] DarknessMask darkness;
        [SerializeField] BannerView banner;
        [SerializeField] Fx fx;
        [SerializeField] AudioManager sfx;

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
        // секунды забега по обычному времени: по ним кружат облака и луны
        float clock;
        // общий сдвиг облаков и свой у каждой луны, градусы
        float cloudTurn;
        float[] moonTurn;
        // луна × спутник: секунды до следующего возможного манёвра
        float[,] assistCooldown;
        Anomaly[] anomalies;
        float nextAnomaly;
        // радиус света, который сейчас показан
        float light;
        // событие Затмение: тьма на время в эре без неё
        bool eclipse;

        // СОЛНЕЧНЫЙ ПАРУС: окно идёт весь удар вспышки и ещё solarSailAfter после него
        public bool Sailing => sinceBurst <= config.flareTime + config.solarSailAfter;

        void Awake()
        {
            moonTurn = new float[moons.Length];
            assistCooldown = new float[moons.Length, config.maxSatellites];
            anomalies = new Anomaly[config.anomalyMax];
            for (int i = 0; i < anomalies.Length; i++) anomalies[i] = new Anomaly();
        }

        public void ResetRun()
        {
            EraRule rule = loop.Era.rule;
            announced = false;
            nextFlare = config.flareFirst;
            flaring = false;
            burst = false;
            hasCentre = false;
            sinceBurst = float.PositiveInfinity;
            sector.Hide();
            clock = 0f;

            Rng rng = loop.Run.simRng;
            // случайный сдвиг тянется из зерна только в своей эре, чтобы другие эры шли как прежде
            bool nebula = rule == EraRule.Nebula;
            cloudTurn = nebula ? rng.Range(0f, 360f) : 0f;
            for (int i = 0; i < clouds.Length; i++) clouds[i].gameObject.SetActive(nebula);

            bool moon = rule == EraRule.Moons;
            for (int m = 0; m < moons.Length; m++)
            {
                moonTurn[m] = moon ? rng.Range(0f, 360f) : 0f;
                moons[m].gameObject.SetActive(moon);
            }
            System.Array.Clear(assistCooldown, 0, assistCooldown.Length);

            // аномалии собираются заново из префабов: правка префаба видна со следующего забега
            for (int i = anomalyRoot.childCount - 1; i >= 0; i--) Destroy(anomalyRoot.GetChild(i).gameObject);
            for (int i = 0; i < anomalies.Length; i++)
            {
                Anomaly a = anomalies[i];
                a.well = Instantiate(art.anomalyWell, anomalyRoot);
                a.repulsor = Instantiate(art.anomalyRepulsor, anomalyRoot);
                a.well.gameObject.SetActive(false);
                a.repulsor.gameObject.SetActive(false);
                a.open = false;
            }
            nextAnomaly = config.anomalyFirst;

            bool dark = rule == EraRule.Darkness;
            eclipse = false;
            darkness.gameObject.SetActive(dark);
            light = config.lightBase;
            if (dark) darkness.Show(light, 1f);
            Place();
        }

        public void Tick(float dt)
        {
            EraDef era = loop.Era;
            if (!announced)
            {
                // первый шаг забега: правило эры строкой сверху (у первой эры правила нет)
                announced = true;
                if (era.rule != EraRule.None) banner.Show(Strings.EraRule[loop.EraNumber - 1], null, Color.white, config.bannerRuleHold);
            }

            clock += dt;
            switch (era.rule)
            {
                case EraRule.Flares: Flares(dt); break;
                case EraRule.Nebula: Nebula(); break;
                case EraRule.Moons: Moons(dt); break;
                case EraRule.Anomalies: Anomalies(dt); break;
                case EraRule.Darkness: Darkness(dt, config.lightBase); break;
            }
            if (eclipse) Darkness(dt, config.eclipseLight);
        }

        // событие Затмение (RunEventSystem): правило тьмы со своим базовым светом, пока событие идёт.
        // В Сердце Тьмы его не бывает
        public void Eclipse(bool on)
        {
            eclipse = on;
            darkness.gameObject.SetActive(on);
            light = config.eclipseLight;
            if (on) darkness.Show(light, 1f);
        }

        // точка внутри одного из облаков Туманности (ЗАСЕВ)
        public bool InCloud(Vector2 p)
        {
            if (loop.Era.rule != EraRule.Nebula) return false;
            float r = config.nebulaRadius;
            for (int i = 0; i < clouds.Length; i++)
                if ((p - (Vector2)clouds[i].localPosition).sqrMagnitude <= r * r) return true;
            return false;
        }

        // облака и луны идут по своим орбитам
        void Place()
        {
            for (int i = 0; i < clouds.Length; i++)
                clouds[i].localPosition = Polar(config.nebulaStart[i] + cloudTurn + config.nebulaDeg[i] * clock, config.nebulaOrbits[i]);
            for (int m = 0; m < moons.Length; m++)
                moons[m].localPosition = Polar(moonTurn[m] + config.moonDirs[m] * 360f * clock / config.moonPeriods[m], config.moonOrbits[m]);
        }

        // Туманность: спутник в облаке остывает, враги в нём вязнут
        void Nebula()
        {
            Place();
            float r = config.nebulaRadius;
            for (int i = 0; i < clouds.Length; i++)
            {
                Vector2 c = clouds[i].localPosition;
                enemies.Field(c, r, 0f, config.nebulaSlow, 0f);
                for (int k = 0; k < satellites.Count; k++)
                    if ((satellites.HitterPosition(k) - c).sqrMagnitude <= r * r) satellites.Fog(k);
            }
        }

        // Газовый гигант: луны гнут орбиту и стягивают к себе врагов, пролёт рядом даёт гравиманёвр
        void Moons(float dt)
        {
            Place();
            bool tidal = loop.Stats.Has(StatId.Tidal);
            float assist = config.moonAssistRadius;
            for (int m = 0; m < moons.Length; m++)
            {
                Vector2 c = moons[m].localPosition;
                satellites.Gravity(c, config.moonWellRadius, config.moonBend);
                enemies.Field(c, config.moonPullRadius, config.moonPull, 1f, 0f);
                // ПРИЛИВНЫЕ СИЛЫ: у луны враги горят
                if (tidal) enemies.Field(c, config.tidalRadius, 0f, 1f, config.tidalDps);

                for (int k = 0; k < satellites.Count; k++)
                {
                    assistCooldown[m, k] -= dt;
                    if (assistCooldown[m, k] > 0f || !satellites.HitterActive(k)) continue;
                    if ((satellites.HitterPosition(k) - c).sqrMagnitude > assist * assist) continue;

                    assistCooldown[m, k] = config.moonAssistCooldown;
                    satellites.Assist(k);
                }
            }
        }

        // Аномалия: колодцы и отталкиватели открываются по расписанию, не больше anomalyMax сразу
        void Anomalies(float dt)
        {
            if (loop.Run.time >= nextAnomaly)
            {
                nextAnomaly += config.anomalyInterval;
                Open();
            }

            bool singularity = loop.Stats.Has(StatId.Singularity);
            float radius = config.anomalyRadius;
            for (int i = 0; i < anomalies.Length; i++)
            {
                Anomaly a = anomalies[i];
                if (!a.open) continue;

                a.age += dt;
                float end = config.anomalyWarn + config.anomalyLife;
                if (a.age >= end + config.anomalyFade)
                {
                    a.open = false;
                    a.visual.gameObject.SetActive(false);
                    continue;
                }
                // проявляется, держится, гаснет; действует только между проявлением и угасанием
                if (a.age < config.anomalyWarn)
                {
                    a.visual.SetAlpha(a.age / config.anomalyWarn);
                    continue;
                }
                if (a.age >= end)
                {
                    a.visual.SetAlpha(1f - (a.age - end) / config.anomalyFade);
                    continue;
                }
                a.visual.SetAlpha(1f);

                float sign = a.isWell ? 1f : -1f;
                satellites.Gravity(a.centre, radius, sign * config.anomalyBend);
                // СИНГУЛЯРНОСТЬ: в колодце враги горят
                enemies.Field(a.centre, radius, sign * config.anomalyPull, 1f, a.isWell && singularity ? config.singularityDps : 0f);
                if (a.isWell) crystals.Pull(a.centre, radius, config.anomalyCrystalPull * dt);
            }
        }

        // новая аномалия в свободном месте пула; если открыто уже anomalyMax, этот срок пропускается
        void Open()
        {
            for (int i = 0; i < anomalies.Length; i++)
            {
                Anomaly a = anomalies[i];
                if (a.open) continue;

                Rng rng = loop.Run.simRng;
                a.open = true;
                a.age = 0f;
                a.isWell = rng.Range(0, 2) == 0;
                a.centre = Polar(rng.Range(0f, 360f), rng.Range(config.anomalyMinR, config.anomalyMaxR));
                a.visual = a.isWell ? a.well : a.repulsor;
                // Visual запоминает цвет при включении, поэтому сначала объект, потом прозрачность
                a.visual.gameObject.SetActive(true);
                a.visual.transform.localPosition = a.centre;
                a.visual.SetAlpha(0f);
                sfx.Play(SfxId.AnomalyOpen);
                return;
            }
        }

        // Сердце Тьмы: свет вокруг внешней орбиты; Затмители и ЗАТМЕНИЕ Бездны его срезают, ФОНАРЬ расширяет
        void Darkness(float dt, float lightBase)
        {
            Stats stats = loop.Stats;
            float target = Mathf.Max(lightBase, satellites.OuterRadius + config.lightAround) + stats.Sum(StatId.Lantern) - enemies.LightCut - boss.LightCut;
            target = Mathf.Max(config.lightMin, target);
            light = Mathf.Lerp(light, target, config.lightLerp * dt);
            // НОЧНОЕ ЗРЕНИЕ: сквозь тьму видны силуэты
            darkness.Show(light, stats.Has(StatId.NightSight) ? config.nightSightAlpha : 1f);
        }

        void Flares(float dt)
        {
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
                sfx.Play(SfxId.FlareBurst);
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
            sfx.Play(SfxId.FlareWarn);

            // новый сектор не ближе flareMinGap к прошлому
            Rng rng = loop.Run.simRng;
            centre = hasCentre ? Mathf.Repeat(centre + rng.Range(config.flareMinGap, 360f - config.flareMinGap), 360f) : rng.Range(0f, 360f);
            hasCentre = true;
        }

        static Vector2 Polar(float degrees, float r)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r);
        }
    }
}
