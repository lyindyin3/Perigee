using UnityEngine;

namespace Perigee
{
    public class SatelliteSystem : MonoBehaviour
    {
        class Satellite
        {
            public Transform transform;
            public TrailRenderer trail;
            public Vector2 position;
            public float angle;
            public float r;
        }

        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] InputHold input;
        [SerializeField] Transform root;

        Satellite[] satellites;
        int count;
        float baseR;

        void Awake()
        {
            satellites = new Satellite[config.maxSatellites];
            Sprite sprite = ShapeFactory.Diamond(config.outlineWidth / config.satRadius);
            for (int i = 0; i < satellites.Length; i++)
            {
                SpriteRenderer body = ShapeFactory.NewRenderer("Satellite" + i, root, sprite, config.cream, config.sortSatellites, config.spriteMaterial);
                TrailRenderer trail = body.gameObject.AddComponent<TrailRenderer>();
                trail.sharedMaterial = config.spriteMaterial;
                trail.startColor = config.cream;
                trail.endColor = config.cream;
                trail.endWidth = 0f;
                trail.sortingOrder = config.sortSatellites;
                body.gameObject.SetActive(false);
                satellites[i] = new Satellite { transform = body.transform, trail = trail };
            }
        }

        public void ResetRun()
        {
            for (int i = 0; i < satellites.Length; i++) satellites[i].transform.gameObject.SetActive(false);

            baseR = config.rMin;
            count = 1;
            Satellite s = satellites[0];
            s.angle = config.satStartAngle;
            s.r = baseR;
            Place(s);
            s.transform.gameObject.SetActive(true);
            s.trail.Clear();
        }

        public void Tick(float dt)
        {
            // при удержании внешний спутник всегда доходит до внешнего кольца
            float target = input.IsHolding ? config.ringMax - (count - 1) * config.ringGap : config.rMin;
            baseR = Mathf.MoveTowards(baseR, target, config.radialSpeed * loop.Stats.radialSpeedMul * dt);

            for (int i = 0; i < count; i++)
            {
                Satellite s = satellites[i];
                s.r = Mathf.Min(baseR + i * config.ringGap, config.satMaxRadius);
                // по часовой стрелке: угол убывает
                s.angle = Mathf.Repeat(s.angle - AngularSpeed(s.r) * dt, 360f);
                Place(s);
            }
        }

        public float AngularSpeed(float r)
        {
            return 360f / config.tMin * Mathf.Pow(config.rMin / r, loop.Era.kExp) * loop.Stats.spinMul;
        }

        public int Count => count;

        public Vector2 Position(int i)
        {
            return satellites[i].position;
        }

        public float Damage(int i)
        {
            return Power(satellites[i].r);
        }

        public float Power(float r)
        {
            float far = config.powerFar + loop.Stats.powerFarBonus;
            float power = Mathf.Lerp(config.powerNear, far, Mathf.InverseLerp(config.rMin, config.powerFarRadius, r)) * loop.Stats.powerMul;
            return Mathf.Max(power, config.powerFloor);
        }

        void Place(Satellite s)
        {
            float rad = s.angle * Mathf.Deg2Rad;
            s.position = new Vector2(Mathf.Cos(rad) * s.r, Mathf.Sin(rad) * s.r);
            s.transform.localPosition = s.position;
            s.transform.localScale = new Vector3(config.satRadius, config.satRadius, 1f);
            s.trail.time = config.satTrailTime;
            s.trail.startWidth = config.satTrailWidth;
        }
    }
}
