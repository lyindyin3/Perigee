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

        void Awake()
        {
            retryLabel.text = Strings.Retry;
            menuLabel.text = Strings.Menu;
        }

        // GameLoop включает экран, когда итоги забега уже посчитаны
        void OnEnable()
        {
            RunState run = loop.Run;
            title.text = run.victory ? Strings.Victory : Strings.Defeat;
            timeLine.text = string.Format(Strings.TimeFormat, Strings.Clock(run.time));
            dustLine.text = string.Format(Strings.DustEarnedFormat, run.earned);

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
    }
}
