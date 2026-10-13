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
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text timeLine;
        // поражение: сколько не хватило до рекорда; победа: открытая эра
        [SerializeField] TMP_Text infoLine;
        // пыль забега по частям и итог
        [SerializeField] TMP_Text dustLines;
        [SerializeField] TMP_Text retryLabel;
        [SerializeField] TMP_Text menuLabel;
        // «Пыль ×2 (реклама)»: один раз на экран, и только если есть что удваивать
        [SerializeField] GameObject doubleButton;
        [SerializeField] TMP_Text doubleLabel;

        readonly StringBuilder lines = new StringBuilder();

        void Awake()
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
            ShowDust();
            doubleButton.SetActive(run.earned > 0);

            if (run.victory) infoLine.text = run.unlockedEra > 0 ? string.Format(Strings.EraUnlockedFormat, Strings.EraTitle[run.unlockedEra]) : string.Empty;
            // рекорд времени ведётся только в режиме Забег
            else if (run.mode != RunMode.Run) infoLine.text = string.Empty;
            else infoLine.text = run.recordGap > 0f ? string.Format(Strings.RecordGapFormat, Mathf.CeilToInt(run.recordGap)) : Strings.NewRecord;
        }

        public void Retry()
        {
            loop.StartRun();
        }

        public void Menu()
        {
            loop.ToMenu();
        }

        public void DoubleDust()
        {
            loop.Rewarded(Doubled);
        }

        void Doubled(bool rewarded)
        {
            if (!rewarded) return;

            loop.Progress.DoubleDust(loop.Run);
            doubleButton.SetActive(false);
            ShowDust();
        }

        // строки пыли: кристаллы × множитель, победа, первая победа, удвоения, итог; нулевые части пропущены
        void ShowDust()
        {
            RunState run = loop.Run;
            lines.Length = 0;
            lines.AppendFormat(CultureInfo.InvariantCulture, Strings.DustCrystalsFormat, run.dust, run.dustMul, run.dustCrystals).Append('\n');
            if (run.dustVictory > 0) lines.AppendFormat(Strings.DustVictoryFormat, run.dustVictory).Append('\n');
            if (run.dustFirstWin > 0) lines.AppendFormat(Strings.DustFirstWinFormat, run.dustFirstWin).Append('\n');
            if (run.dailyDoubled) lines.AppendFormat(Strings.DustDailyFormat, config.dailyDustMul).Append('\n');
            if (run.adDoubled) lines.AppendFormat(Strings.DustAdFormat, config.adDustMul).Append('\n');
            lines.AppendFormat(Strings.DustTotalFormat, run.earned);
            dustLines.text = lines.ToString();
        }
    }
}
