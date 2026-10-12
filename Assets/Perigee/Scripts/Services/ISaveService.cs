using System;

namespace Perigee
{
    // по порядку значений SettingsData.hpRings
    public enum HpRingMode { AfterHit, Always, Never }

    [Serializable]
    public class SettingsData
    {
        public float sfxVolume = 1f;
        public float droneVolume = 0.6f;
        public bool shake = true;
        public bool damageNumbers = true;
        // кольца HP врагов: номер HpRingMode
        public int hpRings;
        public bool simpleBackground;
    }

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
        // бит на каждый AbilityId; ноль = сохранение старше M10, SaveService ставит стартовый набор
        public int abilitiesOwned;
        public int lastAbilityA;
        // дата UTC (yyyyMMdd) дневного забега, который уже получил двойную пыль
        public int dailyBonusDate;
        public SettingsData settings = new SettingsData();
    }

    public interface ISaveService
    {
        SaveData Data { get; }

        // вызывать после каждой правки Data
        void Save();
    }
}
