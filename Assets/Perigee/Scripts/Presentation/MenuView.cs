using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Меню: главная панель, экран эр с режимами и классами звёзд, испытания и Мастерская (чертежи, способности, облик, звёзды). Видна одна панель.
    public class MenuView : MonoBehaviour
    {
        // счётчик пыли, который досчитывает до нового значения
        class DustCounter
        {
            public TMP_Text text;
            // -1 = ещё не показан: первое значение встаёт сразу
            public int shown = -1;
        }

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] SettingsView settings;
        [SerializeField] AudioManager sfx;
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject erasPanel;
        [SerializeField] GameObject challengesPanel;
        [SerializeField] GameObject workshopPanel;
        // подписи кнопок «Назад» на всех панелях
        [SerializeField] TMP_Text[] backLabels;

        // главная панель
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text mainDust;
        [SerializeField] TMP_Text statsLine;
        [SerializeField] TMP_Text playLabel;
        [SerializeField] TMP_Text challengesLabel;
        [SerializeField] TMP_Text workshopLabel;
        [SerializeField] TMP_Text settingsLabel;
        // кнопки, которых нет, пока не пройдено обучение
        [SerializeField] GameObject[] lateButtons;

        // экран эр: вкладки режимов по порядку RunMode (Забег, Бесконечный, День)
        [SerializeField] TMP_Text erasHeader;
        [SerializeField] Button[] modeTabs;
        [SerializeField] TMP_Text[] modeTabLabels;
        // замок с условием поверх вкладки Бесконечного; он же не пропускает нажатие
        [SerializeField] GameObject endlessLock;
        [SerializeField] TMP_Text endlessLockText;
        // лента карточек эр (Забег и Бесконечный), по индексу в config.eras
        [SerializeField] GameObject eraList;
        [SerializeField] EraCard[] eraCards;
        // карточка Дня
        [SerializeField] GameObject dailyCard;
        [SerializeField] TMP_Text dailyTitle;
        [SerializeField] TMP_Text dailyModifier;
        [SerializeField] TMP_Text dailyText;
        [SerializeField] TMP_Text dailyBonus;
        [SerializeField] TMP_Text dailyBest;
        [SerializeField] TMP_Text startLabel;
        // ряд классов звёзд под эрами, по порядку StarClassId, и текст выбранного класса под ним
        [SerializeField] ClassTile[] classTiles;
        [SerializeField] TMP_Text classText;

        // испытания: строки по индексу в config.challenges
        [SerializeField] TMP_Text challengesHeader;
        [SerializeField] ChallengeRow[] challengeRows;

        [SerializeField] TMP_Text workshopHeader;
        [SerializeField] TMP_Text dustText;
        // вкладки Мастерской: кнопка, подпись и панель; порядок как в Strings.WorkshopTab
        [SerializeField] Button[] tabButtons;
        [SerializeField] TMP_Text[] tabLabels;
        [SerializeField] GameObject[] tabPanels;
        // заголовки веток дерева, по порядку BlueprintBranch
        [SerializeField] TMP_Text[] branchLabels;
        // узлы дерева, по индексу в config.blueprints
        [SerializeField] BlueprintNode[] nodes;
        // полоса под деревом: выбранный узел
        [SerializeField] TMP_Text nodeTitle;
        [SerializeField] TMP_Text nodeText;
        [SerializeField] Button nodeBuy;
        [SerializeField] TMP_Text nodeBuyLabel;
        // строки способностей, по индексу в config.abilities
        [SerializeField] ShopRow[] abilityRows;
        // подписи рядов облика, по порядку SkinKind
        [SerializeField] TMP_Text[] skinKindLabels;
        // плитки обликов, по индексу в config.skins
        [SerializeField] SkinTile[] skinTiles;
        // вкладка ЗВЁЗДЫ: плитки классов, по порядку StarClassId
        [SerializeField] ClassTile[] classShopTiles;

        // выбранная эра: номер в config.eras
        int era;
        // открытая вкладка экрана эр: номер RunMode
        int mode;
        // выбранный узел дерева: индекс в config.blueprints
        int node;
        // панели меню: прозрачность и место, куда они въезжают
        GameObject[] panels;
        CanvasGroup[] panelGroups;
        Vector2[] panelHomes;
        DustCounter mainCounter;
        DustCounter shopCounter;
        // купленное в этом кадре прыгает само, толчок нажатия его не перебивает
        Transform bought;
        int boughtFrame = -1;

        void Awake()
        {
            panels = new[] { mainPanel, erasPanel, challengesPanel, workshopPanel };
            panelGroups = new CanvasGroup[panels.Length];
            panelHomes = new Vector2[panels.Length];
            for (int i = 0; i < panels.Length; i++)
            {
                panelGroups[i] = panels[i].GetComponent<CanvasGroup>();
                panelHomes[i] = ((RectTransform)panels[i].transform).anchoredPosition;
            }
            mainCounter = new DustCounter { text = mainDust };
            shopCounter = new DustCounter { text = dustText };

            // любая кнопка меню при нажатии сжимается и возвращается к своему размеру
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Transform t = buttons[i].transform;
                Vector3 size = t.localScale;
                buttons[i].onClick.AddListener(() => Press(t, size));
            }

            Loc.Changed += Localize;
            Localize();
        }

        void Press(Transform t, Vector3 size)
        {
            if (t == bought && Time.frameCount == boughtFrame) return;
            t.DOKill(true);
            t.localScale = size * config.menuPressScale;
            t.DOScale(size, config.menuPressTime).SetLink(gameObject);
        }

        // покупка: купленное подпрыгивает
        void Bought(Transform t)
        {
            bought = t;
            boughtFrame = Time.frameCount;
            t.DOKill(true);
            t.DOPunchScale(Vector3.one * config.menuBuyPunch, config.menuBuyPunchTime).SetLink(gameObject);
        }

        // пыль досчитывает до нового значения
        void Count(DustCounter counter, int value)
        {
            DOTween.Kill(counter);
            if (counter.shown < 0)
            {
                counter.shown = value;
                counter.text.text = string.Format(Strings.DustFormat, value);
                return;
            }
            DOTween.To(() => counter.shown, v =>
            {
                counter.shown = v;
                counter.text.text = string.Format(Strings.DustFormat, v);
            }, value, config.dustCountTime).SetTarget(counter).SetLink(gameObject);
        }

        void OnDestroy()
        {
            Loc.Changed -= Localize;
        }

        // смена языка идёт из настроек, а они открываются только над главной панелью
        void Localize()
        {
            if (mainPanel.activeSelf) Main();
            title.text = Strings.GameTitle;
            playLabel.text = Strings.Play;
            challengesLabel.text = Strings.Challenges;
            workshopLabel.text = Strings.Workshop;
            settingsLabel.text = Strings.Settings;
            erasHeader.text = Strings.Play;
            endlessLockText.text = Strings.EndlessLocked;
            dailyTitle.text = Strings.DailyTitle;
            startLabel.text = Strings.Start;
            challengesHeader.text = Strings.Challenges;
            workshopHeader.text = Strings.Workshop;
            for (int i = 0; i < backLabels.Length; i++) backLabels[i].text = Strings.Back;
            for (int i = 0; i < modeTabLabels.Length; i++) modeTabLabels[i].text = Strings.ModeTab[i];
            for (int i = 0; i < tabLabels.Length; i++) tabLabels[i].text = Strings.WorkshopTab[i];
            for (int i = 0; i < branchLabels.Length; i++) branchLabels[i].text = Strings.BranchTitle[i];
            for (int i = 0; i < skinKindLabels.Length; i++) skinKindLabels[i].text = Strings.SkinKindTitle[i];
        }

        void OnEnable()
        {
            // меню открылось заново: главная панель въезжает
            mainPanel.SetActive(false);
            Main();
        }

        // ИГРАТЬ: экран эр; пока обучение не пройдено, сразу забег-обучение
        public void Play()
        {
            sfx.Play(SfxId.UiClick);
            if (loop.Save.Data.tutorialDone) OpenEras();
            else loop.StartFromMenu(0, RunMode.Run, 0);
        }

        public void OpenEras()
        {
            sfx.Play(SfxId.UiClick);
            Show(erasPanel);
            SaveData data = loop.Save.Data;
            // выбрана эра прошлого забега, если она всё ещё открыта
            era = Mathf.Min(data.lastEra, loop.Progress.ErasOpen - 1);
            // открыта вкладка прошлого режима; после испытания и при закрытом Бесконечном это Забег
            var last = (RunMode)data.lastMode;
            bool known = last == RunMode.Daily || last == RunMode.Endless && loop.Progress.EndlessOpen;
            ShowMode(known ? data.lastMode : (int)RunMode.Run);
            ShowClasses();
        }

        // вызывают плитки классов на экране эр, номер класса задан в сцене; выбор сохраняется
        public void SelectClass(int i)
        {
            sfx.Play(SfxId.UiClick);
            loop.Shop.Select((StarClassId)i);
            ShowClasses();
        }

        // вызывают плитки вкладки ЗВЁЗДЫ, номер класса задан в сцене
        public void BuyClass(int i)
        {
            loop.Shop.Buy(config.starClasses[i]);
            sfx.Play(SfxId.Buy);
            Bought(classShopTiles[i].transform);
            Refresh();
        }

        // вызывают кнопки вкладок режимов, номер RunMode задан в сцене
        public void ShowMode(int tab)
        {
            sfx.Play(SfxId.UiClick);
            mode = tab;
            // кнопка открытой вкладки не нажимается: так она выглядит выбранной
            for (int i = 0; i < modeTabs.Length; i++) modeTabs[i].interactable = i != tab;
            endlessLock.SetActive(!loop.Progress.EndlessOpen);

            bool daily = tab == (int)RunMode.Daily;
            eraList.SetActive(!daily);
            dailyCard.SetActive(daily);
            if (daily) ShowDaily();
            else ShowEras();
        }

        // вызывают карточки эр, номер эры задан в сцене
        public void SelectEra(int i)
        {
            sfx.Play(SfxId.UiClick);
            era = i;
            ShowEras();
        }

        // СТАРТ: забег в выбранной эре и режиме открытой вкладки
        public void StartRun()
        {
            sfx.Play(SfxId.UiClick);
            loop.StartFromMenu(era, (RunMode)mode, 0);
        }

        public void OpenChallenges()
        {
            sfx.Play(SfxId.UiClick);
            Show(challengesPanel);
            Progress progress = loop.Progress;
            for (int i = 0; i < challengeRows.Length; i++)
            {
                ChallengeDef def = config.challenges[i];
                bool open = progress.ChallengeOpen(i);
                string status = string.Empty;
                if (!open) status = string.Format(Strings.ChallengeLockedFormat, def.era);
                else if (progress.ChallengeDone(i)) status = Strings.ChallengeDone;
                string reward = def.hasSkin
                    ? string.Format(Strings.RewardSkinFormat, Strings.SkinTitle[(int)def.rewardSkin])
                    : string.Format(Strings.RewardDustFormat, def.rewardDust);

                ChallengeRow row = challengeRows[i];
                row.Show(i + 1, Strings.ChallengeTitle[i], string.Format(Strings.EraNumberFormat, def.era), Strings.Modifiers(def.mods, Strings.ModifierText), reward, status, open);
                if (!def.hasSkin) continue;

                // облик-награда с картинкой: в SkinId сначала идут облики спутника, за ними облики звезды
                int k = Shop.ArtIndex(def.rewardSkin);
                if (def.rewardSkin < SkinId.Sun) row.ShowSkin(art.satelliteSkins[k].prefab.Body);
                else row.ShowSkin(art.starSkins[k].body);
            }
        }

        // вызывают строки испытаний, номер испытания задан в сцене
        public void PlayChallenge(int i)
        {
            sfx.Play(SfxId.UiClick);
            loop.StartFromMenu(0, RunMode.Challenge, i);
        }

        public void OpenWorkshop()
        {
            sfx.Play(SfxId.UiClick);
            Show(workshopPanel);
            ShowTab(0);
        }

        public void OpenSettings()
        {
            sfx.Play(SfxId.UiClick);
            settings.Open();
        }

        // кнопки «Назад»
        public void ShowMain()
        {
            sfx.Play(SfxId.UiBack);
            Main();
        }

        void Main()
        {
            Show(mainPanel);
            SaveData data = loop.Save.Data;
            // пока обучение не пройдено, в меню только ИГРАТЬ
            for (int i = 0; i < lateButtons.Length; i++) lateButtons[i].SetActive(data.tutorialDone);
            Count(mainCounter, data.dust);
            float best = 0f;
            for (int i = 0; i < data.bestTimes.Length; i++) best = Mathf.Max(best, data.bestTimes[i]);
            statsLine.text = string.Format(Strings.StatsFormat, data.runs, Strings.Clock(best));
        }

        // вызывают кнопки вкладок, номер вкладки задан в сцене
        public void ShowTab(int tab)
        {
            sfx.Play(SfxId.UiClick);
            for (int i = 0; i < tabPanels.Length; i++)
            {
                tabPanels[i].SetActive(i == tab);
                // кнопка открытой вкладки не нажимается: так она выглядит выбранной
                tabButtons[i].interactable = i != tab;
            }
            Refresh();
        }

        // вызывают узлы дерева, номер чертежа задан в сцене
        public void SelectNode(int i)
        {
            sfx.Play(SfxId.UiClick);
            node = i;
            Refresh();
        }

        // кнопка в полосе под деревом: следующий уровень выбранного узла
        public void BuyNode()
        {
            loop.Blueprints.Buy(node);
            sfx.Play(SfxId.Buy);
            Bought(nodes[node].transform);
            Refresh();
        }

        // вызывают строки способностей, номер способности задан в сцене
        public void BuyAbility(int i)
        {
            loop.Shop.Buy(config.abilities[i]);
            sfx.Play(SfxId.Buy);
            Bought(abilityRows[i].transform);
            Refresh();
        }

        // вызывают плитки обликов, номер облика задан в сцене: купленный облик выбирается, не купленный покупается
        public void PressSkin(int i)
        {
            SkinDef def = config.skins[i];
            if (loop.Shop.Owned(def.id))
            {
                loop.Shop.Select(def);
                sfx.Play(SfxId.UiClick);
            }
            else
            {
                loop.Shop.Buy(def);
                sfx.Play(SfxId.Buy);
                Bought(skinTiles[i].transform);
            }
            Refresh();
        }

        // видна одна панель; открытая заново въезжает сбоку и проявляется
        void Show(GameObject panel)
        {
            for (int i = 0; i < panels.Length; i++)
            {
                bool show = panels[i] == panel;
                bool opened = show && !panels[i].activeSelf;
                panels[i].SetActive(show);
                if (!opened) continue;

                var rect = (RectTransform)panels[i].transform;
                rect.DOKill();
                rect.anchoredPosition = panelHomes[i] + Vector2.right * config.panelSlide;
                rect.DOAnchorPos(panelHomes[i], config.panelFade).SetLink(gameObject);
                panelGroups[i].DOKill();
                panelGroups[i].alpha = 0f;
                panelGroups[i].DOFade(1f, config.panelFade).SetLink(gameObject);
            }
        }

        // карточки эр: рекорд у каждой эры свой и зависит от вкладки (Забег или Бесконечный)
        void ShowEras()
        {
            SaveData data = loop.Save.Data;
            float[] best = mode == (int)RunMode.Endless ? data.bestEndless : data.bestTimes;
            int open = loop.Progress.ErasOpen;
            for (int i = 0; i < eraCards.Length; i++)
            {
                string boss = string.Format(Strings.BossFormat, Strings.BossTitle(config.eras[i].boss));
                string record = string.Format(Strings.BestTimeFormat, Strings.Clock(best[i]));
                // эру открывает победа над боссом предыдущей: её номер с единицы равен i
                string locked = string.Format(Strings.EraLockedFormat, i);
                eraCards[i].Show(art.eraIcons[i].sprite, Strings.EraTitle[i], Strings.EraRule[i], boss, Strings.EraEnemies[i], record, (data.erasWon & 1 << i) != 0, i >= open, locked, i == era);
            }
        }

        void ShowDaily()
        {
            Progress progress = loop.Progress;
            RunModifier mods = progress.DailyMod;
            dailyModifier.text = Strings.Modifiers(mods, Strings.ModifierTitle);
            dailyText.text = Strings.Modifiers(mods, Strings.ModifierText);
            dailyBonus.text = progress.DailyBonusLeft ? string.Format(Strings.DailyBonusFormat, config.dailyDustMul) : Strings.DailyPlayed;
            dailyBest.text = string.Format(Strings.BestScoreFormat, loop.Save.Data.bestDaily);
        }

        // вся Мастерская заново: покупка меняет пыль, а с ней и то, что можно купить на других вкладках
        void Refresh()
        {
            Count(shopCounter, loop.Save.Data.dust);
            ShowBlueprints();
            ShowAbilities();
            ShowSkins();
            ShowClassShop();
        }

        void ShowBlueprints()
        {
            Blueprints blueprints = loop.Blueprints;
            BlueprintDef[] defs = config.blueprints;
            for (int i = 0; i < nodes.Length; i++)
            {
                int id = (int)defs[i].id;
                nodes[i].Show(art.blueprintIcons[id].sprite, Strings.BlueprintTitle[id], Strings.Pips(blueprints.Level(i), defs[i].levels.Length), !blueprints.Open(i), i == node);
            }

            int selected = (int)defs[node].id;
            nodeTitle.text = Strings.BlueprintTitle[selected];
            nodeText.text = string.Format(CultureInfo.InvariantCulture, Strings.BlueprintText[selected], blueprints.Total(node));
            if (blueprints.Maxed(node)) nodeBuyLabel.text = Strings.BlueprintMax;
            else if (!blueprints.Open(node)) nodeBuyLabel.text = string.Format(Strings.NeedLevelFormat, Strings.BlueprintTitle[(int)defs[blueprints.Previous(node)].id]);
            else nodeBuyLabel.text = string.Format(Strings.BuyFormat, blueprints.Cost(node));
            nodeBuy.interactable = blueprints.CanBuy(node);
        }

        void ShowAbilities()
        {
            Shop shop = loop.Shop;
            for (int i = 0; i < abilityRows.Length; i++)
            {
                AbilityDef def = config.abilities[i];
                int id = (int)def.id;
                string timing = def.duration > 0f
                    ? string.Format(CultureInfo.InvariantCulture, Strings.AbilityTimingFormat, def.duration, def.cooldown)
                    : string.Format(CultureInfo.InvariantCulture, Strings.AbilityInstantFormat, def.cooldown);
                string state = shop.Owned(def.id) ? Strings.Owned : string.Format(Strings.BuyFormat, def.price);
                abilityRows[i].Show(art.abilityIcons[id].sprite, Strings.AbilityTitle[id], Strings.AbilityText[id], timing, state, shop.CanBuy(def));
            }
        }

        void ShowSkins()
        {
            Shop shop = loop.Shop;
            for (int i = 0; i < skinTiles.Length; i++)
            {
                SkinDef def = config.skins[i];
                string name = Strings.SkinTitle[(int)def.id];
                bool owned = shop.Owned(def.id);
                bool selected = shop.Selected(def);

                // подпись кнопки: выбран, выбрать, цена или условие, за которое облик дают
                string state;
                if (selected) state = Strings.SkinSelected;
                else if (owned) state = Strings.SkinSelect;
                else if (def.eraWin > 0) state = string.Format(Strings.EraWinFormat, def.eraWin);
                else if (def.challenge > 0) state = string.Format(Strings.ChallengeFormat, Strings.ChallengeTitle[def.challenge - 1]);
                else state = string.Format(Strings.BuyFormat, def.price);
                bool active = owned ? !selected : shop.CanBuy(def);

                int k = Shop.ArtIndex(def.id);
                if (def.kind == SkinKind.Satellite) skinTiles[i].Show(art.satelliteSkins[k].prefab.Body, name, state, active);
                else skinTiles[i].Show(art.starSkins[k], name, state, active);
            }
        }

        // условие закрытого класса: победа в эре или цена
        static string ClassLock(StarClassDef def)
        {
            return def.eraWin > 0 ? string.Format(Strings.EraWinFormat, def.eraWin) : string.Format(Strings.PriceFormat, def.price);
        }

        void ShowClasses()
        {
            Shop shop = loop.Shop;
            StarClassId selected = shop.StarClass;
            for (int i = 0; i < classTiles.Length; i++)
            {
                StarClassDef def = config.starClasses[i];
                classTiles[i].ShowPick(art.starClasses[i].icon, def.tint, Strings.ClassTitle[i], !shop.Owned(def.id), ClassLock(def), def.id == selected);
            }
            classText.text = Strings.ClassText[(int)selected];
        }

        void ShowClassShop()
        {
            Shop shop = loop.Shop;
            for (int i = 0; i < classShopTiles.Length; i++)
            {
                StarClassDef def = config.starClasses[i];
                string state = shop.Owned(def.id) ? Strings.Owned : def.eraWin > 0 ? ClassLock(def) : string.Format(Strings.BuyFormat, def.price);
                string stats = string.Format(CultureInfo.InvariantCulture, Strings.ClassStatsFormat, def.maxHp, def.starRadius);
                classShopTiles[i].ShowShop(art.starClasses[i].icon, def.tint, Strings.ClassTitle[i], Strings.ClassText[i], stats, state, shop.CanBuy(def));
            }
        }
    }
}
