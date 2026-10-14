using System;
using UnityEngine;

namespace Perigee
{
    public class SaveService : ISaveService
    {
        const string Key = "perigee_save";
        const int Version = 3;
        // сохранение R3
        const int VersionR3 = 2;
        // способности и облики, которые есть с начала игры
        const int StartAbilities = 1 << (int)AbilityId.Reverse | 1 << (int)AbilityId.Grow | 1 << (int)AbilityId.Pulse;
        const int StartSkins = 1 << (int)SkinId.Romb | 1 << (int)SkinId.Sun;
        const int StartClasses = 1 << (int)StarClassId.Yellow;
        // первые победы в этих эрах (номера в config.eras) дают классы КРАСНЫЙ ГИГАНТ и БЕЛЫЙ КАРЛИК
        const int RedGiantEra = 1;
        const int WhiteDwarfEra = 3;

        readonly GameConfig config;

        public SaveData Data { get; private set; }

        public SaveService(GameConfig config)
        {
            this.config = config;
            Data = Fresh();
            string json = PlayerPrefs.GetString(Key);
            // первый запуск: сохранения ещё нет
            if (json.Length == 0) return;

            // в сохранении R2.1 поля version нет: после чтения оно останется нулём
            Data.version = 0;
            // единственный try/catch в проекте: битое сохранение заменяется новым
            try
            {
                JsonUtility.FromJsonOverwrite(json, Data);
            }
            catch (Exception)
            {
                Data = Fresh();
            }

            if (Data.version >= Version) return;
            if (Data.version < VersionR3) MigrateR3();
            MigrateR4();
            Save();
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public void Reset()
        {
            SettingsData settings = Data.settings;
            Data = Fresh();
            Data.settings = settings;
            Save();
        }

        SaveData Fresh()
        {
            return new SaveData
            {
                version = Version,
                blueprints = new int[config.blueprints.Length],
                abilitiesOwned = StartAbilities,
                skinsOwned = StartSkins,
                satelliteSkin = (int)SkinId.Romb,
                starSkin = (int)SkinId.Sun,
                bestTimes = new float[config.maxEras],
                bestEndless = new float[config.maxEras],
                starClassesOwned = StartClasses,
            };
        }

        // Перенос сохранения R2.1: старые чертежи возвращаются пылью, остальное встаёт на места R3.
        void MigrateR3()
        {
            // полная цена каждого купленного уровня старого чертежа
            LegacyBlueprint[] legacy = config.legacyBlueprintCosts;
            for (int i = 0; i < legacy.Length; i++)
                for (int l = 0; l < Data.blueprints[i]; l++) Data.dust += legacy[i].costs[l];
            Data.blueprints = new int[config.blueprints.Length];

            Data.bestTimes[0] = Data.bestTime;
            // открытая эра значит победу во всех эрах до неё
            int won = Data.erasUnlocked - 1;
            Data.erasWon = (1 << won) - 1;
            Data.abilitiesOwned = StartAbilities;
            Data.skinsOwned = StartSkins;
            // облик за первую победу в уже пройденной эре иначе не получить: второй первой победы не будет
            SkinDef[] skins = config.skins;
            for (int i = 0; i < skins.Length; i++)
                if (skins[i].eraWin > 0 && skins[i].eraWin <= won) Data.skinsOwned |= 1 << (int)skins[i].id;
            // старые РОСТ и ИМПУЛЬС значат то же, остальные номера были ЯКОРЕМ, ХОЛОДОМ и ВСПЛЕСКОМ
            if (Data.lastAbilityA != (int)AbilityId.Grow && Data.lastAbilityA != (int)AbilityId.Pulse) Data.lastAbilityA = (int)AbilityId.Reverse;
            Data.version = VersionR3;
        }

        // Перенос сохранения R3: 8 эр, классы за уже пройденные эры, новые настройки со значениями по умолчанию.
        void MigrateR4()
        {
            System.Array.Resize(ref Data.bestTimes, config.maxEras);
            System.Array.Resize(ref Data.bestEndless, config.maxEras);
            Data.starClassesOwned = StartClasses;
            if ((Data.erasWon & 1 << RedGiantEra) != 0) Data.starClassesOwned |= 1 << (int)StarClassId.RedGiant;
            if ((Data.erasWon & 1 << WhiteDwarfEra) != 0) Data.starClassesOwned |= 1 << (int)StarClassId.WhiteDwarf;
            // старому игроку вступление не навязывается: его можно посмотреть из настроек
            Data.introSeen = Data.tutorialDone;

            SettingsData defaults = new SettingsData();
            SettingsData settings = Data.settings;
            settings.musicVolume = defaults.musicVolume;
            settings.vibration = defaults.vibration;
            settings.language = defaults.language;
            Data.version = Version;
        }
    }
}
