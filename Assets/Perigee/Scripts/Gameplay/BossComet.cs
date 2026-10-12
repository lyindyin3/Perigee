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

        public bool Alive { get; private set; }

        public float Hp => hp;

        public float MaxHp => maxHp;

        void Awake()
        {
            hitCooldown = new float[config.maxSatellites];
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

            age += dt;
            for (int k = 0; k < hitCooldown.Length; k++) hitCooldown[k] -= dt;

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

            float reach = satellites.Radius + def.radius;
            for (int k = 0; k < satellites.Count; k++)
            {
                if (!satellites.Active(k)) continue;
                if (hitCooldown[k] > 0f) continue;
                if ((satellites.Position(k) - position).sqrMagnitude > reach * reach) continue;

                hitCooldown[k] = config.hitCooldown;
                if (Hurt(satellites.Damage(k), satellites.Position(k))) return;
                fx.Shards(satellites.Position(k), config.hitShards);
                fx.HitStop(config.hitStopTime);
                sfx.Play(SfxId.Hit);
            }
        }

        // волна от звезды (ИМПУЛЬС, МАЯК): урон без толчка
        public void Blast(float radius, float damage)
        {
            if (!Alive || age < config.bossFadeIn) return;
            if (position.sqrMagnitude > radius * radius) return;

            Hurt(damage, Vector2.zero);
        }

        // урон прошёл: над Кометой число; пока жива, она белеет и вздрагивает прочь от точки from. true = Комета убита
        bool Hurt(float damage, Vector2 from)
        {
            hp -= damage;
            hurt = true;
            fx.Number(position + Vector2.up * def.radius, damage, NumberKind.Normal);
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
