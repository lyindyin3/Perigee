using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

namespace Perigee
{
    public class ResultView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] AudioManager sfx;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text timeLine;
        // Бесконечный: цикл и убитые боссы; поражение в Забеге и Бесконечном: сколько не хватило до рекорда
        [SerializeField] TMP_Text infoLine;
        // пыль забега по частям и итог
        [SerializeField] TMP_Text dustLines;
        // «Открыто: …»: эра, режим, облик
        [SerializeField] TMP_Text unlockLines;
        [SerializeField] TMP_Text retryLabel;
        [SerializeField] TMP_Text menuLabel;
        // «Пыль ×2 (реклама)»: один раз на экран, и только если есть что удваивать
        [SerializeField] GameObject doubleButton;
        [SerializeField] TMP_Text doubleLabel;

        readonly StringBuilder lines = new StringBuilder();

        void Awake()
        {
            Loc.Changed += Localize;
            Localize();
        }

        void OnDestroy()
        {
            Loc.Changed -= Localize;
        }

        void Localize()
        {
            retryLabel.text = Strings.Retry;
            menuLabel.text = Strings.Menu;
            doubleLabel.text = Strings.DoubleDustAd;
        }

        // GameLoop включает экран, когда итоги забега уже посчитаны
        void OnEnable()
        {
            RunState run = loop.Run;
            title.text = run.victory ? Strings.Victory : Strings.Defeat;
            timeLine.text = string.Format(Strings.TimeFormat, Strings.Clock(run.time));
            ShowInfo();
            ShowDust();
            ShowUnlocked();
            doubleButton.SetActive(run.earned > 0);
            if (unlockLines.text.Length > 0) sfx.Play(SfxId.Unlock);
        }

        public void Retry()
        {
            sfx.Play(SfxId.UiClick);
            loop.StartRun();
        }

        public void Menu()
        {
            sfx.Play(SfxId.UiClick);
            loop.ToMenu();
        }

        public void DoubleDust()
        {
            sfx.Play(SfxId.UiClick);
            loop.Rewarded(Doubled);
        }

        void Doubled(bool rewarded)
        {
            if (!rewarded) return;

            loop.Progress.DoubleDust(loop.Run);
            doubleButton.SetActive(false);
            ShowDust();
        }

        void ShowInfo()
        {
            RunState run = loop.Run;
            bool endless = run.mode == RunMode.Endless;
            lines.Length = 0;
            if (endless) lines.AppendFormat(Strings.EndlessResultFormat, run.cycle + 1, run.bossKills).Append('\n');
            // рекорд времени ведётся только в Забеге и Бесконечном
            if (!run.victory && (endless || run.mode == RunMode.Run))
                lines.Append(run.recordGap > 0f ? string.Format(Strings.RecordGapFormat, Mathf.CeilToInt(run.recordGap)) : Strings.NewRecord);
            infoLine.text = lines.ToString();
        }

        // строки пыли: кристаллы × множитель, победа, первая победа, боссы, награда испытания, удвоения, итог; нулевые части пропущены
        void ShowDust()
        {
            RunState run = loop.Run;
            lines.Length = 0;
            lines.AppendFormat(CultureInfo.InvariantCulture, Strings.DustCrystalsFormat, run.dust, run.dustMul, run.dustCrystals).Append('\n');
            if (run.dustVictory > 0) lines.AppendFormat(Strings.DustVictoryFormat, run.dustVictory).Append('\n');
            if (run.dustFirstWin > 0) lines.AppendFormat(Strings.DustFirstWinFormat, run.dustFirstWin).Append('\n');
            if (run.dustBosses > 0) lines.AppendFormat(Strings.DustBossesFormat, run.bossKills, run.dustBosses).Append('\n');
            if (run.dustReward > 0) lines.AppendFormat(Strings.DustRewardFormat, run.dustReward).Append('\n');
            if (run.dailyDoubled) lines.AppendFormat(Strings.DustDailyFormat, config.dailyDustMul).Append('\n');
            if (run.adDoubled) lines.AppendFormat(Strings.DustAdFormat, config.adDustMul).Append('\n');
            lines.AppendFormat(Strings.DustTotalFormat, run.earned);
            dustLines.text = lines.ToString();
        }

        // всё новое, что открыл этот забег: эра, Бесконечный, облики
        void ShowUnlocked()
        {
            RunState run = loop.Run;
            lines.Length = 0;
            if (run.unlockedEra > 0) lines.AppendFormat(Strings.UnlockedEraFormat, Strings.EraTitle[run.unlockedEra]).Append('\n');
            if (run.unlockedEndless) lines.Append(Strings.UnlockedEndless).Append('\n');
            for (int i = 0; i < Strings.SkinTitle.Length; i++)
                if ((run.unlockedSkins & 1 << i) != 0) lines.AppendFormat(Strings.UnlockedSkinFormat, Strings.SkinTitle[i]).Append('\n');
            unlockLines.text = lines.ToString().TrimEnd('\n');
        }
    }
}
