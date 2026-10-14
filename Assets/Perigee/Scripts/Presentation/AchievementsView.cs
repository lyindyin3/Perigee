using TMPro;
using UnityEngine;

namespace Perigee
{
    // Панель меню ДОСТИЖЕНИЯ: «{done} / 36» и список строк по порядку config.achievements.
    public class AchievementsView : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] ArtSet art;
        [SerializeField] GameLoop loop;
        [SerializeField] TMP_Text header;
        [SerializeField] TMP_Text count;
        [SerializeField] AchievementRow[] rows;

        void Awake()
        {
            Loc.Changed += Show;
        }

        void OnDestroy()
        {
            Loc.Changed -= Show;
        }

        void OnEnable()
        {
            Show();
        }

        void Show()
        {
            Achievements achievements = loop.Achievements;
            AchievementDef[] defs = config.achievements;
            header.text = Strings.Achievements;
            count.text = string.Format(Strings.CountFormat, achievements.DoneCount, defs.Length);
            for (int i = 0; i < rows.Length; i++)
            {
                AchievementDef def = defs[i];
                int id = (int)def.id;
                bool done = achievements.Done(i);
                int counter = achievements.Counter(def);
                // у выполненного счётчика прогресс не растёт выше цели
                string progress = counter >= 0 ? string.Format(Strings.CountFormat, Mathf.Min(counter, def.target), def.target) : string.Empty;
                rows[i].Show(art.achievementIcons[id].sprite, Strings.AchTitle[id], Strings.AchText[id], string.Format(Strings.AchDustFormat, def.dust), progress, done, config.achDimAlpha);
            }
        }
    }
}
