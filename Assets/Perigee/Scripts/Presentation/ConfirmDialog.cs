using System;
using TMPro;
using UnityEngine;

namespace Perigee
{
    // Вопрос с ответами «Да» и «Нет» поверх настроек.
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] AudioManager sfx;
        [SerializeField] TMP_Text text;
        [SerializeField] TMP_Text yesLabel;
        [SerializeField] TMP_Text noLabel;

        Action onYes;

        void Awake()
        {
            Loc.Changed += Localize;
            Localize();
        }

        void OnDestroy()
        {
            Loc.Changed -= Localize;
        }

        void Localize()
        {
            yesLabel.text = Strings.Yes;
            noLabel.text = Strings.No;
        }

        // yes вызывается, если игрок ответил «Да»
        public void Show(string question, Action yes)
        {
            text.text = question;
            onYes = yes;
            gameObject.SetActive(true);
        }

        public void Yes()
        {
            sfx.Play(SfxId.UiClick);
            gameObject.SetActive(false);
            onYes();
        }

        public void No()
        {
            sfx.Play(SfxId.UiBack);
            gameObject.SetActive(false);
        }
    }
}
