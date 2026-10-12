using TMPro;
using UnityEngine;

namespace Perigee
{
    public class ResultView : MonoBehaviour
    {
        [SerializeField] GameLoop loop;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text timeLine;
        // поражение: сколько не хватило до рекорда; победа: открытая эра
        [SerializeField] TMP_Text infoLine;
        [SerializeField] TMP_Text dustLine;
        [SerializeField] TMP_Text retryLabel;
        [SerializeField] TMP_Text menuLabel;
        // «Пыль ×2 (реклама)»: один раз на экран, и только если есть что удваивать
        [SerializeField] GameObject doubleButton;
        [SerializeField] TMP_Text doubleLabel;

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
            dustLine.text = string.Format(Strings.DustEarnedFormat, run.earned);
            doubleButton.SetActive(run.earned > 0);

            if (run.victory) infoLine.text = run.unlockedEra > 0 ? string.Format(Strings.EraUnlockedFormat, Strings.EraTitle[run.unlockedEra]) : string.Empty;
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

            loop.DoubleDust();
            doubleButton.SetActive(false);
            dustLine.text = string.Format(Strings.DustEarnedFormat, loop.Run.earned);
        }
    }
}
