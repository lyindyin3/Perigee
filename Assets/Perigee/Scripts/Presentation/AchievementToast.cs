using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Тост достижения сверху по центру поверх всего: въезжает, держится, уезжает; следующие ждут в очереди.
    public class AchievementToast : MonoBehaviour
    {
        struct Entry
        {
            public Sprite icon;
            public string name;
            public int dust;
        }

        [SerializeField] GameConfig config;
        // сам тост (ребёнок этого объекта, выключен, пока очередь пуста)
        [SerializeField] RectTransform panel;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text dust;

        readonly Queue<Entry> queue = new Queue<Entry>();
        Vector2 home;
        bool showing;

        void Awake()
        {
            home = panel.anchoredPosition;
        }

        public void Show(Sprite sprite, string name, int dustReward)
        {
            queue.Enqueue(new Entry { icon = sprite, name = name, dust = dustReward });
            if (!showing) Next();
        }

        void Next()
        {
            if (queue.Count == 0)
            {
                showing = false;
                panel.gameObject.SetActive(false);
                return;
            }
            showing = true;
            Entry entry = queue.Dequeue();
            icon.sprite = entry.icon;
            title.text = string.Format(Strings.AchToastFormat, entry.name);
            dust.text = string.Format(Strings.AchDustFormat, entry.dust);

            Vector2 hidden = home + Vector2.up * config.toastSlide;
            panel.anchoredPosition = hidden;
            panel.gameObject.SetActive(true);
            // по реальному времени: тост виден и на паузе
            DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                .Append(panel.DOAnchorPos(home, config.toastIn).SetEase(Ease.OutCubic))
                .AppendInterval(config.toastHold)
                .Append(panel.DOAnchorPos(hidden, config.toastOut).SetEase(Ease.InCubic))
                .OnComplete(Next);
        }
    }
}
