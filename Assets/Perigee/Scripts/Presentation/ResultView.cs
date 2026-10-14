using System.Globalization;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Perigee
{
    public class ResultView : MonoBehaviour
    {
        // номер текста пыли в texts: последняя его строка = итог
        const int DustText = 3;

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
        // штамп «НОВЫЙ РЕКОРД»: выходит после всех строк
        [SerializeField] TMP_Text stamp;

        readonly StringBuilder lines = new StringBuilder();
        // тексты экрана сверху вниз и их полный вид; строки появляются по одной через resultLineDelay
        TMP_Text[] texts;
        string[] full;
        int[] lineCounts;
        // номер строки итога пыли среди всех строк экрана
        int totalLine;
        int totalLines;
        int revealed;
        // итог пыли считается от нуля до earned
        int shownTotal;
        bool record;
        float openTime;

        void Awake()
        {
            texts = new[] { title, timeLine, infoLine, dustLines, unlockLines };
            full = new string[texts.Length];
            lineCounts = new int[texts.Length];
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
            stamp.text = Strings.RecordStamp;
        }

        // GameLoop включает экран, когда итоги забега уже посчитаны
        void OnEnable()
        {
            RunState run = loop.Run;
            full[0] = run.victory ? Strings.Victory : Strings.Defeat;
            full[1] = string.Format(Strings.TimeFormat, Strings.Clock(run.time));
            ShowInfo();
            shownTotal = 0;
            ShowDust();
            ShowUnlocked();
            doubleButton.SetActive(run.earned > 0);
            if (full[4].Length > 0) sfx.Play(SfxId.Unlock);
            // рекорд времени ведётся только в Забеге и Бесконечном
            record = !run.victory && (run.mode == RunMode.Run || run.mode == RunMode.Endless) && run.recordGap <= 0f;

            totalLines = 0;
            for (int i = 0; i < texts.Length; i++)
            {
                lineCounts[i] = Lines(full[i]);
                totalLines += lineCounts[i];
            }
            totalLine = TotalLine();
            revealed = 0;
            openTime = Time.unscaledTime;
            Reveal();
            stamp.transform.DOKill();
            stamp.gameObject.SetActive(false);
        }

        void Update()
        {
            float t = Time.unscaledTime - openTime;
            int want = Mathf.Min((int)(t / config.resultLineDelay) + 1, totalLines);
            bool changed = want != revealed;
            revealed = want;

            // итог пыли растёт от нуля с момента, когда показалась его строка
            if (revealed > totalLine)
            {
                float k = Mathf.Clamp01((t - totalLine * config.resultLineDelay) / config.dustCountTime);
                int total = Mathf.RoundToInt(loop.Run.earned * k);
                if (total != shownTotal)
                {
                    shownTotal = total;
                    ShowDust();
                    changed = true;
                }
            }
            if (changed) Reveal();

            if (!record || revealed < totalLines || stamp.gameObject.activeSelf) return;
            // штамп: крупный и прямой, потом своего размера и наклонён
            Transform st = stamp.transform;
            stamp.gameObject.SetActive(true);
            st.localScale = Vector3.one * config.stampFrom;
            st.localRotation = Quaternion.identity;
            st.DOScale(1f, config.stampTime).SetEase(Ease.OutBack).SetLink(gameObject);
            st.DOLocalRotate(new Vector3(0f, 0f, config.stampAngle), config.stampTime).SetLink(gameObject);
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
            // строка удвоения добавилась перед итогом и сразу видна; итог становится новым
            int added = Lines(full[DustText]);
            ShowDust();
            added = Lines(full[DustText]) - added;
            lineCounts[DustText] += added;
            totalLines += added;
            revealed += added;
            totalLine = TotalLine();
            Reveal();
        }

        int TotalLine()
        {
            int line = -1;
            for (int i = 0; i <= DustText; i++) line += lineCounts[i];
            return line;
        }

        // каждому тексту столько его строк, сколько уже открыто по порядку экрана
        void Reveal()
        {
            int left = revealed;
            for (int i = 0; i < texts.Length; i++)
            {
                int n = Mathf.Min(left, lineCounts[i]);
                left -= n;
                texts[i].text = n == lineCounts[i] ? full[i] : Prefix(full[i], n);
            }
        }

        // первые n строк текста
        static string Prefix(string text, int n)
        {
            if (n == 0) return string.Empty;
            int end = -1;
            for (int k = 0; k < n; k++) end = text.IndexOf('\n', end + 1);
            return text.Substring(0, end);
        }

        static int Lines(string text)
        {
            if (text.Length == 0) return 0;
            int n = 1;
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n') n++;
            return n;
        }

        void ShowInfo()
        {
            RunState run = loop.Run;
            bool endless = run.mode == RunMode.Endless;
            lines.Length = 0;
            if (endless) lines.AppendFormat(Strings.EndlessResultFormat, run.cycle + 1, run.bossKills).Append('\n');
            // сколько не хватило до рекорда (только Забег и Бесконечный); новый рекорд показывает штамп
            if (!run.victory && (endless || run.mode == RunMode.Run) && run.recordGap > 0f)
                lines.AppendFormat(Strings.RecordGapFormat, Mathf.CeilToInt(run.recordGap));
            full[2] = lines.ToString().TrimEnd('\n');
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
            if (run.dustRelics > 0) lines.AppendFormat(Strings.DustRelicsFormat, run.dustRelics).Append('\n');
            if (run.dailyDoubled) lines.AppendFormat(Strings.DustDailyFormat, config.dailyDustMul).Append('\n');
            if (run.adDoubled) lines.AppendFormat(Strings.DustAdFormat, config.adDustMul).Append('\n');
            lines.AppendFormat(Strings.DustTotalFormat, shownTotal);
            full[DustText] = lines.ToString();
        }

        // всё новое, что открыл этот забег: эра, Бесконечный, облики, классы звёзд
        void ShowUnlocked()
        {
            RunState run = loop.Run;
            lines.Length = 0;
            if (run.unlockedEra > 0) lines.AppendFormat(Strings.UnlockedEraFormat, Strings.EraTitle[run.unlockedEra]).Append('\n');
            if (run.unlockedEndless) lines.Append(Strings.UnlockedEndless).Append('\n');
            for (int i = 0; i < Strings.SkinTitle.Length; i++)
                if ((run.unlockedSkins & 1 << i) != 0) lines.AppendFormat(Strings.UnlockedSkinFormat, Strings.SkinTitle[i]).Append('\n');
            for (int i = 0; i < Strings.ClassTitle.Length; i++)
                if ((run.unlockedClasses & 1 << i) != 0) lines.AppendFormat(Strings.UnlockedClassFormat, Strings.ClassTitle[i]).Append('\n');
            full[4] = lines.ToString().TrimEnd('\n');
        }
    }
}
