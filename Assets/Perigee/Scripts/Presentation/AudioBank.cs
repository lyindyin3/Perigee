using UnityEngine;

namespace Perigee
{
    // Звуки игры. Пустой клип = тишина.
    [CreateAssetMenu(fileName = "AudioBank", menuName = "Perigee/AudioBank")]
    public class AudioBank : ScriptableObject
    {
        // по порядку SfxId
        public AudioClip[] clips = new AudioClip[System.Enum.GetValues(typeof(SfxId)).Length];
        public AudioClip drone;
    }
}
