using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Perigee
{
    public class SatelliteSystem : MonoBehaviour
    {
        class Satellite
        {
            public Transform transform;
            public SpriteRenderer body;
            public TrailRenderer trail;
            public LineRenderer heatArc;
            public Vector2 position;
            public float angle;
            public float r;
            public float heat;
            public float stun;
            public float fade;
            public bool resonant;
        }

        class Thread
        {
            public LineRenderer line;
            public TextMeshPro label;
            // боковой сдвиг точек нити, у концов всегда 0
            public float[] offsets;
        }

        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] InputHold input;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] Transform root;

        Satellite[] satellites;
        Thread[] threads;
        SpriteRenderer anchorRing;
        Sprite solid;
        Sprite hollow;
        int count;
        float baseR;
        float threadTimer;

        void Awake()
        {
            solid = ShapeFactory.Diamond(config.outlineWidth / config.satRadius);
            hollow = ShapeFactory.Diamond(0f, config.satStunWall);

            Sprite ring = ShapeFactory.Ring(config.anchorRingTicks, config.anchorRingWidth, config.ringDashFill);
            anchorRing = ShapeFactory.NewRenderer("AnchorRing", root, ring, config.silver, config.sortRings, config.spriteMaterial);
            anchorRing.enabled = false;

            satellites = new Satellite[config.maxSatellites];
            for (int i = 0; i < satellites.Length; i++)
            {
                SpriteRenderer body = ShapeFactory.NewRenderer("Satellite" + i, root, solid, config.cream, config.sortSatellites, config.spriteMaterial);
                TrailRenderer trail = body.gameObject.AddComponent<TrailRenderer>();
                trail.sharedMaterial = config.spriteMaterial;
                trail.startColor = config.cream;
                trail.endColor = config.cream;
                trail.endWidth = 0f;
                trail.sortingOrder = config.sortSatellites;

                LineRenderer heatArc = NewLine("Heat" + i, root, config.red, config.sortSatellites);
                satellites[i] = new Satellite { transform = body.transform, body = body, trail = trail, heatArc = heatArc };
                Show(satellites[i], false);
            }

            // по одной нити на каждую пару спутников
            threads = new Thread[satellites.Length * (satellites.Length - 1) / 2];
            for (int i = 0; i < threads.Length; i++)
            {
                LineRenderer line = NewLine("Thread" + i, root, config.gold, config.sortFx);
                line.positionCount = config.threadSegments + 1;

                TextMeshPro label = new GameObject("Label").AddComponent<TextMeshPro>();
                label.transform.SetParent(line.transform, false);
                label.text = string.Format(Strings.MulFormat, config.resonanceMul);
                label.alignment = TextAlignmentOptions.Center;
                label.color = config.gold;
                label.sortingOrder = config.sortFx;

                line.gameObject.SetActive(false);
                threads[i] = new Thread { line = line, label = label, offsets = new float[config.threadSegments + 1] };
            }
        }

        public void ResetRun()
        {
            for (int i = 0; i < satellites.Length; i++)
            {
                satellites[i].body.DOKill();
                Show(satellites[i], false);
            }
            for (int i = 0; i < threads.Length; i++) threads[i].line.gameObject.SetActive(false);
            anchorRing.enabled = false;

            baseR = config.rMin;
            threadTimer = 0f;
            count = 1;
            Satellite s = satellites[0];
            s.angle = config.satStartAngle;
            s.r = baseR;
            s.heat = 0f;
            s.stun = 0f;
            s.fade = 0f;
            s.resonant = false;
            s.body.color = config.cream;
            Place(s);
            Show(s, true);
            s.trail.Clear();
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
            anchorRing.enabled = anchored;
            anchorRing.transform.localScale = new Vector3(baseR, baseR, 1f);

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

            Satellite s = satellites[count];
            s.angle = Mathf.Repeat(bestStart - bestGap * 0.5f, 360f);
            s.r = Mathf.Min(baseR + count * config.ringGap, config.satMaxRadius);
            s.heat = 0f;
            s.stun = 0f;
            s.fade = config.satFadeIn;
            s.resonant = false;
            count++;

            Color clear = config.cream;
            clear.a = 0f;
            s.body.color = clear;
            Place(s);
            Show(s, true);
            s.trail.Clear();
            s.body.DOFade(1f, config.satFadeIn).SetLink(s.body.gameObject);
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
                    DrawThread(threads[used++], a.position, b.position, reroll);
                }
            }
            for (int i = used; i < threads.Length; i++) threads[i].line.gameObject.SetActive(false);
        }

        void DrawThread(Thread thread, Vector2 a, Vector2 b, bool reroll)
        {
            int segments = thread.offsets.Length - 1;
            Vector2 normal = Vector2.Perpendicular(b - a).normalized;
            for (int k = 0; k <= segments; k++)
            {
                if (reroll && k > 0 && k < segments) thread.offsets[k] = Random.Range(-config.threadJitter, config.threadJitter);
                thread.line.SetPosition(k, Vector2.Lerp(a, b, (float)k / segments) + normal * thread.offsets[k]);
            }
            thread.line.startWidth = config.threadWidth;
            thread.line.endWidth = config.threadWidth;
            thread.label.transform.localPosition = (a + b) * 0.5f + normal * config.threadLabelOffset;
            thread.label.fontSize = config.threadLabelSize;
            thread.line.gameObject.SetActive(true);
        }

        void Place(Satellite s)
        {
            float rad = s.angle * Mathf.Deg2Rad;
            s.position = new Vector2(Mathf.Cos(rad) * s.r, Mathf.Sin(rad) * s.r);
            s.transform.localPosition = s.position;
            s.transform.localScale = new Vector3(Radius, Radius, 1f);
            s.trail.time = config.satTrailTime;
            s.trail.startWidth = config.satTrailWidth;
            // ВСПЛЕСК: след золотой, пока способность действует
            Color trail = abilities.Active(AbilityId.Surge) ? config.gold : config.cream;
            s.trail.startColor = trail;
            s.trail.endColor = trail;

            bool stunned = s.stun > 0f;
            s.body.sprite = stunned ? hollow : solid;
            s.trail.emitting = !stunned;
            // прозрачность ведёт твин появления, поэтому её не трогаем
            Color color = stunned ? config.silver : config.cream;
            // ХОЛОД: белая вспышка сразу после нажатия
            if (abilities.Active(AbilityId.Chill) && abilities.Elapsed(AbilityId.Chill) < config.chillFlashTime) color = Color.white;
            color.a = s.body.color.a;
            s.body.color = color;

            DrawHeat(s);
        }

        // красная дуга вокруг спутника: от верхней точки по часовой стрелке, полный круг при нагреве 1
        void DrawHeat(Satellite s)
        {
            LineRenderer arc = s.heatArc;
            int segments = config.heatArcSegments;
            int n = Mathf.CeilToInt(s.heat * segments);
            arc.positionCount = n > 0 ? n + 1 : 0;

            float radius = config.heatArcRadius * loop.Stats.satRadiusMul;
            for (int k = 0; k < arc.positionCount; k++)
            {
                float rad = Mathf.Min((float)k / segments, s.heat) * 2f * Mathf.PI;
                arc.SetPosition(k, s.position + new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * radius);
            }
            arc.startWidth = config.heatArcWidth;
            arc.endWidth = config.heatArcWidth;
            arc.enabled = s.heat < config.heatFlickerFrom || Mathf.Repeat(loop.Run.time * config.heatFlickerHz, 1f) < 0.5f;
        }

        void Show(Satellite s, bool visible)
        {
            s.transform.gameObject.SetActive(visible);
            s.heatArc.gameObject.SetActive(visible);
        }

        LineRenderer NewLine(string name, Transform parent, Color color, int order)
        {
            LineRenderer line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.useWorldSpace = false;
            line.sharedMaterial = config.spriteMaterial;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = order;
            line.positionCount = 0;
            return line;
        }
    }
}
