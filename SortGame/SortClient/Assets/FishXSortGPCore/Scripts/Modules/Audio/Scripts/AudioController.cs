using System.Collections.Generic;
using UnityEngine;

namespace XSortGameCore
{
    [StaticUnload]
    public static class AudioController
    {
        private static List<AudioSourceCase> audioSourcesPool;

        private static AudioClips audioClips;
        public static AudioClips AudioClips => audioClips;

        private static AudioListener audioListener;
        public static AudioListener AudioListener => audioListener;

        private static AudioSave save;

        // Default 3D audio settings
        private static float maxDistance = 30;
        private static float spread = 180;
        private static AnimationCurve rolloffCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 0.0f));

        public static OnVolumeChangedCallback VolumeChanged;

        private static Dictionary<AudioType, float> volumeDictionary;

        public static void Init(AudioClips audioClips, int audioSourcesPoolSize)
        {
            if (audioClips == null)
            {
                Debug.LogError("[AudioController]: Audio Clips is NULL! Please assign audio clips scriptable on Audio Controller script.");

                return;
            }

            // Get volume save
            save = SaveController.GetSaveObject<AudioSave>("audio");

            volumeDictionary = new Dictionary<AudioType, float>();
            if (save.VolumeDatas != null)
            {
                foreach (AudioSave.VolumeData volumeData in save.VolumeDatas)
                {
                    volumeDictionary.Add(volumeData.AudioType, volumeData.Volume);
                }
            }

            foreach (AudioType audioType in EnumUtils.GetEnumArray<AudioType>())
            {
                if (!volumeDictionary.ContainsKey(audioType))
                    volumeDictionary[audioType] = 1.0f;
            }

            // Create audio listener
            CreateAudioListener();

            AudioController.audioClips = audioClips;

            //Create audio source objects
            audioSourcesPool = new List<AudioSourceCase>();
            for (int i = 0; i < audioSourcesPoolSize; i++)
            {
                audioSourcesPool.Add(new AudioSourceCase());
            }
        }

        public static void OverrideDefault3DAudioSettings(float maxDistance, float spread, AnimationCurve rolloffCurve)
        {
            AudioController.maxDistance = maxDistance;
            AudioController.spread = spread;
            AudioController.rolloffCurve = rolloffCurve;
        }

        public static void ApplyDefaultSettings(ref AudioSource audioSource)
        {
            audioSource.maxDistance = maxDistance;
            audioSource.spread = spread;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloffCurve);
        }

        private static void CreateAudioListener()
        {
            if (audioListener != null)
                return;

            // Create game object for listener
            GameObject listenerObject = new GameObject("[AUDIO LISTENER]");
            listenerObject.transform.position = Vector3.zero;

            // Mark as non-destroyable
            GameObject.DontDestroyOnLoad(listenerObject);

            // Add listener component to created object
            audioListener = listenerObject.AddComponent<AudioListener>();
        }

        public static Transform AttachAudioListener(Transform parentObject)
        {
            if (audioListener == null)
                CreateAudioListener();

            Transform audioListenerTransform = audioListener.transform;
            audioListenerTransform.SetParent(parentObject);
            audioListenerTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            return audioListenerTransform;
        }

        public static void ResetAudioLisenerParent()
        {
            if (audioListener == null) return;

            audioListener.transform.SetParent(null);

            GameObject.DontDestroyOnLoad(audioListener.gameObject);
        }

        /// <summary>
        /// Stop all active streams
        /// </summary>
        public static void ReleaseSources()
        {
            foreach (AudioSourceCase sourceCase in audioSourcesPool)
            {
                if (sourceCase.IsPlaying)
                {
                    sourceCase.AudioSource.Stop();
                }
            }
        }

        public static void StopSound(AudioClip clip)
        {
            if (clip == null || audioSourcesPool == null)
            {
                return;
            }

            for (int i = 0; i < audioSourcesPool.Count; i++)
            {
                AudioSourceCase sourceCase = audioSourcesPool[i];
                if (sourceCase != null && sourceCase.AudioSource != null && sourceCase.AudioSource.clip == clip)
                {
                    sourceCase.Stop();
                }
            }
        }

        public static void PlaySound(AudioClip clip, float volumePercentage = 1.0f, float pitch = 1.0f, float minDelay = 0f)
        {
            PlaySoundInternal(clip, volumePercentage, pitch, loop: false, spatialBlend: 0.0f, position: null);
        }

        public static void PlayLoopSound(AudioClip clip, float volumePercentage = 1.0f, AudioType audioType = AudioType.Sound)
        {
            StopSound(clip);
            PlaySoundInternal(clip, volumePercentage, pitch: 1.0f, loop: true, spatialBlend: 0.0f, position: null, audioType);
        }

        public static void PlaySound(AudioClip clip, Vector3 position, float volumePercentage = 1.0f, float pitch = 1.0f, float minDelay = 0f)
        {
            PlaySoundInternal(clip, volumePercentage, pitch, loop: false, spatialBlend: 1.0f, position);
        }

        static void PlaySoundInternal(AudioClip clip, float volumePercentage, float pitch, bool loop, float spatialBlend, Vector3? position, AudioType audioType = AudioType.Sound)
        {
            if (clip == null)
                Debug.LogError("[AudioController]: Audio clip is null");

            if (GetVolume(audioType) <= 0f)
                return;

            AudioSourceCase sourceCase = GetAudioSource();

            AudioSource source = sourceCase.AudioSource;
            if (position.HasValue)
                source.transform.position = position.Value;
            source.spatialBlend = spatialBlend;
            source.pitch = pitch;
            source.loop = loop;

            sourceCase.Play(clip, volumePercentage, audioType);
        }

        private static AudioSourceCase GetAudioSource()
        {
            foreach (AudioSourceCase audioSource in audioSourcesPool)
            {
                if (!audioSource.IsPlaying)
                {
                    return audioSource;
                }
            }

            AudioSourceCase createdSource = new AudioSourceCase();
            audioSourcesPool.Add(createdSource);

            return createdSource;
        }

        public static void SetVolume(AudioType audioType, float volume)
        {
            if (volumeDictionary == null)
                volumeDictionary = new Dictionary<AudioType, float>();

            if (audioSourcesPool != null)
            {
                foreach (AudioSourceCase audioSource in audioSourcesPool)
                {
                    audioSource.OverrideVolume(audioType, volume);
                }
            }

            volumeDictionary[audioType] = volume;

            if (audioType == AudioType.Music)
                MusicEmitter.ApplyVolume(volume);

            SaveController.MarkAsSaveIsRequired();

            VolumeChanged?.Invoke(audioType, volume);
        }

        public static float GetVolume(AudioType audioType)
        {
            if (volumeDictionary.ContainsKey(audioType))
                return volumeDictionary[audioType];

            return 1.0f;
        }

        public static void SetMusicBOpen(bool b)
        {
            var audioType = AudioType.Music;

            if (b)
            {
                float volume = GetVolume(audioType);
                foreach (AudioSourceCase audioSource in audioSourcesPool)
                {
                    audioSource.OverrideVolume(audioType, volume);
                }
                MusicEmitter.ApplyVolume(volume);
                VolumeChanged?.Invoke(audioType, volume);
            }
            else
            {
                float volume = 0;
                foreach (AudioSourceCase audioSource in audioSourcesPool)
                {
                    audioSource.OverrideVolume(audioType, volume);
                }
                MusicEmitter.ApplyVolume(volume);
                VolumeChanged?.Invoke(audioType, volume);
            }
        }

        public static bool IsAudioTypeActive(AudioType audioType)
        {
            return GetVolume(audioType) > 0.5f;
        }

        private static void UnloadStatic()
        {
            audioSourcesPool = null;

            audioClips = null;
            audioListener = null;

            save = null;

            volumeDictionary = null;

            VolumeChanged = null;
        }

        public delegate void OnVolumeChangedCallback(AudioType audioType, float volume);
    }

    public enum AudioType
    {
        Music = 0,
        Sound = 1
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