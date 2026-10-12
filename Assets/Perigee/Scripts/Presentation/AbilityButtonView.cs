using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Perigee
{
    public class AbilityButtonView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] AbilitySystem abilities;
        [SerializeField] int slot;
        // всё видимое: скрыто, пока слот пуст
        [SerializeField] GameObject button;
        [SerializeField] Image fill;
        [SerializeField] Image readyRing;
        [SerializeField] Image icon;

        int shownId = -1;
        bool shownReady;

        void Awake()
        {
            readyRing.enabled = false;
        }

        void Update()
        {
            bool filled = abilities.Filled(slot);
            if (button.activeSelf != filled) button.SetActive(filled);

            float charge = filled ? abilities.Charge(slot) : 0f;
            bool ready = charge >= 1f;
            if (ready != shownReady) SetReady(ready);
            if (!filled) return;

            fill.fillAmount = charge;
            int id = (int)abilities.Id(slot);
            if (id == shownId) return;

            shownId = id;
            icon.sprite = art.abilityIcons[id];
        }

        void SetReady(bool ready)
        {
            shownReady = ready;
            fill.enabled = !ready;
            readyRing.enabled = ready;

            Transform ring = readyRing.transform;
            ring.DOKill();
            ring.localScale = Vector3.one;
            // золотое кольцо пульсирует, пока способность готова
            if (ready) ring.DOScale(config.abilityReadyPulse, config.abilityReadyPulseTime).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
        }
    }
}
