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

    [Serializable]
    public class RingDef
    {
        public float radius;
        public int tickCount;
        public float spinSpeed;
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

    [CreateAssetMenu(fileName = "GameConfig", menuName = "Perigee/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Title("Мир")]
        public float starRadius = 1f;
        public float starMaxHp = 120f;
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
        public float satStunWall = 0.3f;

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
        public float heatArcRadius = 0.32f;
        public float heatArcWidth = 0.05f;
        public int heatArcSegments = 32;
        public float heatFlickerFrom = 0.8f;
        public float heatFlickerHz = 12f;

        [Title("Резонанс")]
        public float resonanceHalfAngle = 35f;
        public float resonanceMul = 2f;
        public int threadSegments = 6;
        public float threadInterval = 0.05f;
        public float threadWidth = 0.05f;
        public float threadJitter = 0.15f;
        public float threadLabelSize = 3f;
        public float threadLabelOffset = 0.3f;

        [Title("Кольцо брони")]
        public float armorRingSpin = 10f;
        public int armorRingTicks = 36;
        [InfoBox("armorRingWidth в долях радиуса кольца")]
        public float armorRingWidth = 0.02f;
        public float armorRingAlpha = 0.7f;
        public float armorShieldSize = 0.2f;

        [Title("Кристаллы")]
        public float crystalLife = 10f;
        public float crystalRadius = 0.14f;
        public float magnetSpeed = 7f;
        public float crystalSpread = 0.3f;
        public float crystalBlinkTime = 2f;
        public float crystalBlinkHz = 6f;
        public float crystalSpin = 180f;
        public int maxCrystals = 256;

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
        public float beaconFxWidth = 0.08f;

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
        public float pulseFxWidth = 0.1f;
        public float chillFlashTime = 0.2f;
        public float surgePowerMul = 2f;
        public int anchorRingTicks = 48;
        [InfoBox("anchorRingWidth и abilityRingWall в долях радиуса")]
        public float anchorRingWidth = 0.02f;
        public float abilityRingWall = 0.12f;
        public float abilityReadyPulse = 1.08f;
        public float abilityReadyPulseTime = 0.6f;

        [Title("Враги")]
        public int maxEnemies = 200;
        public float eraHpMinBaseHp = 2f;
        [TableList]
        public EnemyDef[] enemies =
        {
            new EnemyDef { id = EnemyType.Swarmer, radius = 0.2f, hp = 1f, speed = 1.1f, armor = 0f, starDamage = 3f, crystals = 1, behavior = EnemyBehavior.Straight },
            new EnemyDef { id = EnemyType.Arrow, radius = 0.15f, hp = 1f, speed = 2.6f, armor = 0f, starDamage = 9f, crystals = 2, behavior = EnemyBehavior.Straight },
            new EnemyDef { id = EnemyType.Bolt, radius = 0.15f, hp = 1f, speed = 3.5f, armor = 0f, starDamage = 4f, crystals = 0, behavior = EnemyBehavior.Straight },
            new EnemyDef { id = EnemyType.Bastion, radius = 0.55f, hp = 5f, speed = 0.35f, armor = 2f, starDamage = 20f, crystals = 6, behavior = EnemyBehavior.Straight },
            new EnemyDef { id = EnemyType.Parasite, radius = 0.25f, hp = 5f, speed = 1.6f, armor = 0f, starDamage = 0f, crystals = 3, behavior = EnemyBehavior.Attach },
            new EnemyDef { id = EnemyType.Shooter, radius = 0.3f, hp = 2f, speed = 1f, armor = 0f, starDamage = 0f, crystals = 4, behavior = EnemyBehavior.Stopper },
            new EnemyDef { id = EnemyType.Hive, radius = 0.6f, hp = 4f, speed = 0.7f, armor = 0f, starDamage = 0f, crystals = 8, behavior = EnemyBehavior.Burster },
            new EnemyDef { id = EnemyType.Boss, radius = 0.9f, hp = 80f, speed = 0f, armor = 0f, starDamage = 8f, crystals = 30, behavior = EnemyBehavior.Ellipse },
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

        [Title("Вид врагов")]
        [InfoBox("Trim, Rivet, Body, Wall, Hole, Fill и Pulse заданы в долях радиуса формы; Length, Width и MarkerRadius в единицах мира")]
        public float arrowLength = 0.6f;
        public float arrowWidth = 0.2f;
        public float arrowTailLength = 0.3f;
        public float arrowTailFill = 0.3f;
        public float bastionTrim = 0.1f;
        public float bastionRivetLength = 0.25f;
        public float bastionRivetWidth = 0.04f;
        public float bastionRivetStep = 0.3f;
        public float parasiteBody = 0.6f;
        public float parasiteLegFill = 0.3f;
        public float parasiteMarkerRadius = 0.45f;
        public float parasiteMarkerWall = 0.15f;
        public float parasiteMarkerPulse = 0.2f;
        public float parasiteMarkerPulseHz = 2f;
        public float shooterWall = 0.3f;
        public float shooterBarrelLength = 0.35f;
        public float shooterBarrelWidth = 0.08f;
        public float hiveHoleRadius = 0.25f;
        public float hiveHoleOffset = 0.45f;

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
        public float arrowWedgeSize = 0.35f;
        public float arrowWedgePulse = 0.2f;
        public float arrowWedgePulseHz = 3f;
        public float arrowLineWidth = 0.04f;
        public float arrowLineAlpha = 0.3f;
        public float telegraphInset = 0.5f;
        public float telegraphSize = 0.3f;
        public float telegraphBlinkHz = 4f;
        public int maxMarkers = 8;

        [Title("Эры")]
        [TableList]
        public EraConfig[] eras =
        {
            new EraConfig { kExp = 0.9f, spinMul = 1f, hpMul = 1f, countMul = 1f },
            new EraConfig { kExp = 0.9f, spinMul = 1.3f, hpMul = 1.3f, countMul = 1.2f },
            new EraConfig { kExp = 1.2f, spinMul = 1f, hpMul = 1.6f, countMul = 1.4f },
        };

        [Title("Палитра")]
        public Color background = new Color32(0x0E, 0x0E, 0x0E, 0xFF);
        public Color cream = new Color32(0xEF, 0xE4, 0xC8, 0xFF);
        public Color red = new Color32(0xD6, 0x28, 0x1F, 0xFF);
        public Color gold = new Color32(0xE3, 0xB2, 0x3C, 0xFF);
        public Color silver = new Color32(0xB9, 0xBD, 0xC2, 0xFF);
        public Material spriteMaterial;

        [Title("Вид звезды и спутника")]
        public float outlineWidth = 0.02f;
        public int starRayCount = 36;
        public float starRaysInner = 1.05f;
        public float starRaysOuter = 1.35f;
        public float starRayFill = 0.5f;
        public float starRaysSpin = 6f;
        public float starPulseScale = 1.03f;
        public float starPulseHz = 1.5f;
        public float starLowHpFraction = 0.3f;
        public float starLowHpPulseHz = 3f;
        public float satTrailTime = 0.35f;
        public float satTrailWidth = 0.12f;

        [Title("Фоновые кольца")]
        [TableList]
        public RingDef[] rings =
        {
            new RingDef { radius = 1.7f, tickCount = 60, spinSpeed = 24f },
            new RingDef { radius = 2.6f, tickCount = 48, spinSpeed = -18f },
            new RingDef { radius = 3.5f, tickCount = 40, spinSpeed = 13f },
            new RingDef { radius = 4.4f, tickCount = 32, spinSpeed = -10f },
            new RingDef { radius = 5.3f, tickCount = 28, spinSpeed = 7f },
            new RingDef { radius = 6.2f, tickCount = 24, spinSpeed = -5f },
        };
        public float ringAlpha = 0.35f;
        public float ringWidth = 0.06f;
        public float ringDashFill = 0.5f;

        [Title("Порядок отрисовки")]
        public int sortBars = 0;
        public int sortRings = 10;
        public int sortCrystals = 20;
        public int sortEnemies = 30;
        public int sortStar = 40;
        public int sortSatellites = 50;
        public int sortFx = 60;

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
