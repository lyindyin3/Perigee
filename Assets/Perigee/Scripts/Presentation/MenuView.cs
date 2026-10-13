using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Меню и Мастерская внутри него: чертежи, способности, облик.
    public class MenuView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject workshopPanel;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text bestTime;
        [SerializeField] TMP_Text playLabel;
        [SerializeField] TMP_Text dailyLabel;
        [SerializeField] TMP_Text eraLabel;
        [SerializeField] TMP_Text workshopLabel;
        [SerializeField] TMP_Text workshopHeader;
        [SerializeField] TMP_Text dustText;
        [SerializeField] TMP_Text backLabel;
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

        // выбранная эра: номер в config.eras
        int era;
        // выбранный узел дерева: индекс в config.blueprints
        int node;

        void Awake()
        {
            title.text = Strings.GameTitle;
            playLabel.text = Strings.Play;
            dailyLabel.text = Strings.Daily;
            workshopLabel.text = Strings.Workshop;
            workshopHeader.text = Strings.Workshop;
            backLabel.text = Strings.Back;
            for (int i = 0; i < tabLabels.Length; i++) tabLabels[i].text = Strings.WorkshopTab[i];
            for (int i = 0; i < branchLabels.Length; i++) branchLabels[i].text = Strings.BranchTitle[i];
            for (int i = 0; i < skinKindLabels.Length; i++) skinKindLabels[i].text = Strings.SkinKindTitle[i];
        }

        void OnEnable()
        {
            ShowMain();
        }

        public void Play()
        {
            loop.StartFromMenu(era, RunMode.Run);
        }

        public void Daily()
        {
            loop.StartFromMenu(era, RunMode.Daily);
        }

        // по кругу среди открытых эр
        public void NextEra()
        {
            era = (era + 1) % loop.Progress.ErasOpen;
            ShowEra();
        }

        public void OpenWorkshop()
        {
            mainPanel.SetActive(false);
            workshopPanel.SetActive(true);
            ShowTab(0);
        }

        public void ShowMain()
        {
            mainPanel.SetActive(true);
            workshopPanel.SetActive(false);
            ShowEra();
        }

        // вызывают кнопки вкладок, номер вкладки задан в сцене
        public void ShowTab(int tab)
        {
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
            node = i;
            Refresh();
        }

        // кнопка в полосе под деревом: следующий уровень выбранного узла
        public void BuyNode()
        {
            loop.Blueprints.Buy(node);
            Refresh();
        }

        // вызывают строки способностей, номер способности задан в сцене
        public void BuyAbility(int i)
        {
            loop.Shop.Buy(config.abilities[i]);
            Refresh();
        }

        // вызывают плитки обликов, номер облика задан в сцене: купленный облик выбирается, не купленный покупается
        public void PressSkin(int i)
        {
            SkinDef def = config.skins[i];
            if (loop.Shop.Owned(def.id)) loop.Shop.Select(def);
            else loop.Shop.Buy(def);
            Refresh();
        }

        // рекорд у каждой эры свой
        void ShowEra()
        {
            eraLabel.text = string.Format(Strings.EraFormat, Strings.EraTitle[era]);
            bestTime.text = string.Format(Strings.BestTimeFormat, Strings.Clock(loop.Save.Data.bestTimes[era]));
        }

        // вся Мастерская заново: покупка меняет пыль, а с ней и то, что можно купить на других вкладках
        void Refresh()
        {
            dustText.text = string.Format(Strings.DustFormat, loop.Save.Data.dust);
            ShowBlueprints();
            ShowAbilities();
            ShowSkins();
        }

        void ShowBlueprints()
        {
            Blueprints blueprints = loop.Blueprints;
            BlueprintDef[] defs = config.blueprints;
            for (int i = 0; i < nodes.Length; i++)
            {
                int id = (int)defs[i].id;
                nodes[i].Show(art.blueprintIcons[id], Strings.BlueprintTitle[id], Strings.Pips(blueprints.Level(i), defs[i].levels.Length), !blueprints.Open(i), i == node);
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
                abilityRows[i].Show(art.abilityIcons[id], Strings.AbilityTitle[id], Strings.AbilityText[id], timing, state, shop.CanBuy(def));
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
                if (def.kind == SkinKind.Satellite) skinTiles[i].Show(art.satelliteSkins[k].Body, name, state, active);
                else skinTiles[i].Show(art.starSkins[k], name, state, active);
            }
        }
    }
}
