using UnityEngine;

namespace Perigee
{
    public class EnemySystem : MonoBehaviour
    {
        // обязательная часть префаба Паразита: кольцо, пока он сидит на звезде
        const string AttachMarkerName = "AttachMarker";
        // обязательная часть префаба Щитника: дуга щита, её середина смотрит по оси +Y
        const string ShieldName = "Shield";
        // обязательные части каждого префаба врага: золотое кольцо элиты и значок её свойства
        const string EliteMarkName = "EliteMark";
        const string AffixIconName = "AffixIcon";
        // бит в SaveData.seenEnemies над битами типов: первая элита сохранения уже была
        const int EliteSeenBit = 1 << 31;
        const int Affixes = 4;
        // стороны щита: против часовой стрелки от врага и по часовой
        const int Ccw = 1;
        const int Cw = -1;
        const int Sides = 2;

        class Enemy
        {
            public Visual visual;
            public Transform transform;
            public Transform body;
            public SpriteRenderer flash;
            public HpRing hpRing;
            public DamageStages cracks;
            public GameObject attachMarker;
            public Transform shield;
            public GameObject eliteMark;
            public SpriteRenderer affixIcon;
            public EnemyDef def;
            // свойства элиты: бит на каждый EliteAffix; 0 = обычный враг
            public int affixes;
            // Вестник: гибель открывает выбор реликвии
            public bool herald;
            // броня и урон по звезде: элита меняет их у себя
            public float armor;
            public float starDamage;
            // секунды без урона: элита со свойством Regen лечится, когда их набралось eliteRegenDelay
            public float calm;
            // Мина: радиус, на котором она ждёт; 0 = уже выждала
            public float stop;
            // объект события: скорость по хорде
            public Vector2 velocity;
            // номер Метеора; враг помнит номер последнего Метеора, который его задел
            public int serial;
            public int struck;
            public Vector2 position;
            // радиус столкновений: у Пожирателя растёт
            public float radius;
            public float hp;
            public float maxHp;
            // урон уже был: кольцо HP в режиме «после удара» остаётся на виду
            public bool hurt;
            public float speedMul;
            public float age;
            public float[] hitCooldown;
            // секунды до следующего пореза НИТЬЮ-ПИЛОЙ
            public float sawCooldown;
            // сколько секунд ещё горит (РАСКАЛЁННЫЙ)
            public float burn;
            // Паразит: присосался к звезде. Стрелок: встал на позицию. Кружащий: вышел на орбиту.
            public bool engaged;
            // Паразит: секунды на звезде. Стрелок: секунды до выстрела.
            public float timer;
            // Щитник: сколько ударов ещё держит щит и с какой он стороны
            public int shieldHits;
            public int shieldSide;
            // Призрак: место в цикле «плотный, потом прозрачный», секунды
            public float phase;
            // Пожиратель: сколько кристаллов съел
            public int eaten;
            // МОЛОТ: на столько криты сбили броню
            public float shred;
        }

        // взрыв из очереди: ЦЕПНАЯ РЕАКЦИЯ, ПЕРЕГРУЗКА
        struct Boom
        {
            public Vector2 centre;
            public float radius;
            public float damage;
        }

