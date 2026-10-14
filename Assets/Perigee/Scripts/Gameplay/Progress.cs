using System;
using System.Globalization;
using UnityEngine;

namespace Perigee
{
    // Итоги забега: пыль по частям, рекорды, открытие эр и режимов, награды первой победы и испытаний.
    public class Progress
    {
        // зерно дневного забега и отметка «сегодня уже сыграно» = дата UTC в таком виде
        const string DateFormat = "yyyyMMdd";

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

        // Бесконечный открывает первая победа в первой эре
        public bool EndlessOpen => config.debugUnlockAll || (save.Data.erasWon & 1) != 0;

        // испытание открыто, когда открыта его эра; i = номер в config.challenges
        public bool ChallengeOpen(int i)
        {
            return config.challenges[i].era <= ErasOpen;
        }

        public bool ChallengeDone(int i)
        {
            return (save.Data.challengesDone & 1 << i) != 0;
        }

        // модификатор Дня: один на дату UTC, по кругу из списка
        public RunModifier DailyMod => config.dailyMods[(int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays % config.dailyMods.Length];

        // первый доигранный сегодня День ещё впереди: его пыль удвоится
        public bool DailyBonusLeft => save.Data.dailyBonusDate != Today();

        public static int Today()
        {
            return int.Parse(DateTime.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        // пыль за одного босса в Бесконечном
        public int BossDust(EraDef era)
        {
            return Mathf.RoundToInt(config.endlessBossDust * era.dustMul);
        }

        // забег окончен: в run уже стоит victory; era = индекс эры в config.eras
        public void Finish(RunState run, int era, Stats stats)
        {
            SaveData data = save.Data;
            EraDef def = config.eras[era];
            bool endless = run.mode == RunMode.Endless;
            // первая победа считается только в режиме Забег: День и испытания эр не открывают
            bool firstWin = run.victory && run.mode == RunMode.Run && (data.erasWon & 1 << era) == 0;

            run.unlockedEra = 0;
            run.unlockedEndless = false;
            run.unlockedSkins = 0;

            run.dustMul = def.dustMul * stats.dustMul;
            run.dustCrystals = Mathf.RoundToInt(run.dust * run.dustMul);
            run.dustVictory = run.victory ? def.victoryDust : 0;
            run.dustFirstWin = firstWin ? def.firstWinDust : 0;
            // боссов убивают, не заканчивая забег, только в Бесконечном
            run.dustBosses = run.bossKills * BossDust(def);
            run.dustReward = 0;
            // испытание: награда один раз, за первую победу; облик вместо пыли там, где так задано
            if (run.mode == RunMode.Challenge && run.victory && !ChallengeDone(run.challenge))
            {
                ChallengeDef challenge = config.challenges[run.challenge];
                data.challengesDone |= 1 << run.challenge;
                run.dustReward = challenge.rewardDust;
                if (challenge.hasSkin) Give(run, challenge.rewardSkin);
            }
            run.earned = run.dustCrystals + run.dustVictory + run.dustFirstWin + run.dustBosses + run.dustReward;
            // первый за дату дневной забег удваивает пыль
            run.dailyDoubled = run.mode == RunMode.Daily && data.dailyBonusDate != run.seed;
            if (run.dailyDoubled)
            {
                run.earned *= config.dailyDustMul;
                data.dailyBonusDate = run.seed;
            }
            run.adDoubled = false;
            data.dust += run.earned;

            // рекорд времени свой у каждой эры: отдельно в Забеге и в Бесконечном
            run.recordGap = 0f;
            if (run.mode == RunMode.Run || endless)
            {
                float[] best = endless ? data.bestEndless : data.bestTimes;
                run.recordGap = best[era] - run.time;
                best[era] = Mathf.Max(best[era], run.time);
            }

            // счёт Бесконечного = целые секунды
            run.score = endless ? (int)run.time : run.dust + Mathf.RoundToInt(config.scorePerSecond * run.time) + (run.victory ? config.scoreVictory : 0);
            if (run.mode == RunMode.Daily) data.bestDaily = Mathf.Max(data.bestDaily, run.score);

            if (firstWin)
            {
                data.erasWon |= 1 << era;
                run.unlockedEndless = era == 0;
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
                    if (skins[i].eraWin == era + 1) Give(run, skins[i].id);
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

        // облик в награду; на итогах он попадёт в «Открыто», если его ещё не было
        void Give(RunState run, SkinId id)
        {
            int bit = 1 << (int)id;
            if ((save.Data.skinsOwned & bit) == 0) run.unlockedSkins |= bit;
            shop.Give(id);
        }
    }
}
