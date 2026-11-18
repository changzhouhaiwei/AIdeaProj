using UnityEngine;
using EwingSoft.GameSDK;

namespace FishFramework
{
    [DisallowMultipleComponent]
    public class DataAnalysisModule : MonoBehaviour
    {
        public void TrackEvent(string key, string contentJson)
        {
            ThinkingDataSdk.LogEvent(key, contentJson);
        }

        public void TrackAppsFlyerEvent(string key, string contentJson)
        {
            AttrSdk.TrackEvent(key);
        }

        public void TrackAppFirebaseEvent(string key, string contentJson)
        {
            FirebaseSdk.LogEvent(key);
        }
    }
}
