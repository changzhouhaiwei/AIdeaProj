using System.Collections;
using UnityEngine;

namespace FishFramework
{
    [DisallowMultipleComponent]
    public class GameModule : MonoBehaviour
    {
        public static AudioModule Audio { get; private set; }

        public static ObjectPoolModule ObjectPool { get; private set; }

        public static ResourceModule Resource { get; private set; }

        public static UIModule UI { get; private set; }

        public static SettingModule Setting { get; private set; }

        public static DataAnalysisModule DataAnylasis { get; private set; }

        private bool _lowMemoryReleased;

        private void Awake()
        {
            
            gameObject.name = $"[{nameof(GameModule)}]";
            DontDestroyOnLoad(gameObject);

            Audio = GetComponentInChildren<AudioModule>();
            ObjectPool = GetComponentInChildren<ObjectPoolModule>();
            Resource = GetComponentInChildren<ResourceModule>();
            UI = GetComponentInChildren<UIModule>();
            Setting = GetComponentInChildren<SettingModule>();
            DataAnylasis = GetComponentInChildren<DataAnalysisModule>();
        }

        public IEnumerator Initialize()
        {
            yield return StartCoroutine(Resource.InitializeAsync());
            UI.InitRoot();
            yield return StartCoroutine(Setting.Initialize());
            Application.lowMemory += OnLowMemory;
        }

        private void OnApplicationQuit()
        {
            Application.lowMemory -= OnLowMemory;
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            Audio = null;
            ObjectPool = null;
            Resource = null;
            UI = null;
            Setting = null;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                _lowMemoryReleased = false;
            }
        }

        // 系统通知内存偏紧。同一次前台只卸一次，避免来回切后台反复扫资源。
        private void OnLowMemory()
        {
            if (_lowMemoryReleased)
            {
                return;
            }

            _lowMemoryReleased = true;
            ResourceModule.ReleaseAllReleasableMemory();
        }
    }
}