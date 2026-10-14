using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Perigee
{
    // Комбо в HUD: «×{серия}» с прыжком на каждое убийство, золотая строка ступени, тонкая полоска окна. Видно от comboShowMin.
    public class ComboView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] ComboSystem combo;
        [SerializeField] ScreenFx screenFx;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text count;
        // строка «КОМБО ×1.2»: видна со ступени 1
        [SerializeField] TMP_Text tierLine;
        // полоска окна: опорная точка слева, код меняет масштаб по x
        [SerializeField] RectTransform timerBar;

        RectTransform rect;
        Vector2 home;
        // цвет счётчика из префаба: после золотой вспышки он возвращается
        Color countColor;
        int shown;
        int shownTier;
        // ступень в прошлом кадре: её рост вспыхивает золотом
        int lastTier;

        void Awake()
        {
            rect = (RectTransform)transform;
            home = rect.anchoredPosition;
            countColor = count.color;
            group.alpha = 0f;
            Loc.Changed += Localize;
        }

        void OnDestroy()
        {
            Loc.Changed -= Localize;
        }

        void Localize()
        {
            shownTier = -1;
        }

        void Update()
        {
            int c = combo.Combo;
            if (c != shown)
            {
                if (c > shown && c >= config.comboShowMin) Grow(c);
                // серия оборвалась на виду: дрожь и угасание
                else if (c < shown && shown >= config.comboShowMin) Break();
                shown = c;
            }
            if (combo.Tier > lastTier) TierUp();
            lastTier = combo.Tier;
            if (c < config.comboShowMin) return;

            timerBar.localScale = new Vector3(Mathf.Clamp01(combo.Timer / combo.Window), 1f, 1f);
            if (combo.Tier == shownTier) return;
            shownTier = combo.Tier;
            tierLine.gameObject.SetActive(shownTier > 0);
            if (shownTier > 0) tierLine.text = string.Format(CultureInfo.InvariantCulture, Strings.ComboTierFormat, combo.Mul);
        }

        void Grow(int c)
        {
            // прошлое угасание и прыжок доводим до конца, иначе альфа и масштаб накапливаются
            DOTween.Kill(this, true);
            rect.anchoredPosition = home;
            group.alpha = 1f;
            count.SetText(Strings.ComboCountFormat, c);
            count.rectTransform.DOPunchScale(Vector3.one * config.comboPunch, config.comboPunchTime).SetTarget(this).SetLink(gameObject);
        }

        void TierUp()
        {
            count.DOKill();
            count.color = config.eliteGold;
            count.DOColor(countColor, config.comboTierFlashTime).SetLink(gameObject);
            screenFx.Flash(config.eliteGold, config.comboTierFlash, config.comboTierFlashTime);
        }

        void Break()
        {
            DOTween.Kill(this, true);
            rect.DOShakeAnchorPos(config.comboBreakTime, config.comboShake).SetTarget(this).SetLink(gameObject);
            group.DOFade(0f, config.comboBreakTime).SetTarget(this).SetLink(gameObject);
        }
    }
}
