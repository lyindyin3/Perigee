using UnityEngine;

namespace Perigee
{
    // новые значения только в конец: по номеру идут слоты AudioBank.sfx
    public enum SfxId
    {
        Hit, Clink, Kill, Crystal, Resonance, StarHit, Stun, CardOpen, CardPick, BossIn, Win, Lose,
        UiClick, UiBack, Buy, LevelUp, AbilityReady, Reverse, Grow, Pulse, Stasis, Echo, Pull, Dome, Overheat, BigKill, ShieldBreak,
        FlareWarn, FlareBurst, BossKill, BossPhase, ComboTier, ComboBreak, Evolution, Relic, EventStart, Elite, Mine, Blink, GravAssist,
        AnomalyOpen, Achievement, Unlock, Revive, SecondSun, StoryType, StoryNext
    }

    // по номеру идут слоты AudioBank.music
    public enum MusicId { Menu, Chapter1, Chapter2, Chapter3, Chapter4, Boss, FinalBoss, Story }

    public class AudioManager : MonoBehaviour
    {
        [SerializeField] GameConfig config;
        [SerializeField] AudioBank bank;
        [SerializeField] GameLoop loop;
        [SerializeField] SatelliteSystem satellites;
        // голоса эффектов (Systems/Sfx); играют первые config.maxVoices
        [SerializeField] AudioSource[] voices;
        [SerializeField] AudioSource drone;
        // два зацикленных источника музыки (Systems/Music): новый трек разгорается, прошлый гаснет
        [SerializeField] AudioSource[] musicSources;

        float[] lastPlayed;
        int nextVoice;

        bool hasMusic;
        MusicId currentMusic;
        // источник нового трека, громкость слота гаснущего трека и начало перехода
        int front;
        float fadingVolume;
        float fadeStart;

        void Awake()
        {
            lastPlayed = new float[System.Enum.GetValues(typeof(SfxId)).Length];
            for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = float.NegativeInfinity;
        }

        void Start()
        {
            drone.clip = bank.drone;
            drone.Play();
        }

        public void Play(SfxId id)
        {
            SoundSlot slot = bank.sfx[(int)id];
            float now = Time.unscaledTime;
            if (now - lastPlayed[(int)id] < slot.minInterval) return;
            // пустой слот = тишина
            if (slot.clips.Length == 0) return;
            AudioClip clip = slot.clips[Random.Range(0, slot.clips.Length)];
            if (clip == null) return;

            lastPlayed[(int)id] = now;
            AudioSource voice = FreeVoice();
            voice.clip = clip;
            voice.volume = slot.volume * loop.Save.Data.settings.sfxVolume;
            voice.pitch = 1f + Random.Range(-slot.pitchJitter, slot.pitchJitter);
            voice.Play();
        }

        // тот же трек не начинается заново; переход за config.musicFade
        public void PlayMusic(MusicId id)
        {
            if (hasMusic && id == currentMusic) return;

            fadingVolume = hasMusic ? bank.music[(int)currentMusic].volume : 0f;
            hasMusic = true;
            currentMusic = id;
            front = 1 - front;
            fadeStart = Time.unscaledTime;

            AudioSource source = musicSources[front];
            source.clip = bank.music[(int)id].clip;
            source.volume = 0f;
            // пустой слот = тишина
            if (source.clip != null) source.Play();
            else source.Stop();
        }

        // свободный голос; если заняты все, замолкает тот, что начал раньше остальных
        AudioSource FreeVoice()
        {
            int count = config.maxVoices;
            for (int i = 0; i < count; i++)
                if (!voices[i].isPlaying) return voices[i];
            nextVoice = (nextVoice + 1) % count;
            return voices[nextVoice];
        }

        void Update()
        {
            SettingsData settings = loop.Save.Data.settings;

            // музыка: на паузе тише
            float level = settings.musicVolume * (loop.State == GameState.Paused ? config.musicPauseMul : 1f);
            float k = Mathf.Clamp01((Time.unscaledTime - fadeStart) / config.musicFade);
            if (hasMusic) musicSources[front].volume = k * bank.music[(int)currentMusic].volume * level;
            AudioSource back = musicSources[1 - front];
            back.volume = (1f - k) * fadingVolume * level;
            if (k >= 1f && back.isPlaying) back.Stop();

            drone.volume = bank.droneVolume * settings.droneVolume;
            // гул слышен только в забеге
            bool playing = loop.State == GameState.Playing;
            drone.mute = !playing;
            if (!playing) return;

            // чем быстрее кружат спутники, тем выше гул
            float sum = 0f;
            for (int i = 0; i < satellites.Count; i++) sum += satellites.AngularSpeed(satellites.OrbitRadius(i));
            drone.pitch = Mathf.Lerp(config.dronePitchMin, config.dronePitchMax, sum / satellites.Count / satellites.AngularSpeed(loop.Star.Class.rMin));
        }
    }
}
