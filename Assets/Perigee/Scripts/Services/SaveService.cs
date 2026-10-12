using System;
using UnityEngine;

namespace Perigee
{
    public class SaveService : ISaveService
    {
        const string Key = "perigee_save";
        // способности, которые есть с начала игры
        const int StartAbilities = 1 << (int)AbilityId.Reverse | 1 << (int)AbilityId.Grow | 1 << (int)AbilityId.Pulse;

        public SaveData Data { get; private set; }

        public SaveService(int blueprintCount)
        {
            Data = Fresh(blueprintCount);
            // единственный try/catch в проекте: битое сохранение заменяется новым.
            // Пустая строка (первый запуск) оставляет Data как есть.
            try
            {
                JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(Key), Data);
            }
            catch (Exception)
            {
                Data = Fresh(blueprintCount);
            }

            // Новое сохранение или сохранение старше M10: способностей в нём ещё нет.
            // В старом lastAbilityA РОСТ и ИМПУЛЬС значат то же, остальные номера были ЯКОРЕМ, ХОЛОДОМ и ВСПЛЕСКОМ.
            if (Data.abilitiesOwned != 0) return;
            Data.abilitiesOwned = StartAbilities;
            if (Data.lastAbilityA != (int)AbilityId.Grow && Data.lastAbilityA != (int)AbilityId.Pulse) Data.lastAbilityA = (int)AbilityId.Reverse;
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        static SaveData Fresh(int blueprintCount)
        {
            return new SaveData { blueprints = new int[blueprintCount] };
        }
    }
}
