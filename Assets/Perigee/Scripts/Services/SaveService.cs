using System;
using UnityEngine;

namespace Perigee
{
    public class SaveService : ISaveService
    {
        const string Key = "perigee_save";

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
