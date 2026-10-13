using UnityEngine;

namespace Perigee
{
    // Дерево чертежей: покупка уровней за пыль. Эффект купленных уровней считает Stats.
    public class Blueprints
    {
        const float Percent = 100f;

        readonly GameConfig config;
        readonly ISaveService save;

        public Blueprints(GameConfig config, ISaveService save)
        {
            this.config = config;
            this.save = save;
        }

        // i = индекс чертежа в config.blueprints
        public int Level(int i)
        {
            return save.Data.blueprints[i];
        }

        public bool Maxed(int i)
        {
            return Level(i) >= config.blueprints[i].levels.Length;
        }

        // цена следующего уровня
        public int Cost(int i)
        {
            return config.blueprints[i].levels[Level(i)].cost;
        }

        // узел первого яруса открыт сразу, остальные после первого уровня узла ярусом ниже
        public bool Open(int i)
        {
            int previous = Previous(i);
            return previous < 0 || Level(previous) > 0;
        }

        // узел той же ветки ярусом ниже; у первого яруса его нет: -1
        public int Previous(int i)
        {
            BlueprintDef[] defs = config.blueprints;
            for (int k = 0; k < defs.Length; k++)
                if (defs[k].branch == defs[i].branch && defs[k].tier == defs[i].tier - 1) return k;
            return -1;
        }

        // с debugUnlockAll пыль не тратится
        public bool CanBuy(int i)
        {
            return !Maxed(i) && Open(i) && (config.debugUnlockAll || save.Data.dust >= Cost(i));
        }

        public void Buy(int i)
        {
            if (!config.debugUnlockAll) save.Data.dust -= Cost(i);
            save.Data.blueprints[i]++;
            save.Save();
        }

        // число для строки «Сейчас …»: что купленные уровни дают вместе
        public float Total(int i)
        {
            BlueprintLevel[] levels = config.blueprints[i].levels;
            float sum = 0f;
            float product = 1f;
            for (int l = 0; l < Level(i); l++)
            {
                sum += levels[l].mod.value;
                product *= levels[l].mod.value;
            }

            switch (levels[0].mod.stat)
            {
                case StatId.StarMaxHp:
                case StatId.Regen:
                case StatId.MagnetRadius:
                    return sum;
                case StatId.CritChance:
                    return sum * Percent;
                case StatId.ResonanceBonus:
                    return config.resonanceMul + sum;
                // множители: на сколько процентов они вместе меняют величину
                default:
                    return Mathf.Abs(product - 1f) * Percent;
            }
        }
    }
}
