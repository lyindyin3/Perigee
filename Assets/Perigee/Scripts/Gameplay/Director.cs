using UnityEngine;

namespace Perigee
{
    public class Director : MonoBehaviour
    {
        struct Pending
        {
            public float time;
            public SpawnGroup group;
            public float angle;
            public bool spawned;
        }

        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] EnemySystem enemies;
        [SerializeField] Camera cam;
        [SerializeField] Transform root;

        SpriteRenderer[] markers;
        Pending[] pending;
        Rng spawnRng;
        float ambientTimer;

        void Awake()
        {
            markers = new SpriteRenderer[config.maxMarkers];
            Sprite sprite = ShapeFactory.Triangle();
            for (int i = 0; i < markers.Length; i++)
            {
                markers[i] = ShapeFactory.NewRenderer("Marker", root, sprite, config.red, config.sortFx, config.spriteMaterial);
                markers[i].enabled = false;
            }
        }

        public void ResetRun(int seed)
        {
            spawnRng = new Rng(seed);
            ambientTimer = 0f;

            SpawnEvent[] events = config.opening;
            int total = 0;
            for (int i = 0; i < events.Length; i++) total += events[i].groups.Length;

            // углы групп выбираем сразу: телеграф показывает их раньше, чем группа появится
            pending = new Pending[total];
            int n = 0;
            for (int i = 0; i < events.Length; i++)
                for (int g = 0; g < events[i].groups.Length; g++)
                    pending[n++] = new Pending { time = events[i].time, group = events[i].groups[g], angle = spawnRng.Range(0f, 360f) };

            for (int i = 0; i < markers.Length; i++) markers[i].enabled = false;
        }

        public void Tick(float dt)
        {
            float t = loop.Run.time;

            int used = 0;
            for (int i = 0; i < pending.Length; i++)
            {
                if (pending[i].spawned) continue;

                if (t >= pending[i].time)
                {
                    pending[i].spawned = true;
                    Spawn(pending[i].group, pending[i].angle);
                }
                else if (t >= pending[i].time - config.telegraphLeadSwarm && used < markers.Length)
                {
                    PlaceMarker(markers[used++], pending[i].angle, t);
                }
            }
            for (int i = used; i < markers.Length; i++) markers[i].enabled = false;

            if (t >= config.ambientStart && t <= config.ambientEnd)
            {
                ambientTimer -= dt;
                if (ambientTimer <= 0f)
                {
                    ambientTimer += config.ambientInterval;
                    enemies.Spawn(EnemyType.Swarmer, Polar(spawnRng.Range(0f, 360f), config.spawnRadius), 1f);
                }
            }
        }

        void Spawn(SpawnGroup group, float centre)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(group.count * loop.Era.countMul));
            float radius = group.radius > 0f ? group.radius : config.spawnRadius;
            bool swarm = group.type == EnemyType.Swarmer && n > 1;

            for (int k = 0; k < n; k++)
            {
                float angle = centre;
                float r = radius;
                if (swarm)
                {
                    angle += spawnRng.Range(-0.5f, 0.5f) * group.spreadDeg;
                    r += spawnRng.Range(0f, config.swarmRadiusJitter);
                }
                enemies.Spawn(group.type, Polar(angle, r), group.speedMul);
            }
        }

        void PlaceMarker(SpriteRenderer marker, float angle, float t)
        {
            float rad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            // радиус, на котором луч выходит из видимого прямоугольника с отступом
            float halfW = cam.orthographicSize * cam.aspect - config.telegraphInset;
            float halfH = cam.orthographicSize - config.telegraphInset;
            float edge = Mathf.Min(halfW / Mathf.Abs(cos), halfH / Mathf.Abs(sin));
            float r = Mathf.Min(config.telegraphRadius, edge);

            marker.transform.localPosition = new Vector3(cos * r, sin * r, 0f);
            // вершина стрелки смотрит на звезду
            marker.transform.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
            marker.transform.localScale = new Vector3(config.telegraphSize, config.telegraphSize, 1f);
            marker.enabled = Mathf.Repeat(t * config.telegraphBlinkHz, 1f) < 0.5f;
        }

        static Vector2 Polar(float angle, float r)
        {
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r);
        }
    }
}
