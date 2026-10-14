using UnityEngine;

namespace Perigee
{
    public class Stats
    {
        // флаг включён, когда его карточки набрали от 1
        const float FlagOn = 1f;

        public float radialSpeedMul;
        // множитель скорости только на выходе наружу (минус ПРАЩИ)
        public float radialOutMul;
        public float spinMul;
        public float powerMul;
        public float powerFarBonus;
        // множитель удара спутников и теней, пока открыто окно способности или карточки
        public float hitMul;
        public float heatMul;
        public float resonanceAngleMul;
        // множитель удара в резонансе: базовый и бонус КАМЕРТОНА
        public float resonanceMul;
        public float satRadiusMul;
        public float crystalLifeMul;
        public float arrowSpeedMul;
        // скорость всех врагов (модификатор «Спешка»)
        public float enemySpeedMul;
        public float abilityCooldownMul;
        public float abilityDurationMul;
        public float xpPerCrystalMul;
        public float xpNeedMul;
        public float starDamageMul;
        public float dustMul;
        public float magnetRadius;
        public float starMaxHp;
        public float regen;
        public float critChance;
        public float armorPierce;
        public int extraSatellites;
        // сколько спутников сверх обычного предела можно взять (КВАДРИГА)
        public int addSatBonus;
        public int beacons;
        // чертёж ВЫБОР: бесплатные замены карточек за забег и лишние карточки на экране выбора
        public int freeRerolls;
        public int extraChoices;

        // числа после эволюций (и реликвий): системы берут их отсюда, а не из конфига
        public float critMul;
        public float sawDamage;
        public float sawInterval;
        public float burnDps;
        public float executeFraction;
        public float beaconRadius;
        public float beaconDamage;
        // ПРОТУБЕРАНЕЦ: каждая волна маяка лечит звезду
        public float beaconHeal;
        public float stunTime;
        // ПЕРЕГРУЗКА: взрыв у оглушённого спутника; РЕАКТОР его усиливает
        public float overloadRadius;
        public float overloadDamage;

        float execute;
        float sawThread;
        float burn;
        float chain;
        float heatproof;
        float solarSail;
        float firebird;
        float slingshot;
        float anchorField;
        float crowbar;
        float horizon;
        float darkMatter;
        float accretion;
        float secondSun;

        public bool Execute => execute >= FlagOn;
        public bool SawThread => sawThread >= FlagOn;
        public bool Burn => burn >= FlagOn;
        public bool Chain => chain >= FlagOn;
        public bool Heatproof => heatproof >= FlagOn;
        public bool Firebird => firebird >= FlagOn;
        public bool AnchorField => anchorField >= FlagOn;
        public bool Crowbar => crowbar >= FlagOn;
        public bool Horizon => horizon >= FlagOn;
        public bool DarkMatter => darkMatter >= FlagOn;
        public bool Accretion => accretion >= FlagOn;
        public bool SecondSun => secondSun >= FlagOn;

        // статы R4 (от Shockwave и дальше): просто суммы стаков
        readonly float[] sums = new float[System.Enum.GetValues(typeof(StatId)).Length];

        public float Sum(StatId id)
        {
            return sums[(int)id];
        }

        public bool Has(StatId id)
        {
            return sums[(int)id] >= FlagOn;
        }

