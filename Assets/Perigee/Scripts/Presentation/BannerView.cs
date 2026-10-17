using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    // Строка под полосками: появляется, держится и гаснет. На экране одна, остальные ждут в очереди.
    public class BannerView : MonoBehaviour
    {
        struct Entry
        {
            public string text;
            public Sprite icon;
            public Color iconColor;
            public float hold;
        }

        [SerializeField] GameConfig config;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text label;
        [SerializeField] Image icon;

        readonly Queue<Entry> queue = new Queue<Entry>();
        bool busy;

        public void ResetRun()
        {
            queue.Clear();
            group.DOKill();
            group.alpha = 0f;
            busy = false;
        }

        // sprite может быть пустым: тогда видна только строка
        public void Show(string text, Sprite sprite, Color iconColor, float hold)
        {
            queue.Enqueue(new Entry { text = text, icon = sprite, iconColor = iconColor, hold = hold });
            if (!busy) Next();
        }

        void Next()
        {
            busy = queue.Count > 0;
            if (!busy) return;

            Entry e = queue.Dequeue();
            Typing.Show(label, e.text);
            icon.sprite = e.icon;
            icon.color = e.iconColor;
            icon.gameObject.SetActive(e.icon != null);
            DOTween.Sequence()
                .Append(group.DOFade(1f, config.bannerFadeIn))
                .AppendInterval(e.hold)
                .Append(group.DOFade(0f, config.bannerFadeOut))
                .OnComplete(Next)
                .SetTarget(group)
                .SetLink(gameObject);
        }
    }
}
