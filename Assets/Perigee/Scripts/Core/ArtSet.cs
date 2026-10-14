using System;
using TMPro;
using UnityEngine;

namespace Perigee
{
    // Запись списка с подписью: инспектор показывает «Bastion — Бастион» вместо «Element 3». Подпись ставит OnValidate.
    [Serializable]
    public class SpriteSlot
    {
        public string label;
        public Sprite sprite;
    }

    [Serializable]
    public class PrefabSlot
    {
        public string label;
        public Visual prefab;
    }

    // облик звезды: картинки её тела и лучей
    [Serializable]
    public class StarSkin
    {
        public string label;
        public Sprite body;
        public Sprite rays;
    }

    // класс звезды: значок для плиток меню и аура (декор-ребёнок звезды в забеге; пусто = без ауры)
    [Serializable]
    public class StarClassArt
    {
        public string label;
        public Sprite icon;
        public Visual aura;
    }

    // Картинки, которые код создаёт или назначает сам. Чисел здесь нет: они в GameConfig.
    [CreateAssetMenu(fileName = "ArtSet", menuName = "Perigee/ArtSet")]
    public class ArtSet : ScriptableObject
    {
        const string LabelFormat = "{0} — {1}";

        [Header("Игрок")]
        [Tooltip("Облики спутника: целые префабы, по порядку спутниковых SkinId; запись 0 = обычный ромб")]
        public PrefabSlot[] satelliteSkins;
        [Tooltip("Облики звезды: тело и лучи, по порядку звёздных SkinId")]
        public StarSkin[] starSkins;
        [Tooltip("Тень спутника, пока действует ЭХО")]
        public Visual echo;
        [Tooltip("Кристалл опыта, который роняет убитый враг")]
        public Visual crystal;
        [Tooltip("Нить между спутниками в резонансе с подписью «×2»")]
        public ResonanceThread resonanceThread;
        [Tooltip("Классы звёзд, по порядку StarClassId: значок на плитках класса и аура вокруг звезды в забеге")]
        public StarClassArt[] starClasses;

        [Header("Враги")]
        [Tooltip("Префабы врагов и боссов, по порядку EnemyType")]
        public PrefabSlot[] enemies;
        [Tooltip("Картинки врагов для баннера; пустая запись = спрайт и цвет Body из префаба врага")]
        public SpriteSlot[] enemyIcons;
        [Tooltip("Осколок: разлетается при ударе и гибели врага")]
        public Visual shard;
        [Tooltip("Искра: удар в броню и щит")]
        public Visual spark;
        [Tooltip("Телеграф: стрелка у края там, откуда придёт враг")]
        public Visual telegraphArrow;
        [Tooltip("Телеграф: клин у края перед стаей")]
        public Visual telegraphWedge;
        [Tooltip("Телеграф: линия пути Стрелы")]
        public Visual telegraphLine;
        [Tooltip("Всплывающее число урона")]
        public TMP_Text damageNumber;
        [Tooltip("Кольцо волны Пульсара")]
        public Visual pulsarWave;
        [Tooltip("Значок свойства элиты над врагом, по порядку EliteAffix")]
        public SpriteSlot[] affixIcons;

        [Header("Карточки")]
        [Tooltip("Иконки карточек, по порядку CardId")]
        public SpriteSlot[] cardIcons;
        [Tooltip("Иконки способностей, по порядку AbilityId: кнопки, экран выбора, Мастерская")]
        public SpriteSlot[] abilityIcons;
        [Tooltip("Рамки карточек по цвету CardColor")]
        public SpriteSlot[] cardFrames;
        [Tooltip("Золотая кайма редкой карточки")]
        public Sprite cardRareRim;
        [Tooltip("Рамка способности на экране выбора")]
        public Sprite abilityFrame;
        [Tooltip("Рамка реликвии на экране выбора")]
        public Sprite relicFrame;
        [Tooltip("Иконки реликвий, по порядку RelicId: экран реликвий и ряд в HUD")]
        public SpriteSlot[] relicIcons;

        [Header("Меню")]
        [Tooltip("Иконки чертежей в Мастерской, по порядку BlueprintId")]
        public SpriteSlot[] blueprintIcons;
        [Tooltip("Значки достижений, по порядку AchievementId: список достижений и тост")]
        public SpriteSlot[] achievementIcons;
        [Tooltip("Картинки страниц ОСНОВЫ в Атласе, по порядку страниц")]
        public SpriteSlot[] atlasPictures;

        [Header("Эры")]
        [Tooltip("Значок эры на карточке эры и на карточках эр")]
        public SpriteSlot[] eraIcons;
        [Tooltip("Большая тусклая картинка за кольцами фона; пустая запись = без неё")]
        public SpriteSlot[] eraBackdrops;
        [Tooltip("Картинка звезды в эре; пустая запись = картинка облика звезды")]
        public SpriteSlot[] eraStarSprites;
        [Tooltip("Аномалия-колодец (эра 7): тянет орбиту, врагов и кристаллы")]
        public Visual anomalyWell;
        [Tooltip("Аномалия-отталкиватель (эра 7): толкает орбиту и врагов")]
        public Visual anomalyRepulsor;

