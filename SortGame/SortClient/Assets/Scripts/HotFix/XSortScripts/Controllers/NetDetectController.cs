using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace FishXSortPlay
{
    /// <summary>
    /// 只探测网络，给 main_stage / stage_event 的 is_nonet 用。不弹窗，不改难度。
    /// 请求在 1 秒内失败视为无网；更慢的失败视为弱网，不记 is_nonet。
    /// </summary>
    public class NetDetectController : MonoBehaviour
    {
        const string DetectUrl = "https://www.google.com";
        const int ProbeTimeoutSeconds = 3;
        const float ProbeIntervalSeconds = 3f;
        const float LoseNetSeconds = 1f;

        static NetDetectController netDetectController;
        static bool isLoseNet;
        static bool isLeakNet;

        bool loopRunning;
        bool probeRunning;
        int probeId;

        public static void NotifyMainCity()
        {
            if (!PassGate())
                return;

            Ensure();
            netDetectController.StartProbeLoop();
        }

        public static void NotifyInLevel()
        {
            if (!PassGate())
                return;

            Ensure();
            netDetectController.StartProbeLoop();
        }

        public static bool GetIsLoseNet()
        {
            return isLoseNet;
        }

        public static bool GetIsLeakNet()
        {
            return isLeakNet;
        }

        static bool PassGate()
        {
            return GameData.BFromStartScene && GPUserDataManager.Instance != null && !GPUserDataManager.Instance.GetBNoAd();
        }

        static void Ensure()
        {
            if (netDetectController != null)
                return;

            var go = new GameObject(nameof(NetDetectController));
            netDetectController = go.AddComponent<NetDetectController>();
        }

        void Awake()
        {
            netDetectController = this;
        }

        void OnDestroy()
        {
            if (netDetectController == this)
                netDetectController = null;
        }

        void StartProbeLoop()
        {
            if (loopRunning)
                return;

            loopRunning = true;
            StartCoroutine(CoProbe());
        }

        IEnumerator CoProbe()
        {
            while (true)
            {
                if (PassGate() && !probeRunning)
                    yield return CheckInternet();

                yield return new WaitForSecondsRealtime(ProbeIntervalSeconds);
            }
        }

        IEnumerator CheckInternet()
        {
            float preTime = Time.realtimeSinceStartup;
            int id = ++probeId;
            probeRunning = true;
            UnityWebRequest req = UnityWebRequest.Get(DetectUrl);
            try
            {
                req.timeout = ProbeTimeoutSeconds;
                yield return req.SendWebRequest();

                if (id != probeId)
                    yield break;

                if (req.result != UnityWebRequest.Result.Success)
                {
                    float deltaTime = Time.realtimeSinceStartup - preTime;
                    ApplyProbe(deltaTime < LoseNetSeconds, deltaTime >= LoseNetSeconds);
                }
                else
                {
                    ApplyProbe(false, false);
                }
            }
            finally
            {
                if (id == probeId)
                    probeRunning = false;
                req.Dispose();
            }
        }

        static void ApplyProbe(bool lose, bool leak)
        {
            isLoseNet = lose;
            isLeakNet = leak;
        }
    }
}
