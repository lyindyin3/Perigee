using System;

namespace Perigee
{
    [Serializable]
    public class SaveData
    {
        public int dust;
        // уровни по индексу чертежа в config.blueprints
        public int[] blueprints;
        public float bestTime;
        public int bestDaily;
        public bool tutorialDone;
        public int erasUnlocked = 1;
        public int runs;
        public int defeats;
        public int lastAbilityA;
        // дата UTC (yyyyMMdd) дневного забега, который уже получил двойную пыль
        public int dailyBonusDate;
    }

    public interface ISaveService
    {
        SaveData Data { get; }

        // вызывать после каждой правки Data
        void Save();
    }
}
