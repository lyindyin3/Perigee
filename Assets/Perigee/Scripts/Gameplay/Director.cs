using UnityEngine;

namespace Perigee
{
    public class Director : MonoBehaviour
    {
        struct Pending
        {
            public float time;
            public SpawnGroup group;
            public float centre;
            public bool spawned;
        }

        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] EnemySystem enemies;
        [SerializeField] Camera cam;
        [SerializeField] Transform root;

        SpriteRenderer[] markers;
        SpriteRenderer[] arrowWedges;
        SpriteRenderer[] arrowLines;
        Pending[] pending;
        Rng spawnRng;
        float ambientTimer;
        float lastCentre;

        void Awake()
        {
            markers = NewMarkers("Marker", ShapeFactory.Triangle());
            arrowWedges = NewMarkers("ArrowWedge", ShapeFactory.Wedge());
            arrowLines = NewMarkers("ArrowLine", ShapeFactory.Rect());
        }

        public void ResetRun(int seed)
        {
            spawnRng = new Rng(seed);
            ambientTimer = 0f;
            lastCentre = 0f;

            // углы групп выбираем сразу: телеграф показывает их раньше, чем группа появится
            pending = new Pending[GroupCount(config.opening) + GroupCount(config.timeline)];
            Fill(config.timeline, Fill(config.opening, 0));

            Hide(markers, 0);
            Hide(arrowWedges, 0);
            Hide(arrowLines, 0);
        }

        public void Tick(float dt)
        {
            float t = loop.Run.time;

            int usedMarkers = 0;
            int usedArrows = 0;
            for (int i = 0; i < pending.Length; i++)
            {
                if (pending[i].spawned) continue;

                SpawnGroup group = pending[i].group;
                if (t >= pending[i].time)
                {
                    pending[i].spawned = true;
                    Spawn(group, pending[i].centre);
                    continue;
                }
                if (t < pending[i].time - Lead(group.type)) continue;

                // у стаи Роя одна метка на всю группу, у остальных по метке на каждого
                int n = Units(group);
                int shown = IsSwarm(group, n) ? 1 : n;
                for (int k = 0; k < shown; k++)
                {
                    float angle = UnitAngle(group, pending[i].centre, k, n);
                    if (group.type == EnemyType.Arrow)
                    {
                        if (usedArrows < arrowWedges.Length) PlaceArrowMarker(usedArrows++, angle, Radius(group), t);
                    }
                    else if (usedMarkers < markers.Length)
                    {
                        PlaceMarker(markers[usedMarkers++], angle, t);
                    }
                }
            }
            Hide(markers, usedMarkers);
            Hide(arrowWedges, usedArrows);
            Hide(arrowLines, usedArrows);

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

        int Fill(SpawnEvent[] events, int n)
        {
            for (int i = 0; i < events.Length; i++)
            {
                for (int g = 0; g < events[i].groups.Length; g++)
                {
                    SpawnGroup group = events[i].groups[g];
                    // Opposite: напротив предыдущей группы, даже если она из прошлого события
                    lastCentre = group.angleMode == AngleMode.Opposite ? Mathf.Repeat(lastCentre + 180f, 360f) : spawnRng.Range(0f, 360f);
                    pending[n++] = new Pending { time = events[i].time, group = group, centre = lastCentre };
                }
            }
            return n;
        }

        void Spawn(SpawnGroup group, float centre)
        {
            int n = Units(group);
            float radius = Radius(group);
            bool swarm = IsSwarm(group, n);

            for (int k = 0; k < n; k++)
            {
                float angle = UnitAngle(group, centre, k, n);
                float r = radius;
                if (swarm)
                {
                    angle += spawnRng.Range(-0.5f, 0.5f) * group.spreadDeg;
                    r += spawnRng.Range(0f, config.swarmRadiusJitter);
                }
                enemies.Spawn(group.type, Polar(angle, r), group.speedMul);
            }
        }

        int Units(SpawnGroup group)
        {
            return Mathf.Max(1, Mathf.RoundToInt(group.count * loop.Era.countMul));
        }

        float Radius(SpawnGroup group)
        {
            if (group.radius > 0f) return group.radius;
            return group.type == EnemyType.Arrow ? config.arrowSpawnRadius : config.spawnRadius;
        }

        float Lead(EnemyType type)
        {
            if (type == EnemyType.Arrow) return config.telegraphLeadArrow;
            return type == EnemyType.Swarmer ? config.telegraphLeadSwarm : config.telegraphLeadOther;
        }

        static bool IsSwarm(SpawnGroup group, int n)
        {
            return group.type == EnemyType.Swarmer && n > 1;
        }

        static float UnitAngle(SpawnGroup group, float centre, int k, int n)
        {
            switch (group.angleMode)
            {
                case AngleMode.Spread90: return centre + k * 90f;
                case AngleMode.Even: return centre + k * 360f / n;
                case AngleMode.OppositePair: return centre + k * 180f;
                default: return centre;
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

        // Стрела: пульсирующий клин на кольце появления и бледная линия до звезды
        void PlaceArrowMarker(int i, float angle, float radius, float t)
        {
            SpriteRenderer wedge = arrowWedges[i];
            float size = config.arrowWedgeSize * (1f + config.arrowWedgePulse * Mathf.Sin(t * config.arrowWedgePulseHz * 2f * Mathf.PI));
            wedge.transform.localPosition = Polar(angle, radius);
            // у клина вершина снизу: поворачиваем её к звезде
            wedge.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            wedge.transform.localScale = new Vector3(size, size, 1f);
            wedge.enabled = true;

            SpriteRenderer line = arrowLines[i];
            Color color = config.red;
            color.a = config.arrowLineAlpha;
            line.color = color;
            line.transform.localPosition = Polar(angle, (config.starRadius + radius) * 0.5f);
            line.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            line.transform.localScale = new Vector3((radius - config.starRadius) * 0.5f, config.arrowLineWidth * 0.5f, 1f);
            line.enabled = true;
        }

        SpriteRenderer[] NewMarkers(string name, Sprite sprite)
        {
            SpriteRenderer[] result = new SpriteRenderer[config.maxMarkers];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ShapeFactory.NewRenderer(name, root, sprite, config.red, config.sortFx, config.spriteMaterial);
                result[i].enabled = false;
            }
            return result;
        }

        static void Hide(SpriteRenderer[] list, int from)
        {
            for (int i = from; i < list.Length; i++) list[i].enabled = false;
        }

        static int GroupCount(SpawnEvent[] events)
        {
            int total = 0;
            for (int i = 0; i < events.Length; i++) total += events[i].groups.Length;
            return total;
        }

        static Vector2 Polar(float angle, float r)
        {
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r);
        }
    }
}
