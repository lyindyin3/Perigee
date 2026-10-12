using System.Collections.Generic;
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
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] EnemySystem enemies;
        [SerializeField] Camera cam;
        [SerializeField] Transform root;

        readonly List<Visual> markers = new List<Visual>();
        readonly List<Visual> arrowWedges = new List<Visual>();
        readonly List<Visual> arrowLines = new List<Visual>();
        Pending[] pending;
        Rng spawnRng;
        float ambientTimer;
        float lastCentre;
        bool stopped;

        public void ResetRun(int seed)
        {
            spawnRng = new Rng(seed);
            ambientTimer = 0f;
            lastCentre = 0f;
            stopped = false;

            // углы групп выбираем сразу: телеграф показывает их раньше, чем группа появится
            pending = new Pending[GroupCount(config.opening) + GroupCount(config.timeline)];
            Fill(config.timeline, Fill(config.opening, 0));

            // метки создаются заново из префабов: правка префаба видна со следующего забега
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            Prewarm(markers, art.telegraphArrow);
            Prewarm(arrowWedges, art.telegraphWedge);
            Prewarm(arrowLines, art.telegraphLine);
        }

        // Комета появилась: новые враги больше не приходят
        public void Stop()
        {
            stopped = true;
            Hide(markers, 0);
            Hide(arrowWedges, 0);
            Hide(arrowLines, 0);
        }

        public void Tick(float dt)
        {
            if (stopped) return;

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
                    if (group.type == EnemyType.Arrow) PlaceArrowMarker(usedArrows++, angle, Radius(group));
                    else PlaceMarker(Marker(markers, art.telegraphArrow, usedMarkers++), angle);
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

        void PlaceMarker(Visual marker, float angle)
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
            marker.gameObject.SetActive(true);
        }

        // Стрела: клин на кольце появления и бледная линия до звезды
        void PlaceArrowMarker(int i, float angle, float radius)
        {
            Visual wedge = Marker(arrowWedges, art.telegraphWedge, i);
            wedge.transform.localPosition = Polar(angle, radius);
            // вершина клина смотрит на звезду
            wedge.transform.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
            wedge.gameObject.SetActive(true);

            // длину линии задаёт код, толщину префаб
            Visual line = Marker(arrowLines, art.telegraphLine, i);
            line.transform.localPosition = Polar(angle, (config.starRadius + radius) * 0.5f);
            line.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            line.SetSizeMul(radius - config.starRadius, 1f);
            line.gameObject.SetActive(true);
        }

        void Prewarm(List<Visual> list, Visual prefab)
        {
            list.Clear();
            for (int i = 0; i < config.markerPrewarm; i++) Marker(list, prefab, i).gameObject.SetActive(false);
        }

        // метка номер i из пула; если запаса не хватило, создаётся новая
        Visual Marker(List<Visual> list, Visual prefab, int i)
        {
            if (i == list.Count) list.Add(Instantiate(prefab, root));
            return list[i];
        }

        static void Hide(List<Visual> list, int from)
        {
            for (int i = from; i < list.Count; i++) list[i].gameObject.SetActive(false);
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
