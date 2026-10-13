using UnityEngine;

namespace Perigee
{
    // Итоги забега: пыль по частям, рекорды, открытие эр и награды первой победы.
    public class Progress
    {
        readonly GameConfig config;
        readonly ISaveService save;
        readonly Shop shop;

        public Progress(GameConfig config, ISaveService save, Shop shop)
        {
            this.config = config;
            this.save = save;
            this.shop = shop;
        }

        // сколько эр открыто; с debugUnlockAll открыты все
        public int ErasOpen => config.debugUnlockAll ? config.eras.Length : save.Data.erasUnlocked;

        // забег окончен: в run уже стоит victory; era = индекс эры в config.eras
        public void Finish(RunState run, int era, Stats stats)
        {
            SaveData data = save.Data;
            EraDef def = config.eras[era];
            // первая победа считается только в режиме Забег: День эры не открывает
            bool firstWin = run.victory && run.mode == RunMode.Run && (data.erasWon & 1 << era) == 0;

            run.dustMul = def.dustMul * stats.dustMul;
            run.dustCrystals = Mathf.RoundToInt(run.dust * run.dustMul);
            run.dustVictory = run.victory ? def.victoryDust : 0;
            run.dustFirstWin = firstWin ? def.firstWinDust : 0;
            run.earned = run.dustCrystals + run.dustVictory + run.dustFirstWin;
            // первый за дату дневной забег удваивает пыль
            run.dailyDoubled = run.mode == RunMode.Daily && data.dailyBonusDate != run.seed;
            if (run.dailyDoubled)
            {
                run.earned *= config.dailyDustMul;
                data.dailyBonusDate = run.seed;
            }
            run.adDoubled = false;
            data.dust += run.earned;

            // рекорд времени свой у каждой эры и только в режиме Забег
            run.recordGap = 0f;
            if (run.mode == RunMode.Run)
            {
                run.recordGap = data.bestTimes[era] - run.time;
                data.bestTimes[era] = Mathf.Max(data.bestTimes[era], run.time);
            }

            run.score = run.dust + Mathf.RoundToInt(config.scorePerSecond * run.time) + (run.victory ? config.scoreVictory : 0);
            if (run.mode == RunMode.Daily) data.bestDaily = Mathf.Max(data.bestDaily, run.score);

            run.unlockedEra = 0;
            if (firstWin)
            {
                data.erasWon |= 1 << era;
                // первая победа открывает следующую эру
                int next = era + 1;
                if (next < config.eras.Length && data.erasUnlocked <= next)
                {
                    data.erasUnlocked = next + 1;
                    run.unlockedEra = next;
                }

                // и даёт облик этой эры
                SkinDef[] skins = config.skins;
                for (int i = 0; i < skins.Length; i++)
                    if (skins[i].eraWin == era + 1) shop.Give(skins[i].id);
            }

            data.runs++;
            if (!run.victory) data.defeats++;
            data.totalKills += run.kills;
            data.tutorialDone = true;
            data.lastEra = era;
            data.lastMode = (int)run.mode;
            save.Save();
        }

        // «Пыль ×2» на итогах: пыль забега уже начислена, добавляется разница
        public void DoubleDust(RunState run)
        {
            int bonus = run.earned * (config.adDustMul - 1);
            run.earned += bonus;
            run.adDoubled = true;
            save.Data.dust += bonus;
            save.Save();
        }
    }
}
