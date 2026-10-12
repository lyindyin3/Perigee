using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Perigee
{
    // новые типы дописываются в конец: по номеру идут ArtSet.enemies и бит в SaveData.seenEnemies
    public enum EnemyType
    {
        Swarmer, Arrow, Bolt, Bastion, Parasite, Shooter, Hive, Boss, Splitter, Torch, Orbiter, Shielder, Devourer, Phantom,
        Twin, Colossus, Pulsar
    }

    public enum EnemyBehavior { Straight, Attach, Stopper, Burster, Ellipse, Orbit, Seek }

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

        // Дальше особенности типа, 0 = нет.
        // Дробитель: сколько Роя выпускает при гибели
        public int splitCount;
        // Факел: нагрев спутника за каждый удар по нему
        public float heatOnHit;
        // Щитник: сколько ударов держит щит
        public int shieldHits;
        // Призрак: секунды плотным и секунды прозрачным
        public float phaseOn;
        public float phaseOff;
        // Пожиратель: как далеко чует кристаллы
        public float seekRadius;
        // Кружащий: радиус выхода на орбиту, градусы в секунду и снижение в секунду
        public float orbitRadius;
        public float orbitDeg;
        public float orbitInSpeed;
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

    public enum EraRule { None, Flares, Heavy, Vortex }

    [Serializable]
    public class EraDef
    {
        public float kExp;
        public float spinMul;
        public float hpMul;
        public float countMul;
        // секунды между врагами фонового потока
        public float ambientInterval;
        public float dustMul;
        public float riseMul;
        public float fallMul;
        public EraRule rule;
        public EnemyType boss;
        public SpawnEvent[] opening;
        public SpawnEvent[] timeline;
        public int victoryDust;
        public int firstWinDust;
        // цвет фона камеры
        public Color background;
        // множители цвета Body и Rays звезды
        public Color starTint;
        public Color raysTint;
    }

    // порядок таблицы GDD 6.2; по нему идут ArtSet.cardIcons и строки в Strings
    public enum CardId
    {
        Heavy, FarStrike, Sharp, Pierce, Execute, SawThread, Burn, Chain, AddSat, Swift, Spin, BigSat, Magnet, Greed, Coolant,
        StarPlating, Photosphere, Corona, Beacon, AbilityHaste, AbilityLong
    }

    // цвет рамки: кремовая, красная, чёрная
    public enum CardColor { Orbit, Strike, Star }

    public enum CardRarity { Common, Rare }

    // Параметры на Mul перемножаются, остальные складываются; флаг включён, когда набрано от 1.
    public enum StatId
    {
        PowerMul, RadialSpeedMul, RadialOutMul, HeatMul, SpinMul, ResonanceAngleMul, ResonanceBonus, SatRadiusMul, CrystalLifeMul,
        ArrowSpeedMul, AbilityCooldownMul, AbilityDurationMul, XpPerCrystalMul, XpNeedMul, StarDamageMul, DustMul, MagnetRadius,
        PowerFarBonus, StarMaxHp, Regen, Satellites, AddSatBonus, Beacon, CritChance, ArmorPierce, Execute, SawThread, Burn, Chain,
        Heatproof, SolarSail, Firebird, Slingshot, AnchorField, Crowbar, Horizon, DarkMatter, Accretion, SecondSun, FreeRerolls,
        ExtraChoices
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
        public CardColor color;
        public CardRarity rarity;
        // 0 = карточка любой эры, иначе номер эры с единицы
        public int era;
        // 0 = предела нет (ADD_SAT ограничен числом спутников)
        public int maxStacks;
        public StatMod plus;
        public StatMod minus;
    }

    public enum AbilityId { Reverse, Grow, Pulse, Stasis, Echo, Pull, Dome }

    [Serializable]
    public class AbilityDef
    {
        public AbilityId id;
        public float duration;
        public float cooldown;
        // 0 = есть с начала игры, иначе покупается в Мастерской
        public int price;
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
        [Title("Отладка")]
        // всё открыто без записи в сохранение: эры, режимы, способности, испытания, облик
        public bool debugUnlockAll;

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
        public int weightCommon = 3;
        public int weightRare = 1;
        public int weightEra = 4;
        [TableList]
        public CardDef[] cards =
        {
            Card(CardId.Heavy, CardColor.Strike, CardRarity.Common, 2, StatId.PowerMul, 1.35f, StatId.RadialSpeedMul, 0.75f),
            Card(CardId.FarStrike, CardColor.Strike, CardRarity.Common, 2, StatId.PowerFarBonus, 0.5f, StatId.HeatMul, 1.15f),
            Card(CardId.Sharp, CardColor.Strike, CardRarity.Common, 2, StatId.CritChance, 0.12f, StatId.PowerMul, 0.92f),
            Card(CardId.Pierce, CardColor.Strike, CardRarity.Common, 1, StatId.ArmorPierce, 1f, StatId.SpinMul, 0.9f),
            Card(CardId.Execute, CardColor.Strike, CardRarity.Rare, 1, StatId.Execute, 1f, StatId.CrystalLifeMul, 0.7f),
            Card(CardId.SawThread, CardColor.Strike, CardRarity.Rare, 1, StatId.SawThread, 1f, StatId.PowerMul, 0.9f),
            Card(CardId.Burn, CardColor.Strike, CardRarity.Common, 1, StatId.Burn, 1f, StatId.HeatMul, 1.15f),
            Card(CardId.Chain, CardColor.Strike, CardRarity.Rare, 1, StatId.Chain, 1f, StatId.XpPerCrystalMul, 0.9f),
            Card(CardId.AddSat, CardColor.Orbit, CardRarity.Common, 0, StatId.Satellites, 1f, StatId.HeatMul, 1.2f),
            Card(CardId.Swift, CardColor.Orbit, CardRarity.Common, 2, StatId.RadialSpeedMul, 1.5f, StatId.HeatMul, 1.2f),
            Card(CardId.Spin, CardColor.Orbit, CardRarity.Common, 2, StatId.SpinMul, 1.25f, StatId.HeatMul, 1.15f),
            Card(CardId.BigSat, CardColor.Orbit, CardRarity.Common, 2, StatId.SatRadiusMul, 1.35f, StatId.SpinMul, 0.9f),
            Card(CardId.Magnet, CardColor.Orbit, CardRarity.Common, 2, StatId.MagnetRadius, 1.3f, StatId.ArrowSpeedMul, 1.15f),
            Card(CardId.Greed, CardColor.Orbit, CardRarity.Common, 2, StatId.XpPerCrystalMul, 1.3f, StatId.StarMaxHp, -15f),
            Card(CardId.Coolant, CardColor.Orbit, CardRarity.Common, 2, StatId.HeatMul, 0.6f, StatId.PowerMul, 0.9f),
            Card(CardId.StarPlating, CardColor.Star, CardRarity.Common, 2, StatId.StarMaxHp, 25f, StatId.SpinMul, 0.9f),
            Card(CardId.Photosphere, CardColor.Star, CardRarity.Common, 2, StatId.Regen, 0.4f, StatId.XpPerCrystalMul, 0.85f),
            Card(CardId.Corona, CardColor.Star, CardRarity.Common, 1, StatId.StarDamageMul, 0.7f, StatId.CrystalLifeMul, 0.8f),
            Card(CardId.Beacon, CardColor.Star, CardRarity.Common, 2, StatId.Beacon, 1f, StatId.CrystalLifeMul, 0.6f),
            Card(CardId.AbilityHaste, CardColor.Star, CardRarity.Common, 2, StatId.AbilityCooldownMul, 0.75f, StatId.PowerMul, 0.9f),
            Card(CardId.AbilityLong, CardColor.Star, CardRarity.Common, 1, StatId.AbilityDurationMul, 1.4f, StatId.AbilityCooldownMul, 1.15f),
        };

        [Title("Эффекты карточек")]
        public float critMul = 2f;
        public float executeFraction = 0.25f;
        public float sawWidth = 0.08f;
        public float sawDamage = 1f;
        public float sawInterval = 0.25f;
        public float burnHeatMin = 0.5f;
        public float burnTime = 3f;
        public float burnDps = 1f;
        public float chainRadius = 1f;
        public float chainDamage = 1f;
        // ФОТОСФЕРА лечит порциями, чтобы на полоске был виден золотой «хвост»
        public float regenStep = 1f;

        [Title("Маяк")]
        public float beaconInterval = 8f;
        // каждый стак после первого умножает период на это число
        public float beaconStackMul = 0.7f;
        public float beaconRadius = 2.5f;
        public float beaconDamage = 1f;
        public float beaconFxTime = 0.35f;

        [Title("Способности")]
        public int abilityPickLevel = 3;
        public int abilityChoices = 3;
        [TableList]
        public AbilityDef[] abilities =
        {
            new AbilityDef { id = AbilityId.Reverse, duration = 2f, cooldown = 12f, price = 0 },
            new AbilityDef { id = AbilityId.Grow, duration = 6f, cooldown = 35f, price = 0 },
            new AbilityDef { id = AbilityId.Pulse, duration = 0f, cooldown = 40f, price = 0 },
            new AbilityDef { id = AbilityId.Stasis, duration = 4f, cooldown = 40f, price = 300 },
            new AbilityDef { id = AbilityId.Echo, duration = 6f, cooldown = 35f, price = 500 },
            new AbilityDef { id = AbilityId.Pull, duration = 3f, cooldown = 35f, price = 500 },
            new AbilityDef { id = AbilityId.Dome, duration = 5f, cooldown = 50f, price = 700 },
        };
        public float reverseHitMul = 1.5f;
        public float reverseFxTime = 0.4f;
        public float reverseFxTurn = 360f;
        public float growRadiusMul = 2f;
        public float growResonanceMul = 1.3f;
        public float growScaleTime = 0.2f;
        public float pulseRadius = 3f;
        public float pulseTime = 0.35f;
        public float pulseDamage = 2f;
        public float pulsePush = 1f;
        public float stasisSlow = 0.25f;
        public Color stasisTint = new Color32(0xB9, 0xBD, 0xC2, 0xFF);
        public float stasisTintAmount = 0.5f;
        public float echoLag = 35f;
        public float echoDamageMul = 0.6f;
        public float pullSpeed = 3f;
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
            new EnemyDef { id = EnemyType.Splitter, radius = 0.32f, hp = 2f, speed = 0.8f, armor = 0f, starDamage = 8f, crystals = 2, behavior = EnemyBehavior.Straight, faceStar = true, prewarm = 8, splitCount = 3 },
            new EnemyDef { id = EnemyType.Torch, radius = 0.28f, hp = 3f, speed = 0.9f, armor = 0f, starDamage = 10f, crystals = 3, behavior = EnemyBehavior.Straight, faceStar = true, prewarm = 8, heatOnHit = 0.25f },
            new EnemyDef { id = EnemyType.Orbiter, radius = 0.25f, hp = 3f, speed = 1f, armor = 0f, starDamage = 8f, crystals = 3, behavior = EnemyBehavior.Orbit, faceStar = true, prewarm = 8, orbitRadius = 4.2f, orbitDeg = 50f, orbitInSpeed = 0.2f },
            new EnemyDef { id = EnemyType.Shielder, radius = 0.35f, hp = 3f, speed = 0.6f, armor = 0f, starDamage = 12f, crystals = 4, behavior = EnemyBehavior.Straight, faceStar = true, prewarm = 8, shieldHits = 3 },
            new EnemyDef { id = EnemyType.Devourer, radius = 0.4f, hp = 4f, speed = 0.6f, armor = 0f, starDamage = 15f, crystals = 3, behavior = EnemyBehavior.Seek, faceStar = true, prewarm = 8, seekRadius = 2.5f },
            new EnemyDef { id = EnemyType.Phantom, radius = 0.22f, hp = 1.5f, speed = 1.3f, armor = 0f, starDamage = 6f, crystals = 2, behavior = EnemyBehavior.Straight, faceStar = true, prewarm = 8, phaseOn = 1.5f, phaseOff = 1f },
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
        public float splitSpread = 0.3f;
        public float splitSpeedMul = 1.3f;
        public int shieldBreakSparks = 8;
        public int devourMax = 6;
        public float devourHp = 1f;
        public float devourGrow = 0.04f;
        public float phantomAlpha = 0.25f;

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

        [Title("Эффекты")]
        public float hitStopTime = 0.04f;
        public float clinkHitStop = 0.02f;
        public float hitStopMinInterval = 0.25f;
        public float hitStopMinBaseHp = 2f;
        public float starFlashTime = 0.15f;
        public float starShakeAmp = 0.12f;
        public float starShakeTime = 0.12f;
        public float pulseShakeAmp = 0.15f;
        public float pulseShakeTime = 0.2f;
        public float shakeMinInterval = 0.2f;
        public int killShards = 6;
        public int hitShards = 2;
        public float shardTime = 0.4f;
        public float shardSpeed = 3f;
        public int shardPrewarm = 40;
        public int clinkSparks = 4;
        public float sparkTime = 0.2f;
        public float sparkSpeed = 4f;
        public int sparkPrewarm = 20;
        public float crystalPopTime = 0.1f;
        public float crystalPopScale = 1.8f;
        public float dustPunch = 0.3f;
        public float dustPunchTime = 0.2f;

        [Title("Видимый урон")]
        public float hitFlashAlpha = 0.85f;
        public float hitFlashTime = 0.1f;
        public float recoilDistance = 0.08f;
        public float bossRecoilDistance = 0.04f;
        public float recoilTime = 0.12f;
        public float hpRingMinHp = 3f;
        public float hpRingLerp = 10f;
        public float hpRingOffset = 0.12f;
        public int hpRingPoints = 40;
        public float numberMinHp = 2f;
        public float numberRise = 0.5f;
        public float numberTime = 0.6f;
        public float numberFadeFrom = 0.5f;
        public float numberJitter = 0.15f;
        public float numberScale = 0.35f;
        public float numberScalePerDamage = 0.08f;
        public float numberScaleMaxDamage = 10f;
        public float numberCritScale = 1.4f;
        public Color numberCritColor = new Color32(0xE3, 0xB2, 0x3C, 0xFF);
        public Color numberBlockedColor = new Color32(0xB9, 0xBD, 0xC2, 0xFF);
        public int numberPool = 32;
        public float bigRadius = 0.4f;
        public int bigKillShardMul = 2;
        public float bigKillShake = 0.06f;
        public float bigKillShakeTime = 0.08f;

        [Title("Полоски HUD")]
        public float barGhostDelay = 0.35f;
        public float barGhostTime = 0.4f;
        public float barPunch = 6f;
        public float barPunchTime = 0.15f;
        public float barPunchMinDrop = 0.5f;
        public float barFlashTime = 0.15f;
        public float barLowAlpha = 0.7f;
        public float barLowHz = 2f;
        public float xpFillTime = 0.25f;
        public float xpFlashTime = 0.2f;

        [Title("Звук")]
        public float dronePitchMin = 0.8f;
        public float dronePitchMax = 1.5f;

        [Title("Расписание")]
        // сами расписания лежат в эрах: eras[].opening и eras[].timeline
        public float ambientStart = 6f;
        public float ambientEnd = 175f;
        public float swarmRadiusJitter = 1f;

        [Title("Обучение")]
        // первый забег: вместо opening первой эры
        public SpawnEvent[] tutorialOpening =
        {
            At(0.5f, new SpawnGroup { type = EnemyType.Swarmer, count = 3, spreadDeg = 40f, speedMul = 0.15f, radius = 5.4f }),
            At(8f, new SpawnGroup { type = EnemyType.Swarmer, count = 6, speedMul = 0.6f }),
            At(16f, Of(EnemyType.Bastion, 1)),
        };
        public float tutorialAmbientStart = 20f;
        public float hintFade = 0.3f;
        public float hintHoldTimeout = 12f;
        public float hintReleaseRadius = 3f;
        public float hintReleaseTime = 0.3f;
        public float hintHeatShow = 0.7f;
        public float hintHeatHide = 0.3f;
        public int hintPulseEnemies = 4;
        public float hintPulseRadius = 3f;
        public float hintResonanceTime = 2f;

        [Title("Телеграф")]
        public float telegraphLeadSwarm = 1f;
        public float telegraphLeadArrow = 1.5f;
        public float telegraphLeadOther = 0.8f;
        public float telegraphInset = 0.5f;
        public int markerPrewarm = 8;

        [Title("Баннер")]
        public float bannerFadeIn = 0.2f;
        public float bannerFadeOut = 0.3f;
        public float bannerEnemyHold = 3f;

        [Title("Эры")]
        // СОЛНЦЕ, КРАСНЫЙ КАРЛИК, ТЯЖЁЛАЯ ЗВЕЗДА, ЧЁРНАЯ ДЫРА
        public EraDef[] eras =
        {
            new EraDef
            {
                kExp = 0.9f, spinMul = 1f, hpMul = 1f, countMul = 1f, ambientInterval = 3.5f, dustMul = 1f, riseMul = 1f, fallMul = 1f,
                rule = EraRule.None, boss = EnemyType.Boss, victoryDust = 100, firstWinDust = 200,
                background = new Color32(0x0E, 0x0E, 0x0E, 0xFF), starTint = Color.white, raysTint = Color.white,
                opening = new[]
                {
                    At(2f, Of(EnemyType.Swarmer, 4)),
                    At(8f, Of(EnemyType.Swarmer, 6)),
                    At(14f, Of(EnemyType.Bastion, 1)),
                },
                timeline = new[]
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
                },
            },
            new EraDef
            {
                kExp = 0.9f, spinMul = 1.15f, hpMul = 1.4f, countMul = 1.2f, ambientInterval = 3.3f, dustMul = 1.25f, riseMul = 1f, fallMul = 1f,
                rule = EraRule.Flares, boss = EnemyType.Twin, victoryDust = 150, firstWinDust = 400,
                background = new Color32(0x16, 0x0B, 0x0A, 0xFF), starTint = new Color32(0xFF, 0x9A, 0x80, 0xFF), raysTint = new Color32(0xFF, 0x9A, 0x80, 0xFF),
                opening = new[]
                {
                    At(2f, Of(EnemyType.Swarmer, 4)),
                    At(8f, Of(EnemyType.Splitter, 2)),
                    At(14f, Of(EnemyType.Torch, 1)),
                },
                timeline = new[]
                {
                    At(20f, Of(EnemyType.Swarmer, 8)),
                    At(30f, Of(EnemyType.Splitter, 3)),
                    At(36f, Of(EnemyType.Bastion, 1)),
                    At(46f, Of(EnemyType.Arrow, 2, AngleMode.Spread90)),
                    At(50f, Of(EnemyType.Torch, 1)),
                    At(56f, Of(EnemyType.Swarmer, 10)),
                    At(62f, Of(EnemyType.Arrow, 2, AngleMode.Spread90)),
                    At(70f, Of(EnemyType.Parasite, 1), Of(EnemyType.Splitter, 2, AngleMode.Opposite)),
                    At(80f, Of(EnemyType.Bastion, 1), Of(EnemyType.Swarmer, 8, AngleMode.Opposite)),
                    At(88f, Of(EnemyType.Parasite, 2, AngleMode.Spread90)),
                    At(96f, Of(EnemyType.Torch, 2, AngleMode.OppositePair), Of(EnemyType.Shooter, 1)),
                    At(108f, Of(EnemyType.Arrow, 3, AngleMode.Even)),
                    At(116f, Of(EnemyType.Hive, 1), Of(EnemyType.Splitter, 2, AngleMode.Opposite)),
                    At(126f, Of(EnemyType.Swarmer, 11), Of(EnemyType.Shooter, 1, AngleMode.Opposite)),
                    At(136f, Of(EnemyType.Bastion, 2, AngleMode.OppositePair)),
                    At(140f, Of(EnemyType.Torch, 2, AngleMode.Spread90)),
                    At(146f, Of(EnemyType.Hive, 1), Of(EnemyType.Arrow, 2, AngleMode.Spread90)),
                    At(156f, Of(EnemyType.Parasite, 2, AngleMode.Spread90), Of(EnemyType.Splitter, 3)),
                    At(164f, Of(EnemyType.Shooter, 2, AngleMode.OppositePair), Of(EnemyType.Bastion, 1)),
                    At(172f, Of(EnemyType.Arrow, 4, AngleMode.Even), Of(EnemyType.Splitter, 3), Of(EnemyType.Swarmer, 8)),
                },
            },
            new EraDef
            {
                kExp = 1.2f, spinMul = 1f, hpMul = 1.8f, countMul = 1.35f, ambientInterval = 3.1f, dustMul = 1.5f, riseMul = 0.8f, fallMul = 1.3f,
                rule = EraRule.Heavy, boss = EnemyType.Colossus, victoryDust = 200, firstWinDust = 600,
                background = new Color32(0x0B, 0x0E, 0x14, 0xFF), starTint = new Color32(0xC9, 0xD8, 0xFF, 0xFF), raysTint = new Color32(0xC9, 0xD8, 0xFF, 0xFF),
                opening = new[]
                {
                    At(2f, Of(EnemyType.Swarmer, 4)),
                    At(8f, Of(EnemyType.Orbiter, 1)),
                    At(14f, Of(EnemyType.Bastion, 1)),
                },
                timeline = new[]
                {
                    At(20f, Of(EnemyType.Swarmer, 8)),
                    At(28f, Of(EnemyType.Shielder, 1)),
                    At(36f, Of(EnemyType.Bastion, 1), Of(EnemyType.Orbiter, 1, AngleMode.Opposite)),
                    At(46f, Of(EnemyType.Arrow, 2, AngleMode.Spread90)),
                    At(52f, Of(EnemyType.Orbiter, 2, AngleMode.OppositePair)),
                    At(56f, Of(EnemyType.Swarmer, 10)),
                    At(62f, Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Shielder, 1, AngleMode.Opposite)),
                    At(70f, Of(EnemyType.Parasite, 1), Of(EnemyType.Splitter, 2)),
                    At(80f, Of(EnemyType.Bastion, 1), Of(EnemyType.Swarmer, 8, AngleMode.Opposite)),
                    At(88f, Of(EnemyType.Orbiter, 3, AngleMode.Even)),
                    At(98f, Of(EnemyType.Shooter, 1), Of(EnemyType.Shielder, 1, AngleMode.Opposite)),
                    At(108f, Of(EnemyType.Arrow, 3, AngleMode.Even)),
                    At(116f, Of(EnemyType.Hive, 1), Of(EnemyType.Torch, 1, AngleMode.Opposite)),
                    At(126f, Of(EnemyType.Swarmer, 11), Of(EnemyType.Shielder, 2, AngleMode.OppositePair)),
                    At(136f, Of(EnemyType.Bastion, 2, AngleMode.OppositePair), Of(EnemyType.Orbiter, 2, AngleMode.OppositePair)),
                    At(146f, Of(EnemyType.Hive, 1), Of(EnemyType.Arrow, 2, AngleMode.Spread90)),
                    At(152f, Of(EnemyType.Orbiter, 4, AngleMode.Even)),
                    At(156f, Of(EnemyType.Parasite, 2, AngleMode.Spread90), Of(EnemyType.Swarmer, 10)),
                    At(164f, Of(EnemyType.Shooter, 2, AngleMode.OppositePair), Of(EnemyType.Shielder, 2, AngleMode.Spread90)),
                    At(172f, Of(EnemyType.Arrow, 4, AngleMode.Even), Of(EnemyType.Orbiter, 3, AngleMode.Even), Of(EnemyType.Swarmer, 8)),
                },
            },
            new EraDef
            {
                kExp = 1f, spinMul = 1.1f, hpMul = 2.3f, countMul = 1.5f, ambientInterval = 3f, dustMul = 1.8f, riseMul = 1f, fallMul = 1f,
                rule = EraRule.Vortex, boss = EnemyType.Pulsar, victoryDust = 250, firstWinDust = 800,
                background = new Color32(0x05, 0x05, 0x05, 0xFF), starTint = new Color32(0x26, 0x26, 0x26, 0xFF), raysTint = Color.white,
                opening = new[]
                {
                    At(2f, Of(EnemyType.Swarmer, 4)),
                    At(8f, Of(EnemyType.Phantom, 2, AngleMode.OppositePair)),
                    At(14f, Of(EnemyType.Devourer, 1)),
                },
                timeline = new[]
                {
                    At(20f, Of(EnemyType.Swarmer, 8)),
                    At(28f, Of(EnemyType.Phantom, 3, AngleMode.Even)),
                    At(36f, Of(EnemyType.Bastion, 1), Of(EnemyType.Devourer, 1, AngleMode.Opposite)),
                    At(46f, Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Phantom, 2, AngleMode.OppositePair)),
                    At(56f, Of(EnemyType.Swarmer, 10), Of(EnemyType.Orbiter, 2, AngleMode.OppositePair)),
                    At(62f, Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Shielder, 1)),
                    At(70f, Of(EnemyType.Parasite, 1), Of(EnemyType.Devourer, 1, AngleMode.Opposite)),
                    At(80f, Of(EnemyType.Bastion, 1), Of(EnemyType.Swarmer, 8, AngleMode.Opposite)),
                    At(88f, Of(EnemyType.Phantom, 4, AngleMode.Even)),
                    At(98f, Of(EnemyType.Shooter, 1), Of(EnemyType.Torch, 1, AngleMode.Opposite)),
                    At(108f, Of(EnemyType.Arrow, 3, AngleMode.Even), Of(EnemyType.Splitter, 2)),
                    At(116f, Of(EnemyType.Hive, 1), Of(EnemyType.Devourer, 1, AngleMode.Opposite)),
                    At(126f, Of(EnemyType.Swarmer, 11), Of(EnemyType.Phantom, 3, AngleMode.Even)),
                    At(136f, Of(EnemyType.Bastion, 2, AngleMode.OppositePair), Of(EnemyType.Shielder, 2, AngleMode.OppositePair)),
                    At(146f, Of(EnemyType.Hive, 1), Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Devourer, 1)),
                    At(156f, Of(EnemyType.Parasite, 2, AngleMode.Spread90), Of(EnemyType.Phantom, 4, AngleMode.Even)),
                    At(164f, Of(EnemyType.Shooter, 2, AngleMode.OppositePair), Of(EnemyType.Bastion, 1), Of(EnemyType.Orbiter, 2, AngleMode.OppositePair)),
                    At(172f, Of(EnemyType.Arrow, 4, AngleMode.Even), Of(EnemyType.Phantom, 6, AngleMode.Even), Of(EnemyType.Swarmer, 8)),
                },
            },
        };

        [Title("Итоги")]
        public float victoryTime = 1.5f;
        public int dailyDustMul = 2;
        public float scorePerSecond = 2f;
        public int scoreVictory = 500;

        [Title("Реклама")]
        public float reviveHpFraction = 0.5f;
        public float reviveKillRadius = 4f;
        public float reviveImmuneTime = 1.5f;
        public int adDustMul = 2;
        public int interstitialMinRuns = 3;
        public float interstitialMinRunTime = 30f;
        public float interstitialMinInterval = 150f;
        public float interstitialAfterRewarded = 60f;

        [Title("Чертежи")]
        [TableList]
        public BlueprintDef[] blueprints =
        {
            new BlueprintDef { id = BlueprintId.Hull, perLevel = new StatMod(StatId.StarMaxHp, 10f), costs = new[] { 50, 100, 200, 400, 800 } },
            new BlueprintDef { id = BlueprintId.Capacitor, perLevel = new StatMod(StatId.AbilityCooldownMul, 0.95f), costs = new[] { 50, 100, 200, 400, 800 } },
            new BlueprintDef { id = BlueprintId.CoolantBp, perLevel = new StatMod(StatId.HeatMul, 0.95f), costs = new[] { 50, 100, 200, 400, 800 } },
            new BlueprintDef { id = BlueprintId.Magnetism, perLevel = new StatMod(StatId.MagnetRadius, 0.35f), costs = new[] { 40, 80, 160 } },
        };

        static CardDef Card(CardId id, CardColor color, CardRarity rarity, int maxStacks, StatId plus, float plusValue, StatId minus, float minusValue)
        {
            return new CardDef { id = id, color = color, rarity = rarity, maxStacks = maxStacks, plus = new StatMod(plus, plusValue), minus = new StatMod(minus, minusValue) };
        }

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
