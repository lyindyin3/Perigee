using Febucci.TextAnimatorForUnity;
using TMPro;

namespace Perigee
{
    // Текст с печатной машинкой Text Animator (её ставят в сцене) печатается заново по буквам; остальной ставится сразу.
    // Выключенный текст получает строку сразу: машинка начнёт печатать, когда его включат.
    public static class Typing
    {
        public static void Show(TMP_Text label, string text)
        {
            if (label.isActiveAndEnabled && label.TryGetComponent(out TypewriterComponent typewriter)) typewriter.ShowText(text);
            else label.text = text;
        }
    }
}
