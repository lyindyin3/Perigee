using System.Collections.Generic;
using UnityEngine;

namespace Perigee
{
    public class SatelliteSystem : MonoBehaviour
    {
        // обязательная часть префаба спутника: золотой след ВСПЛЕСКА
        const string SurgeTrailName = "SurgeTrail";

        class Satellite
        {
            public Visual visual;
            public Transform transform;
            public HeatArc heatArc;
            public GameObject surgeTrail;
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
        [SerializeField] Transform root;
        [SerializeField] Visual anchorRing;

        // по одной нити на каждую пару спутников в резонансе
        readonly List<ResonanceThread> threads = new List<ResonanceThread>();
        Satellite[] satellites;
        int count;
        float baseR;
        float threadTimer;

        void Awake()
        {
            satellites = new Satellite[config.maxSatellites];
            for (int i = 0; i < satellites.Length; i++) satellites[i] = new Satellite();
        }

        public void ResetRun()
        {
            // спутники и нити создаются заново из префабов: правка префаба видна со следующего забега
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);

            for (int i = 0; i < satellites.Length; i++)
            {
                Satellite s = satellites[i];
                s.visual = Instantiate(art.satellite, root);
                s.transform = s.visual.transform;
                s.heatArc = s.visual.GetComponentInChildren<HeatArc>();
                s.surgeTrail = s.transform.Find(SurgeTrailName).gameObject;
                s.visual.gameObject.SetActive(false);
            }

            threads.Clear();
            for (int i = 0; i < config.threadPrewarm; i++) Thread(i).gameObject.SetActive(false);
            anchorRing.gameObject.SetActive(false);

            baseR = config.rMin;
            threadTimer = 0f;
            count = 0;
            Launch(config.satStartAngle, 0f);
        }

        public void Tick(float dt)
        {
            while (count < Mathf.Min(1 + loop.Stats.extraSatellites, satellites.Length)) Add();

            // ЯКОРЬ: радиус заморожен, удержание и отпускание не действуют
            bool anchored = abilities.Active(AbilityId.Anchor);
            if (!anchored)
            {
                // при удержании внешний спутник всегда доходит до внешнего кольца
                float target = input.IsHolding ? config.ringMax - (count - 1) * config.ringGap : config.rMin;
                baseR = Mathf.MoveTowards(baseR, target, config.radialSpeed * loop.Stats.radialSpeedMul * dt);
            }
            if (anchorRing.gameObject.activeSelf != anchored) anchorRing.gameObject.SetActive(anchored);
            if (anchored) anchorRing.SetSizeMul(baseR);

            for (int i = 0; i < count; i++)
            {
                Satellite s = satellites[i];
                s.r = Mathf.Min(baseR + i * config.ringGap, config.satMaxRadius);
                // по часовой стрелке: угол убывает
                s.angle = Mathf.Repeat(s.angle - AngularSpeed(s.r) * dt, 360f);
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

        public float Radius => config.satRadius * loop.Stats.satRadiusMul;

        public Vector2 Position(int i)
        {
            return satellites[i].position;
        }

        // оглушённый и ещё не появившийся спутник не бьёт, не собирает и не участвует в резонансе
        public bool Active(int i)
        {
            return satellites[i].stun <= 0f && satellites[i].fade <= 0f;
        }

        public float Damage(int i)
        {
            Satellite s = satellites[i];
            return s.resonant ? Power(s.r) * config.resonanceMul : Power(s.r);
        }

        public float Power(float r)
        {
            float far = config.powerFar + loop.Stats.powerFarBonus;
            float power = Mathf.Lerp(config.powerNear, far, Mathf.InverseLerp(config.rMin, config.powerFarRadius, r)) * loop.Stats.powerMul;
            return Mathf.Max(power, config.powerFloor);
        }

        // ХОЛОД: вспышка всех спутников
        public void Flash(Color color, float sec)
        {
            for (int i = 0; i < count; i++) satellites[i].visual.Flash(color, sec);
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
            // ХОЛОД: нагрев на нуле, оглушение снято, греться нельзя
            if (abilities.Active(AbilityId.Chill))
            {
                s.heat = 0f;
                s.stun = 0f;
                return;
            }

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

            if (s.heat >= 1f) s.stun = config.stunTime;
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

            // ВСПЛЕСК: золотой след, пока способность действует
            bool surge = abilities.Active(AbilityId.Surge);
            if (s.surgeTrail.activeSelf != surge) s.surgeTrail.SetActive(surge);

            s.heatArc.Draw(s.heat, loop.Run.time);
        }

        // круги попаданий в Scene view
        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            for (int i = 0; i < count; i++) Gizmos.DrawWireSphere(satellites[i].position, Radius);
        }
    }
}
