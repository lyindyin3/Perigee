namespace Perigee
{
    // Покупка чертежей за пыль. Эффект купленных уровней считает Stats.
    public class Blueprints
    {
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
            return Level(i) >= config.blueprints[i].costs.Length;
        }

        // цена следующего уровня
        public int Cost(int i)
        {
            return config.blueprints[i].costs[Level(i)];
        }

        public bool CanBuy(int i)
        {
            return !Maxed(i) && save.Data.dust >= Cost(i);
        }

        public void Buy(int i)
        {
            save.Data.dust -= Cost(i);
            save.Data.blueprints[i]++;
            save.Save();
        }
    }
}
