using UnityEngine;

namespace Perigee
{
    // Комета: летит по эллипсу, в фокусе которого звезда, и в перигее задевает её.
    public class BossComet : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] EnemySystem enemies;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] StarController star;
        [SerializeField] CrystalSystem crystals;
        [SerializeField] Director director;
        [SerializeField] BossOrbitLine orbitLine;
        [SerializeField] Fx fx;
        [SerializeField] AudioManager sfx;
        [SerializeField] Transform root;

        Visual visual;
        Transform self;
        Transform body;
        SpriteRenderer flash;
        HpRing hpRing;
        DamageStages cracks;
        EnemyDef def;
        float[] hitCooldown;
        Vector2 position;
        float maxHp;
        float hp;
        // секунды с появления
        float age;
        // поворот эллипса, градусы
        float phi;
        int passes;
        float lineTimer;
        bool spawned;
        // урон уже был: кольцо HP в режиме «после удара» остаётся на виду
        bool hurt;
        // секунды до следующего пореза НИТЬЮ-ПИЛОЙ
        float sawCooldown;
        // сколько секунд ещё горит (РАСКАЛЁННЫЙ)
        float burn;

        public bool Alive { get; private set; }

        public float Hp => hp;

        public float MaxHp => maxHp;

        void Awake()
        {
            hitCooldown = new float[satellites.MaxHitters];
        }

        public void ResetRun()
        {
            // Комета создаётся заново из префаба: правка префаба видна со следующего забега
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            visual = Instantiate(art.enemies[(int)EnemyType.Boss], root);
            visual.gameObject.SetActive(false);
            self = visual.transform;
            body = visual.Body.transform;
            flash = fx.Overlay(visual);
            hpRing = visual.GetComponentInChildren<HpRing>(true);
            cracks = visual.GetComponentInChildren<DamageStages>(true);

            orbitLine.Hide();
            spawned = false;
            Alive = false;
        }

        public void Tick(float dt)
        {
            if (!spawned && loop.Run.time >= config.bossTime) Spawn();
            if (!Alive) return;

            // СТАЗИС замедляет полёт; перезарядка удара, линия орбиты и кольцо HP идут по обычному времени
            float enemyDt = dt * loop.EnemyTimeScale;
            age += enemyDt;
            for (int k = 0; k < hitCooldown.Length; k++) hitCooldown[k] -= dt;
            sawCooldown -= dt;

            // горящая Комета теряет HP по времени врагов, без числа и вспышки
            if (burn > 0f)
            {
                float burning = Mathf.Min(burn, enemyDt);
                burn -= burning;
                hp -= config.burnDps * burning;
                hurt = true;
                if (hp <= 0f)
                {
                    Die();
                    return;
                }
            }

            lineTimer -= dt;
            if (lineTimer <= 0f) orbitLine.Hide();

            // каждый проход перигея: удар по звезде и поворот эллипса
            int passed = Mathf.FloorToInt(MeanAnomaly() / (2f * Mathf.PI));
            if (passed > passes)
            {
                passes = passed;
                star.Damage(def.starDamage);
                phi += config.bossPhiStep;
                ShowOrbit();
            }
            Place();
            Gauge(dt);
        }

        public void Collide()
        {
            // пока Комета проявляется, урон по ней не проходит
            if (!Alive || age < config.bossFadeIn) return;

            Stats stats = loop.Stats;
            float reach = satellites.HitterRadius + def.radius;
            int hitters = satellites.HitterCount;
            for (int k = 0; k < hitters; k++)
            {
                if (!satellites.HitterActive(k)) continue;
                if (hitCooldown[k] > 0f) continue;
                Vector2 hitter = satellites.HitterPosition(k);
                if ((hitter - position).sqrMagnitude > reach * reach) continue;

                hitCooldown[k] = config.hitCooldown;
                float damage = satellites.HitterDamage(k);
                bool crit = loop.Run.simRng.Range(0f, 1f) < stats.critChance;
                if (crit) damage *= config.critMul;
                if (Hurt(damage, hitter, crit ? NumberKind.Crit : NumberKind.Normal)) return;
                // РАСКАЛЁННЫЙ: горячий спутник поджигает, новый поджог начинает отсчёт заново
                if (stats.Burn && satellites.HitterHeat(k) >= config.burnHeatMin) burn = config.burnTime;
                fx.Shards(hitter, config.hitShards);
                fx.HitStop(config.hitStopTime);
                sfx.Play(SfxId.Hit);
            }

            // НИТЬ-ПИЛА режет и Комету
            Vector2 point;
            if (!stats.SawThread || sawCooldown > 0f || !satellites.Sawn(position, def.radius, out point)) return;

            sawCooldown = config.sawInterval;
            if (Hurt(config.sawDamage, point, NumberKind.Normal)) return;
            fx.Shards(point, config.hitShards);
            sfx.Play(SfxId.Hit);
        }

        // РЕВЕРС: каждый хиттер может ударить снова, не дожидаясь перезарядки
        public void ClearHitCooldowns()
        {
            System.Array.Clear(hitCooldown, 0, hitCooldown.Length);
        }

        // волна из точки centre (ИМПУЛЬС, МАЯК, ЦЕПНАЯ РЕАКЦИЯ): урон без толчка
        public void Blast(Vector2 centre, float radius, float damage)
        {
            if (!Alive || age < config.bossFadeIn) return;
            if ((position - centre).sqrMagnitude > radius * radius) return;

            Hurt(damage, centre, NumberKind.Normal);
        }

        // урон прошёл: над Кометой число; пока жива, она белеет и вздрагивает прочь от точки from. true = Комета убита
        bool Hurt(float damage, Vector2 from, NumberKind kind)
        {
            hp -= damage;
            hurt = true;
            fx.Number(position + Vector2.up * def.radius, damage, kind);
            if (hp <= 0f)
            {
                Die();
                return true;
            }

            fx.Flash(flash);
            fx.Recoil(body, (position - from).normalized, config.bossRecoilDistance);
            return false;
        }

        void Spawn()
        {
            spawned = true;
            Alive = true;
            director.Stop();

            def = enemies.Def(EnemyType.Boss);
            maxHp = def.hp * loop.Era.hpMul;
            hp = maxHp;
            hurt = false;
            hpRing.Fill();
            age = 0f;
            passes = 0;
            phi = loop.Run.simRng.Range(0f, 360f);
            sawCooldown = 0f;
            burn = 0f;
            System.Array.Clear(hitCooldown, 0, hitCooldown.Length);

            Place();
            Gauge(0f);
            visual.gameObject.SetActive(true);
            ShowOrbit();
            sfx.Play(SfxId.BossIn);
        }

        void Die()
        {
            Alive = false;
            visual.gameObject.SetActive(false);
            orbitLine.Hide();
            crystals.Spawn(position, def.crystals);
            loop.Win();
        }

        // M = π в апоцентре: Комета появляется в самой дальней точке
        float MeanAnomaly()
        {
            return Mathf.PI + 2f * Mathf.PI * age / config.bossPeriod;
        }

        float SemiMajor => (config.bossPeriapsis + config.bossApoapsis) * 0.5f;

        float Eccentricity => (config.bossApoapsis - config.bossPeriapsis) / (config.bossApoapsis + config.bossPeriapsis);

        void ShowOrbit()
        {
            float a = SemiMajor;
            float e = Eccentricity;
            orbitLine.Show(a, a * Mathf.Sqrt(1f - e * e), e, phi);
            lineTimer = config.bossOrbitLineTime;
        }

        void Place()
        {
            float a = SemiMajor;
            float e = Eccentricity;
            float b = a * Mathf.Sqrt(1f - e * e);

            // уравнение Кеплера E − e·sin E = M, метод Ньютона от E = M: у апоцентра медленно, в нырке быстро
            float m = MeanAnomaly();
            float anomaly = m;
            for (int i = 0; i < config.bossNewtonSteps; i++) anomaly -= (anomaly - e * Mathf.Sin(anomaly) - m) / (1f - e * Mathf.Cos(anomaly));

            // звезда стоит в фокусе, поэтому центр эллипса сдвинут на a·e
            float x = a * (Mathf.Cos(anomaly) - e);
            float y = b * Mathf.Sin(anomaly);
            float rad = phi * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            position = new Vector2(x * cos - y * sin, x * sin + y * cos);

            self.localPosition = position;
            // СТАЗИС: Комета наполовину серебряная
            visual.Tint(config.stasisTint, abilities.Active(AbilityId.Stasis) ? config.stasisTintAmount : 0f);
            visual.SetAlpha(Mathf.Clamp01(age / config.bossFadeIn));
        }

        // кольцо HP и трещины; кольцо зависит от настройки
        void Gauge(float dt)
        {
            float fraction = hp / maxHp;
            HpRingMode mode = (HpRingMode)loop.Save.Data.settings.hpRings;
            bool ring = maxHp >= config.hpRingMinHp && (mode == HpRingMode.Always || mode == HpRingMode.AfterHit && hurt);
            if (hpRing.gameObject.activeSelf != ring) hpRing.gameObject.SetActive(ring);
            if (ring) hpRing.Draw(def.radius + config.hpRingOffset, fraction, dt);
            cracks.Show(fraction);
        }

        // круг попаданий в Scene view
        void OnDrawGizmos()
        {
            if (Alive) Gizmos.DrawWireSphere(position, def.radius);
        }
    }
}
