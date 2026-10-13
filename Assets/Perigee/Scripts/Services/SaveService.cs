using System;
using UnityEngine;

namespace Perigee
{
    public class SaveService : ISaveService
    {
        const string Key = "perigee_save";
        const int Version = 2;
        // способности и облики, которые есть с начала игры
        const int StartAbilities = 1 << (int)AbilityId.Reverse | 1 << (int)AbilityId.Grow | 1 << (int)AbilityId.Pulse;
        const int StartSkins = 1 << (int)SkinId.Romb | 1 << (int)SkinId.Sun;

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

            if (Data.version < Version) Migrate();
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
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
                bestTimes = new float[config.eras.Length],
                bestEndless = new float[config.eras.Length],
            };
        }

        // Перенос сохранения R2.1: старые чертежи возвращаются пылью, остальное встаёт на места R3.
        void Migrate()
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
            Data.version = Version;
            Save();
        }
    }
}
