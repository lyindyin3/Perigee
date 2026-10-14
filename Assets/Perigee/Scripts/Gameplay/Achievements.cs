using UnityEngine;

namespace Perigee
{
    // Достижения (GDD 8.7): проверка на итогах забега и после покупок в Мастерской. Новое: бит, пыль сразу, тост и звук.
    public class Achievements
    {
        readonly GameConfig config;
        readonly ArtSet art;
        readonly ISaveService save;
        readonly Blueprints blueprints;
        readonly AchievementToast toast;
        readonly AudioManager sfx;

        public Achievements(GameConfig config, ArtSet art, ISaveService save, Blueprints blueprints, AchievementToast toast, AudioManager sfx)
        {
            this.config = config;
            this.art = art;
            this.save = save;
            this.blueprints = blueprints;
            this.toast = toast;
            this.sfx = sfx;
        }

        public bool Done(int i)
        {
            return (save.Data.achievements & 1L << (int)config.achievements[i].id) != 0;
        }

        public int DoneCount
        {
            get
            {
                int done = 0;
                for (int i = 0; i < config.achievements.Length; i++)
                    if (Done(i)) done++;
                return done;
            }
        }

        // значение счётчика для строки «12 / 25»; -1 = у достижения счётчика нет
        public int Counter(AchievementDef def)
        {
            SaveData data = save.Data;
            switch (def.kind)
            {
                case AchKind.Kills: return data.totalKills;
                case AchKind.Combo: return data.bestCombo;
                case AchKind.AllEvolutions: return Bits(data.evolutionsTaken);
                case AchKind.HeraldKills: return data.heraldKills;
                case AchKind.EliteKills: return data.eliteKills;
                case AchKind.EndlessBosses: return data.endlessBossBest;
                case AchKind.Challenges: return Bits(data.challengesDone);
                case AchKind.DailyPlayed: return data.dailyPlayed;
                case AchKind.Blueprints: return MaxedBlueprints();
                case AchKind.Atlas: return SeenBestiary();
                case AchKind.GoldComets: return data.goldComets;
                default: return -1;
            }
        }

        // итоги забега: условия одного забега и счётчики сохранения; era = индекс эры в config.eras
        public void Check(RunState run, int era)
        {
            AchievementDef[] defs = config.achievements;
            for (int i = 0; i < defs.Length; i++)
                if (!Done(i) && (Reached(defs[i]) || RunReached(defs[i], run, era))) Grant(defs[i]);
        }

        // после покупки в Мастерской: только счётчики сохранения
        public void Check()
        {
            AchievementDef[] defs = config.achievements;
            for (int i = 0; i < defs.Length; i++)
                if (!Done(i) && Reached(defs[i])) Grant(defs[i]);
        }

        // по сохранению: счётчики, победы в эрах (в режиме Забег) и победы классами
        bool Reached(AchievementDef def)
        {
            SaveData data = save.Data;
            switch (def.kind)
            {
                case AchKind.EraWin: return (data.erasWon & 1 << def.target - 1) != 0;
                case AchKind.ClassWin: return (data.classesWon & 1 << def.target) != 0;
                default: return Counter(def) >= def.target;
            }
        }

        // по забегу: победа в эре в любом режиме и всё, что считается за один забег
        bool RunReached(AchievementDef def, RunState run, int era)
        {
            switch (def.kind)
            {
                case AchKind.EraWin: return run.victory && era + 1 == def.target;
                case AchKind.RunEvolutions: return Evolutions(run) >= def.target;
                case AchKind.RunRelics: return Relics(run) >= def.target;
                case AchKind.Flawless: return run.victory && !run.starDamaged;
                case AchKind.NoAbilities: return run.victory && !run.abilityUsed;
                case AchKind.OneSatellite: return run.victory && run.maxSatellites == 1;
                case AchKind.Satellites: return run.maxSatellites >= def.target;
                case AchKind.EndlessTime: return run.mode == RunMode.Endless && run.time >= def.target;
                default: return false;
            }
        }

        void Grant(AchievementDef def)
        {
            int id = (int)def.id;
            SaveData data = save.Data;
            data.achievements |= 1L << id;
            data.dust += def.dust;
            save.Save();
            toast.Show(art.achievementIcons[id].sprite, Strings.AchTitle[id], def.dust);
            sfx.Play(SfxId.Achievement);
        }

        int Evolutions(RunState run)
        {
            int n = 0;
            for (int i = 0; i < config.cards.Length; i++)
                if (config.cards[i].color == CardColor.Evolution && run.cardStacks[i] > 0) n++;
            return n;
        }

        static int Relics(RunState run)
        {
            int n = 0;
            for (int i = 0; i < run.relicStacks.Length; i++) n += run.relicStacks[i];
            return n;
        }

        int MaxedBlueprints()
        {
            int n = 0;
            for (int i = 0; i < config.blueprints.Length; i++)
                if (blueprints.Maxed(i)) n++;
            return n;
        }

        // сколько плиток бестиария уже открыто
        public int SeenBestiary()
        {
            int n = 0;
            for (int i = 0; i < config.bestiary.Length; i++)
                if (Seen(config.bestiary[i].type)) n++;
            return n;
        }

        public bool Seen(EnemyType type)
        {
            return (save.Data.seenEnemies & 1 << (int)type) != 0;
        }

        static int Bits(int mask)
        {
            int n = 0;
            for (; mask != 0; mask &= mask - 1) n++;
            return n;
        }
    }
}
