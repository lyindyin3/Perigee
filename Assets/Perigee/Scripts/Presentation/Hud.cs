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
        [SerializeField] BossComet boss;
        [SerializeField] RectTransform safeArea;
        [SerializeField] Image hpFill;
        [SerializeField] Image xpFill;
        // полоса HP Кометы: видна, пока Комета жива
        [SerializeField] GameObject bossBar;
        [SerializeField] Image bossFill;
        [SerializeField] TMP_Text dustText;
        [SerializeField] TMP_Text timerText;
        // блоки, которые меняют место при повороте экрана, и их места в двух раскладках
        [SerializeField] RectTransform bars;
        [SerializeField] RectTransform barsPortrait;
        [SerializeField] RectTransform barsLandscape;
        [SerializeField] RectTransform dust;
        [SerializeField] RectTransform dustPortrait;
        [SerializeField] RectTransform dustLandscape;
        [SerializeField] RectTransform cardIconsRoot;
        [SerializeField] GameObject cardIconPrefab;

        // по индексу карточки в config.cards
        GameObject[] cardIcons;
        TMP_Text[] cardIconTexts;
        int[] shownStacks;
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
            hpFill.fillAmount = run.hp / loop.Stats.starMaxHp;
            xpFill.fillAmount = (float)run.xp / cards.Need(run.level);
            if (bossBar.activeSelf != boss.Alive) bossBar.SetActive(boss.Alive);
            if (boss.Alive) bossFill.fillAmount = boss.HpFraction;

            if (run.dust != shownDust)
            {
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
        }
    }
}
