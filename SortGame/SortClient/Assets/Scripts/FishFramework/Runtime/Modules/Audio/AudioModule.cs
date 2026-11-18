using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

namespace FishFramework
{
    [DisallowMultipleComponent]
    public class AudioModule : MonoBehaviour
    {
        [SerializeField] private List<AudioDatabase> databases = new List<AudioDatabase>(0);

        private AudioListener audioListener;

        //AudioListener跟随的Transform
        private Transform listenerTrans;

        public BGMController BGM { get; private set; }
        public SFXController SFX { get; private set; }

        private bool m_bVibrate;
        private void Awake()
        {
            //如果当前有AudioListener组件 先将其销毁
            var hasedListener = FindObjectOfType<AudioListener>();
            if (hasedListener) Destroy(hasedListener);
            //创建一个新的物体设为子级并为其添加AudioListener组件
            var listener = new GameObject("Listener");
            listener.transform.SetParent(transform);
            audioListener = listener.AddComponent<AudioListener>();

            BGM = GetComponentInChildren<BGMController>();
            SFX = GetComponentInChildren<SFXController>();

            InitVibrateAndMusicSound();
        }

        private void Update()
        {
            if (listenerTrans != null)
                audioListener.transform.position = listenerTrans.position;
        }

        public void SetListener(Transform listenerTrans)
        {
            this.listenerTrans = listenerTrans;
        }

        public AudioClip FromDatabase(string databaseName, string clipName)
        {
            AudioDatabase database = databases.Find(m => m.name == databaseName);
            return database != null ? database[clipName] : null;
        }

        public AudioClip FromDatabase(string databaseName, string clipName, out AudioMixerGroup outputAudioMixerGroup)
        {
            outputAudioMixerGroup = null;
            AudioDatabase database = databases.Find(m => m.name == databaseName);
            if (database != null)
            {
                outputAudioMixerGroup = database.outputAudioMixerGroup;
                return database[clipName];
            }

            return null;
        }

        public void InitVibrateAndMusicSound()
        {
            var val1 = PlayerPrefs.GetInt("vibrateBtn", 1);
            m_bVibrate = (val1 == 1 ? true : false);

            var val2 = PlayerPrefs.GetInt("musicBtn", 1);
            BGM.Volume = val2;

            var val3 = PlayerPrefs.GetInt("soundBtn", 1);
            SFX.Volume = val3;
        }

        public void PlayVibrate(float v1, float v2, float v3)
        {
            if (m_bVibrate)
            {
                // HapticPatterns.PlayConstant(v1, v2, v3);
            }
        }

        //是否可以震动
        public void SetBVibrate(bool b)
        {
            m_bVibrate = b;
        }
    }
}