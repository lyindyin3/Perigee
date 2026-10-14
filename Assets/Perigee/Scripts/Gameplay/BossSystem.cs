using UnityEngine;

namespace Perigee
{
    // Босс эры: Комета, Близнецы (две кометы), Колосс, Пульсар, Сеятель, Зеркало, Гравитон или Бездна. Приходит на секунде loop.BossTime,
    // победа над ним (над всеми его телами) заканчивает забег. В Бесконечном он приходит каждый цикл, а забег идёт дальше.
    public class BossSystem : MonoBehaviour
    {
        // у Близнецов два тела, у остальных боссов одно
        const int MaxParts = 2;
        // второе тело Близнецов: средняя аномалия сдвинута на пол-оборота, эллипс развёрнут на 180°
        const float HalfTurn = 180f;
        const float FullTurn = 2f * Mathf.PI;
        // обязательные части префаба Зеркала: два стража напротив друг друга
        static readonly string[] GuardNames = { "Guard0", "Guard1" };
        // фазы Бездны: ГЛАЗ, ЗАТМЕНИЕ, ПАСТЬ
        const int Eye = 0;
        const int Eclipse = 1;
        const int Maw = 2;

        class Part
        {
            public Visual visual;
            public Transform transform;
            public Transform body;
            public SpriteRenderer flash;
            public HpRing hpRing;
            public DamageStages cracks;
            public BossOrbitLine line;
            public float[] hitCooldown;
            public Vector2 position;
            public float hp;
            public bool alive;
            // урон уже был: кольцо HP в режиме «после удара» остаётся на виду
            public bool hurt;
            // секунды до следующего пореза НИТЬЮ-ПИЛОЙ
            public float sawCooldown;
            // сколько секунд ещё горит (РАСКАЛЁННЫЙ)
            public float burn;
            // тело кометы: средняя аномалия в радианах (копится, чтобы смена периода шла без скачка),
            // поворот эллипса в градусах, сколько перигеев пройдено, сколько секунд ещё видна линия орбиты
            public float mean;
            public float phi;
            public int passes;
            public float lineTimer;
        }

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] EnemySystem enemies;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] StarController star;
        [SerializeField] CrystalSystem crystals;
        [SerializeField] Director director;
        [SerializeField] BannerView banner;
        [SerializeField] RelicSystem relics;
        // линии орбит комет: вторая для второго из Близнецов
        [SerializeField] BossOrbitLine[] orbitLines;
        [SerializeField] Fx fx;
        [SerializeField] ScreenFx screenFx;
        [SerializeField] BossIntro intro;
        [SerializeField] AudioManager sfx;
        [SerializeField] Transform root;

        readonly Part[] parts = new Part[MaxParts];
        int partCount;
        int aliveParts;
        EnemyDef def;
        // HP одного тела
        float maxHp;
        // секунды с появления, по времени врагов
        float age;
        // сколько сроков прихода босса уже прошло; больше одного бывает только в Бесконечном
        int arrivals;
        // броня босса: у Колосса падает с каждой фазой
        float armor;
        // Колосс: сколько порогов HP уже пройдено
        int phase;
        // Колосс и Пульсар: угол на поле, градусы
        float angle;
        // Пульсар: дошёл до своей орбиты; секунды на ней; секунды до залпа и до волны
        bool arrived;
        float orbitTime;
        float fireTimer;
        float waveTimer;
        // Пульсар: волна идёт (сначала мигание тела, потом кольцо); секунды с её начала; кого она уже оглушила
        bool waving;
        float waveAge;
        bool[] waveStunned;
        Visual wave;
        // Зеркало и Гравитон: секунды до смены; мигание перед ней уже было
        float flipTimer;
        bool warned;
        // Зеркало: стражи, их угол и направление, перезарядка касания на каждый страж и хиттер
        Transform[] guards;
        Vector2[] guardPositions;
        float[][] guardCooldown;
        float guardAngle;
        int guardDir;
        // Гравитон сейчас отталкиватель
        bool repelling;
        // Бездна: секунды неуязвимости при смене фазы
        float shift;

        // Бездна в фазе ЗАТМЕНИЕ срезает свет во тьме
        public float LightCut => Alive && def.behavior == EnemyBehavior.Abyss && phase == Eclipse ? config.abyssLightCut : 0f;

        // пока босс проявляется и пока Бездна меняет фазу, урон по нему не проходит
        bool Invulnerable => age < config.bossFadeIn || shift > 0f;

        // жив хотя бы один из тел босса
        public bool Alive { get; private set; }

        // у Близнецов сумма HP обоих
        public float Hp
        {
            get
            {
                float sum = 0f;
                for (int p = 0; p < partCount; p++)
                    if (parts[p].alive) sum += parts[p].hp;
                return sum;
            }
        }

        public float MaxHp => maxHp * partCount;

        // броня живого босса (0 = брони нет): по ней встаёт кольцо брони
        public float Armor => Alive ? armor : 0f;

        void Awake()
        {
            for (int p = 0; p < parts.Length; p++) parts[p] = new Part { line = orbitLines[p], hitCooldown = new float[satellites.MaxHitters] };
            waveStunned = new bool[config.maxSatellites];
            guards = new Transform[GuardNames.Length];
            guardPositions = new Vector2[GuardNames.Length];
            guardCooldown = new float[GuardNames.Length][];
            for (int g = 0; g < guards.Length; g++) guardCooldown[g] = new float[satellites.MaxHitters];
        }

        public void ResetRun()
        {
            // босс эры создаётся заново из префаба: правка префаба видна со следующего забега
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);

            def = enemies.Def(loop.Era.boss);
            partCount = def.id == EnemyType.Twin ? MaxParts : 1;
            for (int p = 0; p < parts.Length; p++)
            {
                Part part = parts[p];
                part.alive = false;
                part.line.Hide();
                if (p >= partCount) continue;

                part.visual = Instantiate(art.enemies[(int)def.id].prefab, root);
                part.visual.gameObject.SetActive(false);
                part.transform = part.visual.transform;
                part.body = part.visual.Body.transform;
                part.flash = fx.Overlay(part.visual);
                part.hpRing = part.visual.GetComponentInChildren<HpRing>(true);
                part.cracks = part.visual.GetComponentInChildren<DamageStages>(true);
            }
            if (def.behavior == EnemyBehavior.Pulsar)
            {
                wave = Instantiate(art.pulsarWave, root);
                wave.gameObject.SetActive(false);
            }
            if (def.behavior == EnemyBehavior.Mirror)
                for (int g = 0; g < guards.Length; g++) guards[g] = parts[0].transform.Find(GuardNames[g]);

            arrivals = 0;
            Alive = false;
        }

        public void Tick(float dt)
        {
            // в Бесконечном срок прихода наступает каждый цикл; босс приходит, если прошлый уже убит
            bool endless = loop.Run.mode == RunMode.Endless;
            if ((endless || arrivals == 0) && loop.Run.time >= loop.BossTime + arrivals * config.endlessLoopTime)
            {
                if (!Alive) Spawn(arrivals);
                arrivals++;
            }
            if (!Alive) return;

            // СТАЗИС замедляет босса; перезарядка удара, линии орбит и кольцо HP идут по обычному времени
            float enemyDt = dt * loop.EnemyTimeScale;
            age += enemyDt;
            bool horizon = loop.Stats.Horizon;

            for (int p = 0; p < partCount; p++)
            {
                Part part = parts[p];
                if (!part.alive) continue;

                for (int k = 0; k < part.hitCooldown.Length; k++) part.hitCooldown[k] -= dt;
                part.sawCooldown -= dt;

                // поджог и ГОРИЗОНТ СОБЫТИЙ: босс теряет HP по времени врагов, без числа и вспышки; при смене фазы Бездны не теряет
                float seared = 0f;
                if (shift > 0f) part.burn = 0f;
                if (part.burn > 0f)
                {
                    float burning = Mathf.Min(part.burn, enemyDt);
                    part.burn -= burning;
                    seared += loop.Stats.burnDps * burning;
                }
                if (horizon && shift <= 0f && part.position.sqrMagnitude <= config.horizonRadius * config.horizonRadius) seared += config.horizonDps * enemyDt;
                if (seared > 0f)
                {
                    part.hp -= seared;
                    part.hurt = true;
                    if (part.hp <= 0f)
                    {
                        Die(part);
                        continue;
                    }
                }

                switch (def.behavior)
                {
                    case EnemyBehavior.Colossus: Walk(part, enemyDt); break;
                    case EnemyBehavior.Pulsar: Swing(part, enemyDt); break;
                    case EnemyBehavior.Sower: Sow(part, enemyDt); break;
                    case EnemyBehavior.Mirror: Reflect(part, dt, enemyDt); break;
                    case EnemyBehavior.Graviton: Bend(part, enemyDt); break;
                    case EnemyBehavior.Abyss: Devour(part, dt, enemyDt); break;
                    default: Fly(part, dt, enemyDt); break;
                }
                Place(part);
                Gauge(part, dt);
            }
        }

        public void Collide()
        {
            if (!Alive || Invulnerable) return;

            if (def.behavior == EnemyBehavior.Mirror) Guard();
            Stats stats = loop.Stats;
            float reach = satellites.HitterRadius + def.radius;
            int hitters = satellites.HitterCount;
            for (int p = 0; p < partCount; p++)
            {
                Part part = parts[p];
                for (int k = 0; k < hitters && part.alive; k++)
                {
                    if (!satellites.HitterActive(k)) continue;
                    if (part.hitCooldown[k] > 0f) continue;
                    Vector2 hitter = satellites.HitterPosition(k);
                    if ((hitter - part.position).sqrMagnitude > reach * reach) continue;

                    part.hitCooldown[k] = config.hitCooldown;
                    float damage = satellites.HitterDamage(k);
                    bool crit = loop.Run.simRng.Range(0f, 1f) < stats.CritChance(satellites.HitterHeat(k));
                    if (crit) damage *= stats.critMul;
                    // МОЛОТ: каждый крит навсегда сбивает часть брони
                    if (crit && stats.Has(StatId.Hammer)) armor = Mathf.Max(armor - config.hammerShred, 0f);
                    // броня (Колосс): удар слабее брони урона не наносит; БРОНЕБОЙ её срезает, крит с ПРИЗМОЙ проходит сквозь неё
                    if (!(crit && stats.Has(StatId.Prism)) && damage < Mathf.Max(armor - stats.armorPierce, 0f))
                    {
                        fx.Sparks(hitter, config.clinkSparks);
                        fx.Number(Above(part), 0f, NumberKind.Blocked);
                        fx.HitStop(config.clinkHitStop);
                        sfx.Play(SfxId.Clink);
                        continue;
                    }

                    // ОСКОЛОК КОМЕТЫ: каждый третий удар, который прошёл, сильнее
                    damage += relics.Strike(k);
                    if (Hurt(part, damage, hitter, crit ? NumberKind.Crit : NumberKind.Normal)) break;
                    // РАСКАЛЁННЫЙ: горячий спутник поджигает, новый поджог начинает отсчёт заново
                    if (stats.Burn && satellites.HitterHeat(k) >= config.burnHeatMin) part.burn = config.burnTime;
                    fx.Shards(hitter, config.hitShards, part.position - hitter);
                    fx.HitStop(config.hitStopTime);
                    sfx.Play(SfxId.Hit);
                }

                // НИТЬ-ПИЛА режет и босса, броня не спасает
                Vector2 point;
                if (!part.alive || !stats.SawThread || part.sawCooldown > 0f || !satellites.Sawn(part.position, def.radius, out point)) continue;

                part.sawCooldown = stats.sawInterval;
                if (Hurt(part, stats.sawDamage, point, NumberKind.Normal)) continue;

                fx.Shards(point, config.hitShards, part.position - point);
                sfx.Play(SfxId.Hit);
            }
        }

        // РЕВЕРС: каждый хиттер может ударить снова, не дожидаясь перезарядки
        public void ClearHitCooldowns()
        {
            for (int p = 0; p < partCount; p++) System.Array.Clear(parts[p].hitCooldown, 0, parts[p].hitCooldown.Length);
        }

        // волна из точки centre (ИМПУЛЬС, МАЯК, ЦЕПНАЯ РЕАКЦИЯ): урон без толчка, броня не спасает
        public void Blast(Vector2 centre, float radius, float damage)
        {
            if (!Alive || Invulnerable) return;

            for (int p = 0; p < partCount; p++)
            {
                Part part = parts[p];
                if (part.alive && (part.position - centre).sqrMagnitude <= radius * radius) Hurt(part, damage, centre, NumberKind.Normal);
            }
        }

        // урон прошёл: над боссом число; пока жив, он белеет и вздрагивает прочь от точки from. true = это тело убито
        bool Hurt(Part part, float damage, Vector2 from, NumberKind kind)
        {
            part.hp -= damage;
            part.hurt = true;
            fx.Number(Above(part), damage, kind);
            if (part.hp <= 0f)
            {
                Die(part);
                return true;
            }

            fx.Flash(part.flash);
            fx.Recoil(part.body, (part.position - from).normalized, config.bossRecoilDistance);
            return false;
        }

        // cycle = какой по счёту срок прихода, с нуля; вне Бесконечного всегда 0
        void Spawn(int cycle)
        {
            Alive = true;
            aliveParts = partCount;
            // в Бесконечном враги приходят и при боссе
            if (loop.Run.mode != RunMode.Endless) director.Stop();

            // эра боссов не усиливает; в Бесконечном босс крепче с каждым циклом
            maxHp = def.hp * (1f + config.endlessBossHpStep * cycle);
            armor = def.armor;
            age = 0f;
            phase = 0;
            arrived = false;
            waving = false;
            shift = 0f;
            warned = false;
            repelling = false;
            guardAngle = 0f;
            guardDir = 1;
            for (int g = 0; g < guards.Length; g++) System.Array.Clear(guardCooldown[g], 0, guardCooldown[g].Length);
            // первый залп и первая смена через полный срок после выхода на орбиту
            switch (def.behavior)
            {
                case EnemyBehavior.Sower: fireTimer = config.sowerInterval; break;
                case EnemyBehavior.Mirror: fireTimer = config.mirrorFireInterval; flipTimer = config.mirrorFlip; break;
                case EnemyBehavior.Graviton: flipTimer = config.gravitonFlip; break;
                case EnemyBehavior.Abyss: fireTimer = config.abyssArrowInterval; break;
            }
            angle = loop.Run.simRng.Range(0f, 360f);

            bool comet = def.behavior == EnemyBehavior.Ellipse;
            for (int p = 0; p < partCount; p++)
            {
                Part part = parts[p];
                part.alive = true;
                part.hp = maxHp;
                part.hurt = false;
                part.hpRing.Fill();
                part.sawCooldown = 0f;
                part.burn = 0f;
                System.Array.Clear(part.hitCooldown, 0, part.hitCooldown.Length);

                if (comet)
                {
                    // M = π в апоцентре: Комета появляется в самой дальней точке, второй из Близнецов в это время у звезды
                    part.mean = Mathf.PI + p * Mathf.PI;
                    part.phi = angle + p * HalfTurn;
                    part.passes = Mathf.FloorToInt(part.mean / FullTurn);
                    part.position = Orbit(part);
                    ShowOrbit(part);
                }
                // остальные приходят с края поля
                else part.position = Polar(angle, config.spawnRadius);

                Place(part);
                Gauge(part, 0f);
                part.visual.gameObject.SetActive(true);
            }

            banner.Show(Strings.BossTitle(def.id), null, Color.white, config.bannerBossHold);
            intro.Show(def.id, parts[0].position);
            // босс в бестиарии Атласа
            loop.Save.Data.seenEnemies |= 1 << (int)def.id;
            loop.Save.Save();
            sfx.Play(SfxId.BossIn);
            sfx.PlayMusic(loop.RunMusic);
        }

        void Die(Part part)
        {
            part.alive = false;
            part.visual.gameObject.SetActive(false);
            part.line.Hide();
            crystals.Spawn(part.position, def.crystals);
            aliveParts--;
            bool endless = loop.Run.mode == RunMode.Endless;
            if (aliveParts > 0)
            {
                // первый из Близнецов: гибель как большое убийство и смена фазы (оставшийся летит быстрее)
                fx.Shards(part.position, config.killShards * config.bigKillShardMul);
                sfx.Play(SfxId.Kill);
                sfx.Play(SfxId.BigKill);
                Phase();
                return;
            }

            // босс убит: замедление, белая вспышка, дождь осколков, тряска, вибрация
            Alive = false;
            fx.Slow(config.bossKillSlow, config.bossKillSlowTime);
            screenFx.Flash(Color.white, config.bossKillFlash, config.bossKillFlashTime);
            fx.Shards(part.position, config.bossKillShards);
            fx.Shake(config.bossKillShake, config.bossKillShakeTime);
            sfx.Play(SfxId.BossKill);
            loop.Vibrate(config.vibrateBossKill);
            if (def.behavior == EnemyBehavior.Pulsar) wave.gameObject.SetActive(false);
            if (!endless)
            {
                loop.Win();
                return;
            }

            // Бесконечный: за босса будет пыль на итогах, баннер говорит сколько
            loop.Run.bossKills++;
            sfx.PlayMusic(loop.RunMusic);
            banner.Show(string.Format(Strings.BossDustFormat, loop.Progress.BossDust(loop.Era)), null, Color.white, config.bannerDustHold);
        }

        // Тело кометы (Комета и каждый из Близнецов): эллипс со звездой в фокусе;
        // на каждом перигее удар по звезде и поворот эллипса.
        void Fly(Part part, float dt, float enemyDt)
        {
            part.lineTimer -= dt;
            if (part.lineTimer <= 0f) part.line.Hide();

            part.mean += FullTurn * enemyDt / Period;
            int passed = Mathf.FloorToInt(part.mean / FullTurn);
            if (passed > part.passes)
            {
                part.passes = passed;
                // Бездна в фазе ЗАТМЕНИЕ бьёт своим уроном
                star.Damage(def.behavior == EnemyBehavior.Abyss ? config.abyssDiveDamage : def.starDamage);
                part.phi += config.bossPhiStep;
                ShowOrbit(part);
            }
            part.position = Orbit(part);
        }

        // у Близнецов свой период, а оставшийся один летит быстрее
        float Period
        {
            get
            {
                if (def.behavior == EnemyBehavior.Abyss) return config.abyssPeriod;
                if (def.id != EnemyType.Twin) return config.bossPeriod;
                return aliveParts == partCount ? config.twinPeriod : config.twinEnragedPeriod;
            }
        }

        // эллипс кометы; у Бездны в фазе ЗАТМЕНИЕ свой
        float Periapsis => def.behavior == EnemyBehavior.Abyss ? config.abyssPeriapsis : config.bossPeriapsis;

        float Apoapsis => def.behavior == EnemyBehavior.Abyss ? config.abyssApoapsis : config.bossApoapsis;

        float SemiMajor => (Periapsis + Apoapsis) * 0.5f;

        float Eccentricity => (Apoapsis - Periapsis) / (Apoapsis + Periapsis);

        void ShowOrbit(Part part)
        {
            float a = SemiMajor;
            float e = Eccentricity;
            part.line.Show(a, a * Mathf.Sqrt(1f - e * e), e, part.phi);
            part.lineTimer = config.bossOrbitLineTime;
        }

        Vector2 Orbit(Part part)
        {
            float a = SemiMajor;
            float e = Eccentricity;
            float b = a * Mathf.Sqrt(1f - e * e);

            // уравнение Кеплера E − e·sin E = M, метод Ньютона от E = M: у апоцентра медленно, в нырке быстро
            float m = part.mean;
            float anomaly = m;
            for (int i = 0; i < config.bossNewtonSteps; i++) anomaly -= (anomaly - e * Mathf.Sin(anomaly) - m) / (1f - e * Mathf.Cos(anomaly));

            // звезда стоит в фокусе, поэтому центр эллипса сдвинут на a·e
            float x = a * (Mathf.Cos(anomaly) - e);
            float y = b * Mathf.Sin(anomaly);
            float rad = part.phi * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(x * cos - y * sin, x * sin + y * cos);
        }

        // Колосс идёт прямо на звезду; коснувшись, бьёт её и отлетает назад, сместившись против часовой стрелки
        void Walk(Part part, float enemyDt)
        {
            float r = part.position.magnitude - def.speed * enemyDt;
            if (r <= loop.Star.Radius + def.radius)
            {
                star.Damage(def.starDamage);
                angle += config.colossusBounceDeg;
                r = config.colossusBounceRadius;
            }
            part.position = Polar(angle, r);

            // фазы: на каждом пороге HP он выпускает Рой вокруг себя и теряет часть брони
            float[] thresholds = config.colossusPhaseHp;
            while (phase < thresholds.Length && part.hp < maxHp * thresholds[phase])
            {
                phase++;
                armor -= config.colossusArmorStep;
                Phase();
                for (int k = 0; k < config.colossusShed; k++)
                    enemies.Spawn(EnemyType.Swarmer, part.position + Polar(k * 360f / config.colossusShed, def.radius + config.colossusShedOffset), 1f);
            }
        }

        // Пульсар доходит до своей орбиты и кружит против часовой стрелки, то приближаясь, то удаляясь
        void Swing(Part part, float enemyDt)
        {
            if (!arrived)
            {
                // орбита начинается с самой дальней точки
                float start = config.pulsarMid + config.pulsarAmp;
                float r = Mathf.MoveTowards(part.position.magnitude, start, def.speed * enemyDt);
                part.position = Polar(angle, r);
                if (r > start) return;

                arrived = true;
                orbitTime = 0f;
                fireTimer = config.pulsarFireInterval;
                waveTimer = config.pulsarWaveFirst;
                return;
            }

            orbitTime += enemyDt;
            angle += config.pulsarDeg * enemyDt;
            float radius = config.pulsarMid + config.pulsarAmp * Mathf.Cos(FullTurn * orbitTime / config.pulsarSwing);
            part.position = Polar(angle, radius);

            fireTimer -= enemyDt;
            if (fireTimer <= 0f)
            {
                fireTimer += config.pulsarFireInterval;
                // залп веером: Снаряды стартуют с орбиты Пульсара по обе стороны от него и летят к звезде
                float first = angle - (config.pulsarBolts - 1) * 0.5f * config.pulsarFan;
                for (int k = 0; k < config.pulsarBolts; k++) enemies.Spawn(EnemyType.Bolt, Polar(first + k * config.pulsarFan, radius), 1f);
            }

            waveTimer -= enemyDt;
            if (waveTimer <= 0f)
            {
                waveTimer += config.pulsarWaveInterval;
                waving = true;
                waveAge = 0f;
                System.Array.Clear(waveStunned, 0, waveStunned.Length);
            }
            if (waving) Wave(part, enemyDt);
        }

        // Волна Пульсара: тело мигает (это делает Place), потом от него расходится кольцо
        // и оглушает каждый спутник, которого коснулось. Тени оно не трогает.
        void Wave(Part part, float enemyDt)
        {
            waveAge += enemyDt;
            float t = (waveAge - config.pulsarWaveWarn) / config.pulsarWaveTime;
            if (t < 0f) return;
            if (t >= 1f)
            {
                waving = false;
                wave.gameObject.SetActive(false);
                return;
            }

            float radius = t * config.pulsarWaveRadius;
            // Visual запоминает масштаб при включении, поэтому сначала объект, потом размер
            if (!wave.gameObject.activeSelf) wave.gameObject.SetActive(true);
            wave.transform.localPosition = part.position;
            wave.SetSizeMul(radius);

            float band = satellites.HitterRadius + config.pulsarWaveBand;
            for (int i = 0; i < satellites.Count; i++)
            {
                if (waveStunned[i]) continue;
                if (Mathf.Abs((satellites.HitterPosition(i) - part.position).magnitude - radius) > band) continue;

                waveStunned[i] = true;
                satellites.Stun(i);
            }
        }

        // выход с края прямо на свою орбиту, потом круг против часовой стрелки; true = уже на орбите
        bool Approach(Part part, float radius, float deg, float enemyDt)
        {
            if (!arrived)
            {
                float r = Mathf.MoveTowards(part.position.magnitude, radius, def.speed * enemyDt);
                part.position = Polar(angle, r);
                arrived = r <= radius;
                return false;
            }

            angle += deg * enemyDt;
            part.position = Polar(angle, radius);
            return true;
        }

        // залп веером к звезде: n врагов на радиусе r по обе стороны от угла centre, соседние через step градусов
        void Fan(EnemyType type, int n, float step, float centre, float r, float speedMul)
        {
            float first = centre - (n - 1) * 0.5f * step;
            for (int k = 0; k < n; k++) enemies.Spawn(type, Polar(first + k * step, r), speedMul);
        }

        // n врагов поровну по краю поля, без телеграфа
        void Ring(EnemyType type, int n)
        {
            float start = loop.Run.simRng.Range(0f, 360f);
            for (int k = 0; k < n; k++) enemies.Spawn(type, Polar(start + k * 360f / n, config.spawnRadius), 1f);
        }

        // Сеятель кружит и роняет Ульи со своей орбиты; раненый роняет чаще
        void Sow(Part part, float enemyDt)
        {
            if (!Approach(part, config.sowerRadius, config.sowerDeg, enemyDt)) return;

            fireTimer -= enemyDt;
            if (fireTimer > 0f) return;
            fireTimer += part.hp < maxHp * config.sowerFastHp ? config.sowerFastInterval : config.sowerInterval;
            enemies.Spawn(EnemyType.Hive, part.position, config.sowerHiveSpeed);
        }

        // Зеркало кружит со стражами, стреляет и раз в mirrorFlip разворачивает орбиту игрока
        void Reflect(Part part, float dt, float enemyDt)
        {
            for (int g = 0; g < guards.Length; g++)
                for (int k = 0; k < guardCooldown[g].Length; k++) guardCooldown[g][k] -= dt;

            bool orbiting = Approach(part, config.mirrorRadius, config.mirrorDeg, enemyDt);
            guardAngle += guardDir * config.guardDeg * enemyDt;
            for (int g = 0; g < guards.Length; g++)
            {
                Vector2 offset = Polar(guardAngle + g * HalfTurn, config.guardDistance);
                guardPositions[g] = part.position + offset;
                guards[g].localPosition = offset;
            }
            if (!orbiting) return;

            fireTimer -= enemyDt;
            if (fireTimer <= 0f)
            {
                fireTimer += config.mirrorFireInterval;
                Fan(EnemyType.Bolt, config.mirrorBolts, config.mirrorFan, angle, config.mirrorRadius, 1f);
            }

            flipTimer -= enemyDt;
            if (!warned && flipTimer <= config.bossWarn)
            {
                warned = true;
                part.visual.Flash(config.bossWarnColor, config.bossWarn);
            }
            if (flipTimer > 0f) return;

            flipTimer += config.mirrorFlip;
            warned = false;
            // разворот без бонуса РЕВЕРСА: только направление орбиты и кольцо со стрелками
            satellites.Reverse();
            abilities.TurnFx();
            guardDir = -guardDir;
        }

        // страж Зеркала: касание спутника его не ранит, а греет спутник и роняет орбиту; тени проходят насквозь
        void Guard()
        {
            float reach = satellites.HitterRadius + config.guardRadius;
            for (int g = 0; g < guards.Length; g++)
            {
                for (int k = 0; k < satellites.Count; k++)
                {
                    if (!satellites.HitterActive(k) || guardCooldown[g][k] > 0f) continue;
                    Vector2 hitter = satellites.HitterPosition(k);
                    if ((hitter - guardPositions[g]).sqrMagnitude > reach * reach) continue;

                    guardCooldown[g][k] = config.hitCooldown;
                    satellites.Warm(k, config.guardHeat);
                    satellites.Knock(config.guardKnock);
                    fx.Sparks(hitter, config.clinkSparks);
                    fx.HitStop(config.clinkHitStop);
                    sfx.Play(SfxId.Clink);
                }
            }
        }

        // Гравитон дрейфует колодцем; раз в gravitonFlip на gravitonRepel становится отталкивателем и в этот миг даёт залп
        void Bend(Part part, float enemyDt)
        {
            bool orbiting = Approach(part, config.gravitonRadius, config.gravitonDeg, enemyDt);
            satellites.Gravity(part.position, config.gravitonWellRadius, repelling ? -config.gravitonBend : config.gravitonBend);
            if (!orbiting) return;

            flipTimer -= enemyDt;
            if (!repelling && !warned && flipTimer <= config.bossWarn)
            {
                warned = true;
                part.visual.Flash(config.bossWarnColor, config.bossWarn);
            }
            if (flipTimer > 0f) return;

            repelling = !repelling;
            warned = false;
            flipTimer += repelling ? config.gravitonRepel : config.gravitonFlip;
            if (repelling) Fan(EnemyType.Bolt, config.gravitonBolts, config.gravitonFan, angle, config.gravitonRadius, 1f);
        }

        // Бездна: ГЛАЗ кружит и пускает Стрелы, ЗАТМЕНИЕ ныряет кометой, ПАСТЬ ползёт к звезде колодцем
        void Devour(Part part, float dt, float enemyDt)
        {
            if (shift > 0f)
            {
                shift -= enemyDt;
                return;
            }
            float[] thresholds = config.abyssPhaseHp;
            if (phase < thresholds.Length && part.hp < maxHp * thresholds[phase])
            {
                Shift(part);
                return;
            }

            switch (phase)
            {
                case Eye:
                    if (!Approach(part, config.abyssRadius, config.abyssDeg, enemyDt)) return;
                    fireTimer -= enemyDt;
                    if (fireTimer > 0f) return;
                    fireTimer += config.abyssArrowInterval;
                    Fan(EnemyType.Arrow, config.abyssArrows, config.abyssArrowFan, angle, config.abyssRadius, config.abyssArrowSpeed);
                    return;

                case Eclipse:
                    Fly(part, dt, enemyDt);
                    fireTimer -= enemyDt;
                    if (fireTimer > 0f) return;
                    fireTimer += config.abyssPhantomInterval;
                    Ring(EnemyType.Phantom, config.abyssPhantoms);
                    return;

                default:
                    satellites.Gravity(part.position, config.abyssWellRadius, config.abyssBend);
                    // укус: звезда теряет много, Бездна отлетает назад по тому же лучу
                    float r = part.position.magnitude - config.abyssWalkSpeed * enemyDt;
                    if (r <= loop.Star.Radius + def.radius)
                    {
                        star.Damage(config.abyssBiteDamage);
                        r = config.abyssBounceRadius;
                    }
                    part.position = Polar(angle, r);
                    fireTimer -= enemyDt;
                    if (fireTimer > 0f) return;
                    fireTimer += config.abyssSwarmInterval;
                    Ring(EnemyType.Swarmer, config.abyssSwarm);
                    return;
            }
        }

        // смена фазы босса (пороги Колосса, первый из Близнецов, фазы Бездны): белая вспышка экрана, тряска, звук
        void Phase()
        {
            screenFx.Flash(Color.white, config.bossPhaseFlash, config.bossPhaseFlashTime);
            fx.Shake(config.bossPhaseShake, config.bossPhaseShakeTime);
            sfx.Play(SfxId.BossPhase);
        }

        // смена фазы Бездны: замирает неуязвимой, вспыхивает, экран трясётся, баннер называет фазу
        void Shift(Part part)
        {
            phase++;
            shift = config.abyssShift;
            angle = Mathf.Atan2(part.position.y, part.position.x) * Mathf.Rad2Deg;
            part.visual.Flash(config.bossWarnColor, config.abyssShift);
            banner.Show(Strings.AbyssPhase[phase], null, Color.white, config.bannerBossHold);
            Phase();
            if (phase == Eclipse)
            {
                armor = config.abyssEclipseArmor;
                fireTimer = config.abyssPhantomInterval;
                // эллипс начинается с апоцентра там, где стоит Бездна: при E = π точка лежит на угле phi + 180°
                part.mean = Mathf.PI;
                part.phi = angle + HalfTurn;
                part.passes = 0;
                part.position = Orbit(part);
                ShowOrbit(part);
                return;
            }

            armor = 0f;
            fireTimer = config.abyssSwarmInterval;
            part.line.Hide();
        }

        void Place(Part part)
        {
            part.transform.localPosition = part.position;
            // ось +Y картинки смотрит на звезду
            if (def.faceStar) part.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-part.position.y, -part.position.x) * Mathf.Rad2Deg - 90f);

            // Пульсар перед волной мигает кремовым; СТАЗИС делает босса наполовину серебряным
            if (waving && waveAge < config.pulsarWaveWarn) part.visual.Tint(config.pulsarWarnTint, Mathf.PingPong(waveAge * config.pulsarWarnHz * 2f, 1f));
            else part.visual.Tint(config.stasisTint, abilities.Active(AbilityId.Stasis) ? config.stasisTintAmount : 0f);
            part.visual.SetAlpha(Mathf.Clamp01(age / config.bossFadeIn));
        }

        // кольцо HP и трещины; кольцо зависит от настройки
        void Gauge(Part part, float dt)
        {
            float fraction = part.hp / maxHp;
            HpRingMode mode = (HpRingMode)loop.Save.Data.settings.hpRings;
            bool ring = maxHp >= config.hpRingMinHp && (mode == HpRingMode.Always || mode == HpRingMode.AfterHit && part.hurt);
            if (part.hpRing.gameObject.activeSelf != ring) part.hpRing.gameObject.SetActive(ring);
            if (ring) part.hpRing.Draw(def.radius + config.hpRingOffset, fraction, dt);
            part.cracks.Show(fraction);
        }

        // точка над телом: отсюда всплывает число урона
        Vector2 Above(Part part)
        {
            return part.position + Vector2.up * def.radius;
        }

        static Vector2 Polar(float degrees, float r)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r);
        }

        // круги попаданий в Scene view
        void OnDrawGizmos()
        {
            for (int p = 0; p < partCount; p++)
                if (parts[p].alive) Gizmos.DrawWireSphere(parts[p].position, def.radius);
        }
    }
}
