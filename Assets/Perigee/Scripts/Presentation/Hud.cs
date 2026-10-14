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
        // обязательные части префаба HudCardIcon: картинка карточки и золотая кайма эволюции
        const string IconName = "Icon";
        const string RimName = "Rim";

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
        // под таймером: модификатор Дня или испытания, в Бесконечном номер цикла
        [SerializeField] TMP_Text modeLabel;
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
        // ComboView: под полосками в портрете, под таймером в альбоме
        [SerializeField] RectTransform combo;
        [SerializeField] RectTransform comboPortrait;
        [SerializeField] RectTransform comboLandscape;
        [SerializeField] RectTransform cardIconsRoot;
        [SerializeField] GameObject cardIconPrefab;

        // по индексу карточки в config.cards
        GameObject[] cardIcons;
        TMP_Text[] cardIconTexts;
        int[] shownStacks;
        int shownLevel = -1;
        int shownDust = -1;
        int shownSeconds = -1;
        // что написано под таймером: номер цикла с единицы, минус набор модификаторов или 0 = пусто
        int shownMark;
        Rect shownSafe;
        Vector2Int shownScreen;

        void Awake()
        {
            cardIcons = new GameObject[config.cards.Length];
            cardIconTexts = new TMP_Text[cardIcons.Length];
            shownStacks = new int[cardIcons.Length];
            Loc.Changed += Localize;
        }

        void OnDestroy()
        {
            Loc.Changed -= Localize;
        }

        // смена языка на паузе: подписи под таймером, у босса и на иконках карточек пишутся заново
        void Localize()
        {
            shownMark = int.MinValue;
            bossName.text = Strings.BossTitle(loop.Era.boss);
            // иконки источников, слитых в эволюцию, уже убраны
            for (int i = 0; i < cardIcons.Length; i++)
                if (shownStacks[i] > 0 && cardIcons[i] != null) cardIconTexts[i].text = Strings.CardShort[(int)config.cards[i].id] + "\n" + shownStacks[i];
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

            // под таймером: в Бесконечном номер цикла, в Дне и испытании имя модификатора, в Забеге пусто
            int mark = run.mode == RunMode.Endless ? run.cycle + 1 : -(int)run.mods;
            if (mark != shownMark)
            {
                shownMark = mark;
                if (mark > 0) modeLabel.SetText(Strings.CycleFormat, mark);
                else modeLabel.text = Strings.Modifiers(run.mods, Strings.ModifierTitle);
            }

            for (int i = 0; i < cardIcons.Length; i++)
            {
                int stacks = run.cardStacks[i];
                if (stacks == shownStacks[i]) continue;

                // иконка создаётся, когда карточку взяли впервые, и убирается с новым забегом
                CardDef card = config.cards[i];
                CardId id = card.id;
                if (shownStacks[i] == 0)
                {
                    cardIcons[i] = Instantiate(cardIconPrefab, cardIconsRoot);
                    cardIcons[i].transform.Find(IconName).GetComponent<Image>().sprite = art.cardIcons[(int)id].sprite;
                    cardIconTexts[i] = cardIcons[i].GetComponentInChildren<TMP_Text>();
                    if (card.color == CardColor.Evolution) Evolve(i, card);
                }
                shownStacks[i] = stacks;
                // у слитого источника иконки уже нет
                if (stacks == 0)
                {
                    if (cardIcons[i] != null) Destroy(cardIcons[i]);
                }
                else cardIconTexts[i].text = Strings.CardShort[(int)id] + "\n" + stacks;
            }
        }

        // эволюция взята: иконки двух источников слетаются в её иконку с золотой каймой и гаснут в ней; дальше HUD показывает только эволюцию
        void Evolve(int evolution, CardDef card)
        {
            GameObject evo = cardIcons[evolution];
            evo.transform.Find(RimName).gameObject.SetActive(true);
            CanvasGroup evoGroup = evo.GetComponent<CanvasGroup>();
            evoGroup.alpha = 0f;
            evoGroup.DOFade(1f, config.evoMergeTime).SetLink(evo);

            GameObject a = Detach(cards.Index(card.sourceA));
            GameObject b = Detach(cards.Index(card.sourceB));
            // место иконки эволюции в ряду известно только после перестройки ряда без источников
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardIconsRoot);
            Fly(a, evo.transform.position);
            Fly(b, evo.transform.position);
        }

        // источник уходит из ряда (остаётся поверх него на том же месте) и больше не принадлежит карточке
        GameObject Detach(int source)
        {
            GameObject icon = cardIcons[source];
            cardIcons[source] = null;
            icon.transform.SetParent(cardIconsRoot.parent, true);
            return icon;
        }

        void Fly(GameObject icon, Vector3 target)
        {
            icon.transform.DOMove(target, config.evoMergeTime).SetLink(icon);
            icon.GetComponent<CanvasGroup>().DOFade(0f, config.evoMergeTime).SetLink(icon).OnComplete(() => Destroy(icon));
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
            combo.SetParent(landscape ? comboLandscape : comboPortrait, false);

        }
    }
}
