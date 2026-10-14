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
        public float musicVolume = 0.7f;
        // только на телефоне
        public bool vibration = true;
        // номер Loc.Lang; -1 = язык браузера, пока игрок не выбрал сам
        public int language = -1;
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
        // R4 (version 3)
        // бит на каждый StarClassId
        public int starClassesOwned;
        public int lastStarClass;
        // бит на каждый AchievementId
        public long achievements;
        // бит на каждый CardId: карточку уже предлагали
        public long seenCards;
        // бит на каждый RelicId
        public int seenRelics;
        public int evolutionsTaken;
        // бит на каждый StarClassId: с этим классом уже побеждали
        public int classesWon;
        public int bestCombo;
        public int eliteKills;
        public int heraldKills;
        public int goldComets;
        public int dailyPlayed;
        public int endlessBossBest;
        public bool introSeen;
        public bool endingSeen;
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
