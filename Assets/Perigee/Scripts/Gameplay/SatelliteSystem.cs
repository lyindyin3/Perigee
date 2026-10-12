using System.Collections.Generic;
using UnityEngine;

namespace Perigee
{
    public class SatelliteSystem : MonoBehaviour
    {
        // в начале забега орбита идёт по часовой стрелке: угол убывает
        const int Clockwise = -1;
        // хиттеров вдвое больше спутников: у каждого спутника своя тень ЭХА
        const int HittersPerSatellite = 2;

        class Satellite
        {
            public Visual visual;
            public Transform transform;
            public HeatArc heatArc;
            // тень ЭХА: своя у каждого спутника
            public Visual echo;
            public Transform echoTransform;
            public Vector2 echoPosition;
            public Vector2 position;
            public float angle;
            public float r;
            public float heat;
            public float stun;
            public float fade;
            public bool resonant;
        }

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] InputHold input;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] AudioManager sfx;
        [SerializeField] Transform root;

        // по одной нити на каждую пару спутников в резонансе
        readonly List<ResonanceThread> threads = new List<ResonanceThread>();
        Satellite[] satellites;
        int count;
        int orbitDir;
        float baseR;
        float threadTimer;
        bool resonating;
        // ЭХО действует: тени расставлены в этом шаге
        bool echoing;

        void Awake()
        {
            satellites = new Satellite[config.maxSatellites];
            for (int i = 0; i < satellites.Length; i++) satellites[i] = new Satellite();
        }

        public void ResetRun()
        {
            // спутники, тени и нити создаются заново из префабов: правка префаба видна со следующего забега
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);

            for (int i = 0; i < satellites.Length; i++)
            {
                Satellite s = satellites[i];
                s.visual = Instantiate(art.satellite, root);
                s.transform = s.visual.transform;
                s.heatArc = s.visual.GetComponentInChildren<HeatArc>();
                s.visual.gameObject.SetActive(false);
                s.echo = Instantiate(art.echo, root);
                s.echoTransform = s.echo.transform;
                s.echo.gameObject.SetActive(false);
            }

            threads.Clear();
            for (int i = 0; i < config.threadPrewarm; i++) Thread(i).gameObject.SetActive(false);

            baseR = config.rMin;
            orbitDir = Clockwise;
            threadTimer = 0f;
            resonating = false;
            echoing = false;
            count = 0;
            Launch(config.satStartAngle, 0f);
        }

        public void Tick(float dt)
        {
            while (count < Mathf.Min(1 + loop.Stats.extraSatellites, satellites.Length)) Add();

            // при удержании внешний спутник всегда доходит до внешнего кольца
            float target = input.IsHolding ? config.ringMax - (count - 1) * config.ringGap : config.rMin;
            baseR = Mathf.MoveTowards(baseR, target, config.radialSpeed * loop.Stats.radialSpeedMul * dt);

            echoing = abilities.Active(AbilityId.Echo);
            for (int i = 0; i < count; i++)
            {
                Satellite s = satellites[i];
                s.r = Mathf.Min(baseR + i * config.ringGap, config.satMaxRadius);
                s.angle = Mathf.Repeat(s.angle + orbitDir * AngularSpeed(s.r) * dt, 360f);
                s.fade = Mathf.Max(s.fade - dt, 0f);
                Heat(s, dt);
                Place(s);
            }

            Resonate(dt);
        }

        public float AngularSpeed(float r)
        {
            return 360f / config.tMin * Mathf.Pow(config.rMin / r, loop.Era.kExp) * loop.Stats.spinMul;
        }

        public int Count => count;

        // -1 = по часовой стрелке, 1 = против
        public int OrbitDir => orbitDir;

        // РЕВЕРС: орбита сразу идёт в другую сторону
        public void Reverse()
        {
            orbitDir = -orbitDir;
        }

        public float OrbitRadius(int i)
        {
            return satellites[i].r;
        }

        // радиус орбиты внешнего спутника
        public float OuterRadius => satellites[count - 1].r;

        // Хиттеры: всё, что бьёт и собирает. Сначала спутники, за ними их тени, пока действует ЭХО.
        public int HitterCount => echoing ? count * HittersPerSatellite : count;

        // сколько хиттеров бывает самое большее: размер массивов перезарядки удара у врагов
        public int MaxHitters => HittersPerSatellite * config.maxSatellites;

        public float HitterRadius => config.satRadius * loop.Stats.satRadiusMul;

        public Vector2 HitterPosition(int k)
        {
            return k < count ? satellites[k].position : satellites[k - count].echoPosition;
        }

        // тень активна, пока активен её спутник
        public bool HitterActive(int k)
        {
            return Active(k < count ? k : k - count);
        }

        public float HitterDamage(int k)
        {
            // тень бьёт долей удара своего спутника без резонанса
            if (k >= count) return Power(satellites[k - count].r) * loop.Stats.hitMul * config.echoDamageMul;

            Satellite s = satellites[k];
            float damage = Power(s.r) * loop.Stats.hitMul;
            return s.resonant ? damage * config.resonanceMul : damage;
        }

        // тень не греется
        public float HitterHeat(int k)
        {
            return k < count ? satellites[k].heat : 0f;
        }

        public float Power(float r)
        {
            float far = config.powerFar + loop.Stats.powerFarBonus;
            float power = Mathf.Lerp(config.powerNear, far, Mathf.InverseLerp(config.rMin, config.powerFarRadius, r)) * loop.Stats.powerMul;
            return Mathf.Max(power, config.powerFloor);
        }

        public bool Resonating => resonating;

        // нагрев самого горячего спутника, 0..1
        public float MaxHeat
        {
            get
            {
                float max = 0f;
                for (int i = 0; i < count; i++) max = Mathf.Max(max, satellites[i].heat);
                return max;
            }
        }

        // возрождение: нагрев на нуле, оглушение снято
        public void Cool()
        {
            for (int i = 0; i < count; i++)
            {
                satellites[i].heat = 0f;
                satellites[i].stun = 0f;
            }
        }

        // оглушённый и ещё не появившийся спутник не бьёт, не собирает и не участвует в резонансе
        bool Active(int i)
        {
            return satellites[i].stun <= 0f && satellites[i].fade <= 0f;
        }

        // новый спутник встаёт внешним, в середину самого большого углового промежутка
        void Add()
        {
            float bestGap = 0f;
            float bestStart = 0f;
            for (int i = 0; i < count; i++)
            {
                // промежуток от спутника i по часовой стрелке до ближайшего соседа
                float gap = 360f;
                for (int j = 0; j < count; j++)
                    if (j != i) gap = Mathf.Min(gap, Mathf.Repeat(satellites[i].angle - satellites[j].angle, 360f));
                if (gap <= bestGap) continue;
                bestGap = gap;
                bestStart = satellites[i].angle;
            }

            Launch(Mathf.Repeat(bestStart - bestGap * 0.5f, 360f), config.satFadeIn);
        }

        // fade: за сколько секунд спутник проявится
        void Launch(float angle, float fade)
        {
            Satellite s = satellites[count];
            s.angle = angle;
            s.r = Mathf.Min(baseR + count * config.ringGap, config.satMaxRadius);
            s.heat = 0f;
            s.stun = 0f;
            s.fade = fade;
            s.resonant = false;
            count++;

            Place(s);
            s.visual.gameObject.SetActive(true);
        }

        void Heat(Satellite s, float dt)
        {
            // пока спутник оглушён, нагрев держится на 1
            if (s.stun > 0f)
            {
                s.stun -= dt;
                if (s.stun <= 0f) s.heat = config.heatAfterStun;
                return;
            }

            if (s.r <= config.heatZoneMax) s.heat += config.heatRate * loop.Stats.heatMul * dt;
            else if (s.r >= config.coolZoneMin) s.heat -= config.coolRate * dt;
            s.heat = Mathf.Clamp01(s.heat);

            if (s.heat >= 1f)
            {
                s.stun = config.stunTime;
                sfx.Play(SfxId.Stun);
            }
        }

        void Resonate(float dt)
        {
            for (int i = 0; i < count; i++) satellites[i].resonant = false;

            threadTimer -= dt;
            bool reroll = threadTimer <= 0f;
            if (reroll) threadTimer += config.threadInterval;

            float limit = config.resonanceHalfAngle * loop.Stats.resonanceAngleMul;
            int used = 0;
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (!Active(i) || !Active(j)) continue;
                    Satellite a = satellites[i];
                    Satellite b = satellites[j];
                    if (Mathf.Abs(Mathf.DeltaAngle(a.angle, b.angle)) > limit) continue;

                    // множитель не складывается: спутник в двух парах всё равно бьёт ×resonanceMul
                    a.resonant = true;
                    b.resonant = true;
                    Thread(used++).Draw(a.position, b.position, reroll);
                }
            }
            for (int i = used; i < threads.Count; i++) threads[i].gameObject.SetActive(false);

            // звук только в момент, когда резонанс возник
            if (used > 0 && !resonating) sfx.Play(SfxId.Resonance);
            resonating = used > 0;
        }

        // нить номер i из пула; если запаса не хватило, создаётся новая
        ResonanceThread Thread(int i)
        {
            if (i == threads.Count) threads.Add(Instantiate(art.resonanceThread, root));
            return threads[i];
        }

        void Place(Satellite s)
        {
            float rad = s.angle * Mathf.Deg2Rad;
            s.position = new Vector2(Mathf.Cos(rad) * s.r, Mathf.Sin(rad) * s.r);
            s.transform.localPosition = s.position;
            s.visual.SetSizeMul(loop.Stats.satRadiusMul);

            if (s.stun > 0f) s.visual.Tint(config.stunTint);
            else s.visual.Untint();
            // появление: прозрачность идёт за таймером fade
            s.visual.SetAlpha(1f - s.fade / config.satFadeIn);

            s.heatArc.Draw(s.heat, loop.Run.time);

            // тень видна, пока бьёт: ЭХО действует, а её спутник не оглушён и уже появился
            bool echo = echoing && s.stun <= 0f && s.fade <= 0f;
            if (s.echo.gameObject.activeSelf != echo) s.echo.gameObject.SetActive(echo);
            if (!echo) return;

            // тень идёт по той же орбите позади спутника
            float lag = (s.angle - orbitDir * config.echoLag) * Mathf.Deg2Rad;
            s.echoPosition = new Vector2(Mathf.Cos(lag) * s.r, Mathf.Sin(lag) * s.r);
            s.echoTransform.localPosition = s.echoPosition;
            s.echo.SetSizeMul(loop.Stats.satRadiusMul);
        }

        // круги попаданий в Scene view
        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            for (int k = 0; k < HitterCount; k++) Gizmos.DrawWireSphere(HitterPosition(k), HitterRadius);
        }
    }
}
