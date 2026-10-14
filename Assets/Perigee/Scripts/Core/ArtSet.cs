using System;
using TMPro;
using UnityEngine;

namespace Perigee
{
    // облик звезды: картинки её тела и лучей
    [Serializable]
    public class StarSkin
    {
        public Sprite body;
        public Sprite rays;
    }

    // Картинки, которые код создаёт или назначает сам. Чисел здесь нет: они в GameConfig.
    [CreateAssetMenu(fileName = "ArtSet", menuName = "Perigee/ArtSet")]
    public class ArtSet : ScriptableObject
    {
        // облики спутника: целые префабы, по порядку спутниковых SkinId; запись 0 = обычный ромб
        public Visual[] satelliteSkins;
        // облики звезды, по порядку звёздных SkinId
        public StarSkin[] starSkins;
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
        // кольцо волны Пульсара
        public Visual pulsarWave;
        // картинки эр, по порядку config.eras: значок на карточке эры
        public Sprite[] eraIcons;
        // большая тусклая картинка за кольцами фона, по порядку config.eras; пустая запись = без неё
        public Sprite[] eraBackdrops;
        // картинка звезды в эре, по порядку config.eras; пустая запись = обычная картинка звезды
        public Sprite[] eraStarSprites;
        // по порядку CardId
        public Sprite[] cardIcons;
        // по порядку AbilityId
        public Sprite[] abilityIcons;
        // по порядку BlueprintId
        public Sprite[] blueprintIcons;
        // рамки карточек по порядку CardColor
        public Sprite[] cardFrames;
        // золотая кайма редкой карточки
        public Sprite cardRareRim;
        // рамка способности на экране выбора
        public Sprite abilityFrame;
    }
}
