using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Карточка на экране выбора. Рамку, кайму и иконку код берёт из ArtSet, тексты из Strings; остальное задано в префабе.
    public class CardView : MonoBehaviour
    {
        // белая вспышка до слияния: прозрачная, но белая, чтобы DOFade давал белый
        static readonly Color WhiteClear = new Color(1f, 1f, 1f, 0f);

        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] Image frame;
        // прозрачность всей карточки: на экране выбора невыбранные гаснут
        [SerializeField] CanvasGroup group;
        // золотая кайма: видна у редких карточек и пульсирует
        [SerializeField] Image rareRim;
        // блик эволюции: полоса внутри маски по размеру карточки, проходит её слева направо
        [SerializeField] RectTransform shine;
        // значок эры: виден у карточек эр
        [SerializeField] Image eraBadge;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text pips;
        [SerializeField] TMP_Text plusLine;
        [SerializeField] TMP_Text minusLine;
        // эволюция: значки двух карточек-источников с «+» между ними; каждый значок лежит в своём слоте в нуле
        [SerializeField] GameObject evoSources;
        [SerializeField] Image sourceA;
        [SerializeField] Image sourceB;
        // белая вспышка поверх карточки при слиянии
        [SerializeField] Image flash;

        // stacks = сколько стаков станет после выбора
        public void Show(CardDef card, int stacks)
        {
            int id = (int)card.id;
            Reset();
            frame.sprite = art.cardFrames[(int)card.color].sprite;
            bool rare = card.rarity == CardRarity.Rare;
            rareRim.gameObject.SetActive(rare);
            if (rare) rareRim.DOFade(config.rareRimMinAlpha, config.rareRimPulseTime).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetTarget(this).SetLink(gameObject);
            eraBadge.gameObject.SetActive(card.era != 0);
            if (card.era != 0) eraBadge.sprite = art.eraIcons[card.era - 1].sprite;
            icon.sprite = art.cardIcons[id].sprite;
            title.text = Strings.CardTitle[id];
            pips.text = Strings.Pips(stacks, card.maxStacks);
            plusLine.text = Strings.CardPlus[id];

            bool evolution = card.color == CardColor.Evolution;
            // у эволюции минуса нет, вместо него рецепт под названием
            minusLine.gameObject.SetActive(!evolution);
            minusLine.text = Strings.CardMinus[id];
            evoSources.SetActive(evolution);
            if (!evolution) return;
            sourceA.sprite = art.cardIcons[(int)card.sourceA].sprite;
            sourceB.sprite = art.cardIcons[(int)card.sourceB].sprite;

            // блик раз в shineInterval: от левого края маски за правый
            float width = ((RectTransform)shine.parent).rect.width;
            shine.gameObject.SetActive(true);
            shine.anchoredPosition = new Vector2(-width, 0f);
            Sequence pass = DOTween.Sequence().SetTarget(this).SetLink(gameObject).SetLoops(-1);
            pass.Append(shine.DOAnchorPosX(width, config.shineTime));
            pass.AppendInterval(config.shineInterval - config.shineTime);
        }

        // вид реликвии (её выбор строит M21): рамка реликвии, одна строка эффекта, точки стаков
        public void ShowRelic(Sprite relicIcon, string relicTitle, string line, int stacks, int maxStacks)
        {
            Reset();
            frame.sprite = art.relicFrame;
            rareRim.gameObject.SetActive(false);
            eraBadge.gameObject.SetActive(false);
            evoSources.SetActive(false);
            minusLine.gameObject.SetActive(false);
            icon.sprite = relicIcon;
            title.text = relicTitle;
            pips.text = Strings.Pips(stacks, maxStacks);
            plusLine.text = line;
        }

        // выбрана эволюция: значки источников въезжают в главную иконку, карточка вспыхивает белым, иконка подпрыгивает
        public void Merge()
        {
            float time = config.evoMergeTime;
            Vector3 target = icon.rectTransform.position;
            Sequence merge = DOTween.Sequence().SetTarget(this).SetLink(gameObject);
            merge.Append(sourceA.rectTransform.DOMove(target, time));
            merge.Join(sourceB.rectTransform.DOMove(target, time));
            merge.Join(sourceA.DOFade(0f, time));
            merge.Join(sourceB.DOFade(0f, time));
            merge.Append(flash.DOFade(1f, 0f));
            merge.Append(flash.DOFade(0f, config.evoFlashTime));
            merge.Join(icon.rectTransform.DOPunchScale(Vector3.one * config.evoPunch, config.evoFlashTime));
        }

        // не выбрана: падает и гаснет
        public void Drop()
        {
            transform.DOLocalMoveY(-config.cardDropOffset, config.cardDropTime).SetRelative().SetLink(gameObject);
            group.DOFade(0f, config.cardDropTime).SetTarget(this).SetLink(gameObject);
        }

        // вид до слияния: значки на своих местах, вспышки, блика и пульса нет
        void Reset()
        {
            DOTween.Kill(this, true);
            group.alpha = 1f;
            Color rim = rareRim.color;
            rim.a = 1f;
            rareRim.color = rim;
            shine.gameObject.SetActive(false);
            sourceA.rectTransform.anchoredPosition = Vector2.zero;
            sourceB.rectTransform.anchoredPosition = Vector2.zero;
            sourceA.color = Color.white;
            sourceB.color = Color.white;
            icon.rectTransform.localScale = Vector3.one;
            flash.color = WhiteClear;
        }
    }
}
