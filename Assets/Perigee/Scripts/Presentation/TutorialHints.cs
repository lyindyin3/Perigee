using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Perigee
{
    public enum HintId { Hold, Release, Heat, Armor, Pulse, Resonance }

    // Подсказки первого забега: на экране одна, каждая показывается один раз.
    public class TutorialHints : MonoBehaviour
    {
        const int None = -1;

        [SerializeField] GameConfig config;
        [SerializeField] GameLoop loop;
        [SerializeField] InputHold input;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] EnemySystem enemies;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] ArmorRing armorRing;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text label;

        // по номеру HintId: подсказка уже была
        readonly bool[] done = new bool[Strings.Hint.Length];
        int current = None;
        // секунда забега, когда появилась текущая подсказка
        float shownTime;
        // сколько секунд подряд игрок не держит
        float released;
        bool enemiesSeen;
        bool bastionSeen;
        bool bastionGone;

        public void ResetRun()
        {
            System.Array.Clear(done, 0, done.Length);
            current = None;
            released = 0f;
            enemiesSeen = false;
            bastionSeen = false;
            bastionGone = false;
            group.DOKill();
            group.alpha = 0f;
            armorRing.SetPulse(false);
        }

        // шаг симуляции, только в забеге-обучении
        public void Tick(float dt)
        {
            if (enemies.Count > 0) enemiesSeen = true;
            released = input.IsHolding ? 0f : released + dt;

            // первый Бастион: пока он на поле, кольцо брони мигает
            bool bastion = enemies.CountOf(EnemyType.Bastion) > 0;
            if (bastionSeen && !bastion) bastionGone = true;
            if (bastion) bastionSeen = true;
            armorRing.SetPulse(FirstBastion);

            if (current != None)
            {
                if (!Expired((HintId)current)) return;
                done[current] = true;
                current = None;
                Fade(0f);
                return;
            }

            for (int i = 0; i < done.Length; i++)
            {
                if (done[i] || !Ready((HintId)i)) continue;
                current = i;
                shownTime = loop.Run.time;
                released = 0f;
                label.text = Strings.Hint[i];
                Fade(1f);
                return;
            }
        }

        bool FirstBastion => bastionSeen && !bastionGone;

        bool Ready(HintId id)
        {
            switch (id)
            {
                case HintId.Release: return done[(int)HintId.Hold] && enemies.Within(config.hintReleaseRadius) > 0;
                case HintId.Heat: return satellites.MaxHeat >= config.hintHeatShow;
                case HintId.Armor: return FirstBastion;
                // в обучении слот A всегда занят ИМПУЛЬСОМ
                case HintId.Pulse: return abilities.Charge(AbilitySystem.SlotA) >= 1f && enemies.Within(config.hintPulseRadius) >= config.hintPulseEnemies;
                case HintId.Resonance: return satellites.Resonating;
                default: return true;
            }
        }

        bool Expired(HintId id)
        {
            float shown = loop.Run.time - shownTime;
            switch (id)
            {
                // три первых Роя убиты: до второй группы других врагов на поле нет
                case HintId.Hold: return (enemiesSeen && enemies.Count == 0) || shown >= config.hintHoldTimeout;
                case HintId.Release: return released >= config.hintReleaseTime;
                case HintId.Heat: return satellites.MaxHeat < config.hintHeatHide;
                case HintId.Armor: return !FirstBastion;
                case HintId.Pulse: return abilities.Charge(AbilitySystem.SlotA) < 1f;
                default: return shown >= config.hintResonanceTime;
            }
        }

        void Fade(float alpha)
        {
            group.DOKill();
            group.DOFade(alpha, config.hintFade).SetLink(gameObject);
        }
    }
}
