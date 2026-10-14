using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Perigee
{
    // новые типы дописываются в конец: по номеру идут ArtSet.enemies и бит в SaveData.seenEnemies
    public enum EnemyType
    {
        Swarmer, Arrow, Bolt, Bastion, Parasite, Shooter, Hive, Boss, Splitter, Torch, Orbiter, Shielder, Devourer, Phantom,
        Twin, Colossus, Pulsar,
        // R4: враги эр 5–8, боссы эр 5–8, объекты событий
        Jelly, Mine, Bumper, Blinker, Eclipser, Sower, Mirror, Graviton, Abyss, Meteor, GoldComet
    }

    // Ellipse = тело кометы (Комета и Близнецы); Ellipse, Colossus и Pulsar ведёт BossSystem.
    // Chord = объект события: пролетает поле по хорде
    public enum EnemyBehavior { Straight, Attach, Stopper, Burster, Ellipse, Orbit, Seek, Colossus, Pulsar, Mine, Blink, Chord, Sower, Mirror, Graviton, Abyss }

    // свойство элиты: по нему идёт ArtSet.affixIcons
    public enum EliteAffix { Armored, Swift, Giant, Regen }

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
        // Студень: удар по нему замедляет спутник. Отбойник: удар по нему подбрасывает орбиту
        public bool slowHitter;
        public bool kickHitter;
        // Затмитель: пока жив, радиус света во тьме меньше на столько
        public float lightCut;
        // Прыгун: раз в столько секунд прыгает вбок по своему кругу на столько градусов
        public float blinkInterval;
        public float blinkDeg;
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

    public enum EraRule { None, Flares, Heavy, Vortex, Nebula, Moons, Anomalies, Darkness }

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
        // Вестник: элита, которая несёт реликвию
        public EnemyType herald;
        // доля врагов Директора, которые приходят элитой
        public float eliteChance;
        // музыка забега в этой эре
        public MusicId music;
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

    // порядок GDD 6.3 (конец): 6.2, карточки эр 2–4, 6.5, карточки эр 5–8, эволюции 6.6; по нему идут ArtSet.cardIcons и строки в Strings
    public enum CardId
    {
        Heavy, FarStrike, Sharp, Pierce, Execute, SawThread, Burn, Chain, AddSat, Swift, Spin, BigSat, Magnet, Greed, Coolant,
        StarPlating, Photosphere, Corona, Beacon, AbilityHaste, AbilityLong,
        Heatproof, SolarSail, Firebird, Slingshot, AnchorField, Crowbar, EventHorizon, DarkMatter, Accretion,
        Shockwave, Prism, SolarWind, Tide, Inertia, Overload,
        Condensate, SmoothHull, CloudSeed, GravAssist, Sapper, Tidal, Stabilizer, Lock, Singularity, Lantern, NightSight, LastLight,
        String, Wildfire, Prominence, Hammer, Harvest, Guillotine, LivingStar, Reactor
    }

    // цвет рамки: кремовая, красная, чёрная, золотая (эволюция)
    public enum CardColor { Orbit, Strike, Star, Evolution }

    public enum CardRarity { Common, Rare }

    // Параметры на Mul перемножаются, остальные складываются; флаг включён, когда набрано от 1.
    public enum StatId
    {
        PowerMul, RadialSpeedMul, RadialOutMul, HeatMul, SpinMul, ResonanceAngleMul, ResonanceBonus, SatRadiusMul, CrystalLifeMul,
        ArrowSpeedMul, AbilityCooldownMul, AbilityDurationMul, XpPerCrystalMul, XpNeedMul, StarDamageMul, DustMul, MagnetRadius,
        PowerFarBonus, StarMaxHp, Regen, Satellites, AddSatBonus, Beacon, CritChance, ArmorPierce, Execute, SawThread, Burn, Chain,
        Heatproof, SolarSail, Firebird, Slingshot, AnchorField, Crowbar, Horizon, DarkMatter, Accretion, SecondSun, FreeRerolls,
        ExtraChoices,
        // R4: складываются, системы читают их через Stats.Sum и Stats.Has
        Shockwave, Prism, SolarWind, TideHeal, Inertia, Overload, Condensate, Smooth, CloudSeed, GravAssist, Sapper, Tidal, Stabilizer,
        Lock, Singularity, Lantern, NightSight, LastLight, String, Wildfire, Prominence, Hammer, Harvest, Guillotine, LivingStar, Reactor,
        ThirdHit, CritMulBonus, Aegis, AutoStasis, ComboWindow, LastBreath, Thorns, Feather
    }

    // классы звёзд (GDD 8.6): бит в SaveData.starClassesOwned
    public enum StarClassId { Yellow, RedGiant, WhiteDwarf, Neutron, Cepheid }

    // черта класса: Блеск, Автоимпульс, Дыхание (Шипы Красного гиганта = стат Thorns в mods)
    public enum StarTrait { None, Glint, AutoPulse, Breath }

    // реликвии (GDD 6.7): по порядку идут ArtSet.relicIcons, строки в Strings и RunState.relicStacks
    public enum RelicId
    {
        CometShard, Lens, Heartwood, Compass, Chronometer, Aegis, Hourglass, GravLens, LifeCrystal, IceCore, Nozzle, Trophy, Laurel,
        LastBreath, Thorns, Feather
    }

    // Реликвия: сильное свойство до конца забега, действует через Stats, как карточка. Запись i списка relics = RelicId i
    [Serializable]
    public class RelicDef
    {
        public RelicId id;
        public int maxStacks;
        public StatMod[] mods;
    }

    // события забега (GDD 5.2): по порядку идут строки в Strings
    public enum RunEventType { MeteorShower, GoldenComet, Storm, CrystalTide, Invasion, Eclipse }

    // Класс звезды (GDD 8.6): выбирается перед забегом. Запись i списка starClasses = StarClassId i
    [Serializable]
    public class StarClassDef
    {
        public StarClassId id;
        public float starRadius;
        public int maxHp;
        public float rMin;
        public StatMod[] mods;
        public StarTrait trait;
        // умножает цвет тела звезды вместе с цветом эры
        public Color tint;
        // цена в пыли; 0 = не продаётся
        public int price;
        // номер эры с единицы: класс даётся за первую победу в ней
        public int eraWin;
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
        // у эволюции минуса нет: поле не действует
        public StatMod minus;
        // эволюция: две карточки-источника, обе на максимуме стаков
        public CardId sourceA;
        public CardId sourceB;
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

    // порядок таблицы GDD 8.3: ветка за веткой, внутри ветки по ярусам; по нему идут ArtSet.blueprintIcons и строки в Strings
    public enum BlueprintId
    {
        Hull, Core, Reflector, SecondSun, Forge, TuningFork, Edge, Twin, Thrust, Radiator, Capacitor, Quadriga, Magnetism, Insight,
        Alchemy, Choice
    }

    // ветки дерева: чёрная, красная, кремовая, золотая
    public enum BlueprintBranch { Star, Strike, Orbit, Crystal }

    [Serializable]
    public struct BlueprintLevel
    {
        public int cost;
        public StatMod mod;

        public BlueprintLevel(int cost, StatMod mod)
        {
            this.cost = cost;
            this.mod = mod;
        }
    }

    [Serializable]
    public class BlueprintDef
    {
        public BlueprintId id;
        public BlueprintBranch branch;
        // ярус в ветке с единицы: узел открыт, когда у узла ярусом ниже куплен хотя бы один уровень
        public int tier;
        // эффект узла = эффекты всех купленных уровней
        public BlueprintLevel[] levels;
    }

    // цены уровней одного чертежа R2.1: по ним перенос старого сохранения возвращает пыль
    [Serializable]
    public class LegacyBlueprint
    {
        public int[] costs;
    }

    // сначала облики спутника, потом звезды: по этому порядку идут ArtSet.satelliteSkins, ArtSet.starSkins и бит в SaveData.skinsOwned
    public enum SkinId { Romb, Sputnik, Gear, Moon, RedStar, Eye, Sun, Oko, Sunflower, Motor }

    public enum SkinKind { Satellite, Star }

    [Serializable]
    public class SkinDef
    {
        public SkinId id;
        public SkinKind kind;
        // задано не больше одного из трёх; все три 0 = облик есть с начала игры
        public int price;
        // номер эры с единицы: облик даётся за первую победу в ней
        public int eraWin;
        // номер испытания с единицы: облик даётся за его прохождение
        public int challenge;
    }

    // Испытание: забег в своей эре с модификаторами; награда даётся один раз, за первую победу.
    [Serializable]
    public class ChallengeDef
    {
        // номер эры с единицы
        public int era;
        public RunModifier mods;
        public int rewardDust;
        public SkinId rewardSkin;
        // награда = облик rewardSkin, а не пыль
        public bool hasSkin;
    }

    [CreateAssetMenu(fileName = "GameConfig", menuName = "Perigee/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Title("Отладка")]
        // всё открыто без записи в сохранение: эры, режимы, способности, испытания, облик
        public bool debugUnlockAll;

        [Title("Мир")]
        public float starLowHpFraction = 0.3f;
        // ВТОРОЕ СОЛНЦЕ: доля HP, с которой звезда вспыхивает вместо гибели
        public float secondSunHp = 0.25f;
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
        public float ringMax = 6.2f;
        public float satMaxRadius = 6.2f;
        public float ringGap = 0.8f;
        public float satRadius = 0.2f;
        // больше спутников не бывает (с чертежом КВАДРИГА): размер массивов
        public int maxSatellites = 4;
        // предел без КВАДРИГИ
        public int satelliteCap = 3;
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
        // эволюция после первого показа
        public int weightEvolution = 6;
        // слияние значков эволюции на карточке и в HUD
        public float evoMergeTime = 0.35f;
        public float evoFlashTime = 0.25f;
        public float evoPunch = 0.3f;
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
            // карточки эр: идут только в своей эре
            Card(CardId.Heatproof, CardColor.Orbit, CardRarity.Common, 1, StatId.Heatproof, 1f, StatId.SpinMul, 0.95f, 2),
            Card(CardId.SolarSail, CardColor.Strike, CardRarity.Common, 1, StatId.SolarSail, 1f, StatId.HeatMul, 1.1f, 2),
            Card(CardId.Firebird, CardColor.Strike, CardRarity.Rare, 1, StatId.Firebird, 1f, StatId.HeatMul, 1.2f, 2),
            Card(CardId.Slingshot, CardColor.Strike, CardRarity.Common, 1, StatId.Slingshot, 1f, StatId.RadialOutMul, 0.9f, 3),
            Card(CardId.AnchorField, CardColor.Orbit, CardRarity.Common, 1, StatId.AnchorField, 1f, StatId.PowerMul, 0.95f, 3),
            Card(CardId.Crowbar, CardColor.Strike, CardRarity.Rare, 1, StatId.Crowbar, 1f, StatId.SpinMul, 0.9f, 3),
            Card(CardId.EventHorizon, CardColor.Star, CardRarity.Common, 1, StatId.Horizon, 1f, StatId.StarMaxHp, -10f, 4),
            Card(CardId.DarkMatter, CardColor.Strike, CardRarity.Common, 1, StatId.DarkMatter, 1f, StatId.PowerMul, 0.95f, 4),
            Card(CardId.Accretion, CardColor.Star, CardRarity.Rare, 1, StatId.Accretion, 1f, StatId.CrystalLifeMul, 0.8f, 4),
            // R4: новые карточки любой эры
            Card(CardId.Shockwave, CardColor.Strike, CardRarity.Common, 1, StatId.Shockwave, 1f, StatId.PowerMul, 0.95f),
            Card(CardId.Prism, CardColor.Strike, CardRarity.Rare, 1, StatId.Prism, 1f, StatId.HeatMul, 1.1f),
            Card(CardId.SolarWind, CardColor.Star, CardRarity.Common, 1, StatId.SolarWind, 1f, StatId.XpPerCrystalMul, 0.9f),
            Card(CardId.Tide, CardColor.Star, CardRarity.Common, 2, StatId.TideHeal, 1f, StatId.CrystalLifeMul, 0.85f),
            Card(CardId.Inertia, CardColor.Orbit, CardRarity.Common, 2, StatId.Inertia, 0.3f, StatId.SpinMul, 0.92f),
            Card(CardId.Overload, CardColor.Orbit, CardRarity.Rare, 1, StatId.Overload, 1f, StatId.HeatMul, 1.1f),
            // эволюции: две карточки-источника на максимуме
            Evolution(CardId.String, StatId.String, CardId.SawThread, CardId.Spin),
            Evolution(CardId.Wildfire, StatId.Wildfire, CardId.Burn, CardId.Chain),
            Evolution(CardId.Prominence, StatId.Prominence, CardId.Beacon, CardId.Corona),
            Evolution(CardId.Hammer, StatId.Hammer, CardId.Heavy, CardId.Sharp),
            Evolution(CardId.Harvest, StatId.Harvest, CardId.Magnet, CardId.Greed),
            Evolution(CardId.Guillotine, StatId.Guillotine, CardId.Execute, CardId.Pierce),
            Evolution(CardId.LivingStar, StatId.LivingStar, CardId.StarPlating, CardId.Photosphere),
            Evolution(CardId.Reactor, StatId.Reactor, CardId.Overload, CardId.Coolant),
            // R4: карточки эр 5–8
            Card(CardId.Condensate, CardColor.Strike, CardRarity.Common, 1, StatId.Condensate, 1f, StatId.HeatMul, 1.1f, 5),
            Card(CardId.SmoothHull, CardColor.Orbit, CardRarity.Common, 1, StatId.Smooth, 1f, StatId.PowerMul, 0.95f, 5),
            Card(CardId.CloudSeed, CardColor.Star, CardRarity.Rare, 1, StatId.CloudSeed, 1f, StatId.SpinMul, 0.92f, 5),
            Card(CardId.GravAssist, CardColor.Orbit, CardRarity.Common, 1, StatId.GravAssist, 1f, StatId.HeatMul, 1.1f, 6),
            Card(CardId.Sapper, CardColor.Strike, CardRarity.Common, 1, StatId.Sapper, 1f, StatId.SpinMul, 0.95f, 6),
            Card(CardId.Tidal, CardColor.Star, CardRarity.Rare, 1, StatId.Tidal, 1f, StatId.CrystalLifeMul, 0.8f, 6),
            Card(CardId.Stabilizer, CardColor.Orbit, CardRarity.Common, 1, StatId.Stabilizer, 1f, StatId.PowerMul, 0.95f, 7),
            Card(CardId.Lock, CardColor.Strike, CardRarity.Common, 1, StatId.Lock, 1f, StatId.SpinMul, 0.95f, 7),
            Card(CardId.Singularity, CardColor.Star, CardRarity.Rare, 1, StatId.Singularity, 1f, StatId.StarMaxHp, -10f, 7),
            Card(CardId.Lantern, CardColor.Orbit, CardRarity.Common, 2, StatId.Lantern, 1.5f, StatId.SpinMul, 0.95f, 8),
            Card(CardId.NightSight, CardColor.Star, CardRarity.Common, 1, StatId.NightSight, 1f, StatId.XpPerCrystalMul, 0.9f, 8),
            Card(CardId.LastLight, CardColor.Strike, CardRarity.Rare, 1, StatId.LastLight, 1f, StatId.StarMaxHp, -10f, 8),
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
        // СОЛНЕЧНЫЙ ПАРУС: окно идёт всю вспышку и ещё столько секунд после неё
        public float solarSailAfter = 3f;
        public float solarSailMul = 2f;
        public float firebirdHeat = 0.7f;
        public float firebirdMul = 1.5f;
        public float slingshotTime = 1f;
        public float slingshotMul = 1.5f;
        // ЛОМ: во сколько раз больнее удар по Кружащему
        public float crowbarOrbiterMul = 2f;
        public float horizonRadius = 1.8f;
        public float horizonDps = 2f;
        public float accretionRadius = 3f;
        public float accretionHeal = 1f;
        // УДАРНАЯ ВОЛНА: убийство хиттером отбрасывает соседей от звезды
        public float shockRadius = 1.2f;
        public float shockPush = 0.8f;
        // СОЛНЕЧНЫЙ ВЕТЕР: враги ближе starRadius + solarWindRange идут медленнее
        public float solarWindRange = 0.8f;
        public float solarWindMul = 0.5f;
        // ПРИЛИВ: каждые tideEvery подобранных кристаллов лечат tideHeal за стак
        public int tideEvery = 25;
        public float tideHeal = 2f;
        // ПЕРЕГРУЗКА: взрыв у оглушённого спутника
        public float overloadRadius = 1.5f;
        public float overloadDamage = 3f;
        // КОНДЕНСАТ: окно после выхода спутника из облака
        public float condensateTime = 2f;
        public float condensateMul = 1.5f;
        // ЗАСЕВ: лишние кристаллы за убийство в облаке
        public int cloudSeedCrystals = 2;
        // САПЁР: во сколько раз сильнее взрыв Мины
        public float sapperMineMul = 2f;
        // ПРИЛИВНЫЕ СИЛЫ: урон у луны; СИНГУЛЯРНОСТЬ: урон в колодце аномалии
        public float tidalRadius = 1.2f;
        public float tidalDps = 2f;
        public float singularityDps = 2f;
        // ПОСЛЕДНИЙ СВЕТ: доля HP звезды, ниже которой удар сильнее
        public float lastLightHp = 0.3f;
        public float lastLightMul = 1.4f;

        [Title("Эволюции")]
        public float stringSawMul = 3f;
        public float stringSawInterval = 0.15f;
        public float wildfireBurnDps = 2f;
        public float wildfireRadius = 1.2f;
        public float prominenceRadius = 3.5f;
        public float prominenceDamage = 3f;
        public float prominenceHeal = 2f;
        public float hammerCritMul = 3f;
        public float hammerShred = 0.5f;
        public float harvestInterval = 8f;
        public float guillotineFraction = 0.4f;
        public float livingStarRegenMul = 2f;
        public float livingStarHp = 40f;
        public float reactorRadius = 2.5f;
        public float reactorDamage = 6f;
        public float reactorStunMul = 0.5f;

        [Title("Комбо")]
        public float comboWindow = 2f;
        // пороги серии и множители опыта и пыли за кристалл; ниже первого порога множитель 1
        public int[] comboTiers = { 10, 25, 50, 100 };
        public float[] comboMuls = { 1.2f, 1.5f, 2f, 3f };
        public int comboShowMin = 5;
        // ComboView: прыжок числа на каждое убийство, тряска (px) и угасание при обрыве
        public float comboPunch = 0.25f;
        public float comboPunchTime = 0.15f;
        public float comboShake = 12f;
        public float comboBreakTime = 0.3f;

        [Title("Реликвии")]
        public int relicChoices = 3;
        public RelicDef[] relics =
        {
            Relic(RelicId.CometShard, 1, new StatMod(StatId.ThirdHit, 2f)),
            Relic(RelicId.Lens, 1, new StatMod(StatId.CritChance, 0.08f), new StatMod(StatId.CritMulBonus, 0.5f)),
            Relic(RelicId.Heartwood, 3, new StatMod(StatId.StarMaxHp, 20f)),
            Relic(RelicId.Compass, 1, new StatMod(StatId.CrystalLifeMul, 2f), new StatMod(StatId.MagnetRadius, 0.5f)),
            Relic(RelicId.Chronometer, 2, new StatMod(StatId.AbilityCooldownMul, 0.85f)),
            Relic(RelicId.Aegis, 1, new StatMod(StatId.Aegis, 1f)),
            Relic(RelicId.Hourglass, 1, new StatMod(StatId.AutoStasis, 1f)),
            Relic(RelicId.GravLens, 2, new StatMod(StatId.SatRadiusMul, 1.15f)),
            Relic(RelicId.LifeCrystal, 3, new StatMod(StatId.TideHeal, 1f)),
            Relic(RelicId.IceCore, 2, new StatMod(StatId.HeatMul, 0.8f)),
            Relic(RelicId.Nozzle, 2, new StatMod(StatId.RadialSpeedMul, 1.2f)),
            Relic(RelicId.Trophy, 1, new StatMod(StatId.DustMul, 1.2f)),
            Relic(RelicId.Laurel, 1, new StatMod(StatId.ComboWindow, 1f)),
            Relic(RelicId.LastBreath, 1, new StatMod(StatId.LastBreath, 1f)),
            Relic(RelicId.Thorns, 2, new StatMod(StatId.Thorns, 1f)),
            Relic(RelicId.Feather, 1, new StatMod(StatId.Feather, 1f)),
        };
        // реликвий не осталось: вместо выбора столько пыли
        public int relicNoneDust = 50;
        public float bannerRelicHold = 2f;
        // ОСКОЛОК КОМЕТЫ: каждый такой по счёту удар спутника, который прошёл, сильнее
        public int thirdHitEvery = 3;
        // ЩИТ ПРЕДКОВ: первый удар по звезде после стольких секунд без гашения гасится
        public float aegisInterval = 20f;
        public Color aegisFlashColor = Color.white;
        // ПЕСОЧНЫЕ ЧАСЫ: раз в столько секунд враги на столько секунд замедляются, как под СТАЗИСОМ
        public float autoStasisInterval = 30f;
        public float autoStasisTime = 2f;
        // ПОСЛЕДНИЙ ВЗДОХ: доля HP и волна
        public float lastBreathHp = 0.25f;
        public float lastBreathRadius = 4f;
        public float lastBreathDamage = 3f;
        // ПЕРО ЖАР-ПТИЦЫ: убийство спутником с таким нагревом даёт ещё кристаллы
        public float featherHeat = 0.7f;
        public int featherCrystals = 2;

        [Title("Вестник и события")]
        // Вестник: секунда в цикле (в Бесконечном + endlessLoopTime за каждый цикл)
        public float heraldTime = 95f;
        public float bannerHeraldHold = 2.5f;
        public float[] eventTimes = { 70f, 130f };
        public float eventChance = 0.6f;
        public float bannerEventHold = 2.5f;
        public int meteorCount = 10;
        public float meteorInterval = 0.5f;
        // все метеоры входят в пределах ± стольких градусов от одного угла
        public float meteorSpread = 40f;
        public float goldCometTime = 7f;
        public float stormTime = 8f;
        public float stormPowerMul = 1.5f;
        public float stormHeatMul = 1.5f;
        public float tideTime = 10f;
        public int tideCrystalMul = 2;
        public int invasionCount = 30;
        public float invasionSpeedMul = 0.7f;
        public float eclipseTime = 8f;
        public float eclipseLight = 2.5f;

        [Title("Поля у врагов")]
        // сколько областей EnemySystem.Field за один шаг
        public int maxFields = 32;

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
            // боссы эр 2–4; у Близнецов hp и кристаллы на каждое из двух тел
            new EnemyDef { id = EnemyType.Twin, radius = 0.75f, hp = 70f, speed = 0f, armor = 0f, starDamage = 6f, crystals = 15, behavior = EnemyBehavior.Ellipse, faceStar = false, prewarm = 0 },
            new EnemyDef { id = EnemyType.Colossus, radius = 1.1f, hp = 220f, speed = 0.25f, armor = 2.5f, starDamage = 25f, crystals = 40, behavior = EnemyBehavior.Colossus, faceStar = true, prewarm = 0 },
            new EnemyDef { id = EnemyType.Pulsar, radius = 1f, hp = 160f, speed = 1f, armor = 0f, starDamage = 0f, crystals = 40, behavior = EnemyBehavior.Pulsar, faceStar = false, prewarm = 0 },
            // R4: враги эр 5–8
            new EnemyDef { id = EnemyType.Jelly, radius = 0.38f, hp = 4f, speed = 0.6f, armor = 0f, starDamage = 12f, crystals = 4, behavior = EnemyBehavior.Straight, faceStar = true, prewarm = 8, slowHitter = true },
            new EnemyDef { id = EnemyType.Mine, radius = 0.3f, hp = 2f, speed = 1.2f, armor = 0f, starDamage = 14f, crystals = 3, behavior = EnemyBehavior.Mine, faceStar = true, prewarm = 8 },
            new EnemyDef { id = EnemyType.Bumper, radius = 0.34f, hp = 3f, speed = 0.8f, armor = 0f, starDamage = 10f, crystals = 3, behavior = EnemyBehavior.Straight, faceStar = true, prewarm = 8, kickHitter = true },
            new EnemyDef { id = EnemyType.Blinker, radius = 0.24f, hp = 2f, speed = 1.1f, armor = 0f, starDamage = 8f, crystals = 3, behavior = EnemyBehavior.Blink, faceStar = true, prewarm = 8, blinkInterval = 1.6f, blinkDeg = 35f },
            new EnemyDef { id = EnemyType.Eclipser, radius = 0.45f, hp = 6f, speed = 0.5f, armor = 1f, starDamage = 18f, crystals = 6, behavior = EnemyBehavior.Straight, faceStar = true, prewarm = 4, lightCut = 0.8f },
            // боссы эр 5–8; speed = как быстро они выходят на свою орбиту
            new EnemyDef { id = EnemyType.Sower, radius = 0.9f, hp = 260f, speed = 0.8f, armor = 0f, starDamage = 0f, crystals = 40, behavior = EnemyBehavior.Sower, faceStar = false, prewarm = 0 },
            new EnemyDef { id = EnemyType.Mirror, radius = 0.8f, hp = 240f, speed = 0.8f, armor = 1.5f, starDamage = 0f, crystals = 40, behavior = EnemyBehavior.Mirror, faceStar = false, prewarm = 0 },
            new EnemyDef { id = EnemyType.Graviton, radius = 1f, hp = 300f, speed = 0.8f, armor = 0f, starDamage = 0f, crystals = 40, behavior = EnemyBehavior.Graviton, faceStar = false, prewarm = 0 },
            new EnemyDef { id = EnemyType.Abyss, radius = 1.3f, hp = 700f, speed = 0.8f, armor = 0f, starDamage = 0f, crystals = 0, behavior = EnemyBehavior.Abyss, faceStar = false, prewarm = 0 },
            // объекты событий: Метеор отдаёт meteorCrystals, только когда его разбил спутник
            new EnemyDef { id = EnemyType.Meteor, radius = 0.22f, hp = 1f, speed = 4f, armor = 0f, starDamage = 3f, crystals = 0, behavior = EnemyBehavior.Chord, faceStar = false, prewarm = 10 },
            new EnemyDef { id = EnemyType.GoldComet, radius = 0.4f, hp = 6f, speed = 2f, armor = 0f, starDamage = 0f, crystals = 40, behavior = EnemyBehavior.Chord, faceStar = false, prewarm = 1 },
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
        // Студень: замедление спутника после удара; Отбойник: подброс орбиты
        public float jellyTime = 1f;
        public float jellySlow = 0.5f;
        public float bumperKick = 1.2f;
        // Мина: встаёт на случайный радиус, ждёт и летит дальше; взрыв при гибели, нагрев убившего спутника
        public float mineStopMin = 2.6f;
        public float mineStopMax = 5f;
        public float mineWait = 12f;
        public float mineRadius = 1.3f;
        public float mineDamage = 4f;
        public float mineHeat = 0.6f;
        // Прыгун мигает столько секунд перед прыжком
        public float blinkWarn = 0.25f;
        public Color blinkFlashColor = new Color32(0xEF, 0xE4, 0xC8, 0xFF);
        // хорда объектов событий: цель в круге chordInner, объект пропадает за spawnRadius + chordExit
        public float chordInner = 3f;
        public float chordExit = 0.5f;
        // Золотая комета пролетает не ближе этого к центру
        public float goldCometMiss = 3f;
        public float meteorDamage = 3f;
        public int meteorCrystals = 3;

        [Title("Элита")]
        // элитой бывают враги с базовым hp от этого числа
        public float eliteMinBaseHp = 2f;
        public float eliteHpMul = 1.5f;
        public int eliteCrystalMul = 3;
        public float eliteArmor = 1.5f;
        public float eliteSpeedMul = 1.5f;
        public float giantHpMul = 2f;
        public float giantRadiusMul = 1.35f;
        public float giantStarDamageMul = 1.5f;
        // доля maxHp в секунду, пока враг не получал урона eliteRegenDelay секунд
        public float eliteRegen = 0.08f;
        public float eliteRegenDelay = 1.5f;
        // Бесконечный: прибавка к шансу элиты за цикл; общий шанс не выше eliteMax
        public float endlessEliteStep = 0.03f;
        public float eliteMax = 0.3f;
        // золотые искры при появлении и золотые осколки при гибели
        public int eliteSparks = 8;
        public Color eliteGold = new Color32(0xE3, 0xB2, 0x3C, 0xFF);

        [Title("Правила эр")]
        // вспышки Красного карлика
        public float flareFirst = 25f;
        public float flareInterval = 25f;
        public float flareMinGap = 60f;
        public float flareArc = 90f;
        public float flareWarn = 1.5f;
        public float flareTime = 1f;
        public float flareDamage = 2f;
        public float flareHeatRate = 0.8f;
        public float flareShakeAmp = 0.05f;
        public float flareShakeTime = 0.1f;
        // пока идёт предупреждение, сектор мигает от нуля до этой прозрачности
        public float flareWarnAlpha = 0.4f;
        public float flareWarnHz = 3f;
        // Тяжёлая звезда: скорость, с которой кристаллы тонут в ней
        public float crystalSink = 0.35f;
        // Чёрная дыра: градусы в секунду, на которые закручивается путь врагов
        public float vortexDeg = 14f;
        // гравитационный сдвиг орбиты (луны, аномалии, Гравитон, Бездна): предел, скорость, зазор до поверхности звезды
        public float gravMaxOffset = 1f;
        public float gravOffsetSpeed = 2f;
        public float gravStarGap = 0.25f;
        // сколько источников притяжения за один шаг
        public int maxGravitySources = 8;
        // Туманность: облака кружат вокруг звезды; спутник в облаке остывает, враги в нём вязнут
        public float nebulaRadius = 1.3f;
        public float[] nebulaOrbits = { 2.2f, 3.6f, 5f };
        public float[] nebulaDeg = { 8f, -6f, 5f };
        public float[] nebulaStart = { 0f, 120f, 240f };
        public float nebulaCool = 0.6f;
        public float nebulaSlow = 0.6f;
        // Газовый гигант: луны (направление 1 = против часовой стрелки), их притяжение и гравиманёвр
        public float moonSize = 0.5f;
        public float[] moonOrbits = { 3f, 5f };
        public float[] moonPeriods = { 20f, 32f };
        public int[] moonDirs = { 1, -1 };
        public float moonWellRadius = 1.4f;
        public float moonBend = 0.4f;
        public float moonPullRadius = 1.8f;
        public float moonPull = 0.8f;
        public float moonAssistRadius = 0.9f;
        public float moonAssistTime = 1.5f;
        public float moonAssistMul = 1.5f;
        // манёвр у одной луны одним спутником не чаще раза в столько секунд
        public float moonAssistCooldown = 1.5f;
        public float gravAssistTime = 2.5f;
        public float gravAssistMul = 2f;
        public Color assistFlashColor = new Color32(0xE3, 0xB2, 0x3C, 0xFF);
        public float assistFlashTime = 0.3f;
        // Аномалия: колодцы и отталкиватели
        public float anomalyFirst = 10f;
        public float anomalyInterval = 9f;
        public float anomalyMinR = 2.2f;
        public float anomalyMaxR = 5.2f;
        public int anomalyMax = 2;
        public float anomalyWarn = 1f;
        public float anomalyLife = 7f;
        public float anomalyFade = 0.5f;
        public float anomalyRadius = 2f;
        public float anomalyBend = 0.7f;
        public float anomalyPull = 1.2f;
        public float anomalyCrystalPull = 2f;
        // Сердце Тьмы: радиус света вокруг орбиты
        public float lightMin = 1.8f;
        public float lightBase = 2.6f;
        public float lightAround = 1.2f;
        public float lightLerp = 4f;
        public float nightSightAlpha = 0.65f;

        [Title("Боссы")]
        public float bossTime = 180f;
        public float bossFadeIn = 0.5f;
        // Комета и Близнецы: общий эллипс
        public float bossPeriapsis = 1.8f;
        public float bossApoapsis = 5.5f;
        public float bossPeriod = 6f;
        public float bossPhiStep = 40f;
        public int bossNewtonSteps = 4;
        public float bossOrbitLineTime = 1.5f;
        public int bossOrbitLinePoints = 64;
        public float twinPeriod = 6.5f;
        // период оставшегося из Близнецов
        public float twinEnragedPeriod = 4.5f;
        public float colossusBounceRadius = 5.5f;
        public float colossusBounceDeg = 60f;
        // доли HP, на которых Колосс выпускает Рой и теряет часть брони
        public float[] colossusPhaseHp = { 0.75f, 0.5f, 0.25f };
        public int colossusShed = 6;
        public float colossusShedOffset = 0.3f;
        public float colossusArmorStep = 0.5f;
        public float pulsarDeg = 22f;
        public float pulsarMid = 4f;
        public float pulsarAmp = 1f;
        public float pulsarSwing = 16f;
        public float pulsarFireInterval = 3.5f;
        public int pulsarBolts = 3;
        public float pulsarFan = 12f;
        public float pulsarWaveFirst = 6f;
        public float pulsarWaveInterval = 12f;
        public float pulsarWaveWarn = 1f;
        public float pulsarWaveRadius = 3.2f;
        public float pulsarWaveTime = 1.2f;
        // запас к радиусу спутника, в котором кольцо волны его задевает
        public float pulsarWaveBand = 0.1f;
        public Color pulsarWarnTint = new Color32(0xEF, 0xE4, 0xC8, 0xFF);
        public float pulsarWarnHz = 4f;
        // Сеятель: орбита, Ульи с неё; ниже доли HP чаще
        public float sowerRadius = 5f;
        public float sowerDeg = 15f;
        public float sowerInterval = 4f;
        public float sowerFastInterval = 3f;
        public float sowerFastHp = 0.5f;
        public float sowerHiveSpeed = 1.2f;
        // Зеркало: орбита, стражи, разворот орбиты игрока, залпы
        public float mirrorRadius = 4.2f;
        public float mirrorDeg = 20f;
        public float guardRadius = 0.25f;
        public float guardDistance = 1.1f;
        public float guardDeg = 180f;
        public float guardHeat = 0.3f;
        public float guardKnock = 1f;
        public float mirrorFlip = 10f;
        public float mirrorFireInterval = 5f;
        public int mirrorBolts = 2;
        // угол между соседними Снарядами залпа
        public float mirrorFan = 20f;
        // Зеркало и Гравитон мигают за столько секунд до смены
        public float bossWarn = 1f;
        public Color bossWarnColor = new Color32(0xEF, 0xE4, 0xC8, 0xFF);
        // Гравитон: колодец, на время отталкиватель; в миг смены залп
        public float gravitonRadius = 4.6f;
        public float gravitonDeg = 8f;
        public float gravitonWellRadius = 2.6f;
        public float gravitonBend = 1f;
        public float gravitonFlip = 8f;
        public float gravitonRepel = 3f;
        public int gravitonBolts = 5;
        public float gravitonFan = 12f;
        // Бездна: пороги фаз, неуязвимость при смене
        public float[] abyssPhaseHp = { 0.66f, 0.33f };
        public float abyssShift = 1.5f;
        // фаза 1 ГЛАЗ: орбита и Стрелы к звезде
        public float abyssRadius = 5.2f;
        public float abyssDeg = 12f;
        public float abyssArrowInterval = 3f;
        public int abyssArrows = 3;
        public float abyssArrowFan = 15f;
        public float abyssArrowSpeed = 0.8f;
        // фаза 2 ЗАТМЕНИЕ: броня, меньше света, эллипс кометы, Призраки
        public float abyssEclipseArmor = 1.5f;
        public float abyssLightCut = 1f;
        public float abyssPeriapsis = 2.2f;
        public float abyssApoapsis = 5.5f;
        public float abyssPeriod = 7f;
        public float abyssDiveDamage = 10f;
        public float abyssPhantomInterval = 8f;
        public int abyssPhantoms = 4;
        // фаза 3 ПАСТЬ: колодец, шаг к звезде, укус и отскок, Рой
        public float abyssWellRadius = 3f;
        public float abyssBend = 1f;
        public float abyssWalkSpeed = 0.15f;
        public float abyssBiteDamage = 40f;
        public float abyssBounceRadius = 5f;
        public float abyssSwarmInterval = 5f;
        public int abyssSwarm = 8;

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

        [Title("Эффекты R4")]
        // приход босса: камера сдвигается к нему на долю пути и приближается, время замедляется, крупное имя с подписью
        public float bossCamShift = 0.3f;
        public float bossZoom = 0.85f;
        public float bossCamTime = 0.4f;
        public float bossIntroSlow = 0.3f;
        public float bossIntroSlowTime = 0.8f;
        public float bossIntroHold = 1.6f;
        public float bossIntroFade = 0.25f;
        // гибель босса
        public float bossKillSlow = 0.25f;
        public float bossKillSlowTime = 0.8f;
        public float bossKillFlash = 0.6f;
        public float bossKillFlashTime = 0.4f;
        public int bossKillShards = 30;
        public float bossKillShake = 0.3f;
        public float bossKillShakeTime = 0.4f;
        // смена фазы босса
        public float bossPhaseFlash = 0.3f;
        public float bossPhaseFlashTime = 0.4f;
        public float bossPhaseShake = 0.15f;
        public float bossPhaseShakeTime = 0.3f;
        // старт забега: разгон, толчок звезды, проявление спутников
        public float warpFrom = 0.2f;
        public float warpTo = 1.6f;
        public float warpTime = 0.8f;
        public float startStarPunch = 0.15f;
        public float startStarPunchTime = 0.3f;
        public float startSatFadeIn = 0.6f;
        // кольцо нового уровня; экран карточек ждёт его
        public float levelUpRingRadius = 6.5f;
        public float levelUpRingTime = 0.5f;
        // экран карточек: вылет снизу по очереди с переворотом, пульс редкой каймы, блик эволюции, улёт выбранной
        public float cardInStagger = 0.07f;
        public float cardInTime = 0.3f;
        public float cardInOffset = 600f;
        public float rareRimMinAlpha = 0.45f;
        public float rareRimPulseTime = 0.6f;
        public float shineInterval = 1.5f;
        public float shineTime = 0.5f;
        public float cardPickPunch = 0.12f;
        public float cardPickPunchTime = 0.15f;
        public float cardFlyTime = 0.35f;
        public float cardFlyScale = 0.2f;
        public float cardDropTime = 0.25f;
        public float cardDropOffset = 150f;
        // способности: вспышка экрана цветом способности (по порядку AbilityId), толчок кнопки
        public Color[] abilityColors =
        {
            new Color32(0xEF, 0xE4, 0xC8, 0xFF), new Color32(0xE3, 0xB2, 0x3C, 0xFF), new Color32(0xEF, 0xE4, 0xC8, 0xFF), new Color32(0xB9, 0xBD, 0xC2, 0xFF),
            new Color32(0xB9, 0xBD, 0xC2, 0xFF), new Color32(0xD6, 0x28, 0x1F, 0xFF), new Color32(0xE3, 0xB2, 0x3C, 0xFF),
        };
        public float abilityFlash = 0.15f;
        public float abilityFlashTime = 0.3f;
        public float abilityPressPunch = 0.15f;
        public float abilityReadyPunch = 0.15f;
        public float abilityPunchTime = 0.2f;
        // удар по звезде: толчок и красное кольцо
        public float starHitPunch = 0.08f;
        public float starHitPunchTime = 0.15f;
        public float starHitRingGrow = 1.2f;
        public float starHitRingTime = 0.3f;
        // красная виньетка: ниже первой доли HP слабая, ниже второй сильная
        public float vignetteLowHp = 0.3f;
        public float vignetteLowAlpha = 0.35f;
        public float vignetteCriticalHp = 0.15f;
        public float vignetteCriticalAlpha = 0.55f;
        public float vignetteHz = 1.5f;
        // осколки удара летят конусом от хиттера к врагу, градусы
        public float hitShardCone = 70f;
        // кристалл летит к счётчику пыли дугой (высота дуги в пикселях холста)
        public float crystalFlyTime = 0.45f;
        public float crystalFlyArc = 150f;
        // нагрев: след краснеет, тело дрожит от порога; оглушение выпускает пар
        public Color heatTrailColor = new Color32(0xD6, 0x28, 0x1F, 0xFF);
        public float heatJitterFrom = 0.8f;
        public float heatJitter = 0.02f;
        public int stunSteamSparks = 6;
        public int resonanceSparks = 8;
        public int eliteShardMul = 2;
        // крит: добавка к hit-stop и прыжок числа
        public float critHitStop = 0.02f;
        public float critNumberPunch = 0.6f;
        public float critNumberPunchTime = 0.2f;
        // комбо: новая ступень красит счётчик золотом и края экрана
        public float comboTierFlash = 0.1f;
        public float comboTierFlashTime = 0.3f;
        public float evoScreenFlash = 0.25f;
        public float evoScreenFlashTime = 0.4f;
        // меню: нажатие, покупка, панели, счёт пыли
        public float menuPressScale = 0.92f;
        public float menuPressTime = 0.15f;
        public float menuBuyPunch = 0.15f;
        public float menuBuyPunchTime = 0.25f;
        public float panelSlide = 40f;
        public float panelFade = 0.25f;
        public float dustCountTime = 0.6f;
        // итоги: строки по одной, штамп рекорда
        public float resultLineDelay = 0.12f;
        public float stampFrom = 2f;
        public float stampAngle = -8f;
        public float stampTime = 0.3f;
        // вибрация, мс
        public int vibrateStarHit = 40;
        public int vibrateStun = 30;
        public int vibrateEliteKill = 20;
        public int vibrateBossKill = 150;

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
        // голосов эффектов одновременно (столько AudioSource лежит под Systems/Sfx)
        public int maxVoices = 16;
        // переход между треками музыки, секунды
        public float musicFade = 1f;
        // музыка на паузе
        public float musicPauseMul = 0.4f;
        // номер эры с единицы, где у босса музыка FinalBoss
        public int finalBossEra = 8;
        // звук Overheat: нагрев дошёл до первого числа; снова звучит, когда спутник остыл ниже второго
        public float overheatSound = 0.8f;
        public float overheatReset = 0.5f;

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
        public float bannerBossHold = 2f;
        public float bannerRuleHold = 3f;
        // «+N пыли» за босса в Бесконечном
        public float bannerDustHold = 2f;

        [Title("Эры")]
        // длина списков эр в сохранении (bestTimes, bestEndless)
        public int maxEras = 8;
        // СОЛНЦЕ, КРАСНЫЙ КАРЛИК, ТЯЖЁЛАЯ ЗВЕЗДА, ЧЁРНАЯ ДЫРА, ТУМАННОСТЬ, ГАЗОВЫЙ ГИГАНТ, АНОМАЛИЯ, СЕРДЦЕ ТЬМЫ
        public EraDef[] eras =
        {
            new EraDef
            {
                kExp = 0.9f, spinMul = 1f, hpMul = 1f, countMul = 1f, ambientInterval = 3.5f, dustMul = 1f, riseMul = 1f, fallMul = 1f,
                rule = EraRule.None, boss = EnemyType.Boss, herald = EnemyType.Bastion, eliteChance = 0f, music = MusicId.Chapter1, victoryDust = 100, firstWinDust = 200,
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
                rule = EraRule.Flares, boss = EnemyType.Twin, herald = EnemyType.Splitter, eliteChance = 0f, music = MusicId.Chapter1, victoryDust = 150, firstWinDust = 400,
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
                rule = EraRule.Heavy, boss = EnemyType.Colossus, herald = EnemyType.Shielder, eliteChance = 0f, music = MusicId.Chapter2, victoryDust = 200, firstWinDust = 600,
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
                rule = EraRule.Vortex, boss = EnemyType.Pulsar, herald = EnemyType.Devourer, eliteChance = 0f, music = MusicId.Chapter2, victoryDust = 250, firstWinDust = 800,
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
            // эры 5–8
            new EraDef
            {
                kExp = 1f, spinMul = 1.1f, hpMul = 2.6f, countMul = 1.55f, ambientInterval = 2.9f, dustMul = 2.1f, riseMul = 1f, fallMul = 1f,
                rule = EraRule.Nebula, boss = EnemyType.Sower, herald = EnemyType.Jelly, eliteChance = 0.06f, music = MusicId.Chapter3, victoryDust = 300, firstWinDust = 1000,
                background = new Color32(0x0D, 0x0A, 0x12, 0xFF), starTint = new Color32(0xE8, 0xD2, 0xFF, 0xFF), raysTint = new Color32(0xE8, 0xD2, 0xFF, 0xFF),
                opening = new[]
                {
                    At(2f, Of(EnemyType.Swarmer, 4)),
                    At(8f, Of(EnemyType.Jelly, 1)),
                    At(14f, Of(EnemyType.Bastion, 1)),
                },
                timeline = new[]
                {
                    At(20f, Of(EnemyType.Swarmer, 8)),
                    At(28f, Of(EnemyType.Jelly, 2, AngleMode.OppositePair)),
                    At(36f, Of(EnemyType.Bastion, 1), Of(EnemyType.Splitter, 2, AngleMode.Opposite)),
                    At(46f, Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Jelly, 1)),
                    At(56f, Of(EnemyType.Swarmer, 10), Of(EnemyType.Torch, 2, AngleMode.OppositePair)),
                    At(62f, Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Shielder, 1)),
                    At(70f, Of(EnemyType.Parasite, 1), Of(EnemyType.Jelly, 2, AngleMode.Opposite)),
                    At(80f, Of(EnemyType.Bastion, 1), Of(EnemyType.Swarmer, 8, AngleMode.Opposite)),
                    At(88f, Of(EnemyType.Hive, 1), Of(EnemyType.Jelly, 2, AngleMode.Spread90)),
                    At(98f, Of(EnemyType.Shooter, 1), Of(EnemyType.Orbiter, 2, AngleMode.OppositePair)),
                    At(108f, Of(EnemyType.Arrow, 3, AngleMode.Even), Of(EnemyType.Splitter, 2)),
                    At(116f, Of(EnemyType.Hive, 2, AngleMode.OppositePair)),
                    At(126f, Of(EnemyType.Swarmer, 11), Of(EnemyType.Jelly, 3, AngleMode.Even)),
                    At(136f, Of(EnemyType.Bastion, 2, AngleMode.OppositePair), Of(EnemyType.Devourer, 1)),
                    At(146f, Of(EnemyType.Hive, 1), Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Phantom, 2)),
                    At(156f, Of(EnemyType.Parasite, 2, AngleMode.Spread90), Of(EnemyType.Jelly, 3, AngleMode.Even)),
                    At(164f, Of(EnemyType.Shooter, 2, AngleMode.OppositePair), Of(EnemyType.Bastion, 1)),
                    At(172f, Of(EnemyType.Arrow, 4, AngleMode.Even), Of(EnemyType.Jelly, 2, AngleMode.OppositePair), Of(EnemyType.Swarmer, 10)),
                },
            },
            new EraDef
            {
                kExp = 0.95f, spinMul = 1.1f, hpMul = 2.9f, countMul = 1.6f, ambientInterval = 2.8f, dustMul = 2.4f, riseMul = 1f, fallMul = 1f,
                rule = EraRule.Moons, boss = EnemyType.Mirror, herald = EnemyType.Bumper, eliteChance = 0.08f, music = MusicId.Chapter3, victoryDust = 350, firstWinDust = 1200,
                background = new Color32(0x10, 0x0C, 0x07, 0xFF), starTint = new Color32(0xFF, 0xD9, 0xA0, 0xFF), raysTint = new Color32(0xFF, 0xD9, 0xA0, 0xFF),
                opening = new[]
                {
                    At(2f, Of(EnemyType.Swarmer, 4)),
                    At(8f, Of(EnemyType.Mine, 2, AngleMode.OppositePair)),
                    At(14f, Of(EnemyType.Bumper, 1)),
                },
                timeline = new[]
                {
                    At(20f, Of(EnemyType.Swarmer, 8)),
                    At(28f, Of(EnemyType.Mine, 3, AngleMode.Even)),
                    At(36f, Of(EnemyType.Bastion, 1), Of(EnemyType.Bumper, 1, AngleMode.Opposite)),
                    At(46f, Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Mine, 2)),
                    At(56f, Of(EnemyType.Swarmer, 10), Of(EnemyType.Bumper, 2, AngleMode.OppositePair)),
                    At(62f, Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Jelly, 1)),
                    At(70f, Of(EnemyType.Parasite, 1), Of(EnemyType.Mine, 3, AngleMode.Even)),
                    At(80f, Of(EnemyType.Bastion, 1), Of(EnemyType.Swarmer, 8, AngleMode.Opposite), Of(EnemyType.Bumper, 1)),
                    At(88f, Of(EnemyType.Orbiter, 3, AngleMode.Even)),
                    At(98f, Of(EnemyType.Shooter, 1), Of(EnemyType.Mine, 2, AngleMode.Opposite)),
                    At(108f, Of(EnemyType.Arrow, 3, AngleMode.Even), Of(EnemyType.Bumper, 2)),
                    At(116f, Of(EnemyType.Hive, 1), Of(EnemyType.Shielder, 2, AngleMode.OppositePair)),
                    At(126f, Of(EnemyType.Swarmer, 11), Of(EnemyType.Mine, 4, AngleMode.Even)),
                    At(136f, Of(EnemyType.Bastion, 2, AngleMode.OppositePair), Of(EnemyType.Bumper, 2, AngleMode.OppositePair)),
                    At(146f, Of(EnemyType.Hive, 1), Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Jelly, 2)),
                    At(156f, Of(EnemyType.Parasite, 2, AngleMode.Spread90), Of(EnemyType.Mine, 3, AngleMode.Even)),
                    At(164f, Of(EnemyType.Shooter, 2, AngleMode.OppositePair), Of(EnemyType.Bastion, 1), Of(EnemyType.Bumper, 2)),
                    At(172f, Of(EnemyType.Arrow, 4, AngleMode.Even), Of(EnemyType.Mine, 4, AngleMode.Even), Of(EnemyType.Swarmer, 10)),
                },
            },
            new EraDef
            {
                kExp = 1f, spinMul = 1.15f, hpMul = 3.2f, countMul = 1.65f, ambientInterval = 2.7f, dustMul = 2.7f, riseMul = 1f, fallMul = 1f,
                rule = EraRule.Anomalies, boss = EnemyType.Graviton, herald = EnemyType.Blinker, eliteChance = 0.1f, music = MusicId.Chapter4, victoryDust = 400, firstWinDust = 1400,
                background = new Color32(0x06, 0x10, 0x0E, 0xFF), starTint = new Color32(0xC8, 0xFF, 0xF0, 0xFF), raysTint = new Color32(0xC8, 0xFF, 0xF0, 0xFF),
                opening = new[]
                {
                    At(2f, Of(EnemyType.Swarmer, 4)),
                    At(8f, Of(EnemyType.Blinker, 2, AngleMode.OppositePair)),
                    At(14f, Of(EnemyType.Bastion, 1)),
                },
                timeline = new[]
                {
                    At(20f, Of(EnemyType.Swarmer, 8)),
                    At(28f, Of(EnemyType.Blinker, 3, AngleMode.Even)),
                    At(36f, Of(EnemyType.Bastion, 1), Of(EnemyType.Orbiter, 2, AngleMode.Opposite)),
                    At(46f, Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Blinker, 2)),
                    At(56f, Of(EnemyType.Swarmer, 10), Of(EnemyType.Mine, 2, AngleMode.OppositePair)),
                    At(62f, Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Shielder, 1), Of(EnemyType.Blinker, 1)),
                    At(70f, Of(EnemyType.Parasite, 1), Of(EnemyType.Bumper, 2, AngleMode.Opposite)),
                    At(80f, Of(EnemyType.Bastion, 1), Of(EnemyType.Swarmer, 8, AngleMode.Opposite)),
                    At(88f, Of(EnemyType.Blinker, 4, AngleMode.Even)),
                    At(98f, Of(EnemyType.Shooter, 1), Of(EnemyType.Phantom, 3, AngleMode.Even)),
                    At(108f, Of(EnemyType.Arrow, 3, AngleMode.Even), Of(EnemyType.Blinker, 2)),
                    At(116f, Of(EnemyType.Hive, 1), Of(EnemyType.Devourer, 1, AngleMode.Opposite)),
                    At(126f, Of(EnemyType.Swarmer, 11), Of(EnemyType.Blinker, 3, AngleMode.Even)),
                    At(136f, Of(EnemyType.Bastion, 2, AngleMode.OppositePair), Of(EnemyType.Jelly, 2, AngleMode.OppositePair)),
                    At(146f, Of(EnemyType.Hive, 1), Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Blinker, 2)),
                    At(156f, Of(EnemyType.Parasite, 2, AngleMode.Spread90), Of(EnemyType.Orbiter, 3, AngleMode.Even)),
                    At(164f, Of(EnemyType.Shooter, 2, AngleMode.OppositePair), Of(EnemyType.Bastion, 1), Of(EnemyType.Blinker, 2)),
                    At(172f, Of(EnemyType.Arrow, 4, AngleMode.Even), Of(EnemyType.Blinker, 4, AngleMode.Even), Of(EnemyType.Swarmer, 10)),
                },
            },
            new EraDef
            {
                kExp = 1f, spinMul = 1.15f, hpMul = 3.5f, countMul = 1.7f, ambientInterval = 2.6f, dustMul = 3f, riseMul = 1f, fallMul = 1f,
                rule = EraRule.Darkness, boss = EnemyType.Abyss, herald = EnemyType.Eclipser, eliteChance = 0.12f, music = MusicId.Chapter4, victoryDust = 500, firstWinDust = 2000,
                background = new Color32(0x00, 0x00, 0x00, 0xFF), starTint = Color.white, raysTint = new Color32(0xD6, 0x28, 0x1F, 0xFF),
                opening = new[]
                {
                    At(2f, Of(EnemyType.Swarmer, 4)),
                    At(8f, Of(EnemyType.Phantom, 2, AngleMode.OppositePair)),
                    At(14f, Of(EnemyType.Eclipser, 1)),
                },
                timeline = new[]
                {
                    At(20f, Of(EnemyType.Swarmer, 8)),
                    At(28f, Of(EnemyType.Phantom, 3, AngleMode.Even)),
                    At(36f, Of(EnemyType.Bastion, 1), Of(EnemyType.Eclipser, 1, AngleMode.Opposite)),
                    At(46f, Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Blinker, 2)),
                    At(56f, Of(EnemyType.Swarmer, 10), Of(EnemyType.Jelly, 2, AngleMode.OppositePair)),
                    At(62f, Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Mine, 2)),
                    At(70f, Of(EnemyType.Parasite, 1), Of(EnemyType.Eclipser, 1, AngleMode.Opposite)),
                    At(80f, Of(EnemyType.Bastion, 1), Of(EnemyType.Swarmer, 8, AngleMode.Opposite), Of(EnemyType.Bumper, 1)),
                    At(88f, Of(EnemyType.Phantom, 4, AngleMode.Even)),
                    At(98f, Of(EnemyType.Shooter, 1), Of(EnemyType.Devourer, 1, AngleMode.Opposite)),
                    At(108f, Of(EnemyType.Arrow, 3, AngleMode.Even), Of(EnemyType.Eclipser, 1)),
                    At(116f, Of(EnemyType.Hive, 1), Of(EnemyType.Blinker, 2, AngleMode.Opposite)),
                    At(126f, Of(EnemyType.Swarmer, 11), Of(EnemyType.Phantom, 3, AngleMode.Even)),
                    At(136f, Of(EnemyType.Bastion, 2, AngleMode.OppositePair), Of(EnemyType.Eclipser, 1)),
                    At(146f, Of(EnemyType.Hive, 1), Of(EnemyType.Arrow, 2, AngleMode.Spread90), Of(EnemyType.Orbiter, 2)),
                    At(156f, Of(EnemyType.Parasite, 2, AngleMode.Spread90), Of(EnemyType.Phantom, 4, AngleMode.Even)),
                    At(164f, Of(EnemyType.Shooter, 2, AngleMode.OppositePair), Of(EnemyType.Eclipser, 2, AngleMode.OppositePair)),
                    At(172f, Of(EnemyType.Arrow, 4, AngleMode.Even), Of(EnemyType.Phantom, 6, AngleMode.Even), Of(EnemyType.Swarmer, 10)),
                },

            },
        };

        [Title("Режимы")]
        // Бесконечный: расписание эры повторяется циклами этой длины, босс приходит каждый цикл
        public float endlessLoopTime = 160f;
        // прибавка за каждый цикл: HP врагов, их число, HP босса
        public float endlessHpStep = 0.35f;
        public float endlessCountStep = 0.2f;
        public float endlessBossHpStep = 0.5f;
        // фоновый поток с каждым циклом чаще, но не чаще этой доли обычного интервала
        public float endlessAmbientStep = 0.1f;
        public float endlessAmbientMin = 0.5f;
        // пыль за каждого убитого босса, умножается на множитель эры
        public int endlessBossDust = 60;
        // День: модификатор дня = dailyMods[дней с 1970 года % длину списка]
        public RunModifier[] dailyMods =
        {
            RunModifier.DoubleArrows, RunModifier.FastEnemies, RunModifier.DoubleHeat, RunModifier.SwarmOnly, RunModifier.GlassStar, RunModifier.HighOrbit,
        };
        // ОДИНОЧКА, СТЕКЛЯННАЯ ЗВЕЗДА, ВЫСОКАЯ ОРБИТА, РОЙ, БЕЗ СПОСОБНОСТЕЙ, ЖАРА, ВСТРЕЧНЫЙ ВЕТЕР, МАРАФОН
        public ChallengeDef[] challenges =
        {
            new ChallengeDef { era = 1, mods = RunModifier.SingleSat, rewardDust = 300 },
            new ChallengeDef { era = 1, mods = RunModifier.GlassStar, rewardSkin = SkinId.Sunflower, hasSkin = true },
            new ChallengeDef { era = 1, mods = RunModifier.HighOrbit, rewardDust = 300 },
            new ChallengeDef { era = 1, mods = RunModifier.SwarmOnly, rewardDust = 300 },
            new ChallengeDef { era = 2, mods = RunModifier.NoAbilities, rewardDust = 400 },
            new ChallengeDef { era = 2, mods = RunModifier.DoubleHeat, rewardSkin = SkinId.RedStar, hasSkin = true },
            new ChallengeDef { era = 3, mods = RunModifier.AutoReverse, rewardDust = 500 },
            new ChallengeDef { era = 3, mods = RunModifier.Marathon, rewardDust = 600 },
        };

        [Title("Модификаторы")]
        public float glassStarHpMul = 0.4f;
        public float highOrbitMin = 3f;
        // «Рой»: сколько Роя приходит вместо каждого крупного врага
        public int swarmOnlyPack = 3;
        public float doubleHeatMul = 2f;
        public float autoReverseInterval = 8f;
        // «Марафон»: босс позже, а до него события расписания от marathonReplayFrom идут второй раз со сдвигом marathonShift
        public float marathonBossTime = 300f;
        public float marathonReplayFrom = 60f;
        public float marathonShift = 120f;
        public float marathonHpMul = 1.3f;
        public float fastEnemiesMul = 1.2f;
        public int doubleArrowsMul = 2;

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
        public BlueprintDef[] blueprints =
        {
            Node(BlueprintId.Hull, BlueprintBranch.Star, 1, StatId.StarMaxHp, 8f, 40, 70, 110, 160, 220),
            Node(BlueprintId.Core, BlueprintBranch.Star, 2, StatId.Regen, 0.07f, 120, 200, 300),
            Node(BlueprintId.Reflector, BlueprintBranch.Star, 3, StatId.StarDamageMul, 0.95f, 200, 320, 480),
            Node(BlueprintId.SecondSun, BlueprintBranch.Star, 4, StatId.SecondSun, 1f, 1200),
            Node(BlueprintId.Forge, BlueprintBranch.Strike, 1, StatId.PowerMul, 1.03f, 40, 70, 100, 140, 190),
            Node(BlueprintId.TuningFork, BlueprintBranch.Strike, 2, StatId.ResonanceBonus, 0.1f, 120, 200, 300),
            Node(BlueprintId.Edge, BlueprintBranch.Strike, 3, StatId.CritChance, 0.03f, 200, 320, 480),
            Node(BlueprintId.Twin, BlueprintBranch.Strike, 4, StatId.Satellites, 1f, 1200),
            Node(BlueprintId.Thrust, BlueprintBranch.Orbit, 1, StatId.RadialSpeedMul, 1.05f, 30, 60, 90, 130),
            Node(BlueprintId.Radiator, BlueprintBranch.Orbit, 2, StatId.HeatMul, 0.94f, 80, 130, 190, 270),
            Node(BlueprintId.Capacitor, BlueprintBranch.Orbit, 3, StatId.AbilityCooldownMul, 0.95f, 160, 240, 340, 450),
            Node(BlueprintId.Quadriga, BlueprintBranch.Orbit, 4, StatId.AddSatBonus, 1f, 1400),
            Node(BlueprintId.Magnetism, BlueprintBranch.Crystal, 1, StatId.MagnetRadius, 0.3f, 30, 50, 80, 120),
            Node(BlueprintId.Insight, BlueprintBranch.Crystal, 2, StatId.XpNeedMul, 0.95f, 120, 200, 300),
            Node(BlueprintId.Alchemy, BlueprintBranch.Crystal, 3, StatId.DustMul, 1.08f, 240, 400, 600),
            // ВЫБОР: у двух уровней разные эффекты
            new BlueprintDef
            {
                id = BlueprintId.Choice, branch = BlueprintBranch.Crystal, tier = 4,
                levels = new[]
                {
                    new BlueprintLevel(700, new StatMod(StatId.FreeRerolls, 1f)),
                    new BlueprintLevel(1100, new StatMod(StatId.ExtraChoices, 1f)),
                },
            },
        };
        // чертежи R2.1 в старом порядке: КОРПУС, КОНДЕНСАТОР, ОХЛАДИТЕЛЬ, МАГНЕТИЗМ
        public LegacyBlueprint[] legacyBlueprintCosts =
        {
            new LegacyBlueprint { costs = new[] { 50, 100, 200, 400, 800 } },
            new LegacyBlueprint { costs = new[] { 50, 100, 200, 400, 800 } },
            new LegacyBlueprint { costs = new[] { 50, 100, 200, 400, 800 } },
            new LegacyBlueprint { costs = new[] { 40, 80, 160 } },
        };

        [Title("Облик")]
        [TableList]
        [Title("Классы звёзд")]
        // радиус, HP и rMin звезды берутся из класса забега; обучение всегда ЖЁЛТЫЙ КАРЛИК
        public StarClassDef[] starClasses =
        {
            new StarClassDef { id = StarClassId.Yellow, starRadius = 1f, maxHp = 120, rMin = 1.35f, mods = new StatMod[0], tint = Color.white },
            new StarClassDef
            {
                id = StarClassId.RedGiant, starRadius = 1.35f, maxHp = 190, rMin = 1.7f, tint = new Color32(0xFF, 0x8A, 0x6A, 0xFF), eraWin = 2,
                mods = new[] { new StatMod(StatId.SpinMul, 0.9f), new StatMod(StatId.StarDamageMul, 0.85f), new StatMod(StatId.Thorns, 1f) },
            },
            new StarClassDef
            {
                id = StarClassId.WhiteDwarf, starRadius = 0.7f, maxHp = 80, rMin = 1f, trait = StarTrait.Glint, tint = new Color32(0xDD, 0xE8, 0xFF, 0xFF), eraWin = 4,
                mods = new[] { new StatMod(StatId.SpinMul, 1.2f), new StatMod(StatId.PowerMul, 1.15f), new StatMod(StatId.HeatMul, 1.25f) },
            },
            new StarClassDef
            {
                id = StarClassId.Neutron, starRadius = 0.8f, maxHp = 110, rMin = 1.2f, trait = StarTrait.AutoPulse, tint = new Color32(0xB9, 0xBD, 0xC2, 0xFF), eraWin = 6,
                mods = new[] { new StatMod(StatId.XpNeedMul, 1.1f) },
            },
            new StarClassDef { id = StarClassId.Cepheid, starRadius = 1f, maxHp = 130, rMin = 1.35f, mods = new StatMod[0], trait = StarTrait.Breath, tint = new Color32(0xFF, 0xE7, 0xA0, 0xFF), price = 1500 },
        };
        // ШИПЫ: враги ближе радиуса звезды + thornsGap теряют thornsDps HP/с за каждый стак
        public float thornsGap = 0.6f;
        public float thornsDps = 1f;
        // Блеск (Белый карлик): шанс крита выше у спутника с нагревом от glintHeat
        public float glintCrit = 0.1f;
        public float glintHeat = 0.5f;
        // Автоимпульс (Нейтронная): волна от звезды; Рой и Стрелы гибнут, остальные получают урон, без отброса
        public float autoPulseInterval = 10f;
        public float autoPulseRadius = 2.4f;
        public float autoPulseDamage = 1.5f;
        // Дыхание (Цефеида): цикл breathCycle, первые breathBright секунд яркая фаза, потом тусклая
        public float breathCycle = 12f;
        public float breathBright = 6f;
        // яркая фаза: радиус звезды растёт за breathGrowTime, лечение и удар спутников меняются
        public float breathRadiusMul = 1.25f;
        public float breathGrowTime = 0.5f;
        public float breathRegen = 1f;
        public float breathBrightPower = 0.9f;
        public float breathDimPower = 1.3f;

        public SkinDef[] skins =
        {
            new SkinDef { id = SkinId.Romb, kind = SkinKind.Satellite },
            new SkinDef { id = SkinId.Sputnik, kind = SkinKind.Satellite, price = 300 },
            new SkinDef { id = SkinId.Gear, kind = SkinKind.Satellite, price = 400 },
            new SkinDef { id = SkinId.Moon, kind = SkinKind.Satellite, eraWin = 2 },
            new SkinDef { id = SkinId.RedStar, kind = SkinKind.Satellite, challenge = 6 },
            new SkinDef { id = SkinId.Eye, kind = SkinKind.Satellite, eraWin = 4 },
            new SkinDef { id = SkinId.Sun, kind = SkinKind.Star },
            new SkinDef { id = SkinId.Oko, kind = SkinKind.Star, price = 400 },
            new SkinDef { id = SkinId.Sunflower, kind = SkinKind.Star, challenge = 2 },
            new SkinDef { id = SkinId.Motor, kind = SkinKind.Star, eraWin = 3 },
        };

        // узел дерева, у которого все уровни дают одно и то же
        static BlueprintDef Node(BlueprintId id, BlueprintBranch branch, int tier, StatId stat, float value, params int[] costs)
        {
            var levels = new BlueprintLevel[costs.Length];
            for (int i = 0; i < costs.Length; i++) levels[i] = new BlueprintLevel(costs[i], new StatMod(stat, value));
            return new BlueprintDef { id = id, branch = branch, tier = tier, levels = levels };
        }

        static CardDef Card(CardId id, CardColor color, CardRarity rarity, int maxStacks, StatId plus, float plusValue, StatId minus, float minusValue, int era = 0)
        {
            return new CardDef { id = id, color = color, rarity = rarity, era = era, maxStacks = maxStacks, plus = new StatMod(plus, plusValue), minus = new StatMod(minus, minusValue) };
        }

        static CardDef Evolution(CardId id, StatId plus, CardId sourceA, CardId sourceB)
        {
            return new CardDef { id = id, color = CardColor.Evolution, rarity = CardRarity.Rare, maxStacks = 1, plus = new StatMod(plus, 1f), sourceA = sourceA, sourceB = sourceB };
        }

        static RelicDef Relic(RelicId id, int maxStacks, params StatMod[] mods)
        {
            return new RelicDef { id = id, maxStacks = maxStacks, mods = mods };
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
