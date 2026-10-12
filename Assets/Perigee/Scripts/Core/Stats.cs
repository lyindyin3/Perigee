using UnityEngine;

namespace Perigee
{
    public class Stats
    {
        public float radialSpeedMul;
        public float spinMul;
        public float powerMul;
        public float powerFarBonus;
        // множитель удара спутников и теней, пока открыто окно способности
        public float hitMul;
        public float heatMul;
        public float resonanceAngleMul;
        public float satRadiusMul;
        public float crystalLifeMul;
        public float arrowSpeedMul;
        public float abilityCooldownMul;
        public float abilityDurationMul;
        public float magnetRadius;
        public float starMaxHp;
        public int extraSatellites;
        public int beacons;

        // база × карточки × чертежи × активные способности: множители перемножаются, бонусы складываются
        public void Recompute(GameConfig config, EraConfig era, int[] cardStacks, int[] blueprintLevels, AbilitySystem abilities)
        {
            radialSpeedMul = 1f;
            spinMul = era.spinMul;
            powerMul = 1f;
            powerFarBonus = 0f;
            hitMul = 1f;
            heatMul = 1f;
            resonanceAngleMul = 1f;
            satRadiusMul = 1f;
            crystalLifeMul = 1f;
            arrowSpeedMul = 1f;
            abilityCooldownMul = 1f;
            abilityDurationMul = 1f;
            magnetRadius = 0f;
            starMaxHp = config.starMaxHp;
            extraSatellites = 0;
            beacons = 0;

            CardDef[] cards = config.cards;
            for (int i = 0; i < cards.Length; i++)
            {
                for (int s = 0; s < cardStacks[i]; s++)
                {
                    Apply(cards[i].plus);
                    Apply(cards[i].minus);
                }
            }

            // чертежи: эффект за каждый купленный уровень
            BlueprintDef[] blueprints = config.blueprints;
            for (int i = 0; i < blueprints.Length; i++)
                for (int l = 0; l < blueprintLevels[i]; l++) Apply(blueprints[i].perLevel);

            // временные множители способностей, отдельно от карточек
            satRadiusMul *= Mathf.Lerp(1f, config.growRadiusMul, abilities.GrowBlend);
            if (abilities.Active(AbilityId.Grow)) resonanceAngleMul *= config.growResonanceMul;
            if (abilities.Active(AbilityId.Reverse)) hitMul *= config.reverseHitMul;
        }

        void Apply(StatMod mod)
        {
            switch (mod.stat)
            {
                case StatId.PowerMul: powerMul *= mod.value; break;
                case StatId.RadialSpeedMul: radialSpeedMul *= mod.value; break;
                case StatId.HeatMul: heatMul *= mod.value; break;
                case StatId.SpinMul: spinMul *= mod.value; break;
                case StatId.ResonanceAngleMul: resonanceAngleMul *= mod.value; break;
                case StatId.SatRadiusMul: satRadiusMul *= mod.value; break;
                case StatId.CrystalLifeMul: crystalLifeMul *= mod.value; break;
                case StatId.ArrowSpeedMul: arrowSpeedMul *= mod.value; break;
                case StatId.AbilityCooldownMul: abilityCooldownMul *= mod.value; break;
                case StatId.AbilityDurationMul: abilityDurationMul *= mod.value; break;
                case StatId.MagnetRadius: magnetRadius += mod.value; break;
                case StatId.PowerFarBonus: powerFarBonus += mod.value; break;
                case StatId.StarMaxHp: starMaxHp += mod.value; break;
                case StatId.Satellites: extraSatellites += (int)mod.value; break;
                case StatId.Beacon: beacons += (int)mod.value; break;
            }
        }
    }
}
