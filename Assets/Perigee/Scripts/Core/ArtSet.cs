using TMPro;
using UnityEngine;

namespace Perigee
{
    // Картинки, которые код создаёт или назначает сам. Чисел здесь нет: они в GameConfig.
    [CreateAssetMenu(fileName = "ArtSet", menuName = "Perigee/ArtSet")]
    public class ArtSet : ScriptableObject
    {
        public Visual satellite;
        // тень спутника, пока действует ЭХО
        public Visual echo;
        public Visual crystal;
        // по порядку EnemyType
        public Visual[] enemies;
        // картинки врагов для баннера, по порядку EnemyType; пустая запись = спрайт и цвет Body из префаба врага
        public Sprite[] enemyIcons;
        public Visual shard;
        public Visual spark;
        public ResonanceThread resonanceThread;
        public Visual telegraphArrow;
        public Visual telegraphWedge;
        public Visual telegraphLine;
        public TMP_Text damageNumber;
        // по порядку CardId
        public Sprite[] cardIcons;
        // по порядку AbilityId
        public Sprite[] abilityIcons;
        // рамки карточек по порядку CardColor
        public Sprite[] cardFrames;
        // золотая кайма редкой карточки
        public Sprite cardRareRim;
        // рамка способности на экране выбора
        public Sprite abilityFrame;
    }
}
