using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Perigee
{
    [Serializable]
    public class EraConfig
    {
        public float kExp;
        public float spinMul;
        public float hpMul;
        public float countMul;
    }

    public enum EnemyType { Swarmer, Arrow, Bolt, Bastion, Parasite, Shooter, Hive, Boss }

    public enum EnemyBehavior { Straight, Attach, Stopper, Burster, Ellipse }

    public enum AngleMode { Random, Opposite, Spread90, Even, OppositePair }

    [Serializable]
    public class EnemyDef
    {
        public EnemyType id;
        public float radius;
        public float hp;
        public float speed;
        public float armor;
        public float starDamage;
        public int crystals;
        public EnemyBehavior behavior;
        // корень картинки поворачивается осью +Y к звезде
        public bool faceStar;
        // сколько объектов создаётся заранее в начале забега
        public int prewarm;
    }

    [Serializable]
    public class SpawnGroup
    {
        public EnemyType type;
        public int count = 1;
        public AngleMode angleMode;
        public float spreadDeg = 30f;
        public float speedMul = 1f;
        public float radius;
    }

    [Serializable]
    public class SpawnEvent
    {
        public float time;
        public SpawnGroup[] groups;
    }

    public enum CardId { Heavy, Swift, AddSat, Magnet, Coolant, StarPlating, Resonator, Apogee, BigSat, Beacon, AbilityHaste, AbilityLong }

    // Параметры на Mul перемножаются, остальные складываются.
    public enum StatId
    {
        PowerMul, RadialSpeedMul, HeatMul, SpinMul, ResonanceAngleMul, SatRadiusMul, CrystalLifeMul, ArrowSpeedMul,
        AbilityCooldownMul, AbilityDurationMul, MagnetRadius, PowerFarBonus, StarMaxHp, Satellites, Beacon
    }

    [Serializable]
    public struct StatMod
    {
        public StatId stat;
        public float value;

        public StatMod(StatId stat, float value)
        {
            this.stat = stat;
            this.value = value;
        }
    }

    [Serializable]
    public class CardDef
    {
        public CardId id;
        public int maxStacks;
        public StatMod plus;
        public StatMod minus;
    }

    public enum AbilityId { Anchor, Grow, Pulse, Chill, Surge }

    [Serializable]
    public class AbilityDef
    {
        public AbilityId id;
        public float duration;
        public float cooldown;
    }

    public enum BlueprintId { Hull, Capacitor, CoolantBp, Magnetism }

    [Serializable]
    public class BlueprintDef
    {
        public BlueprintId id;
        // эффект одного уровня
        public StatMod perLevel;
        // цена каждого уровня: уровней столько, сколько цен
        public int[] costs;
    }

    [CreateAssetMenu(fileName = "GameConfig", menuName = "Perigee/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Title("Мир")]
        public float starRadius = 1f;
        public float starMaxHp = 120f;
        public float starLowHpFraction = 0.3f;
        public Color starHitFlashColor = new Color32(0xD6, 0x28, 0x1F, 0xFF);
        public float H = 6.2f;
        public float spawnRadius = 7f;
        public float arrowSpawnRadius = 5.3f;
        public float spawnFadeIn = 0.25f;
        public float telegraphRadius = 6.6f;

        [Title("Цикл симуляции")]
        public float simHz = 60f;
        public float maxFrameDelta = 0.1f;
        public int maxStepsPerFrame = 4;

        [Title("Спутники")]
        public float rMin = 1.35f;
        public float ringMax = 6.2f;
        public float satMaxRadius = 6.2f;
        public float ringGap = 0.8f;
        public float satRadius = 0.2f;
        public int maxSatellites = 3;
        public float radialSpeed = 8f;
        public float tMin = 1f;
        public float satStartAngle = 90f;
        public float satFadeIn = 0.3f;

        [Title("Сила удара")]
        public float powerNear = 3f;
        public float powerFar = 1f;
        public float powerFarRadius = 4.6f;
        public float powerFloor = 1f;
        public float armorBase = 2f;
        public float hitCooldown = 0.2f;

        [Title("Нагрев")]
        public float heatZoneMax = 2f;
        public float coolZoneMin = 2.6f;
        public float heatRate = 0.35f;
        public float coolRate = 0.25f;
        public float stunTime = 1f;
        public float heatAfterStun = 0.4f;
        public Color stunTint = new Color32(0xB9, 0xBD, 0xC2, 0x99);
        public float heatArcRadius = 0.32f;
        public int heatArcPoints = 48;
        public float heatFlickerFrom = 0.8f;
        public float heatFlickerHz = 12f;

        [Title("Резонанс")]
        public float resonanceHalfAngle = 35f;
        public float resonanceMul = 2f;
        public int threadSegments = 6;
        public float threadInterval = 0.05f;
        public float threadJitter = 0.15f;
        public float threadLabelOffset = 0.3f;
        public int threadPrewarm = 3;

        [Title("Кристаллы")]
        public float crystalLife = 10f;
        public float crystalRadius = 0.14f;
        public float magnetSpeed = 7f;
        public float crystalSpread = 0.3f;
        public float crystalBlinkTime = 2f;
        public float crystalBlinkHz = 6f;
        public int maxCrystals = 256;
        public int crystalPrewarm = 60;

        [Title("Опыт и карточки")]
        public int xpBase = 8;
        public int xpPerLevel = 3;
        public int cardChoices = 3;
        public float pickInputDelay = 0.4f;
        public float pickFadeOut = 0.4f;
        [TableList]
        public CardDef[] cards =
        {
            new CardDef { id = CardId.Heavy, maxStacks = 2, plus = new StatMod(StatId.PowerMul, 1.4f), minus = new StatMod(StatId.RadialSpeedMul, 0.7f) },
            new CardDef { id = CardId.Swift, maxStacks = 2, plus = new StatMod(StatId.RadialSpeedMul, 1.5f), minus = new StatMod(StatId.HeatMul, 1.2f) },
            new CardDef { id = CardId.AddSat, maxStacks = 2, plus = new StatMod(StatId.Satellites, 1f), minus = new StatMod(StatId.HeatMul, 1.25f) },
            new CardDef { id = CardId.Magnet, maxStacks = 1, plus = new StatMod(StatId.MagnetRadius, 1.3f), minus = new StatMod(StatId.ArrowSpeedMul, 1.15f) },
            new CardDef { id = CardId.Coolant, maxStacks = 2, plus = new StatMod(StatId.HeatMul, 0.6f), minus = new StatMod(StatId.PowerMul, 0.9f) },
            new CardDef { id = CardId.StarPlating, maxStacks = 2, plus = new StatMod(StatId.StarMaxHp, 25f), minus = new StatMod(StatId.SpinMul, 0.9f) },
            new CardDef { id = CardId.Resonator, maxStacks = 1, plus = new StatMod(StatId.ResonanceAngleMul, 1.5f), minus = new StatMod(StatId.PowerMul, 0.9f) },
            new CardDef { id = CardId.Apogee, maxStacks = 2, plus = new StatMod(StatId.PowerFarBonus, 0.5f), minus = new StatMod(StatId.HeatMul, 1.15f) },
            new CardDef { id = CardId.BigSat, maxStacks = 1, plus = new StatMod(StatId.SatRadiusMul, 1.5f), minus = new StatMod(StatId.HeatMul, 1.2f) },
            new CardDef { id = CardId.Beacon, maxStacks = 1, plus = new StatMod(StatId.Beacon, 1f), minus = new StatMod(StatId.CrystalLifeMul, 0.6f) },
            new CardDef { id = CardId.AbilityHaste, maxStacks = 2, plus = new StatMod(StatId.AbilityCooldownMul, 0.75f), minus = new StatMod(StatId.PowerMul, 0.9f) },
            new CardDef { id = CardId.AbilityLong, maxStacks = 1, plus = new StatMod(StatId.AbilityDurationMul, 1.4f), minus = new StatMod(StatId.AbilityCooldownMul, 1.15f) },
        };

        [Title("Маяк")]
        public float beaconInterval = 8f;
        public float beaconRadius = 2.5f;
        public float beaconDamage = 1f;
        public float beaconFxTime = 0.35f;

        [Title("Способности")]
        public int abilityPickLevel = 3;
        public int abilityChoices = 3;
        [TableList]
        public AbilityDef[] abilities =
        {
            new AbilityDef { id = AbilityId.Anchor, duration = 4f, cooldown = 30f },
            new AbilityDef { id = AbilityId.Grow, duration = 6f, cooldown = 35f },
            new AbilityDef { id = AbilityId.Pulse, duration = 0f, cooldown = 45f },
            new AbilityDef { id = AbilityId.Chill, duration = 5f, cooldown = 30f },
            new AbilityDef { id = AbilityId.Surge, duration = 4f, cooldown = 40f },
        };
        public float growRadiusMul = 2f;
        public float growResonanceMul = 1.3f;
        public float growScaleTime = 0.2f;
        public float pulseRadius = 3f;
        public float pulseTime = 0.35f;
        public float pulseDamage = 2f;
        public float pulsePush = 1f;
        public float chillFlashTime = 0.2f;
        public float surgePowerMul = 2f;
        public float abilityReadyPulse = 1.08f;
        public float abilityReadyPulseTime = 0.6f;

        [Title("Враги")]
        public int maxEnemies = 200;
        public float eraHpMinBaseHp = 2f;
        [TableList]
        public EnemyDef[] enemies =
        {
            new EnemyDef { id = EnemyType.Swarmer, radius = 0.2f, hp = 1f, speed = 1.1f, armor = 0f, starDamage = 3f, crystals = 1, behavior = EnemyBehavior.Straight, faceStar = true, prewarm = 60 },
            new EnemyDef { id = EnemyType.Arrow, radius = 0.15f, hp = 1f, speed = 2.6f, armor = 0f, starDamage = 9f, crystals = 2, behavior = EnemyBehavior.Straight, faceStar = true, prewarm = 8 },
            new EnemyDef { id = EnemyType.Bolt, radius = 0.15f, hp = 1f, speed = 3.5f, armor = 0f, starDamage = 4f, crystals = 0, behavior = EnemyBehavior.Straight, faceStar = true, prewarm = 8 },
            new EnemyDef { id = EnemyType.Bastion, radius = 0.55f, hp = 5f, speed = 0.35f, armor = 2f, starDamage = 20f, crystals = 6, behavior = EnemyBehavior.Straight, faceStar = true, prewarm = 8 },
            new EnemyDef { id = EnemyType.Parasite, radius = 0.25f, hp = 5f, speed = 1.6f, armor = 0f, starDamage = 0f, crystals = 3, behavior = EnemyBehavior.Attach, faceStar = false, prewarm = 8 },
            new EnemyDef { id = EnemyType.Shooter, radius = 0.3f, hp = 2f, speed = 1f, armor = 0f, starDamage = 0f, crystals = 4, behavior = EnemyBehavior.Stopper, faceStar = true, prewarm = 8 },
            new EnemyDef { id = EnemyType.Hive, radius = 0.6f, hp = 4f, speed = 0.7f, armor = 0f, starDamage = 0f, crystals = 8, behavior = EnemyBehavior.Burster, faceStar = false, prewarm = 8 },
            new EnemyDef { id = EnemyType.Boss, radius = 0.9f, hp = 80f, speed = 0f, armor = 0f, starDamage = 8f, crystals = 30, behavior = EnemyBehavior.Ellipse, faceStar = false, prewarm = 0 },
        };

        [Title("Поведение врагов")]
        public float parasiteAttachOffset = 0.15f;
        public float parasiteDrainBase = 1.5f;
        public float parasiteDrainGrowth = 0.4f;
        public float parasiteDrainMax = 5f;
        public float shooterStopRadius = 5.8f;
        public float shooterFirstDelay = 2f;
        public float shooterFireInterval = 2.5f;
        public float hiveBurstRadius = 3f;
        public int hiveSwarmers = 8;
        public float hiveBurstSpread = 0.5f;

        [Title("Комета")]
        public float bossTime = 180f;
        public float bossPeriapsis = 1.8f;
        public float bossApoapsis = 5.5f;
        public float bossPeriod = 6f;
        public float bossFadeIn = 0.5f;
        public float bossPhiStep = 40f;
        public int bossNewtonSteps = 4;
        public float bossOrbitLineTime = 1.5f;
        public int bossOrbitLinePoints = 64;

        [Title("Расписание")]
        public SpawnEvent[] opening =
        {
            At(2f, Of(EnemyType.Swarmer, 4)),
            At(8f, Of(EnemyType.Swarmer, 6)),
            At(14f, Of(EnemyType.Bastion, 1)),
        };
        public SpawnEvent[] timeline =
        {
            At(20f, Of(EnemyType.Swarmer, 8)),
            At(30f, Of(EnemyType.Swarmer, 8)),
            At(36f, Of(EnemyType.Bastion, 1)),
            At(46f, Of(EnemyType.Arrow, 2, AngleMode.Spread90)),
            At(56f, Of(EnemyType.Swarmer, 10)),
            At(62f, Of(EnemyType.Arrow, 2, AngleMode.Spread90)),
            At(70f, Of(EnemyType.Parasite, 1)),
            At(80f, Of(EnemyType.Bastion, 1), Of(EnemyType.Swarmer, 8, AngleMode.Opposite)),
            At(88f, Of(EnemyType.Parasite, 2, AngleMode.Spread90)),
            At(98f, Of(EnemyType.Shooter, 1)),
            At(108f, Of(EnemyType.Arrow, 3, AngleMode.Even)),
            At(116f, Of(EnemyType.Hive, 1)),
            At(126f, Of(EnemyType.Swarmer, 11), Of(EnemyType.Shooter, 1, AngleMode.Opposite)),
            At(136f, Of(EnemyType.Bastion, 2, AngleMode.OppositePair)),
            At(146f, Of(EnemyType.Hive, 1), Of(EnemyType.Arrow, 2, AngleMode.Spread90)),
            At(156f, Of(EnemyType.Parasite, 2, AngleMode.Spread90), Of(EnemyType.Swarmer, 10)),
            At(164f, Of(EnemyType.Shooter, 2, AngleMode.OppositePair), Of(EnemyType.Bastion, 1)),
            At(172f, Of(EnemyType.Arrow, 4, AngleMode.Even), Of(EnemyType.Swarmer, 11)),
        };
        public float ambientStart = 6f;
        public float ambientEnd = 175f;
        public float ambientInterval = 3.5f;
        public float swarmRadiusJitter = 1f;

        [Title("Телеграф")]
        public float telegraphLeadSwarm = 1f;
        public float telegraphLeadArrow = 1.5f;
        public float telegraphLeadOther = 0.8f;
        public float telegraphInset = 0.5f;
        public int markerPrewarm = 8;

        [Title("Эры")]
        [TableList]
        public EraConfig[] eras =
        {
            new EraConfig { kExp = 0.9f, spinMul = 1f, hpMul = 1f, countMul = 1f },
            new EraConfig { kExp = 0.9f, spinMul = 1.3f, hpMul = 1.3f, countMul = 1.2f },
            new EraConfig { kExp = 1.2f, spinMul = 1f, hpMul = 1.6f, countMul = 1.4f },
        };

        [Title("Итоги")]
        public float victoryTime = 1.5f;
        public int victoryDust = 50;
        public int dailyDustMul = 2;
        public float scorePerSecond = 2f;
        public int scoreVictory = 500;

        [Title("Чертежи")]
        [TableList]
        public BlueprintDef[] blueprints =
        {
            new BlueprintDef { id = BlueprintId.Hull, perLevel = new StatMod(StatId.StarMaxHp, 10f), costs = new[] { 50, 100, 200, 400, 800 } },
            new BlueprintDef { id = BlueprintId.Capacitor, perLevel = new StatMod(StatId.AbilityCooldownMul, 0.95f), costs = new[] { 50, 100, 200, 400, 800 } },
            new BlueprintDef { id = BlueprintId.CoolantBp, perLevel = new StatMod(StatId.HeatMul, 0.95f), costs = new[] { 50, 100, 200, 400, 800 } },
            new BlueprintDef { id = BlueprintId.Magnetism, perLevel = new StatMod(StatId.MagnetRadius, 0.35f), costs = new[] { 40, 80, 160 } },
        };

        static SpawnEvent At(float time, params SpawnGroup[] groups)
        {
            return new SpawnEvent { time = time, groups = groups };
        }

        static SpawnGroup Of(EnemyType type, int count, AngleMode angleMode = AngleMode.Random)
        {
            return new SpawnGroup { type = type, count = count, angleMode = angleMode };
        }
    }
}