        // область на один шаг (EnemySystem.Field)
        struct Area
        {
            public Vector2 centre;
            public float radius;
            public float pull;
            public float speedMul;
            public float dps;
        }

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] BossSystem boss;
        [SerializeField] StarController star;
        [SerializeField] CrystalSystem crystals;
        [SerializeField] ComboSystem combo;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] Director director;
        [SerializeField] EraRuleSystem eraRule;
        [SerializeField] BannerView banner;
        [SerializeField] RelicSystem relics;
        [SerializeField] RunEventSystem events;
        [SerializeField] Fx fx;
        [SerializeField] AudioManager sfx;
        [SerializeField] Transform root;

        // живые враги лежат в начале массива: [0, count)
        Enemy[] live;
        int count;
        // запас выключенных врагов, свой на каждый тип: free[тип][0, freeCount[тип])
        Enemy[][] free;
        int[] freeCount;
        // очередь взрывов (ЦЕПНАЯ РЕАКЦИЯ, ПЕРЕГРУЗКА): [0, blastCount)
        Boom[] blasts;
        int blastCount;
        // области этого шага: [0, fieldCount)
        Area[] fields;
        int fieldCount;
        int meteorSerial;
        // нагрев хиттера, чей удар сейчас разбирается (ПЕРО ЖАР-ПТИЦЫ)
        float strikerHeat;

        void Awake()
        {
            live = new Enemy[config.maxEnemies];
            blasts = new Boom[config.maxEnemies];
            fields = new Area[config.maxFields];
            free = new Enemy[art.enemies.Length][];
            freeCount = new int[free.Length];
            for (int t = 0; t < free.Length; t++) free[t] = new Enemy[config.maxEnemies];
        }

        public void ResetRun()
        {
            // пулы собираются заново из префабов: правка префаба видна со следующего забега
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            count = 0;
            blastCount = 0;
            fieldCount = 0;

            EnemyDef[] defs = config.enemies;
            for (int i = 0; i < defs.Length; i++)
            {
                int t = (int)defs[i].id;
                freeCount[t] = 0;
                for (int k = 0; k < defs[i].prewarm; k++) free[t][freeCount[t]++] = Create(defs[i]);
            }
        }

        // hpMul = множитель HP от режима (циклы Бесконечного, повтор «Марафона»); действует там же, где множитель эры.
        // affixes = свойства элиты (бит на каждый EliteAffix), 0 = обычный враг; herald = это Вестник
        public void Spawn(EnemyType type, Vector2 position, float speedMul, float hpMul = 1f, int affixes = 0, bool herald = false)
        {
            if (count == live.Length) return;

            EnemyDef def = Def(type);
            int t = (int)type;
            Enemy e = freeCount[t] > 0 ? free[t][--freeCount[t]] : Create(def);
            live[count++] = e;
            e.def = def;
            e.position = position;
            e.radius = def.radius;
            // эра усиливает только врагов с базовым hp от eraHpMinBaseHp
            e.maxHp = def.hp >= config.eraHpMinBaseHp ? def.hp * loop.Era.hpMul * hpMul : def.hp;
            e.speedMul = speedMul;
            e.armor = def.armor;
            e.starDamage = def.starDamage;
            e.affixes = affixes;
            e.herald = herald;
            if (affixes != 0) Elite(e);
            e.visual.SetSizeMul(e.radius / def.radius);
            e.hp = e.maxHp;
            e.hurt = false;
            e.calm = 0f;
            e.struck = 0;
            e.hpRing.Fill();
            e.age = 0f;
            e.engaged = false;
            e.timer = 0f;
            e.sawCooldown = 0f;
            e.burn = 0f;
            e.eaten = 0;
            e.shred = 0f;
            e.shieldHits = def.shieldHits;
            if (def.shieldHits > 0)
            {
                // сторону щита выбирает зерно появления: в дневном забеге она у всех одна.
                // Ось +X врага, повёрнутого к звезде, идёт против часовой стрелки: туда и смотрит дуга щита Ccw.
                e.shieldSide = director.SpawnRng.Range(0, Sides) == 0 ? Ccw : Cw;
                e.shield.localRotation = Quaternion.Euler(0f, 0f, -90f * e.shieldSide);
            }
            // Призрак начинает со случайного места цикла
            e.phase = def.phaseOn > 0f ? loop.Run.simRng.Range(0f, def.phaseOn + def.phaseOff) : 0f;
            // Мина ждёт на случайном радиусе
            e.stop = def.behavior == EnemyBehavior.Mine ? loop.Run.simRng.Range(config.mineStopMin, config.mineStopMax) : 0f;
            if (def.behavior == EnemyBehavior.Chord) e.velocity = Chord(type, position) * (def.speed * speedMul);
            e.serial = ++meteorSerial;
            System.Array.Clear(e.hitCooldown, 0, e.hitCooldown.Length);
            Place(e);
            Gauge(e, 0f);
            e.eliteMark.SetActive(affixes != 0);
            e.affixIcon.gameObject.SetActive(affixes != 0);
            e.visual.gameObject.SetActive(true);
            Meet(type);
            if (affixes != 0) Announce(e);
        }

        // элита крепче обычного и несёт свойства
        void Elite(Enemy e)
        {
            e.maxHp *= config.eliteHpMul;
            if (Has(e, EliteAffix.Armored)) e.armor += config.eliteArmor;
            if (Has(e, EliteAffix.Swift)) e.speedMul *= config.eliteSpeedMul;
            if (Has(e, EliteAffix.Giant))
            {
                e.maxHp *= config.giantHpMul;
                e.radius *= config.giantRadiusMul;
                e.starDamage *= config.giantStarDamageMul;
            }
        }

        static bool Has(Enemy e, EliteAffix affix)
        {
            return (e.affixes & 1 << (int)affix) != 0;
        }

        // появилась элита: значок первого свойства, золотые искры, звук; первая элита сохранения выходит баннером
        void Announce(Enemy e)
        {
            int first = 0;
            while (!Has(e, (EliteAffix)first)) first++;
            Sprite icon = art.affixIcons[first].sprite;
            e.affixIcon.sprite = icon;
            fx.Sparks(e.position, config.eliteSparks, true);
            sfx.Play(SfxId.Elite);

            SaveData data = loop.Save.Data;
            if ((data.seenEnemies & EliteSeenBit) != 0) return;
            data.seenEnemies |= EliteSeenBit;
            loop.Save.Save();
            banner.Show(Strings.EliteTip, icon, Color.white, config.bannerEnemyHold);
        }

        // случайное свойство элиты (бит EliteAffix)
        public int RandomAffix()
        {
            return 1 << loop.Run.simRng.Range(0, Affixes);
        }

        // направление хорды: к случайной точке у центра и дальше за неё; Золотая комета идёт по касательной к кругу goldCometMiss
        Vector2 Chord(EnemyType type, Vector2 from)
        {
            Rng rng = loop.Run.simRng;
            if (type == EnemyType.GoldComet)
            {
                float turn = Mathf.Asin(Mathf.Min(config.goldCometMiss / from.magnitude, 1f)) * Mathf.Rad2Deg;
                return Turned(-from.normalized, rng.Range(0, Sides) == 0 ? turn : -turn);
            }
            float r = config.chordInner * Mathf.Sqrt(rng.Range(0f, 1f));
            float rad = rng.Range(0f, 360f) * Mathf.Deg2Rad;
            return (new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r) - from).normalized;
        }

        // Затмители на поле: на столько меньше радиус света во тьме (их срезы складываются)
        public float LightCut
        {
            get
            {
                float cut = 0f;
                for (int i = 0; i < count; i++) cut += live[i].def.lightCut;
                return cut;
            }
        }

        public void Tick(float dt)
        {
            // СТАЗИС замедляет врагов; перезарядка удара и кольцо HP идут по обычному времени
            float enemyDt = dt * loop.EnemyTimeScale;
            bool pull = abilities.Active(AbilityId.Pull);
            bool dome = abilities.Active(AbilityId.Dome);
            bool horizon = loop.Stats.Horizon;
            float pullRadius = satellites.OuterRadius;

            for (int i = count - 1; i >= 0; i--)
            {
                Enemy e = live[i];
                e.age += enemyDt;
                if (e.def.phaseOn > 0f) e.phase = Mathf.Repeat(e.phase + enemyDt, e.def.phaseOn + e.def.phaseOff);
                for (int k = 0; k < e.hitCooldown.Length; k++) e.hitCooldown[k] -= dt;
                e.sawCooldown -= dt;
                e.calm += enemyDt;

                // поджог и ГОРИЗОНТ СОБЫТИЙ: враг теряет HP по времени врагов, без числа и вспышки
                float seared = 0f;
                if (e.burn > 0f)
                {
                    float burning = Mathf.Min(e.burn, enemyDt);
                    e.burn -= burning;
                    seared += loop.Stats.burnDps * burning;
                }
                // области шага: тянут, замедляют и жгут, как поджог
                float moveMul = 1f;
                for (int f = 0; f < fieldCount; f++)
                {
                    Area a = fields[f];
                    Vector2 toCentre = a.centre - e.position;
                    if (toCentre.sqrMagnitude > a.radius * a.radius) continue;
                    moveMul *= a.speedMul;
                    seared += a.dps * enemyDt;
                    e.position += toCentre.normalized * (a.pull * enemyDt);
                }
                float moveDt = enemyDt * moveMul;
                if (horizon && e.position.sqrMagnitude <= config.horizonRadius * config.horizonRadius) seared += config.horizonDps * enemyDt;
                if (seared > 0f)
                {
                    e.hp -= seared;
                    e.hurt = true;
                    e.calm = 0f;
                    if (e.hp <= 0f)
                    {
                        Kill(i);
                        continue;
                    }
                }
                // элита Regen подлечивается, пока её не бьют
                if (Has(e, EliteAffix.Regen) && e.calm >= config.eliteRegenDelay) e.hp = Mathf.Min(e.hp + config.eliteRegen * e.maxHp * enemyDt, e.maxHp);

                // объект события летит своей хордой и под ПРИТЯЖЕНИЕМ
                if (pull && e.def.behavior != EnemyBehavior.Chord)
                {
                    // ПРИТЯЖЕНИЕ: своё поведение стоит, враг идёт по радиусу к орбите внешнего спутника.
                    // Паразит отцепляется, Стрелок перестаёт стрелять; потом оба продолжают с того места, где оказались.
                    float r = e.position.magnitude;
                    e.position *= Mathf.MoveTowards(r, pullRadius, config.pullSpeed * dt) / r;
                    e.engaged = false;
                    e.timer = 0f;
                    Place(e);
                    Gauge(e, dt);
                    continue;
                }

                switch (e.def.behavior)
                {
                    case EnemyBehavior.Straight:
                        Drift(e, moveDt);
                        if (Advance(e, 0f, moveDt) <= loop.Star.Radius + e.radius)
                        {
                            Strike(i, dome);
                            continue;
                        }
                        break;

                    case EnemyBehavior.Attach:
                        if (!e.engaged && Advance(e, 0f, moveDt) > loop.Star.Radius + e.radius) break;

                        // КУПОЛ: Паразит у звезды разбивается, сидел он на ней или только долетел
                        if (dome)
                        {
                            Shatter(i);
                            continue;
                        }
                        if (e.engaged)
                        {
                            // чем дольше сидит, тем быстрее пьёт
                            e.timer += enemyDt;
                            star.Drain(Mathf.Min(config.parasiteDrainBase + config.parasiteDrainGrowth * e.timer, config.parasiteDrainMax) * enemyDt);
                        }
                        else
                        {
                            e.engaged = true;
                            e.timer = 0f;
                            e.position = e.position.normalized * (loop.Star.Radius + config.parasiteAttachOffset);
                        }
                        break;

                    case EnemyBehavior.Stopper:
                        if (e.engaged)
                        {
                            e.timer -= enemyDt;
                            if (e.timer <= 0f)
                            {
                                e.timer += config.shooterFireInterval;
                                Spawn(EnemyType.Bolt, e.position, 1f);
                            }
                        }
                        else if (Advance(e, config.shooterStopRadius, moveDt) <= config.shooterStopRadius)
                        {
                            e.engaged = true;
                            e.timer = config.shooterFirstDelay;
                        }
                        break;

                    case EnemyBehavior.Burster:
                        if (Advance(e, 0f, moveDt) <= config.hiveBurstRadius)
                        {
                            Burst(i);
                            continue;
                        }
                        break;

                    case EnemyBehavior.Orbit:
                        // идёт прямо до своей орбиты, дальше кружит, пока не коснётся звезды
                        if (!e.engaged) e.engaged = Advance(e, e.def.orbitRadius, moveDt) <= e.def.orbitRadius;
                        else if (Circle(e, moveDt) <= loop.Star.Radius + e.radius)
                        {
                            Strike(i, dome);
                            continue;
                        }
                        break;

                    case EnemyBehavior.Seek:
                        if (Seek(e, moveDt) <= loop.Star.Radius + e.radius)
                        {
                            Strike(i, dome);
                            continue;
                        }
                        break;

                    case EnemyBehavior.Mine:
                        // летит до своего радиуса, ждёт mineWait и летит дальше к звезде; толчок прерывает ожидание
                        if (e.timer > 0f)
                        {
                            e.timer -= enemyDt;
                            if (e.timer <= 0f) e.stop = 0f;
                            break;
                        }
                        Drift(e, moveDt);
                        float reached = Advance(e, e.stop, moveDt);
                        if (reached <= loop.Star.Radius + e.radius)
                        {
                            Strike(i, dome);
                            continue;
                        }
                        if (e.stop > 0f && reached <= e.stop) e.timer = config.mineWait;
                        break;

                    case EnemyBehavior.Blink:
                        Drift(e, moveDt);
                        if (Advance(e, 0f, moveDt) <= loop.Star.Radius + e.radius)
                        {
                            Strike(i, dome);
                            continue;
                        }
                        // ЗАМОК: раненый Прыгун больше не прыгает
                        if (loop.Stats.Has(StatId.Lock) && e.hurt) break;
                        e.timer += enemyDt;
                        // engaged = перед прыжком уже мигнул
                        if (!e.engaged && e.timer >= e.def.blinkInterval - config.blinkWarn)
                        {
                            e.engaged = true;
                            e.visual.Flash(config.blinkFlashColor, config.blinkWarn);
                        }
                        if (e.timer >= e.def.blinkInterval) Blink(e);
                        break;

                    case EnemyBehavior.Chord:
                        e.position += e.velocity * moveDt;
                        // пересёк поле: пропадает, звезде ничего
                        float exit = config.spawnRadius + config.chordExit;
                        if (Vector2.Dot(e.position, e.velocity) > 0f && e.position.sqrMagnitude > exit * exit)
                        {
                            Remove(i);
                            continue;
                        }
                        if (e.starDamage > 0f && e.position.magnitude <= loop.Star.Radius + e.radius)
                        {
                            Strike(i, dome);
                            continue;
                        }
                        break;
                }
                Place(e);
                Gauge(e, dt);
            }
            // области живут один шаг: их ставят заново
            fieldCount = 0;
        }

        // Прыгун скачет вбок по своему кругу; удары по нему снова доступны всем хиттерам
        void Blink(Enemy e)
        {
            e.timer -= e.def.blinkInterval;
            e.engaged = false;
            e.position = Turned(e.position, loop.Run.simRng.Range(0, Sides) == 0 ? e.def.blinkDeg : -e.def.blinkDeg);
            System.Array.Clear(e.hitCooldown, 0, e.hitCooldown.Length);
            sfx.Play(SfxId.Blink);
        }

        // круглая область на ближайший шаг врагов: тянет к centre (pull > 0) или от него (pull < 0) со скоростью |pull|,
        // умножает скорость врагов на speedMul и жжёт их на dps в секунду, как поджог. Боссов не трогает.
        public void Field(Vector2 centre, float radius, float pull, float speedMul, float dps)
        {
            if (fieldCount == fields.Length) return;
            fields[fieldCount++] = new Area { centre = centre, radius = radius, pull = pull, speedMul = speedMul, dps = dps };
        }

        // взрыв в точке centre в следующем шаге столкновений: Рой гибнет, остальные и босс получают урон
        public void QueueBlast(Vector2 centre, float radius, float damage)
        {
            if (blastCount == blasts.Length) return;
            blasts[blastCount++] = new Boom { centre = centre, radius = radius, damage = damage };
        }

        public void Collide()
        {
            Stats stats = loop.Stats;
            // взрывы, поставленные до этого шага; убийства этого шага взорвутся в следующем
            int due = blastCount;
            int hitters = satellites.HitterCount;
            bool prism = stats.Has(StatId.Prism);
            bool hammer = stats.Has(StatId.Hammer);
            Meteors();
            for (int i = count - 1; i >= 0; i--)
            {
                Enemy e = live[i];
                // сквозь прозрачного Призрака хиттеры проходят: ни удара, ни перезарядки
                if (e.age < config.spawnFadeIn || Phased(e)) continue;

                float reach = satellites.HitterRadius + e.radius;
                for (int k = 0; k < hitters; k++)
                {
                    if (!satellites.HitterActive(k)) continue;
                    if (e.hitCooldown[k] > 0f) continue;
                    Vector2 hitter = satellites.HitterPosition(k);
                    if ((hitter - e.position).sqrMagnitude > reach * reach) continue;

                    strikerHeat = satellites.HitterHeat(k);
                    // Метеор от удара спутника или тени раскалывается на кристаллы
                    if (e.def.id == EnemyType.Meteor)
                    {
                        Kill(i, true);
                        break;
                    }
                    e.hitCooldown[k] = config.hitCooldown;
                    float damage = satellites.HitterDamage(k);
                    bool crit = loop.Run.simRng.Range(0f, 1f) < stats.CritChance(satellites.HitterHeat(k));
                    if (crit) damage *= stats.critMul;
                    // ПРИЗМА: крит проходит сквозь щит и броню
                    bool pierce = crit && prism;
                    // щит: хиттер, идущий по часовой стрелке, приходит к врагу с его стороны «против часовой».
                    // Щит с этой стороны держит удар и теряет прочность; на нуле ломается.
                    if (!pierce && e.shieldHits > 0 && e.shieldSide == -satellites.OrbitDir)
                    {
                        // ЛОМ ломает щит первым же ударом в него
                        e.shieldHits = stats.Crowbar ? 0 : e.shieldHits - 1;
                        if (e.shieldHits == 0)
                        {
                            fx.Sparks(e.position, config.shieldBreakSparks);
                            sfx.Play(SfxId.ShieldBreak);
                        }
                        Clink(e, hitter);
                        continue;
                    }
                    // МОЛОТ: каждый крит навсегда сбивает часть брони
                    if (crit && hammer) e.shred = Mathf.Min(e.shred + config.hammerShred, e.armor);
                    // броня: удар слабее брони урона не наносит; БРОНЕБОЙ её срезает
                    if (!pierce && damage < Mathf.Max(e.armor - e.shred - stats.armorPierce, 0f))
                    {
                        Clink(e, hitter);
                        continue;
                    }

                    // ОСКОЛОК КОМЕТЫ: каждый третий удар, который прошёл, сильнее
                    damage += relics.Strike(k);
                    // Факел греет спутник каждым ударом, который по нему прошёл; ЖАРОСТОЙКОСТЬ от этого спасает
                    if (e.def.heatOnHit > 0f && !stats.Heatproof) satellites.Warm(k, e.def.heatOnHit * stats.heatMul);
                    // ЛОМ: Кружащий получает удар дважды
                    if (stats.Crowbar && e.def.behavior == EnemyBehavior.Orbit) damage *= config.crowbarOrbiterMul;
                    // Студень замедляет ударивший спутник, Отбойник подбрасывает орбиту; удар тени ни того ни другого не делает
                    if (k < satellites.Count)
                    {
                        if (e.def.slowHitter) satellites.Slow(k);
                        if (e.def.kickHitter) satellites.Kick();
                    }
                    if (Hurt(i, damage, hitter, crit ? NumberKind.Crit : NumberKind.Normal, true))
                    {
                        Detonated(e, k);
                        break;
                    }
                    // РАСКАЛЁННЫЙ: горячий спутник поджигает, новый поджог начинает отсчёт заново
                    if (stats.Burn && satellites.HitterHeat(k) >= config.burnHeatMin) e.burn = config.burnTime;
                    // РАСКОЛ (ГИЛЬОТИНА поднимает порог): недобитый враг гибнет от любого удара
                    if (stats.Execute && e.hp <= e.maxHp * stats.executeFraction)
                    {
                        Kill(i, true);
                        Detonated(e, k);
                        break;
                    }
                    fx.Shards(hitter, config.hitShards, e.position - hitter);
                    sfx.Play(SfxId.Hit);
                }
            }

            if (stats.SawThread) Saw();

            for (int b = 0; b < due; b++)
            {
                Boom boom = blasts[b];
                Blast(boom.centre, boom.radius, boom.damage);
                boss.Blast(boom.centre, boom.radius, boom.damage);
            }
            // взрывы, поставленные за этот шаг, сдвигаются в начало очереди
            blastCount -= due;
            System.Array.Copy(blasts, due, blasts, 0, blastCount);
        }

        // Мину убил спутник: взрыв греет его (САПЁР от этого спасает); тень не греется
        void Detonated(Enemy e, int k)
        {
            if (e.def.behavior == EnemyBehavior.Mine && !loop.Stats.Has(StatId.Sapper)) satellites.Warm(k, config.mineHeat * loop.Stats.heatMul);
        }

        // Метеор задевает врагов на пути: каждому один удар, Рой гибнет
        void Meteors()
        {
            for (int i = count - 1; i >= 0; i--)
            {
                Enemy m = live[i];
                if (m.def.id != EnemyType.Meteor) continue;

                for (int j = count - 1; j >= 0; j--)
                {
                    Enemy e = live[j];
                    if (e.def.behavior == EnemyBehavior.Chord || e.struck == m.serial || e.age < config.spawnFadeIn) continue;
                    float reach = m.radius + e.radius;
                    if ((e.position - m.position).sqrMagnitude > reach * reach) continue;

                    e.struck = m.serial;
                    bool dead = e.def.id == EnemyType.Swarmer;
                    if (dead) Kill(j);
                    else dead = Hurt(j, config.meteorDamage, m.position, NumberKind.Normal);
                    // на место убитого мог встать сам Метеор
                    if (dead) i = System.Array.IndexOf(live, m, 0, count);
                }
            }
        }

        // НИТЬ-ПИЛА: нить резонанса режет каждого, кого задевает; броня и щит не спасают
        void Saw()
        {
            for (int i = count - 1; i >= 0; i--)
            {
                Enemy e = live[i];
                if (e.age < config.spawnFadeIn || e.sawCooldown > 0f) continue;
                Vector2 point;
                if (!satellites.Sawn(e.position, e.radius, out point)) continue;

                e.sawCooldown = loop.Stats.sawInterval;
                if (Hurt(i, loop.Stats.sawDamage, point, NumberKind.Normal)) continue;
                fx.Shards(point, config.hitShards, e.position - point);
                sfx.Play(SfxId.Hit);
            }
        }

        // волна из точки centre (МАЯК, ЦЕПНАЯ РЕАКЦИЯ): Рой гибнет сразу, остальные получают урон
        public void Blast(Vector2 centre, float radius, float damage)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                Enemy e = live[i];
                if ((e.position - centre).sqrMagnitude > radius * radius) continue;

                if (e.def.id == EnemyType.Swarmer) Kill(i);
                else Hurt(i, damage, centre, NumberKind.Normal);
            }
        }

        // вспышка Красного карлика: враги в секторе получают урон, Рой гибнет; броня и щит не спасают.
        // centre = середина сектора, halfArc = половина его ширины, градусы
        public void Flare(float centre, float halfArc, float damage)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                Enemy e = live[i];
                float angle = Mathf.Atan2(e.position.y, e.position.x) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(angle, centre)) > halfArc) continue;

                if (e.def.id == EnemyType.Swarmer) Kill(i);
                else Hurt(i, damage, Vector2.zero, NumberKind.Normal);
            }
        }

        // ИМПУЛЬС: Рой и Стрелы гибнут, остальные получают урон и отлетают наружу
        public void Pulse(float radius, float damage, float push)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                Enemy e = live[i];
                float r = e.position.magnitude;
                if (r > radius) continue;

                if (e.def.id == EnemyType.Swarmer || e.def.id == EnemyType.Arrow)
                {
                    Kill(i);
                    continue;
                }
                if (Hurt(i, damage, Vector2.zero, NumberKind.Normal)) continue;

                e.position *= (r + push) / r;
                // Паразит отцепляется и летит обратно, Стрелок заново идёт на позицию
                e.engaged = false;
                e.timer = 0f;
                Place(e);
            }
        }

        // возрождение: все враги рядом со звездой гибнут и роняют кристаллы
        public void Wipe(float radius)
        {
            for (int i = count - 1; i >= 0; i--)
                if (live[i].position.sqrMagnitude <= radius * radius) Kill(i);
        }

        // РЕВЕРС: каждый хиттер может ударить снова, не дожидаясь перезарядки
        public void ClearHitCooldowns()
        {
            for (int i = 0; i < count; i++) System.Array.Clear(live[i].hitCooldown, 0, live[i].hitCooldown.Length);
        }

        public int Count => count;

        public int CountOf(EnemyType type)
        {
            int n = 0;
            for (int i = 0; i < count; i++)
                if (live[i].def.id == type) n++;
            return n;
        }

        // сколько врагов не дальше radius от звезды
        public int Within(float radius)
        {
            int n = 0;
            for (int i = 0; i < count; i++)
                if (live[i].position.sqrMagnitude <= radius * radius) n++;
            return n;
        }

        // шаг к центру, но не ближе stopRadius; возвращает новое расстояние до центра
        float Advance(Enemy e, float stopRadius, float dt)
        {
            float speed = e.def.speed * e.speedMul * loop.Stats.enemySpeedMul;
            if (e.def.id == EnemyType.Arrow) speed *= loop.Stats.arrowSpeedMul;

            float r = e.position.magnitude;
            float next = Mathf.MoveTowards(r, stopRadius, speed * dt);
            e.position *= next / r;
            return next;
        }

        // воронка Чёрной дыры: путь к центру закручивается против часовой стрелки
        void Drift(Enemy e, float dt)
        {
            if (loop.Era.rule == EraRule.Vortex) e.position = Turned(e.position, config.vortexDeg * dt);
        }

        // Кружащий: поворот вокруг звезды против часовой стрелки и понемногу вниз; возвращает новое расстояние до центра
        float Circle(Enemy e, float dt)
        {
            float r = e.position.magnitude;
            float next = r - e.def.orbitInSpeed * dt;
            e.position = Turned(e.position * (next / r), e.def.orbitDeg * dt);
            return next;
        }

        // точка p, повёрнутая вокруг звезды против часовой стрелки
        static Vector2 Turned(Vector2 p, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos);
        }

        // Пожиратель: к ближайшему кристаллу рядом, иначе к звезде; возвращает новое расстояние до центра
        float Seek(Enemy e, float dt)
        {
            // сытый кристаллы больше не ищет
            int c = e.eaten < config.devourMax ? crystals.Nearest(e.position, e.def.seekRadius) : CrystalSystem.None;
            if (c == CrystalSystem.None)
            {
                Drift(e, dt);
                return Advance(e, 0f, dt);
            }

            Vector2 target = crystals.Position(c);
            e.position = Vector2.MoveTowards(e.position, target, e.def.speed * e.speedMul * loop.Stats.enemySpeedMul * dt);
            float reach = e.radius + config.crystalRadius;
            if ((target - e.position).sqrMagnitude <= reach * reach)
            {
                // съел: стал крепче и крупнее, после гибели вернёт кристалл
                crystals.Eat(c);
                e.eaten++;
                e.hp += config.devourHp;
                e.maxHp += config.devourHp;
                e.radius += config.devourGrow;
                e.visual.SetSizeMul(e.radius / e.def.radius);
            }
            return e.position.magnitude;
        }

        // враг дошёл до звезды: бьёт её и исчезает, о КУПОЛ разбивается
        void Strike(int i, bool dome)
        {
            if (dome)
            {
                Shatter(i);
                return;
            }
            star.Damage(live[i].starDamage);
            Remove(i);
        }

        // Улей лопается у звезды: кристаллов нет, вместо них Рой
        void Burst(int i)
        {
            Vector2 centre = live[i].position;
            Remove(i);
            Scatter(centre, config.hiveSwarmers, config.hiveBurstSpread, 1f);
        }

        // Рой россыпью вокруг точки centre, без телеграфа
        void Scatter(Vector2 centre, int amount, float spread, float speedMul)
        {
            Rng rng = loop.Run.simRng;
            for (int k = 0; k < amount; k++)
            {
                // корень из случайного числа даёт равномерный разброс по площади круга
                float r = spread * Mathf.Sqrt(rng.Range(0f, 1f));
                float rad = rng.Range(0f, 360f) * Mathf.Deg2Rad;
                Spawn(EnemyType.Swarmer, centre + new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r), speedMul);
            }
        }

        // первая встреча с типом врага (не в обучении и не в Дне): подсказка сверху.
        // Рою, Снаряду и объектам событий она не нужна: у них пустая строка.
        void Meet(EnemyType type)
        {
            if (loop.Run.tutorial || loop.Run.mode == RunMode.Daily || Strings.EnemyTip[(int)type].Length == 0) return;

            SaveData data = loop.Save.Data;
            int bit = 1 << (int)type;
            if ((data.seenEnemies & bit) != 0) return;

            data.seenEnemies |= bit;
            loop.Save.Save();
            // пустая запись в enemyIcons: на баннере картинка и цвет Body из префаба врага
            Sprite icon = art.enemyIcons[(int)type].sprite;
            SpriteRenderer look = art.enemies[(int)type].prefab.Body;
            banner.Show(Strings.EnemyTip[(int)type], icon != null ? icon : look.sprite, icon != null ? Color.white : look.color, config.bannerEnemyHold);
        }

        // урон прошёл: над врагом число; выживший белеет и вздрагивает прочь от точки from. true = враг убит.
        // byHitter = удар спутника или тени
        bool Hurt(int i, float damage, Vector2 from, NumberKind kind, bool byHitter = false)
        {
            Enemy e = live[i];
            e.hp -= damage;
            e.hurt = true;
            e.calm = 0f;
            if (e.maxHp >= config.numberMinHp) fx.Number(Above(e), damage, kind);
            if (e.hp <= 0f)
            {
                Kill(i, byHitter);
                return true;
            }

            fx.Flash(e.flash);
            fx.Recoil(e.body, (e.position - from).normalized, config.recoilDistance);
            return false;
        }

        // удар не прошёл (броня или щит): искры от хиттера и серебряный «0»
        void Clink(Enemy e, Vector2 hitter)
        {
            fx.Sparks(hitter, config.clinkSparks);
            if (e.maxHp >= config.numberMinHp) fx.Number(Above(e), 0f, NumberKind.Blocked);
            fx.HitStop(config.clinkHitStop);
            sfx.Play(SfxId.Clink);
        }

        // точка над врагом: отсюда всплывает число урона
        static Vector2 Above(Enemy e)
        {
            return e.position + Vector2.up * e.radius;
        }

        // Призрак сейчас прозрачный; с ТЁМНОЙ МАТЕРИЕЙ он плотный всегда
        bool Phased(Enemy e)
        {
            return e.def.phaseOn > 0f && e.phase >= e.def.phaseOn && !loop.Stats.DarkMatter;
        }

        void Kill(int i, bool byHitter = false)
        {
            Enemy e = live[i];
            Vector2 at = e.position;
            bool burning = e.burn > 0f;
            bool elite = e.affixes != 0;
            // Метеор отдаёт кристаллы, только когда его разбил спутник или тень
            int drop = e.def.id == EnemyType.Meteor ? (byHitter ? config.meteorCrystals : 0) : e.def.crystals;
            // элита роняет кристаллов втрое больше, Пожиратель отдаёт и съеденные
            // ЗАСЕВ: убитый в облаке Туманности роняет ещё кристаллы
            int seeded = loop.Stats.Has(StatId.CloudSeed) && eraRule.InCloud(at) ? config.cloudSeedCrystals : 0;
            // ПЕРО ЖАР-ПТИЦЫ: убийство горячим спутником или тенью даёт ещё кристаллы
            int feather = byHitter && loop.Stats.Has(StatId.Feather) && strikerHeat >= config.featherHeat ? config.featherCrystals : 0;
            // Кристальный прилив умножает всё, что выпало
            crystals.Spawn(e.position, ((elite ? drop * config.eliteCrystalMul : drop) + e.eaten + seeded + feather) * events.CrystalMul);
            if (e.herald) relics.Herald();
            // большой враг: осколков больше и экран вздрагивает; у элиты осколки золотые и их вдвое больше
            bool big = e.radius >= config.bigRadius;
            int shards = big ? config.killShards * config.bigKillShardMul : config.killShards;
            fx.Shards(e.position, elite ? shards * config.eliteShardMul : shards, elite);
            if (elite) loop.Vibrate(config.vibrateEliteKill);
            if (big)
            {
                fx.Shake(config.bigKillShake, config.bigKillShakeTime);
                sfx.Play(SfxId.BigKill);
            }
            // мелочь гибнет без остановки, иначе стая Роя превращается в слайд-шоу
            if (e.def.hp >= config.hitStopMinBaseHp) fx.HitStop(config.hitStopTime);
            sfx.Play(SfxId.Kill);
            // Дробитель распадается на быстрый Рой
            if (e.def.splitCount > 0) Scatter(e.position, e.def.splitCount, config.splitSpread, config.splitSpeedMul);
            // Мина взрывается от любой гибели; взрыв идёт в очередь, как у ЦЕПНОЙ РЕАКЦИИ; САПЁР его усиливает
            if (e.def.behavior == EnemyBehavior.Mine)
            {
                QueueBlast(at, config.mineRadius, loop.Stats.Has(StatId.Sapper) ? config.mineDamage * config.sapperMineMul : config.mineDamage);
                sfx.Play(SfxId.Mine);
            }
            // ЦЕПНАЯ РЕАКЦИЯ: на месте гибели будет взрыв
            if (loop.Stats.Chain) QueueBlast(at, config.chainRadius, config.chainDamage);
            // АККРЕЦИЯ: гибель врага рядом со звездой лечит её
            if (loop.Stats.Accretion && at.sqrMagnitude <= config.accretionRadius * config.accretionRadius) star.Heal(config.accretionHeal);
            loop.Run.kills++;
            combo.Kill();
            Remove(i);
            // убитый уже не в списке: огонь и волна достаются только соседям; огонь первым, пока волна их не отбросила
            if (burning && loop.Stats.Has(StatId.Wildfire)) Ignite(at);
            if (byHitter && loop.Stats.Has(StatId.Shockwave)) Shock(at);
        }

        // УДАРНАЯ ВОЛНА: соседи убитого отлетают от звезды, как от ИМПУЛЬСА
        void Shock(Vector2 centre)
        {
            float radius = config.shockRadius;
            for (int i = 0; i < count; i++)
            {
                Enemy e = live[i];
                if ((e.position - centre).sqrMagnitude > radius * radius) continue;

                float r = e.position.magnitude;
                e.position *= (r + config.shockPush) / r;
                e.engaged = false;
                e.timer = 0f;
                Place(e);
            }
        }

        // ПОЖАР: огонь горящего убитого перекидывается на соседей
        void Ignite(Vector2 centre)
        {
            float radius = config.wildfireRadius;
            for (int i = 0; i < count; i++)
                if ((live[i].position - centre).sqrMagnitude <= radius * radius) live[i].burn = config.burnTime;
        }


        // КУПОЛ: враг разбивается о звезду, кристаллов нет
        void Shatter(int i)
        {
            fx.Shards(live[i].position, config.killShards);
            sfx.Play(SfxId.Kill);
            Remove(i);
        }

        // на место убранного встаёт последний живой; при обходе с конца он уже обработан
        void Remove(int i)
        {
            Enemy e = live[i];
            e.visual.gameObject.SetActive(false);
            count--;
            live[i] = live[count];

            int t = (int)e.def.id;
            free[t][freeCount[t]++] = e;
        }

        Enemy Create(EnemyDef def)
        {
            Visual visual = Instantiate(art.enemies[(int)def.id].prefab, root);
            visual.gameObject.SetActive(false);

            // перезарядка удара своя на каждый хиттер: спутники и их тени
            var e = new Enemy { visual = visual, transform = visual.transform, def = def, hitCooldown = new float[satellites.MaxHitters] };
            e.body = visual.Body.transform;
            e.flash = fx.Overlay(visual);
            e.hpRing = visual.GetComponentInChildren<HpRing>(true);
            e.cracks = visual.GetComponentInChildren<DamageStages>(true);
            if (def.behavior == EnemyBehavior.Attach) e.attachMarker = e.transform.Find(AttachMarkerName).gameObject;
            if (def.shieldHits > 0) e.shield = e.transform.Find(ShieldName);
            e.eliteMark = e.transform.Find(EliteMarkName).gameObject;
            e.affixIcon = e.transform.Find(AffixIconName).GetComponent<SpriteRenderer>();
            return e;
        }

        void Place(Enemy e)
        {
            e.transform.localPosition = e.position;
            if (e.def.faceStar)
            {
                // ось +Y картинки смотрит на звезду, а у Кружащего на орбите по ходу движения
                Vector2 forward = -e.position;
                if (e.def.behavior == EnemyBehavior.Orbit && e.engaged) forward = new Vector2(-e.position.y, e.position.x) * (e.def.orbitDeg * Mathf.Deg2Rad) + forward.normalized * e.def.orbitInSpeed;
                e.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90f);
            }
            // СТАЗИС: враг наполовину серебряный
            e.visual.Tint(config.stasisTint, abilities.Active(AbilityId.Stasis) ? config.stasisTintAmount : 0f);
            e.visual.SetAlpha(Mathf.Clamp01(e.age / config.spawnFadeIn) * (Phased(e) ? config.phantomAlpha : 1f));

            if (e.def.behavior == EnemyBehavior.Attach && e.attachMarker.activeSelf != e.engaged) e.attachMarker.SetActive(e.engaged);
            // сломанный щит пропадает
            if (e.def.shieldHits > 0 && e.shield.gameObject.activeSelf != e.shieldHits > 0) e.shield.gameObject.SetActive(e.shieldHits > 0);
        }

        // кольцо HP и трещины; кольцо есть только у врагов с большим запасом HP и зависит от настройки.
        // Радиус кольца идёт от базового радиуса: Пожиратель растёт масштабом корня, и кольцо вместе с ним.
        void Gauge(Enemy e, float dt)
        {
            float fraction = e.hp / e.maxHp;
            HpRingMode mode = (HpRingMode)loop.Save.Data.settings.hpRings;
            bool ring = e.maxHp >= config.hpRingMinHp && (mode == HpRingMode.Always || mode == HpRingMode.AfterHit && e.hurt);
            if (e.hpRing.gameObject.activeSelf != ring) e.hpRing.gameObject.SetActive(ring);
            if (ring) e.hpRing.Draw(e.def.radius + config.hpRingOffset, fraction, dt);
            e.cracks.Show(fraction);
        }

        public EnemyDef Def(EnemyType type)
        {
            EnemyDef[] defs = config.enemies;
            for (int i = 0; i < defs.Length; i++)
                if (defs[i].id == type) return defs[i];
            return null;
        }

        // круги попаданий в Scene view
        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            for (int i = 0; i < count; i++) Gizmos.DrawWireSphere(live[i].position, live[i].radius);
        }
    }
}
