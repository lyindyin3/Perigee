using UnityEngine;

namespace Perigee
{
    // Картинки, которые код создаёт или назначает сам. Чисел здесь нет: они в GameConfig.
    [CreateAssetMenu(fileName = "ArtSet", menuName = "Perigee/ArtSet")]
    public class ArtSet : ScriptableObject
    {
        public Visual satellite;
        public Visual crystal;
        // по порядку EnemyType
        public Visual[] enemies;
        public ResonanceThread resonanceThread;
        public Visual telegraphArrow;
        public Visual telegraphWedge;
        public Visual telegraphLine;
        // по порядку CardId
        public Sprite[] cardIcons;
        // по порядку AbilityId
        public Sprite[] abilityIcons;
    }
}
