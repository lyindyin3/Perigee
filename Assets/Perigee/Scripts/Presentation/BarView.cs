using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    public enum BarKind { Health, Progress }

    // Полоска HUD: HP звезды, HP босса или опыт. Код ведёт слои Ghost, Fill и Frame; Back и деления: декор.
    public class BarView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] BarKind kind;
        [SerializeField] Image ghost;
        [SerializeField] Image fill;
        [SerializeField] Image frame;
        // подпись «120 / 120»; у полоски без подписи поле пустое
        [SerializeField] TMP_Text text;
        // цвет Ghost при лечении
        [SerializeField] Color healColor;
        // цвет вспышки Frame
        [SerializeField] Color flashColor;

        RectTransform rect;
        Color ghostColor;
        Color fillColor;
        Color frameColor;
        // доля, к которой идёт Fill
        float target;
        float fillFrom;
        float fillStart;
        float ghostFrom;
        float ghostStart;
        // опыт, новый уровень: Fill доходит до конца, Frame вспыхивает, потом Fill растёт с нуля
        bool wrapping;
        bool wrapFlashed;
        // после включения первое значение встаёт без анимации
        bool fresh;
        int shownValue = -1;
        int shownMax = -1;

        void Awake()
        {
            rect = (RectTransform)transform;
            ghostColor = ghost.color;
            fillColor = fill.color;
            frameColor = frame.color;
        }

        void OnEnable()
        {
            fresh = true;
        }

        public void Set(float value, float max)
        {
            if (text != null) Label(Mathf.CeilToInt(value), Mathf.CeilToInt(max));

            float f = Mathf.Clamp01(value / max);
            if (fresh)
            {
                Snap(f);
                return;
            }
            if (f == target) return;

            float now = Time.unscaledTime;
            if (kind == BarKind.Progress)
            {
                // пока полоска идёт через конец, меняется только цель
                if (!wrapping)
                {
                    fillFrom = fill.fillAmount;
                    fillStart = now;
                }
            }
            else if (f > target)
            {
                // лечение: Ghost сразу встаёт на новое значение, Fill дорастает до него
                ghost.color = healColor;
                ghostFrom = f;
                fillFrom = fill.fillAmount;
                fillStart = now;
            }
            else
            {
                // урон: Fill падает сразу, Ghost держится и потом догоняет
                ghost.color = ghostColor;
                ghostFrom = ghost.fillAmount;
                ghostStart = now + config.barGhostDelay;
                fillFrom = f;
                // мелкая убыль (Паразит пьёт каждый шаг) полоску не дёргает
                if ((target - f) * max >= config.barPunchMinDrop) Hit();
            }
            target = f;
        }

        // опыт: уровень вырос
        public void Wrap()
        {
            wrapping = true;
            wrapFlashed = false;
            fillFrom = fill.fillAmount;
            fillStart = Time.unscaledTime;
        }

        void Update()
        {
            // значения ещё не было: полоска ждёт первого Set
            if (fresh) return;

            float now = Time.unscaledTime;
            if (kind == BarKind.Progress)
            {
                if (wrapping) WrapStep(now);
                else fill.fillAmount = Eased(fillFrom, target, (now - fillStart) / config.xpFillTime);
                return;
            }

            fill.fillAmount = Eased(fillFrom, target, (now - fillStart) / config.barGhostTime);
            ghost.fillAmount = Eased(ghostFrom, target, (now - ghostStart) / config.barGhostTime);

            // мало HP: Fill мерцает
            Color color = fillColor;
            if (target < config.starLowHpFraction) color.a *= Mathf.Lerp(config.barLowAlpha, 1f, Mathf.PingPong(now * config.barLowHz * 2f, 1f));
            fill.color = color;
        }

        void WrapStep(float now)
        {
            float t = now - fillStart;
            fill.fillAmount = Eased(fillFrom, 1f, t / config.xpFillTime);
            if (t < config.xpFillTime) return;

            if (!wrapFlashed)
            {
                wrapFlashed = true;
                Flash(config.xpFlashTime);
            }
            if (t < config.xpFillTime + config.xpFlashTime) return;

            wrapping = false;
            fillFrom = 0f;
            fillStart = now;
        }

        void Snap(float f)
        {
            fresh = false;
            wrapping = false;
            target = f;
            fillFrom = f;
            ghostFrom = f;
            fill.fillAmount = f;
            if (kind == BarKind.Health) ghost.fillAmount = f;
        }

        // заметный урон: полоска вздрагивает, рамка вспыхивает
        void Hit()
        {
            // прошлый толчок доводим до конца, иначе сдвиг накапливается
            rect.DOKill(true);
            rect.DOPunchAnchorPos(Vector2.down * config.barPunch, config.barPunchTime).SetLink(gameObject);
            Flash(config.barFlashTime);
        }

        void Flash(float time)
        {
            frame.DOKill();
            frame.color = flashColor;
            frame.DOColor(frameColor, time).SetLink(gameObject);
        }

        void Label(int value, int max)
        {
            if (value == shownValue && max == shownMax) return;

            shownValue = value;
            shownMax = max;
            text.SetText("{0} / {1}", value, max);
        }

        // время t идёт от 0 до 1
        static float Eased(float from, float to, float t)
        {
            return DOVirtual.EasedValue(from, to, Mathf.Clamp01(t), Ease.OutQuad);
        }
    }
}
