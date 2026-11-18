using System;
using System.Collections;
using UnityEngine;
using xasset;

namespace FishFramework
{
    [DisallowMultipleComponent]
    public class DataAnalysisModule : MonoBehaviour
    {
        private float m_lastTm = 0f;
        //通用事件
        public void TrackEvent(string key, string contentJson)
        {
            GPSDKMgr.Instance.TrackEvent(key, contentJson);
        }   

        //AppFlyer
        public void TrackAppsFlyerEvent(string key, string contentJson)
        {
            GPSDKMgr.Instance.TrackAppsFlyerEvent(key, contentJson);
        }

        //Firebase
        public void TrackAppFirebaseEvent(string key,string contentJson)
        {
            GPSDKMgr.Instance.TrackFirebaseEvent(key,contentJson);
        }
        

        private void Update()
        {
            if (m_lastTm == 0f)
            {
                m_lastTm = Time.realtimeSinceStartup;
            }

            if (Time.realtimeSinceStartup - m_lastTm >= 60)
            {
                GameBAData.ExcuteOnline();
                m_lastTm = Time.realtimeSinceStartup;
                Debug.Log("online event");
            }
        }

        public void ExcuteLoadingEvent10008() => GameBAData.ExcuteLoadingEvent10008();

        public void ExcuteLoadingEvent10009() => GameBAData.ExcuteLoadingEvent10009();

        public void ExcuteLoadingEvent10010() => GameBAData.ExcuteLoadingEvent10010();

        public void ExcuteLoadingEvent10014() => GameBAData.ExcuteLoadingEvent10014();

        public void ExcuteLoadingEvent10015() => GameBAData.ExcuteLoadingEvent10015();

        public void ExcuteLoadingEvent10016() => GameBAData.ExcuteLoadingEvent10016();

        public void ExcuteLoadingEvent10017() => GameBAData.ExcuteLoadingEvent10017();

        public void ExcuteLoadingEvent10029() => GameBAData.ExcuteLoadingEvent10029();
    }
}