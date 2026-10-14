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
            // кто придёт на самом деле: «Рой» меняет крупных врагов на Рой
            public EnemyType type;
            // сколько мест в группе и сколько врагов встаёт на каждое (больше одного только у «Роя»)
            public int units;
            public int pack;
            // множитель HP: циклы Бесконечного и повтор «Марафона»
            public float hpMul;
            // Вестник: элита со своими свойствами, его гибель открывает выбор реликвии
            public bool herald;
        }

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] EnemySystem enemies;
        [SerializeField] Camera cam;
        [SerializeField] Transform root;
        [SerializeField] BannerView banner;

        readonly List<Visual> markers = new List<Visual>();
        // вне расписания: Вестник и Нашествие; вышедшие убираются
        readonly List<Pending> extra = new List<Pending>();
        readonly List<Visual> arrowWedges = new List<Visual>();
        readonly List<Visual> arrowLines = new List<Visual>();
        Pending[] pending;
        // с этого места в pending лежит основное расписание: Бесконечный пишет сюда каждый следующий цикл
        int mainStart;
        // какой цикл расписания сейчас лежит в pending
        int filledCycle;
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

            // углы групп выбираем сразу: телеграф показывает их раньше, чем группа появится.
            // Расписание своё у каждой эры; обучение меняет только вступление.
            EraDef era = loop.Era;
            SpawnEvent[] opening = loop.Run.tutorial ? config.tutorialOpening : era.opening;
            bool marathon = loop.Run.Has(RunModifier.Marathon);
            mainStart = GroupCount(opening, 0f);
            filledCycle = 0;
            pending = new Pending[mainStart + GroupCount(era.timeline, 0f) + (marathon ? GroupCount(era.timeline, config.marathonReplayFrom) : 0)];
            int n = Fill(era.timeline, Fill(opening, 0, 0f, 0f, 1f, 1f), 0f, 0f, 1f, 1f);
            // «Марафон»: вторая часть расписания идёт ещё раз, позже и с врагами крепче
            if (marathon) Fill(era.timeline, n, config.marathonReplayFrom, config.marathonShift, config.marathonHpMul, 1f);
            extra.Clear();
            if (!loop.Run.tutorial) extra.Add(Herald(config.heraldTime, 1f));

            // метки создаются заново из префабов: правка префаба видна со следующего забега
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            Prewarm(markers, art.telegraphArrow);
            Prewarm(arrowWedges, art.telegraphWedge);
            Prewarm(arrowLines, art.telegraphLine);
        }

        // зерно появления: из него же Щитник берёт сторону щита
        public Rng SpawnRng => spawnRng;

        // босс появился: новые враги больше не приходят
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
            bool endless = loop.Run.mode == RunMode.Endless;
            // Бесконечный: цикл 0 идёт до первого босса, дальше новый цикл с каждым следующим
            if (endless) loop.Run.cycle = Mathf.Max(0, Mathf.FloorToInt((t - config.bossTime) / config.endlessLoopTime) + 1);

            int usedMarkers = 0;
            int usedArrows = 0;
            bool allSpawned = true;
            for (int i = 0; i < pending.Length; i++)
                if (!pending[i].spawned && !Due(ref pending[i], t, ref usedMarkers, ref usedArrows)) allSpawned = false;
            for (int i = extra.Count - 1; i >= 0; i--)
            {
                Pending p = extra[i];
                if (Due(ref p, t, ref usedMarkers, ref usedArrows))
                {
                    extra.RemoveAt(i);
                    // Бесконечный: Вестник приходит каждый цикл, с врагами цикла крепче
                    if (p.herald && endless) extra.Add(Herald(p.time + config.endlessLoopTime, p.hpMul + config.endlessHpStep));
                }
                else extra[i] = p;
            }
            Hide(markers, usedMarkers);
            Hide(arrowWedges, usedArrows);
            Hide(arrowLines, usedArrows);

            // Бесконечный: цикл расписания вышел весь, на его место встаёт следующий: позже, с врагами крепче и гуще
            if (endless && allSpawned)
            {
                filledCycle++;
                Fill(loop.Era.timeline, mainStart, 0f, config.endlessLoopTime * filledCycle, 1f + config.endlessHpStep * filledCycle, 1f + config.endlessCountStep * filledCycle);
            }

            // в обучении фоновый поток начинается позже; в «Марафоне» идёт до нового времени босса, в Бесконечном не кончается
            float ambientStart = loop.Run.tutorial ? config.tutorialAmbientStart : config.ambientStart;
            float ambientEnd = loop.Run.Has(RunModifier.Marathon) ? config.ambientEnd + config.marathonShift : config.ambientEnd;
            if (t >= ambientStart && (endless || t <= ambientEnd))
            {
                ambientTimer -= dt;
                if (ambientTimer <= 0f)
                {
                    // в Бесконечном поток с каждым циклом чаще
                    float pace = endless ? Mathf.Max(config.endlessAmbientMin, 1f - config.endlessAmbientStep * loop.Run.cycle) : 1f;
                    ambientTimer += loop.Era.ambientInterval * pace;
                    enemies.Spawn(EnemyType.Swarmer, Polar(spawnRng.Range(0f, 360f), config.spawnRadius), 1f);
                }
            }
        }

        // группа выходит в своё время, а до него за время упреждения показывает телеграф; true = вышла
        bool Due(ref Pending p, float t, ref int usedMarkers, ref int usedArrows)
        {
            if (t >= p.time)
            {
                p.spawned = true;
                Spawn(p);
                return true;
            }
            if (t < p.time - Lead(p.type)) return false;

            // у стаи Роя одна метка на всю группу, у остальных по метке на каждое место; Рою по кругу (Нашествие) метка на каждого
            int n = p.units;
            int shown = IsSwarm(p) && p.group.angleMode != AngleMode.Even ? 1 : n;
            for (int k = 0; k < shown; k++)
            {
                float angle = UnitAngle(p.group, p.centre, k, n);
                if (p.type == EnemyType.Arrow) PlaceArrowMarker(usedArrows++, angle, Radius(p));
                else PlaceMarker(Marker(markers, art.telegraphArrow, usedMarkers++), angle);
            }
            return false;
        }

        // Вестник эры на секунде time: Великан в броне с обычным телеграфом
        Pending Herald(float time, float hpMul)
        {
            EnemyType type = loop.Era.herald;
            return new Pending
            {
                time = time,
                group = new SpawnGroup { type = type },
                centre = spawnRng.Range(0f, 360f),
                type = type,
                units = 1,
                pack = 1,
                hpMul = hpMul,
                herald = true,
            };
        }

        // событие Нашествие: Рой по кругу со всех сторон, с телеграфом, через время его упреждения
        public void Invade()
        {
            var group = new SpawnGroup { type = EnemyType.Swarmer, count = config.invasionCount, angleMode = AngleMode.Even, spreadDeg = 0f, speedMul = config.invasionSpeedMul };
            extra.Add(new Pending
            {
                time = loop.Run.time + config.telegraphLeadSwarm,
                group = group,
                centre = spawnRng.Range(0f, 360f),
                type = EnemyType.Swarmer,
                units = config.invasionCount,
                pack = 1,
                hpMul = 1f,
            });
        }

        // события не раньше секунды from встают в pending с места n со сдвигом shift; возвращает место за последним
        int Fill(SpawnEvent[] events, int n, float from, float shift, float hpMul, float countMul)
        {
            RunState run = loop.Run;
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i].time < from) continue;

                for (int g = 0; g < events[i].groups.Length; g++)
                {
                    SpawnGroup group = events[i].groups[g];
                    // Opposite: напротив предыдущей группы, даже если она из прошлого события
                    lastCentre = group.angleMode == AngleMode.Opposite ? Mathf.Repeat(lastCentre + 180f, 360f) : spawnRng.Range(0f, 360f);

                    // «Рой»: вместо каждого крупного врага на его месте стая Роя; «Ливень стрел»: Стрел вдвое больше
                    bool swarmed = run.Has(RunModifier.SwarmOnly) && group.type != EnemyType.Swarmer && group.type != EnemyType.Arrow;
                    float count = group.count * loop.Era.countMul * countMul;
                    if (group.type == EnemyType.Arrow && run.Has(RunModifier.DoubleArrows)) count *= config.doubleArrowsMul;
                    pending[n++] = new Pending
                    {
                        time = events[i].time + shift,
                        group = group,
                        centre = lastCentre,
                        type = swarmed ? EnemyType.Swarmer : group.type,
                        units = Mathf.Max(1, Mathf.RoundToInt(count)),
                        pack = swarmed ? config.swarmOnlyPack : 1,
                        hpMul = hpMul,
                    };
                }
            }
            return n;
        }

        void Spawn(Pending p)
        {
            SpawnGroup group = p.group;
            float radius = Radius(p);
            if (p.herald)
            {
                enemies.Spawn(p.type, Polar(p.centre, radius), group.speedMul, p.hpMul, 1 << (int)EliteAffix.Giant | 1 << (int)EliteAffix.Armored, true);
                banner.Show(Strings.HeraldBanner, null, Color.white, config.bannerHeraldHold);
                return;
            }
            // стая разбросана по дуге: вокруг середины группы, а у «Роя» вокруг места каждого заменённого врага
            bool scattered = IsSwarm(p) || p.pack > 1;

            for (int k = 0; k < p.units; k++)
            {
                float place = UnitAngle(group, p.centre, k, p.units);
                for (int m = 0; m < p.pack; m++)
                {
                    float angle = place;
                    float r = radius;
                    if (scattered)
                    {
                        angle += spawnRng.Range(-0.5f, 0.5f) * group.spreadDeg;
                        r += spawnRng.Range(0f, config.swarmRadiusJitter);
                    }
                    enemies.Spawn(p.type, Polar(angle, r), group.speedMul, p.hpMul, Elite(p.type));
                }
            }
        }

        // элита: свойство из зерна симуляции или 0. В обучении её нет, в Бесконечном её с каждым циклом больше
        int Elite(EnemyType type)
        {
            RunState run = loop.Run;
            EnemyDef def = enemies.Def(type);
            if (run.tutorial || def.hp < config.eliteMinBaseHp || def.behavior == EnemyBehavior.Chord) return 0;

            float chance = loop.Era.eliteChance;
            if (run.mode == RunMode.Endless) chance += config.endlessEliteStep * run.cycle;
            // без шанса зерно появления не трогаем: расписание эр 1–4 идёт как раньше
            if (chance <= 0f || spawnRng.Range(0f, 1f) >= Mathf.Min(chance, config.eliteMax)) return 0;
            return enemies.RandomAffix();
        }

        float Radius(Pending p)
        {
            if (p.group.radius > 0f) return p.group.radius;
            return p.type == EnemyType.Arrow ? config.arrowSpawnRadius : config.spawnRadius;
        }

        float Lead(EnemyType type)
        {
            if (type == EnemyType.Arrow) return config.telegraphLeadArrow;
            return type == EnemyType.Swarmer ? config.telegraphLeadSwarm : config.telegraphLeadOther;
        }

        // стая Роя из расписания: одна группа вокруг общей середины
        static bool IsSwarm(Pending p)
        {
            return p.group.type == EnemyType.Swarmer && p.units > 1;
        }

        static float UnitAngle(SpawnGroup group, float centre, int k, int n)
        {
            switch (group.angleMode)
            {
                case AngleMode.Spread90: return centre + k * 90f;
                case AngleMode.Even: return centre + k * 360f / n;
                // пара напротив друг друга; лишние от множителя эры встают рядом через spreadDeg, а не в ту же точку
                case AngleMode.OppositePair: return centre + (k % 2) * 180f + (k / 2) * group.spreadDeg;
                // Random и Opposite: группа стоит веером по дуге spreadDeg; стаю Роя разбрасывает Spawn
                default: return n > 1 && group.type != EnemyType.Swarmer ? centre + ((float)k / (n - 1) - 0.5f) * group.spreadDeg : centre;
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
            line.transform.localPosition = Polar(angle, (loop.Star.Radius + radius) * 0.5f);
            line.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            line.SetSizeMul(radius - loop.Star.Radius, 1f);
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

        // сколько групп в событиях не раньше секунды from
        static int GroupCount(SpawnEvent[] events, float from)
        {
            int total = 0;
            for (int i = 0; i < events.Length; i++)
                if (events[i].time >= from) total += events[i].groups.Length;
            return total;
        }

        static Vector2 Polar(float angle, float r)
        {
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r);
        }
    }
}
