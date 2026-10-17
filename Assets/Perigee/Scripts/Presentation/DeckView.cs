using TMPro;
using UnityEngine;

namespace Perigee
{
    // «Твои карточки» (Tab или нажатие на иконку карточки в HUD): игра на паузе, видны взятые карточки и реликвии со стаками, как в Атласе.
    public class DeckView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] AudioManager sfx;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text empty;
        [SerializeField] TMP_Text resumeLabel;
        // клетки по порядку; лишние выключены, сетка сдвигает взятые вплотную
        [SerializeField] AtlasCard[] cards;

        void Awake()
        {
            Loc.Changed += Show;
        }

        void OnDestroy()
        {
            Loc.Changed -= Show;
        }

        // GameLoop включает окно при входе в Deck
        void OnEnable()
        {
            Show();
        }

        public void Resume()
        {
            loop.ToggleDeck();
        }

        void Show()
        {
            title.text = Strings.DeckTitle;
            empty.text = Strings.DeckEmpty;
            resumeLabel.text = Strings.Resume;

            RunState run = loop.Run;
            CardDef[] defs = config.cards;
            int k = 0;
            // по цветам рамок, как в Атласе: кремовые, красные, чёрные, эволюции; за ними реликвии
            for (int color = 0; color <= (int)CardColor.Evolution; color++)
                for (int i = 0; i < defs.Length; i++)
                    if ((int)defs[i].color == color && run.cardStacks[i] > 0) cards[k++].ShowTaken(defs[i], run.cardStacks[i]);

            RelicDef[] relics = config.relics;
            for (int i = 0; i < relics.Length; i++)
                if (run.relicStacks[i] > 0) cards[k++].ShowTakenRelic(relics[i], run.relicStacks[i]);

            empty.gameObject.SetActive(k == 0);
            for (int i = 0; i < cards.Length; i++) cards[i].gameObject.SetActive(i < k);
        }
    }
}
