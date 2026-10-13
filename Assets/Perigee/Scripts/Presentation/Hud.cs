using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    public class Hud : MonoBehaviour
    {
        const int SecondsPerMinute = 60;
        const int DigitBase = 10;
        // обязательная часть префаба HudCardIcon: картинка карточки
        const string IconName = "Icon";

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] CardSystem cards;
        [SerializeField] BossSystem boss;
        [SerializeField] RectTransform safeArea;
        [SerializeField] BarView hpBar;
        [SerializeField] BarView xpBar;
        [SerializeField] TMP_Text levelText;
        // имя и полоса HP босса: видны, пока он жив; у Близнецов полоса показывает сумму
        [SerializeField] GameObject bossPanel;
        [SerializeField] BarView bossBar;
        [SerializeField] TMP_Text bossName;
        [SerializeField] TMP_Text dustText;
        [SerializeField] TMP_Text timerText;
        // блоки, которые меняют место при повороте экрана, и их места в двух раскладках
        [SerializeField] RectTransform bars;
        [SerializeField] RectTransform barsPortrait;
        [SerializeField] RectTransform barsLandscape;
        [SerializeField] RectTransform dust;
        [SerializeField] RectTransform dustPortrait;
        [SerializeField] RectTransform dustLandscape;
        [SerializeField] RectTransform hint;
        [SerializeField] RectTransform hintPortrait;
        [SerializeField] RectTransform hintLandscape;
        [SerializeField] RectTransform cardIconsRoot;
        [SerializeField] GameObject cardIconPrefab;

        // по индексу карточки в config.cards
        GameObject[] cardIcons;
        TMP_Text[] cardIconTexts;
        int[] shownStacks;
        int shownLevel = -1;
        int shownDust = -1;
        int shownSeconds = -1;
        Rect shownSafe;
        Vector2Int shownScreen;

        void Awake()
        {
            cardIcons = new GameObject[config.cards.Length];
            cardIconTexts = new TMP_Text[cardIcons.Length];
            shownStacks = new int[cardIcons.Length];
        }

        void Update()
        {
            Layout();

            RunState run = loop.Run;
            hpBar.Set(run.hp, loop.Stats.starMaxHp);
            if (run.level != shownLevel)
            {
                // уровень вырос: полоска опыта доходит до конца и начинает с нуля; первый показ и сброс в начале забега не в счёт
                if (shownLevel >= 0 && run.level > shownLevel) xpBar.Wrap();
                shownLevel = run.level;
                // уровни считаются с нуля, игрок видит их с первого
                levelText.SetText("{0}", shownLevel + 1);
            }
            xpBar.Set(run.xp, cards.Need(run.level));
            if (bossPanel.activeSelf != boss.Alive)
            {
                bossPanel.SetActive(boss.Alive);
                bossName.text = Strings.BossTitle(loop.Era.boss);
            }
            if (boss.Alive) bossBar.Set(boss.Hp, boss.MaxHp);

            if (run.dust != shownDust)
            {
                // подобран кристалл: счётчик подпрыгивает; первый показ и сброс в начале забега не в счёт
                if (shownDust >= 0 && run.dust > shownDust)
                {
                    // прошлый прыжок доводим до конца, иначе масштаб накапливается
                    dustText.rectTransform.DOKill(true);
                    dustText.rectTransform.DOPunchScale(Vector3.one * config.dustPunch, config.dustPunchTime).SetLink(gameObject);
                }
                shownDust = run.dust;
                dustText.SetText("{0}", shownDust);
            }

            int seconds = (int)run.time;
            if (seconds != shownSeconds)
            {
                shownSeconds = seconds;
                int m = seconds / SecondsPerMinute;
                int s = seconds % SecondsPerMinute;
                timerText.SetText("{0}{1}:{2}{3}", m / DigitBase, m % DigitBase, s / DigitBase, s % DigitBase);
            }

            for (int i = 0; i < cardIcons.Length; i++)
            {
                int stacks = run.cardStacks[i];
                if (stacks == shownStacks[i]) continue;

                // иконка создаётся, когда карточку взяли впервые, и убирается с новым забегом
                CardId id = config.cards[i].id;
                if (shownStacks[i] == 0)
                {
                    cardIcons[i] = Instantiate(cardIconPrefab, cardIconsRoot);
                    cardIcons[i].transform.Find(IconName).GetComponent<Image>().sprite = art.cardIcons[(int)id];
                    cardIconTexts[i] = cardIcons[i].GetComponentInChildren<TMP_Text>();
                }
                shownStacks[i] = stacks;
                if (stacks == 0) Destroy(cardIcons[i]);
                else cardIconTexts[i].text = Strings.CardShort[(int)id] + "\n" + stacks;
            }
        }

        void Layout()
        {
            Rect safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (safe == shownSafe && screen == shownScreen) return;

            shownSafe = safe;
            shownScreen = screen;
            safeArea.anchorMin = new Vector2(safe.xMin / screen.x, safe.yMin / screen.y);
            safeArea.anchorMax = new Vector2(safe.xMax / screen.x, safe.yMax / screen.y);

            bool landscape = screen.x > screen.y;
            bars.SetParent(landscape ? barsLandscape : barsPortrait, false);
            dust.SetParent(landscape ? dustLandscape : dustPortrait, false);
            hint.SetParent(landscape ? hintLandscape : hintPortrait, false);
        }
    }
}