        // русские имена типов врагов для подписей в инспекторе, по порядку EnemyType
        static readonly string[] EnemyNames =
        {
            "Рой", "Стрела", "Снаряд", "Бастион", "Паразит", "Стрелок", "Улей", "Комета", "Дробитель", "Факел", "Кружащий", "Щитник",
            "Пожиратель", "Призрак", "Близнецы", "Колосс", "Пульсар", "Студень", "Мина", "Отбойник", "Прыгун", "Затмитель", "Сеятель",
            "Зеркало", "Гравитон", "Бездна", "Метеор", "Золотая комета",
        };

        // EliteAffix
        static readonly string[] AffixNames = { "броня", "скорость", "великан", "лечение" };

        // CardColor: рамка по цвету
        static readonly string[] FrameNames = { "кремовая", "красная", "чёрная", "золотая" };

        void OnValidate()
        {
            Loc.Lang current = Loc.Current;
            // подписи по-русски, на каком бы языке ни шла игра
            Loc.Use(Loc.Lang.Ru);

            string[] skins = Enum.GetNames(typeof(SkinId));
            int firstStar = (int)SkinId.Sun;
            satelliteSkins = Fit(satelliteSkins, firstStar);
            for (int i = 0; i < satelliteSkins.Length; i++) satelliteSkins[i].label = Label(skins[i], Strings.SkinTitle[i]);
            starSkins = Fit(starSkins, skins.Length - firstStar);
            for (int i = 0; i < starSkins.Length; i++) starSkins[i].label = Label(skins[firstStar + i], Strings.SkinTitle[firstStar + i]);

            string[] classes = Enum.GetNames(typeof(StarClassId));
            starClasses = Fit(starClasses, classes.Length);
            for (int i = 0; i < classes.Length; i++) starClasses[i].label = Label(classes[i], Strings.ClassTitle[i]);

            string[] enemyTypes = Enum.GetNames(typeof(EnemyType));
            enemies = Fit(enemies, enemyTypes.Length);
            enemyIcons = Fit(enemyIcons, enemyTypes.Length);
            for (int i = 0; i < enemyTypes.Length; i++)
            {
                enemies[i].label = Label(enemyTypes[i], EnemyNames[i]);
                enemyIcons[i].label = enemies[i].label;
            }

            Name(cardIcons = Fit(cardIcons, Strings.CardTitle.Length), typeof(CardId), Strings.CardTitle);
            Name(abilityIcons = Fit(abilityIcons, Strings.AbilityTitle.Length), typeof(AbilityId), Strings.AbilityTitle);
            Name(blueprintIcons = Fit(blueprintIcons, Strings.BlueprintTitle.Length), typeof(BlueprintId), Strings.BlueprintTitle);
            Name(cardFrames = Fit(cardFrames, FrameNames.Length), typeof(CardColor), FrameNames);
            Name(affixIcons = Fit(affixIcons, AffixNames.Length), typeof(EliteAffix), AffixNames);
            Name(relicIcons = Fit(relicIcons, Strings.RelicTitle.Length), typeof(RelicId), Strings.RelicTitle);
            Name(achievementIcons = Fit(achievementIcons, Strings.AchTitle.Length), typeof(AchievementId), Strings.AchTitle);
            atlasPictures = Fit(atlasPictures, Strings.AtlasPageTitle.Length);
            for (int i = 0; i < atlasPictures.Length; i++) atlasPictures[i].label = (i + 1) + " " + Strings.AtlasPageTitle[i];

            int eras = Strings.EraTitle.Length;
            eraIcons = Fit(eraIcons, eras);
            eraBackdrops = Fit(eraBackdrops, eras);
            eraStarSprites = Fit(eraStarSprites, eras);
            for (int i = 0; i < eras; i++)
            {
                string label = (i + 1) + " " + Strings.EraTitle[i];
                eraIcons[i].label = label;
                eraBackdrops[i].label = label;
                eraStarSprites[i].label = label;
            }

            Loc.Use(current);
        }

        static string Label(string id, string name)
        {
            return string.Format(LabelFormat, id, name);
        }

        static void Name(SpriteSlot[] slots, Type ids, string[] names)
        {
            string[] idNames = Enum.GetNames(ids);
            for (int i = 0; i < slots.Length; i++) slots[i].label = Label(idNames[i], names[i]);
        }

        // список нужной длины: старые записи остаются на местах, новые пустые
        public static T[] Fit<T>(T[] slots, int length) where T : new()
        {
            if (slots == null) slots = new T[0];
            if (slots.Length != length) Array.Resize(ref slots, length);
            for (int i = 0; i < length; i++)
                if (slots[i] == null) slots[i] = new T();
            return slots;
        }
    }
}
