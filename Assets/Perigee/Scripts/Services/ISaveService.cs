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
        // меньше SaveService.Version = сохранение R2.1: при загрузке оно переносится
        public int version;
        public int dust;
        // уровни по индексу чертежа в config.blueprints
        public int[] blueprints;
        // бит на каждый AbilityId
        public int abilitiesOwned;
        // бит на каждый SkinId
        public int skinsOwned;
        // выбранные облики: номера SkinId
        public int satelliteSkin;
        public int starSkin;
        public int erasUnlocked = 1;
        // бит на каждую эру по индексу в config.eras: в ней уже побеждали в режиме Забег
        public int erasWon;
        // лучшее время по эрам: режим Забег и Бесконечный
        public float[] bestTimes;
        public float[] bestEndless;
        public int bestDaily;
        // дата UTC (yyyyMMdd) дневного забега, который уже получил двойную пыль
        public int dailyBonusDate;
        // бит на каждое испытание
        public int challengesDone;
        // бит на каждый EnemyType: подсказка о первой встрече уже была
        public int seenEnemies;
        public bool tutorialDone;
        public int runs;
        public int defeats;
        public int totalKills;
        public int lastAbilityA;
        public int lastEra;
        // номер RunMode
        public int lastMode;
        public SettingsData settings = new SettingsData();
        // поле R2.1: его читает только перенос
        public float bestTime;
    }

    public interface ISaveService
    {
        SaveData Data { get; }

        // вызывать после каждой правки Data
        void Save();

        // сброс прогресса: новое сохранение, настройки остаются
        void Reset();
    }
}
