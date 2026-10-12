using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Меню и экран чертежей внутри него.
    public class MenuView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject blueprintPanel;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text bestTime;
        [SerializeField] TMP_Text playLabel;
        [SerializeField] TMP_Text dailyLabel;
        [SerializeField] TMP_Text eraLabel;
        [SerializeField] TMP_Text blueprintsLabel;
        [SerializeField] TMP_Text blueprintsHeader;
        [SerializeField] TMP_Text dustText;
        [SerializeField] TMP_Text backLabel;
        // строки чертежей, по индексу в config.blueprints
        [SerializeField] TMP_Text[] names;
        [SerializeField] TMP_Text[] effects;
        [SerializeField] TMP_Text[] costs;
        [SerializeField] Button[] buyButtons;

        // выбранная эра: номер в config.eras
        int era;

        void Awake()
        {
            title.text = Strings.GameTitle;
            playLabel.text = Strings.Play;
            dailyLabel.text = Strings.Daily;
            blueprintsLabel.text = Strings.Blueprints;
            blueprintsHeader.text = Strings.Blueprints;
            backLabel.text = Strings.Back;
        }

        void OnEnable()
        {
            ShowMain();
        }

        public void Play()
        {
            loop.StartFromMenu(era, false);
        }

        public void Daily()
        {
            loop.StartFromMenu(era, true);
        }

        // по кругу среди открытых эр
        public void NextEra()
        {
            era = (era + 1) % loop.Save.Data.erasUnlocked;
            eraLabel.text = string.Format(Strings.EraFormat, Strings.EraTitle[era]);
        }

        public void OpenBlueprints()
        {
            mainPanel.SetActive(false);
            blueprintPanel.SetActive(true);
            ShowBlueprints();
        }

        public void ShowMain()
        {
            mainPanel.SetActive(true);
            blueprintPanel.SetActive(false);
            bestTime.text = string.Format(Strings.BestTimeFormat, Strings.Clock(loop.Save.Data.bestTime));
            eraLabel.text = string.Format(Strings.EraFormat, Strings.EraTitle[era]);
        }

        // вызывают кнопки строк, номер чертежа задан в сцене
        public void Buy(int i)
        {
            loop.Blueprints.Buy(i);
            ShowBlueprints();
        }

        void ShowBlueprints()
        {
            Blueprints blueprints = loop.Blueprints;
            dustText.text = string.Format(Strings.DustFormat, loop.Save.Data.dust);
            for (int i = 0; i < names.Length; i++)
            {
                BlueprintDef def = config.blueprints[i];
                names[i].text = string.Format(Strings.BlueprintLevelFormat, Strings.BlueprintTitle[(int)def.id], blueprints.Level(i), def.costs.Length);
                effects[i].text = string.Format(CultureInfo.InvariantCulture, Strings.BlueprintEffect[(int)def.id], def.perLevel.value);
                costs[i].text = blueprints.Maxed(i) ? Strings.BlueprintMax : blueprints.Cost(i).ToString();
                // кнопка серая, если пыли не хватает или уровень последний
                buyButtons[i].interactable = blueprints.CanBuy(i);
            }
        }
    }
}
