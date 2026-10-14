using UnityEngine;

namespace Perigee
{
    public enum SfxId { Hit, Clink, Kill, Crystal, Resonance, StarHit, Stun, CardOpen, CardPick, BossIn, Win, Lose }

    public class AudioManager : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] AudioBank bank;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioSource drone;

        void Start()
        {
            drone.clip = bank.drone;
            drone.Play();
        }

        public void Play(SfxId id)
        {
            // пустой клип = тишина
            AudioClip clip = bank.clips[(int)id];
            if (clip != null) sfx.PlayOneShot(clip, loop.Save.Data.settings.sfxVolume);
        }

        void Update()
        {
            // громкости из настроек
            drone.volume = loop.Save.Data.settings.droneVolume;
            // гул слышен только в забеге
            bool playing = loop.State == GameState.Playing;
            drone.mute = !playing;
            if (!playing) return;

            // чем быстрее кружат спутники, тем выше гул
            float sum = 0f;
            for (int i = 0; i < satellites.Count; i++) sum += satellites.AngularSpeed(satellites.OrbitRadius(i));
            drone.pitch = Mathf.Lerp(config.dronePitchMin, config.dronePitchMax, sum / satellites.Count / satellites.AngularSpeed(config.rMin));
        }
    }
}
