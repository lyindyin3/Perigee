using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    public class Hud : MonoBehaviour
    {
        const int SecondsPerMinute = 60;
        const int DigitBase = 10;

        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] CardSystem cards;
        [SerializeField] RectTransform safeArea;
        [SerializeField] Image hpFill;
        [SerializeField] Image xpFill;
        [SerializeField] Image starIcon;
        [SerializeField] Image crystalIcon;
        [SerializeField] TMP_Text dustText;
        [SerializeField] TMP_Text timerText;
        // блоки, которые меняют место при повороте экрана, и их места в двух раскладках
        [SerializeField] RectTransform bars;
        [SerializeField] RectTransform barsPortrait;
        [SerializeField] RectTransform barsLandscape;
        [SerializeField] RectTransform dust;
        [SerializeField] RectTransform dustPortrait;
        [SerializeField] RectTransform dustLandscape;
        // по индексу карточки в config.cards
        [SerializeField] GameObject[] cardIcons;
        [SerializeField] TMP_Text[] cardIconTexts;

        int[] shownStacks;
        int shownDust = -1;
        int shownSeconds = -1;
        Rect shownSafe;
        Vector2Int shownScreen;

        void Awake()
        {
            shownStacks = new int[cardIcons.Length];
            // Image.fillAmount работает только со спрайтом
            Sprite rect = ShapeFactory.Rect();
            hpFill.sprite = rect;
            xpFill.sprite = rect;
            starIcon.sprite = ShapeFactory.Circle();
            crystalIcon.sprite = ShapeFactory.Diamond();
        }

        void Update()
        {
            Layout();

            RunState run = loop.Run;
            hpFill.fillAmount = run.hp / loop.Stats.starMaxHp;
            xpFill.fillAmount = (float)run.xp / cards.Need(run.level);

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

                shownStacks[i] = stacks;
                cardIcons[i].SetActive(stacks > 0);
                if (stacks > 0) cardIconTexts[i].text = Strings.CardShort[(int)config.cards[i].id] + "\n" + stacks;
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
