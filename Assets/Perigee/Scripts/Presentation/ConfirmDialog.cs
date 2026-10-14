using System;
using TMPro;
using UnityEngine;

namespace Perigee
{
    // Вопрос с ответами «Да» и «Нет» поверх настроек.
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] TMP_Text text;
        [SerializeField] TMP_Text yesLabel;
        [SerializeField] TMP_Text noLabel;

        Action onYes;

        void Awake()
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
            gameObject.SetActive(false);
            onYes();
        }

        public void No()
        {
            gameObject.SetActive(false);
        }
    }
}
