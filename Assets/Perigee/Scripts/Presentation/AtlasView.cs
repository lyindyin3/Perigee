using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Perigee
{
    // Панель меню АТЛАС (GDD 10.9): вкладки ОСНОВЫ (страницы со стрелками и свайпом), БЕСТИАРИЙ, КАРТОЧКИ, СПОСОБНОСТИ.
    public class AtlasView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        const int BasicsTab = 0;

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] AudioManager sfx;
        [SerializeField] TMP_Text header;
        // вкладки: кнопка, подпись и панель; порядок как в Strings.AtlasTab
        [SerializeField] Button[] tabButtons;
        [SerializeField] TMP_Text[] tabLabels;
        [SerializeField] GameObject[] tabPanels;

        // ОСНОВЫ
        [SerializeField] Image picture;
        [SerializeField] TMP_Text pageTitle;
        [SerializeField] TMP_Text pageText;
        [SerializeField] TMP_Text pageNumber;

        // БЕСТИАРИЙ: плитки по порядку config.bestiary и карточка выбранного типа
        [SerializeField] AtlasEntry[] entries;
        [SerializeField] GameObject details;
        [SerializeField] Image detailIcon;
        [SerializeField] TMP_Text detailName;
        [SerializeField] TMP_Text detailStats;
        [SerializeField] TMP_Text detailTip;
        [SerializeField] TMP_Text detailEra;

        // КАРТОЧКИ: сначала все карточки по цветам, за ними реликвии
        [SerializeField] AtlasCard[] cards;

        // СПОСОБНОСТИ: строки по индексу в config.abilities
        [SerializeField] ShopRow[] abilityRows;

        int tab;
        int page;
        // выбранная плитка бестиария; -1 = ничего
        int entry = -1;

        void Awake()
        {
            Loc.Changed += Refresh;
        }

        void OnDestroy()
        {
            Loc.Changed -= Refresh;
        }

        // MenuView открыл панель: первая вкладка, первая страница
        public void Open()
        {
            page = 0;
            entry = -1;
            Select(BasicsTab);
        }

        // вызывают кнопки вкладок, номер вкладки задан в сцене
        public void ShowTab(int i)
        {
            sfx.Play(SfxId.UiClick);
            Select(i);
        }

        void Select(int i)
        {
            tab = i;
            for (int k = 0; k < tabPanels.Length; k++)
            {
                tabPanels[k].SetActive(k == i);
                // кнопка открытой вкладки не нажимается: так она выглядит выбранной
                tabButtons[k].interactable = k != i;
            }
            Refresh();
        }

        public void PrevPage()
        {
            Turn(-1);
        }

        public void NextPage()
        {
            Turn(1);
        }

        // вызывают плитки бестиария, номер плитки задан в сцене
        public void SelectEntry(int i)
        {
            sfx.Play(SfxId.UiClick);
            entry = i;
            ShowBestiary();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        // свайп по ОСНОВАМ: влево — следующая страница, вправо — предыдущая
        public void OnEndDrag(PointerEventData eventData)
        {
            if (tab != BasicsTab) return;
            float dx = eventData.position.x - eventData.pressPosition.x;
            if (Mathf.Abs(dx) < config.atlasSwipe) return;
            Turn(dx < 0f ? 1 : -1);
        }

        // страницы идут по кругу
        void Turn(int step)
        {
            sfx.Play(SfxId.UiClick);
            int count = Strings.AtlasPageTitle.Length;
            page = (page + step + count) % count;
            ShowBasics();
        }

        void Refresh()
        {
            header.text = Strings.Atlas;
            for (int i = 0; i < tabLabels.Length; i++) tabLabels[i].text = Strings.AtlasTab[i];
            ShowBasics();
            ShowBestiary();
            ShowCards();
            ShowAbilities();
        }

        void ShowBasics()
        {
            picture.sprite = art.atlasPictures[page].sprite;
            pageTitle.text = Strings.AtlasPageTitle[page];
            pageText.text = Strings.AtlasPageText[page];
            pageNumber.text = string.Format(Strings.CountFormat, page + 1, Strings.AtlasPageTitle.Length);
        }

        void ShowBestiary()
        {
            Achievements achievements = loop.Achievements;
            BestiaryEntry[] beasts = config.bestiary;
            for (int i = 0; i < entries.Length; i++)
            {
                EnemyType type = beasts[i].type;
                Color color;
                Sprite icon = Icon(type, out color);
                entries[i].Show(icon, color, Strings.EnemyName[(int)type], achievements.Seen(type), i == entry);
            }

            details.SetActive(entry >= 0);
            if (entry < 0) return;
            EnemyType selected = beasts[entry].type;
            EnemyDef def = Def(selected);
            Color tint;
            detailIcon.sprite = Icon(selected, out tint);
            detailIcon.color = tint;
            detailName.text = Strings.EnemyName[(int)selected];
            detailStats.text = string.Format(CultureInfo.InvariantCulture, Strings.BestiaryStatsFormat, def.hp, def.speed, def.armor);
            bool boss = Array.Exists(config.eras, era => era.boss == selected);
            detailTip.text = boss ? Strings.BossTip(selected) : Strings.EnemyTip[(int)selected];
            detailEra.text = string.Format(Strings.EraNumberFormat, beasts[entry].era);
        }

        // картинка типа: своя из enemyIcons, а пустая запись = картинка и цвет Body из префаба
        Sprite Icon(EnemyType type, out Color color)
        {
            Sprite icon = art.enemyIcons[(int)type].sprite;
            SpriteRenderer look = art.enemies[(int)type].prefab.Body;
            color = icon != null ? Color.white : look.color;
            return icon != null ? icon : look.sprite;
        }

        EnemyDef Def(EnemyType type)
        {
            return Array.Find(config.enemies, def => def.id == type);
        }

        void ShowCards()
        {
            SaveData data = loop.Save.Data;
            CardDef[] defs = config.cards;
            int k = 0;
            // по цветам рамок: кремовые, красные, чёрные, эволюции
            for (int color = 0; color <= (int)CardColor.Evolution; color++)
                for (int i = 0; i < defs.Length; i++)
                    if ((int)defs[i].color == color) cards[k++].ShowCard(defs[i], (data.seenCards & 1L << (int)defs[i].id) != 0);

            RelicDef[] relics = config.relics;
            for (int i = 0; i < relics.Length; i++) cards[k++].ShowRelic(relics[i], (data.seenRelics & 1 << (int)relics[i].id) != 0);
        }

        // как в Мастерской, но без покупки
        void ShowAbilities()
        {
            for (int i = 0; i < abilityRows.Length; i++)
            {
                AbilityDef def = config.abilities[i];
                int id = (int)def.id;
                string timing = def.duration > 0f
                    ? string.Format(CultureInfo.InvariantCulture, Strings.AbilityTimingFormat, def.duration, def.cooldown)
                    : string.Format(CultureInfo.InvariantCulture, Strings.AbilityInstantFormat, def.cooldown);
                abilityRows[i].Show(art.abilityIcons[id].sprite, Strings.AbilityTitle[id], Strings.AbilityText[id], timing, string.Empty, false);
            }
        }
    }
}
