using TMPro;
using UnityEngine;

namespace Perigee
{
    // Неизменная надпись сцены: текст = строка Strings с именем key на текущем языке.
    [RequireComponent(typeof(TMP_Text))]
    public class LocText : MonoBehaviour
    {
        // имя строкового свойства в Strings, например Back
        [SerializeField] string key;

        TMP_Text label;

        void Awake()
        {
            label = GetComponent<TMP_Text>();
            Loc.Changed += Apply;
            Apply();
        }

        void OnDestroy()
        {
            Loc.Changed -= Apply;
        }

        void Apply()
        {
            label.text = (string)typeof(Strings).GetProperty(key).GetValue(null);
        }
    }
}
