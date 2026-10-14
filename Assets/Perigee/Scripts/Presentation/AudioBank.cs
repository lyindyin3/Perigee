using System;
using UnityEngine;

namespace Perigee
{
    // Слот звука: клип выбирается случайно; пустой слот = тишина.
    [Serializable]
    public class SoundSlot
    {
        public string label;
        public AudioClip[] clips = new AudioClip[0];
        public float volume = 1f;
        // высота тона 1 ± pitchJitter
        public float pitchJitter = 0.05f;
        // тот же звук чаще этого не играет (секунды)
        public float minInterval = 0.03f;
    }

    [Serializable]
    public class MusicSlot
    {
        public string label;
        public AudioClip clip;
        public float volume = 0.8f;
    }

    // Звуки и музыка игры: единственное место для звуковых файлов.
    [CreateAssetMenu(fileName = "AudioBank", menuName = "Perigee/AudioBank")]
    public class AudioBank : ScriptableObject
    {
        const string LabelFormat = "{0} — {1}";

        // когда звучит, по порядку SfxId
        static readonly string[] SfxWhen =
        {
            "удар спутника по врагу", "удар в броню или щит", "враг убит", "кристалл подобран", "возник резонанс", "урон по звезде",
            "спутник оглушён", "открылся экран карточек", "карточка выбрана", "пришёл босс", "победа", "поражение",
            "нажатие кнопки", "кнопка «Назад» и закрытие", "покупка в Мастерской", "новый уровень", "способность готова",
            "РЕВЕРС", "РОСТ", "ИМПУЛЬС, МАЯК, волна Нейтронной", "СТАЗИС", "ЭХО", "ПРИТЯЖЕНИЕ", "КУПОЛ",
            "нагрев дошёл до 0.8 (раз до остывания ниже 0.5)", "убит крупный враг", "щит Щитника сломан", "предупреждение вспышки",
            "удар вспышки", "босс убит", "смена фазы босса", "новый уровень комбо", "комбо от 10 и выше оборвалось", "взята эволюция",
            "экран реликвий открылся", "началось событие", "появилась элита или Вестник", "взрыв мины", "прыжок Прыгуна",
            "гравиманёвр у луны", "открылась аномалия", "получено достижение", "открыта эра, класс или облик (итоги)",
            "возрождение за рекламу", "сработало ВТОРОЕ СОЛНЦЕ", "печать текста сюжета", "следующий слайд сюжета",
        };

        // когда играет, по порядку MusicId
        static readonly string[] MusicWhen =
        {
            "меню и итоги", "эры 1–2", "эры 3–4", "эры 5–6", "эры 7–8", "приход босса", "босс эры 8", "сюжет",
        };

        [Header("Звуки")]
        [Tooltip("По порядку SfxId: перетащи один или несколько клипов в слот")]
        public SoundSlot[] sfx;

        [Header("Музыка")]
        [Tooltip("По порядку MusicId: зацикленная музыка")]
        public MusicSlot[] music;

        [Header("Гул")]
        [Tooltip("Зацикленный гул в забеге: чем быстрее спутники, тем выше")]
        public AudioClip drone;
        [Tooltip("Громкость гула (умножается на «Гул» в настройках)")]
        public float droneVolume = 1f;

        void OnValidate()
        {
            string[] ids = Enum.GetNames(typeof(SfxId));
            sfx = ArtSet.Fit(sfx, ids.Length);
            for (int i = 0; i < ids.Length; i++) sfx[i].label = string.Format(LabelFormat, ids[i], SfxWhen[i]);

            string[] tracks = Enum.GetNames(typeof(MusicId));
            music = ArtSet.Fit(music, tracks.Length);
            for (int i = 0; i < tracks.Length; i++) music[i].label = string.Format(LabelFormat, tracks[i], MusicWhen[i]);
        }
    }
}