        // база × эра × карточки × чертежи × модификаторы забега × активные способности: множители перемножаются, бонусы складываются.
        // slinging = идёт окно ПРАЩИ после отпускания, sailing = идёт окно СОЛНЕЧНОГО ПАРУСА вокруг вспышки.
        public void Recompute(GameConfig config, EraDef era, RunModifier mods, int[] cardStacks, int[] blueprintLevels, AbilitySystem abilities, bool slinging, bool sailing)
        {
            radialSpeedMul = 1f;
            radialOutMul = 1f;
            spinMul = era.spinMul;
            powerMul = 1f;
            powerFarBonus = 0f;
            hitMul = 1f;
            heatMul = 1f;
            resonanceAngleMul = 1f;
            resonanceMul = config.resonanceMul;
            satRadiusMul = 1f;
            crystalLifeMul = 1f;
            arrowSpeedMul = 1f;
            enemySpeedMul = 1f;
            abilityCooldownMul = 1f;
            abilityDurationMul = 1f;
            xpPerCrystalMul = 1f;
            xpNeedMul = 1f;
            starDamageMul = 1f;
            dustMul = 1f;
            magnetRadius = 0f;
            starMaxHp = config.starMaxHp;
            regen = 0f;
            critChance = 0f;
            armorPierce = 0f;
            extraSatellites = 0;
            addSatBonus = 0;
            beacons = 0;
            freeRerolls = 0;
            extraChoices = 0;
            execute = 0f;
            sawThread = 0f;
            burn = 0f;
            chain = 0f;
            heatproof = 0f;
            solarSail = 0f;
            firebird = 0f;
            slingshot = 0f;
            anchorField = 0f;
            crowbar = 0f;
            horizon = 0f;
            darkMatter = 0f;
            accretion = 0f;
            secondSun = 0f;
            System.Array.Clear(sums, 0, sums.Length);

            CardDef[] cards = config.cards;
            for (int i = 0; i < cards.Length; i++)
            {
                for (int s = 0; s < cardStacks[i]; s++)
                {
                    Apply(cards[i].plus);
                    // у эволюции минуса нет
                    if (cards[i].color != CardColor.Evolution) Apply(cards[i].minus);
                }
            }

            // чертежи: эффект каждого купленного уровня
            BlueprintDef[] blueprints = config.blueprints;
            for (int i = 0; i < blueprints.Length; i++)
                for (int l = 0; l < blueprintLevels[i]; l++) Apply(blueprints[i].levels[l].mod);

            // эволюции: свои числа поверх конфига
            bool hammer = Has(StatId.Hammer);
            critMul = (hammer ? config.hammerCritMul : config.critMul) + Sum(StatId.CritMulBonus);
            bool stringSaw = Has(StatId.String);
            sawDamage = stringSaw ? config.sawDamage * config.stringSawMul : config.sawDamage;
            sawInterval = stringSaw ? config.stringSawInterval : config.sawInterval;
            burnDps = Has(StatId.Wildfire) ? config.wildfireBurnDps : config.burnDps;
            executeFraction = Has(StatId.Guillotine) ? config.guillotineFraction : config.executeFraction;
            bool prominence = Has(StatId.Prominence);
            beaconRadius = prominence ? config.prominenceRadius : config.beaconRadius;
            beaconDamage = prominence ? config.prominenceDamage : config.beaconDamage;
            beaconHeal = prominence ? config.prominenceHeal : 0f;
            bool reactor = Has(StatId.Reactor);
            stunTime = reactor ? config.stunTime * config.reactorStunMul : config.stunTime;
            overloadRadius = reactor ? config.reactorRadius : config.overloadRadius;
            overloadDamage = reactor ? config.reactorDamage : config.overloadDamage;
            if (Has(StatId.LivingStar))
            {
                regen *= config.livingStarRegenMul;
                starMaxHp += config.livingStarHp;
            }

            // модификаторы забега (День и испытания)
            if ((mods & RunModifier.DoubleHeat) != 0) heatMul *= config.doubleHeatMul;
            if ((mods & RunModifier.FastEnemies) != 0) enemySpeedMul *= config.fastEnemiesMul;
            if ((mods & RunModifier.GlassStar) != 0)
            {
                // стеклянная звезда: HP меньше и никакого лечения
                starMaxHp *= config.glassStarHpMul;
                regen = 0f;
            }
            // «Одиночка»: НОВЫЙ СПУТНИК и чертёж ПАРА ничего не дают
            if ((mods & RunModifier.SingleSat) != 0) extraSatellites = 0;

            // временные множители способностей, отдельно от карточек
            satRadiusMul *= Mathf.Lerp(1f, config.growRadiusMul, abilities.GrowBlend);
            if (abilities.Active(AbilityId.Grow)) resonanceAngleMul *= config.growResonanceMul;
            if (abilities.Active(AbilityId.Reverse)) hitMul *= config.reverseHitMul;

            // окна карточек эр: тоже временные множители удара
            if (slinging && slingshot >= FlagOn) hitMul *= config.slingshotMul;
            if (sailing && solarSail >= FlagOn) hitMul *= config.solarSailMul;
        }

        void Apply(StatMod mod)
        {
            switch (mod.stat)
            {
                case StatId.PowerMul: powerMul *= mod.value; break;
                case StatId.RadialSpeedMul: radialSpeedMul *= mod.value; break;
                case StatId.RadialOutMul: radialOutMul *= mod.value; break;
                case StatId.HeatMul: heatMul *= mod.value; break;
                case StatId.SpinMul: spinMul *= mod.value; break;
                case StatId.ResonanceAngleMul: resonanceAngleMul *= mod.value; break;
                case StatId.ResonanceBonus: resonanceMul += mod.value; break;
                case StatId.SatRadiusMul: satRadiusMul *= mod.value; break;
                case StatId.CrystalLifeMul: crystalLifeMul *= mod.value; break;
                case StatId.ArrowSpeedMul: arrowSpeedMul *= mod.value; break;
                case StatId.AbilityCooldownMul: abilityCooldownMul *= mod.value; break;
                case StatId.AbilityDurationMul: abilityDurationMul *= mod.value; break;
                case StatId.XpPerCrystalMul: xpPerCrystalMul *= mod.value; break;
                case StatId.XpNeedMul: xpNeedMul *= mod.value; break;
                case StatId.StarDamageMul: starDamageMul *= mod.value; break;
                case StatId.DustMul: dustMul *= mod.value; break;
                case StatId.MagnetRadius: magnetRadius += mod.value; break;
                case StatId.PowerFarBonus: powerFarBonus += mod.value; break;
                case StatId.StarMaxHp: starMaxHp += mod.value; break;
                case StatId.Regen: regen += mod.value; break;
                case StatId.Satellites: extraSatellites += (int)mod.value; break;
                case StatId.AddSatBonus: addSatBonus += (int)mod.value; break;
                case StatId.Beacon: beacons += (int)mod.value; break;
                case StatId.CritChance: critChance += mod.value; break;
                case StatId.ArmorPierce: armorPierce += mod.value; break;
                case StatId.Execute: execute += mod.value; break;
                case StatId.SawThread: sawThread += mod.value; break;
                case StatId.Burn: burn += mod.value; break;
                case StatId.Chain: chain += mod.value; break;
                case StatId.Heatproof: heatproof += mod.value; break;
                case StatId.SolarSail: solarSail += mod.value; break;
                case StatId.Firebird: firebird += mod.value; break;
                case StatId.Slingshot: slingshot += mod.value; break;
                case StatId.AnchorField: anchorField += mod.value; break;
                case StatId.Crowbar: crowbar += mod.value; break;
                case StatId.Horizon: horizon += mod.value; break;
                case StatId.DarkMatter: darkMatter += mod.value; break;
                case StatId.Accretion: accretion += mod.value; break;
                case StatId.SecondSun: secondSun += mod.value; break;
                case StatId.FreeRerolls: freeRerolls += (int)mod.value; break;
                case StatId.ExtraChoices: extraChoices += (int)mod.value; break;
                default: sums[(int)mod.stat] += mod.value; break;
            }
        }
    }
}
