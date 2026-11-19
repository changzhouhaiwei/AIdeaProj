using System.Collections.Generic;
using UnityEngine;

namespace XSortGameCore
{
    [StaticUnload]
    [RequireComponent(typeof(AudioSource))]
    public class MusicEmitter : MonoBehaviour
    {
        private const float DEFAULT_FADE_DURATION = 0.3f;

        private static MusicEmitter defaultMusicSource;

        private static MusicEmitter activeMusicSource;
        public static MusicEmitter ActiveMusicSource => activeMusicSource;

        private static readonly List<MusicEmitter> registeredSources = new List<MusicEmitter>();

        [SerializeField] bool activateAutomatically = false;

        private AudioSource audioSource;
        public AudioSource AudioSource => audioSource;

        private TweenClip fadeTweenCase;

        private float volumeMultiplier = 1.0f;
        private float clipGain = 1f;
        private bool isInitialized;

        private void Awake()
        {
            if(activateAutomatically)
            {
                Init();
                Activate();
            }
        }

        public void Init()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;

            volumeMultiplier = audioSource.volume;
            isInitialized = true;

            RegisterSource();
            SubscribeVolumeChanged();
            ApplyMusicVolume(AudioController.GetVolume(AudioType.Music));
        }

        public void Unload()
        {
            UnsubscribeVolumeChanged();
        }

        private void OnEnable()
        {
            if (!isInitialized)
                return;

            RegisterSource();
            SubscribeVolumeChanged();
            ApplyMusicVolume(AudioController.GetVolume(AudioType.Music));
        }

        private void OnDisable()
        {
            UnsubscribeVolumeChanged();
        }

        private void OnDestroy()
        {
            UnsubscribeVolumeChanged();
            registeredSources.Remove(this);
        }

        private void RegisterSource()
        {
            if (!registeredSources.Contains(this))
                registeredSources.Add(this);
        }

        private void SubscribeVolumeChanged()
        {
            AudioController.VolumeChanged -= OnVolumeChanged;
            AudioController.VolumeChanged += OnVolumeChanged;
        }

        private void UnsubscribeVolumeChanged()
        {
            AudioController.VolumeChanged -= OnVolumeChanged;
        }

        public static void ApplyVolume(float volume)
        {
            for (int i = registeredSources.Count - 1; i >= 0; i--)
            {
                MusicEmitter source = registeredSources[i];
                if (source == null)
                {
                    registeredSources.RemoveAt(i);
                    continue;
                }

                source.ApplyMusicVolume(volume);
            }
        }

        private void ApplyMusicVolume(float volume)
        {
            if (audioSource == null)
                return;

            fadeTweenCase.KillActive();
            audioSource.volume = volume * volumeMultiplier * clipGain;
        }

        public void SetAsDefault()
        {
            defaultMusicSource = this;
        }

        public void Activate()
        {
            if (activeMusicSource == this) return;

            if(activeMusicSource != null)
            {
                activeMusicSource.audioSource.volume = 0.0f;
                activeMusicSource.audioSource.Stop();
            }

            audioSource.Play();

            Fade(1.0f, DEFAULT_FADE_DURATION);

            activeMusicSource = this;
        }

        /// <summary>换曲并淡入。同一首已在播则跳过，连关不重头播。</summary>
        public void PlayClip(AudioClip clip, float gain = 1f)
        {
            if (clip == null || audioSource == null)
                return;

            clipGain = gain;

            if (activeMusicSource == this && audioSource.clip == clip && audioSource.isPlaying)
                return;

            if (activeMusicSource != null && activeMusicSource != this)
            {
                activeMusicSource.audioSource.volume = 0.0f;
                activeMusicSource.audioSource.Stop();
            }

            if (audioSource.clip != clip || !audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.clip = clip;
                audioSource.Play();
            }

            Fade(1.0f, DEFAULT_FADE_DURATION);
            activeMusicSource = this;
        }

        public void SetVolume(float volume)
        {
            audioSource.volume = volume * AudioController.GetVolume(AudioType.Music) * volumeMultiplier * clipGain;
        }

        public void Fade(float value, float duration, float delay = 0, SimpleCallback onComplete = null)
        {
            fadeTweenCase.KillActive();

            fadeTweenCase = Tween.DoFloat(audioSource.volume, value, duration, (value) =>
            {
                audioSource.volume = value * AudioController.GetVolume(AudioType.Music) * volumeMultiplier * clipGain;
            }, delay).OnComplete(onComplete);
        }

        private void OnVolumeChanged(AudioType audioType, float volume)
        {
            if (audioType != AudioType.Music) return;

            ApplyMusicVolume(volume);
        }

        public bool IsActive()
        {
            return activeMusicSource == this;
        }

        public static void ActivateDefault()
        {
            if (activeMusicSource == defaultMusicSource) return;

            defaultMusicSource.Activate();
        }

        private static void UnloadStatic()
        {
            defaultMusicSource = null;
            activeMusicSource = null;
            registeredSources.Clear();
        }
    }
}

// -----------------
// Audio Controller v 0.4
// -----------------

// Changelog
// v 0.4
// • Vibration settings removed
// v 0.3.3
// • Method for separate music and sound volume override
// v 0.3.2
// • Added audio listener creation method
// v 0.3.2
// • Added volume float
// • AudioSettings variable removed (now sounds, music and vibrations can be reached directly)
// v 0.3.1
// • Added OnVolumeChanged callback
// • Renamed AudioSettings to Settings
// v 0.3
// • Added IsAudioModuleEnabled method
// • Added IsVibrationModuleEnabled method
// • Removed VibrationToggleButton class
// v 0.2
// • Removed MODULE_VIBRATION
// v 0.1
// • Added basic version
// • Added support of new initialization
// • Music and Sound volume is combined