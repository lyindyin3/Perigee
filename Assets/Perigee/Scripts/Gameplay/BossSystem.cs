using UnityEngine;

namespace Perigee
{
    // Босс эры: Комета, Близнецы (две кометы), Колосс или Пульсар. Приходит на секунде bossTime,
    // победа над ним (над всеми его телами) заканчивает забег.
    public class BossSystem : MonoBehaviour
    {
        // у Близнецов два тела, у остальных боссов одно
        const int MaxParts = 2;
        // второе тело Близнецов: средняя аномалия сдвинута на пол-оборота, эллипс развёрнут на 180°
        const float HalfTurn = 180f;
        const float FullTurn = 2f * Mathf.PI;

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
        // линии орбит комет: вторая для второго из Близнецов
        [SerializeField] BossOrbitLine[] orbitLines;
        [SerializeField] Fx fx;
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
        bool spawned;
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

                part.visual = Instantiate(art.enemies[(int)def.id], root);
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

            spawned = false;
            Alive = false;
        }

        public void Tick(float dt)
        {
            if (!spawned && loop.Run.time >= config.bossTime) Spawn();
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

                // поджог и ГОРИЗОНТ СОБЫТИЙ: босс теряет HP по времени врагов, без числа и вспышки
                float seared = 0f;
                if (part.burn > 0f)
                {
                    float burning = Mathf.Min(part.burn, enemyDt);
                    part.burn -= burning;
                    seared += config.burnDps * burning;
                }
                if (horizon && part.position.sqrMagnitude <= config.horizonRadius * config.horizonRadius) seared += config.horizonDps * enemyDt;
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
                    default: Fly(part, dt, enemyDt); break;
                }
                Place(part);
                Gauge(part, dt);
            }
        }

        public void Collide()
        {
            // пока босс проявляется, урон по нему не проходит
            if (!Alive || age < config.bossFadeIn) return;

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
                    bool crit = loop.Run.simRng.Range(0f, 1f) < stats.critChance;
                    if (crit) damage *= config.critMul;
                    // броня (Колосс): удар слабее брони урона не наносит; БРОНЕБОЙ её срезает
                    if (damage < Mathf.Max(armor - stats.armorPierce, 0f))
                    {
                        fx.Sparks(hitter, config.clinkSparks);
                        fx.Number(Above(part), 0f, NumberKind.Blocked);
                        fx.HitStop(config.clinkHitStop);
                        sfx.Play(SfxId.Clink);
                        continue;
                    }

                    if (Hurt(part, damage, hitter, crit ? NumberKind.Crit : NumberKind.Normal)) break;
                    // РАСКАЛЁННЫЙ: горячий спутник поджигает, новый поджог начинает отсчёт заново
                    if (stats.Burn && satellites.HitterHeat(k) >= config.burnHeatMin) part.burn = config.burnTime;
                    fx.Shards(hitter, config.hitShards);
                    fx.HitStop(config.hitStopTime);
                    sfx.Play(SfxId.Hit);
                }

                // НИТЬ-ПИЛА режет и босса, броня не спасает
                Vector2 point;
                if (!part.alive || !stats.SawThread || part.sawCooldown > 0f || !satellites.Sawn(part.position, def.radius, out point)) continue;

                part.sawCooldown = config.sawInterval;
                if (Hurt(part, config.sawDamage, point, NumberKind.Normal)) continue;
                fx.Shards(point, config.hitShards);
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
            if (!Alive || age < config.bossFadeIn) return;

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

        void Spawn()
        {
            spawned = true;
            Alive = true;
            aliveParts = partCount;
            director.Stop();

            // эра боссов не усиливает
            maxHp = def.hp;
            armor = def.armor;
            age = 0f;
            phase = 0;
            arrived = false;
            waving = false;
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
                // Колосс и Пульсар приходят с края поля
                else part.position = Polar(angle, config.spawnRadius);

                Place(part);
                Gauge(part, 0f);
                part.visual.gameObject.SetActive(true);
            }

            banner.Show(Strings.BossTitle(def.id), null, Color.white, config.bannerBossHold);
            sfx.Play(SfxId.BossIn);
        }

        void Die(Part part)
        {
            part.alive = false;
            part.visual.gameObject.SetActive(false);
            part.line.Hide();
            crystals.Spawn(part.position, def.crystals);
            aliveParts--;
            if (aliveParts > 0)
            {
                // бой идёт дальше (оставшийся из Близнецов летит быстрее): гибель видна и слышна, как большое убийство
                fx.Shards(part.position, config.killShards * config.bigKillShardMul);
                fx.Shake(config.bigKillShake, config.bigKillShakeTime);
                sfx.Play(SfxId.Kill);
                return;
            }

            Alive = false;
            if (def.behavior == EnemyBehavior.Pulsar) wave.gameObject.SetActive(false);
            loop.Win();
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
                star.Damage(def.starDamage);
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
                if (def.id != EnemyType.Twin) return config.bossPeriod;
                return aliveParts == partCount ? config.twinPeriod : config.twinEnragedPeriod;
            }
        }

        float SemiMajor => (config.bossPeriapsis + config.bossApoapsis) * 0.5f;

        float Eccentricity => (config.bossApoapsis - config.bossPeriapsis) / (config.bossApoapsis + config.bossPeriapsis);

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
            if (r <= config.starRadius + def.radius)
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
