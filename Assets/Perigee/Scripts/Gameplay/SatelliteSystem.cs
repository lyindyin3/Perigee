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
        // второй спутник чертежа ПАРА стартует напротив первого
        const float Opposite = 180f;

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
            // звук перегрева уже был; снова прозвучит, когда спутник остынет
            public bool overheatHeard;
        }

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] InputHold input;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] EnemySystem enemies;
        [SerializeField] AudioManager sfx;
        [SerializeField] Transform root;

        // по одной нити на каждую пару спутников в резонансе
        readonly List<ResonanceThread> threads = new List<ResonanceThread>();
        // концы показанных нитей: по ним режет НИТЬ-ПИЛА
        Vector2[] threadStart;
        Vector2[] threadEnd;
        int threadCount;
        Satellite[] satellites;
        int count;
        int orbitDir;
        float baseR;
        float threadTimer;
        bool resonating;
        // ЭХО действует: тени расставлены в этом шаге
        bool echoing;
        // секунды с момента, когда удержание отпущено; 0 = игрок держит
        float sinceRelease;
        // «Встречный ветер»: секунды с прошлого разворота орбиты
        float reverseTimer;
        // орбита в этом шаге идёт вверх или вниз (ИНЕРЦИЯ)
        bool shifting;

        void Awake()
        {
            satellites = new Satellite[config.maxSatellites];
            for (int i = 0; i < satellites.Length; i++) satellites[i] = new Satellite();

            // нитей не больше, чем пар спутников
            int pairs = satellites.Length * (satellites.Length - 1) / 2;
            threadStart = new Vector2[pairs];
            threadEnd = new Vector2[pairs];
        }

        public void ResetRun()
        {
            // спутники, тени и нити создаются заново из префабов: правка префаба видна со следующего забега
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);

            // облик спутника: префаб, выбранный в Мастерской
            Visual skin = art.satelliteSkins[loop.Shop.SatelliteSkin].prefab;
            for (int i = 0; i < satellites.Length; i++)
            {
                Satellite s = satellites[i];
                s.visual = Instantiate(skin, root);
                s.transform = s.visual.transform;
                s.heatArc = s.visual.GetComponentInChildren<HeatArc>();
                s.visual.gameObject.SetActive(false);
                s.echo = Instantiate(art.echo, root);
                s.echoTransform = s.echo.transform;
                // тень показывает картинку своего спутника
                s.echo.Body.sprite = s.visual.Body.sprite;
                s.echo.gameObject.SetActive(false);
            }

            threads.Clear();
            for (int i = 0; i < config.threadPrewarm; i++) Thread(i).gameObject.SetActive(false);

            baseR = RestRadius;
            orbitDir = Clockwise;
            reverseTimer = 0f;
            threadTimer = 0f;
            threadCount = 0;
            resonating = false;
            echoing = false;
            // до первого удержания окна ПРАЩИ нет
            sinceRelease = float.PositiveInfinity;
            count = 0;
            Launch(config.satStartAngle, 0f);
            // чертёж ПАРА: забег начинается с двумя спутниками
            if (loop.Stats.extraSatellites > 0) Launch(config.satStartAngle + Opposite, 0f);
        }

        public void Tick(float dt)
        {
            while (count < Mathf.Min(1 + loop.Stats.extraSatellites, Cap)) Add();

            bool holding = input.IsHolding;
            sinceRelease = holding ? 0f : sinceRelease + dt;

            // «Встречный ветер»: орбита сама разворачивается через равные промежутки
            if (loop.Run.Has(RunModifier.AutoReverse))
            {
                reverseTimer += dt;
                if (reverseTimer >= config.autoReverseInterval)
                {
                    reverseTimer -= config.autoReverseInterval;
                    Reverse();
                }
            }

            // при удержании внешний спутник всегда доходит до внешнего кольца
            float target = holding ? config.ringMax - (count - 1) * config.ringGap : RestRadius;
            // эра и карточки по-разному меняют выход наружу и падение (Тяжёлая звезда, минус ПРАЩИ)
            EraDef era = loop.Era;
            float k = target > baseR ? era.riseMul * loop.Stats.radialOutMul : era.fallMul;
            shifting = baseR != target;
            baseR = Mathf.MoveTowards(baseR, target, config.radialSpeed * loop.Stats.radialSpeedMul * k * dt);

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

        // радиус, на который спутник падает без удержания; «Высокая орбита» держит его дальше от звезды
        float RestRadius => loop.Run.Has(RunModifier.HighOrbit) ? config.highOrbitMin : config.rMin;

        // больше спутников не станет: четвёртый даёт только чертёж КВАДРИГА
        public int Cap => config.satelliteCap + loop.Stats.addSatBonus;

        // -1 = по часовой стрелке, 1 = против
        public int OrbitDir => orbitDir;

        // РЕВЕРС: орбита сразу идёт в другую сторону
        public void Reverse()
        {
            orbitDir = -orbitDir;
        }

        // ПРАЩА: окно slingshotTime после того, как отпущен последний палец
        public bool Slinging => sinceRelease > 0f && sinceRelease <= config.slingshotTime;

        public float OrbitRadius(int i)
        {
            return satellites[i].r;
        }

        // угол спутника i на орбите, градусы 0..360
        public float Angle(int i)
        {
            return satellites[i].angle;
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
            // ИНЕРЦИЯ: пока орбита идёт вверх или вниз, удар сильнее
            float hitMul = shifting ? loop.Stats.hitMul * (1f + loop.Stats.Sum(StatId.Inertia)) : loop.Stats.hitMul;
            // тень бьёт долей удара своего спутника без резонанса
            if (k >= count) return Power(satellites[k - count].r) * hitMul * config.echoDamageMul;

            Satellite s = satellites[k];
            float damage = Power(s.r) * hitMul;
            // ЖАР-ПТИЦА: горячий спутник бьёт сильнее; тень холодная, ей множитель не достаётся
            if (loop.Stats.Firebird && s.heat >= config.firebirdHeat) damage *= config.firebirdMul;
            return s.resonant ? damage * loop.Stats.resonanceMul : damage;
        }

        // тень не греется
        public float HitterHeat(int k)
        {
            return k < count ? satellites[k].heat : 0f;
        }

        // нагрев извне (удар по Факелу, вспышка): спутник k греется на amount, тень нет; оглушённый уже на пределе
        public void Warm(int k, float amount)
        {
            if (k >= count || satellites[k].stun > 0f) return;
            satellites[k].heat += amount;
            Overheat(satellites[k]);
        }

        // волна Пульсара: спутник i оглушён сразу, сколько бы нагрева у него ни было
        public void Stun(int i)
        {
            Satellite s = satellites[i];
            if (s.stun > 0f) return;

            s.heat = 1f;
            Stunned(s);
        }

        // оглушение: РЕАКТОР его укорачивает; ПЕРЕГРУЗКА взрывает спутник, взрыв идёт в очередь ЦЕПНОЙ РЕАКЦИИ
        void Stunned(Satellite s)
        {
            Stats stats = loop.Stats;
            s.stun = stats.stunTime;
            sfx.Play(SfxId.Stun);
            if (stats.Has(StatId.Overload)) enemies.QueueBlast(s.position, stats.overloadRadius, stats.overloadDamage);
        }

        public float Power(float r)
        {
            float far = config.powerFar + loop.Stats.powerFarBonus;
            float power = Mathf.Lerp(config.powerNear, far, Mathf.InverseLerp(config.rMin, config.powerFarRadius, r)) * loop.Stats.powerMul;
            return Mathf.Max(power, config.powerFloor);
        }

        public bool Resonating => resonating;

        // НИТЬ-ПИЛА: круг задевает прямой отрезок одной из показанных нитей; point = ближайшая точка нити
        public bool Sawn(Vector2 centre, float radius, out Vector2 point)
        {
            float reach = radius + config.sawWidth;
            for (int i = 0; i < threadCount; i++)
            {
                Vector2 along = threadEnd[i] - threadStart[i];
                float t = Mathf.Clamp01(Vector2.Dot(centre - threadStart[i], along) / along.sqrMagnitude);
                point = threadStart[i] + along * t;
                if ((centre - point).sqrMagnitude <= reach * reach) return true;
            }
            point = centre;
            return false;
        }

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
            Overheat(s);
        }

        // нагрев дошёл до 1: спутник оглушён
        void Overheat(Satellite s)
        {
            s.heat = Mathf.Clamp01(s.heat);
            if (s.heat < config.overheatReset) s.overheatHeard = false;
            else if (!s.overheatHeard && s.heat >= config.overheatSound)
            {
                s.overheatHeard = true;
                sfx.Play(SfxId.Overheat);
            }
            if (s.heat < 1f) return;

            Stunned(s);

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
                    threadStart[used] = a.position;
                    threadEnd[used] = b.position;
                    Thread(used++).Draw(a.position, b.position, reroll);
                }
            }
            for (int i = used; i < threads.Count; i++) threads[i].gameObject.SetActive(false);
            threadCount = used;

            // звук только в момент, когда резонанс возник
            if (used > 0 && !resonating) sfx.Play(SfxId.Resonance);
            resonating = used > 0;
        }

        // нить номер i из пула; если запаса не хватило, создаётся новая
        ResonanceThread Thread(int i)
        {
            if (i == threads.Count)
            {
                threads.Add(Instantiate(art.resonanceThread, root));
                threads[i].SetMultiplier(loop.Stats.resonanceMul);
            }
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
