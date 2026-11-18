using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EwingSoft.GameSDK
{
    [Serializable]
    public class ExtMetadataItem
    {
        public string DataKey { get; }
        public string DataValue { get; }
        public long ExpireTimestamp { get; }
        public int ExpireDays { get; }

        public ExtMetadataItem(string dataKey, string dataValue)
            : this(dataKey, dataValue, -1L, -1)
        {
        }

        public ExtMetadataItem(string dataKey, string dataValue, long expireTimestamp, int expireDays)
        {
            DataKey = dataKey;
            DataValue = dataValue;
            ExpireTimestamp = expireTimestamp;
            ExpireDays = expireDays;
        }
    }

    public class RemoteDataSdk : MonoBehaviour
    {
        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.data.remote.RemoteDataSdk";

        private static GameSdkNoneParamVoidCallback   mPostUserDataCallback;
        private static GameSdkNoneParamVoidCallback   mGetUserDataSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetUserDataFailedCallback;
        private static GameSdkNoneParamVoidCallback   mDeleteUserDataSuccessCallback;
        private static GameSdkIntParamVoidCallback    mDeleteUserDataFailedCallback;
        private static GameSdkStringParamVoidCallback mGetRankDataSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetRankDataFailedCallback;
        private static GameSdkStringParamVoidCallback mGetEventStatusSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetEventStatusFailedCallback;
        private static GameSdkLongParamVoidCallback   mGetServerTimeSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetServerTimeFailedCallback;

        private static GameSdkNoneParamVoidCallback   mLinkUserWithOpenIdSuccessCallback;
        private static GameSdkIntParamVoidCallback    mLinkUserWithOpenIdFailedCallback;
        private static GameSdkNoneParamVoidCallback   mUnlinkUserWithOpenIdSuccessCallback;
        private static GameSdkIntParamVoidCallback    mUnlinkUserWithOpenIdFailedCallback;

        private static GameSdkStringParamVoidCallback mGetPlayerExtMetadataSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetPlayerExtMetadataFailedCallback;
        private static GameSdkStringParamVoidCallback mSavePlayerExtMetadataSuccessCallback;
        private static GameSdkIntParamVoidCallback    mSavePlayerExtMetadataFailedCallback;
        private static GameSdkStringParamVoidCallback mBatchGetPlayerExtMetadataSuccessCallback;
        private static GameSdkIntParamVoidCallback    mBatchGetPlayerExtMetadataFailedCallback;
        private static GameSdkStringParamVoidCallback mBatchSavePlayerExtMetadataSuccessCallback;
        private static GameSdkIntParamVoidCallback    mBatchSavePlayerExtMetadataFailedCallback;
        private static GameSdkStringParamVoidCallback mGetGroupExtMetadataSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetGroupExtMetadataFailedCallback;
        private static GameSdkStringParamVoidCallback mSaveGroupExtMetadataSuccessCallback;
        private static GameSdkIntParamVoidCallback    mSaveGroupExtMetadataFailedCallback;
        private static GameSdkStringParamVoidCallback mBatchGetGroupExtMetadataSuccessCallback;
        private static GameSdkIntParamVoidCallback    mBatchGetGroupExtMetadataFailedCallback;
        private static GameSdkStringParamVoidCallback mBatchSaveGroupExtMetadataSuccessCallback;
        private static GameSdkIntParamVoidCallback    mBatchSaveGroupExtMetadataFailedCallback;

        private static GameSdkStringParamVoidCallback mGetGroupConfigSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetGroupConfigFailedCallback;
        private static GameSdkStringParamVoidCallback mGetGroupByUserIdSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetGroupByUserIdFailedCallback;
        private static GameSdkStringParamVoidCallback mCreateGroupSuccessCallback;
        private static GameSdkIntParamVoidCallback    mCreateGroupFailedCallback;
        private static GameSdkStringParamVoidCallback mEditGroupSuccessCallback;
        private static GameSdkIntParamVoidCallback    mEditGroupFailedCallback;
        private static GameSdkStringParamVoidCallback mRecommendGroupsSuccessCallback;
        private static GameSdkIntParamVoidCallback    mRecommendGroupsFailedCallback;
        private static GameSdkStringParamVoidCallback mSearchGroupsSuccessCallback;
        private static GameSdkIntParamVoidCallback    mSearchGroupsFailedCallback;
        private static GameSdkStringParamVoidCallback mJoinGroupSuccessCallback;
        private static GameSdkIntParamVoidCallback    mJoinGroupFailedCallback;
        private static GameSdkStringParamVoidCallback mLeaveGroupSuccessCallback;
        private static GameSdkIntParamVoidCallback    mLeaveGroupFailedCallback;
        private static GameSdkStringParamVoidCallback mKickPlayerSuccessCallback;
        private static GameSdkIntParamVoidCallback    mKickPlayerFailedCallback;
        private static GameSdkStringParamVoidCallback mGetGroupMembersSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetGroupMembersFailedCallback;
        private static GameSdkStringParamVoidCallback mGetGroupMembersWithMetadataSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetGroupMembersWithMetadataFailedCallback;
        private static GameSdkStringParamVoidCallback mGetApplyListSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetApplyListFailedCallback;
        private static GameSdkStringParamVoidCallback mApproveApplicationSuccessCallback;
        private static GameSdkIntParamVoidCallback    mApproveApplicationFailedCallback;
        private static GameSdkStringParamVoidCallback mRejectApplicationSuccessCallback;
        private static GameSdkIntParamVoidCallback    mRejectApplicationFailedCallback;
        private static GameSdkStringParamVoidCallback mAssignRoleSuccessCallback;
        private static GameSdkIntParamVoidCallback    mAssignRoleFailedCallback;
        private static GameSdkStringParamVoidCallback mDeleteRoleSuccessCallback;
        private static GameSdkIntParamVoidCallback    mDeleteRoleFailedCallback;
        private static GameSdkStringParamVoidCallback mTransferRoleSuccessCallback;
        private static GameSdkIntParamVoidCallback    mTransferRoleFailedCallback;

        private static GameSdkNoneParamVoidCallback   mGroupChatConnectSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGroupChatConnectFailedCallback;
        private static GameSdkStringParamVoidCallback mGroupChatMessageCallback;
        private static GameSdkNoneParamVoidCallback   mGroupChatMessageSentSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGroupChatMessageSentFailedCallback;
        private static readonly Dictionary<int, GameSdkStringParamVoidCallback> mGetGroupChatMessagesByTypeSuccessCallbacks = new Dictionary<int, GameSdkStringParamVoidCallback>();
        private static readonly Dictionary<int, GameSdkIntParamVoidCallback>    mGetGroupChatMessagesByTypeFailedCallbacks = new Dictionary<int, GameSdkIntParamVoidCallback>();
        private static bool mGetGroupChatMessagesByTypeCallbacksRegistered;

        private static GameSdkStringParamVoidCallback mGetUGPPropertyByIdSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetUGPPropertyByIdFailedCallback;
        private static GameSdkStringParamVoidCallback mCreateUGPPropertySuccessCallback;
        private static GameSdkIntParamVoidCallback    mCreateUGPPropertyFailedCallback;
        private static GameSdkStringParamVoidCallback mClaimUGPPropertySuccessCallback;
        private static GameSdkIntParamVoidCallback    mClaimUGPPropertyFailedCallback;
        private static GameSdkStringParamVoidCallback mGetUGPPropertyClaimsSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetUGPPropertyClaimsFailedCallback;
        private static GameSdkStringParamVoidCallback mCheckUGPPropertyClaimedSuccessCallback;
        private static GameSdkIntParamVoidCallback    mCheckUGPPropertyClaimedFailedCallback;
        private static GameSdkStringParamVoidCallback mGetUGPPropertiesByGroupSuccessCallback;
        private static GameSdkIntParamVoidCallback    mGetUGPPropertiesByGroupFailedCallback;

        public static void InitRemoteDataSdk(string url, string appId)
        {
            if(Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                    {
                        javaRemoteData.CallStatic("init", currentActivity, url, appId);
                    }
                }
            }
        }

        public static void SetRemoteAppSecret(string appSecret)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("setRemoteAppSecret", appSecret);
                }
            }
        }

        public static void GetUserDataByUid(GameSdkNoneParamVoidCallback callback, GameSdkIntParamVoidCallback failedCallback, string uid)
        {
            mGetUserDataSuccessCallback = callback;
            mGetUserDataFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGetUserDataSuccessEvent += OnGetUserData;
            GameSDKCallbackListener.OnGetUserDataFailEvent += OnGetUserDataFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getUserDataByUid", uid);
                }
            }
            else
            {
                OnGetUserDataFailed(-1);
            }
        }

        public static void GetUserDataByOpenId(GameSdkNoneParamVoidCallback callback, GameSdkIntParamVoidCallback failedCallback, int loginType, string openId)
        {
            mGetUserDataSuccessCallback = callback;
            mGetUserDataFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGetUserDataSuccessEvent += OnGetUserData;
            GameSDKCallbackListener.OnGetUserDataFailEvent += OnGetUserDataFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getUserDataByOpenId", loginType, openId);
                }
            }
            else
            {
                OnGetUserDataFailed(-1);
            }
        }

        public static void LinkUserWithOpenId(GameSdkNoneParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string uid, int loginType, string openId)
        {
            mLinkUserWithOpenIdSuccessCallback = successCallback;
            mLinkUserWithOpenIdFailedCallback = failedCallback;
            GameSDKCallbackListener.OnLinkUserWithOpenIdSuccessEvent += OnLinkUserWithOpenIdSuccess;
            GameSDKCallbackListener.OnLinkUserWithOpenIdFailEvent += OnLinkUserWithOpenIdFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("linkUserWithOpenId", uid, loginType, openId);
                }
            }
            else
            {
                OnLinkUserWithOpenIdFailed(-1);
            }
        }

        public static void UnlinkUserWithOpenId(GameSdkNoneParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string uid, int loginType)
        {
            mUnlinkUserWithOpenIdSuccessCallback = successCallback;
            mUnlinkUserWithOpenIdFailedCallback = failedCallback;
            GameSDKCallbackListener.OnUnlinkUserWithOpenIdSuccessEvent += OnUnlinkUserWithOpenIdSuccess;
            GameSDKCallbackListener.OnUnlinkUserWithOpenIdFailEvent += OnUnlinkUserWithOpenIdFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("unlinkUserWithOpenId", uid, loginType);
                }
            }
            else
            {
                OnUnlinkUserWithOpenIdFailed(-1);
            }
        }

        private static void OnLinkUserWithOpenIdSuccess()
        {
            mLinkUserWithOpenIdSuccessCallback?.Invoke();
            ResetLinkUserWithOpenIdCallbacks();
        }

        private static void OnLinkUserWithOpenIdFailed(int responseCode)
        {
            mLinkUserWithOpenIdFailedCallback?.Invoke(responseCode);
            ResetLinkUserWithOpenIdCallbacks();
        }

        private static void ResetLinkUserWithOpenIdCallbacks()
        {
            mLinkUserWithOpenIdSuccessCallback = null;
            mLinkUserWithOpenIdFailedCallback = null;
            GameSDKCallbackListener.OnLinkUserWithOpenIdSuccessEvent -= OnLinkUserWithOpenIdSuccess;
            GameSDKCallbackListener.OnLinkUserWithOpenIdFailEvent -= OnLinkUserWithOpenIdFailed;
        }

        private static void OnUnlinkUserWithOpenIdSuccess()
        {
            mUnlinkUserWithOpenIdSuccessCallback?.Invoke();
            ResetUnlinkUserWithOpenIdCallbacks();
        }

        private static void OnUnlinkUserWithOpenIdFailed(int responseCode)
        {
            mUnlinkUserWithOpenIdFailedCallback?.Invoke(responseCode);
            ResetUnlinkUserWithOpenIdCallbacks();
        }

        private static void ResetUnlinkUserWithOpenIdCallbacks()
        {
            mUnlinkUserWithOpenIdSuccessCallback = null;
            mUnlinkUserWithOpenIdFailedCallback = null;
            GameSDKCallbackListener.OnUnlinkUserWithOpenIdSuccessEvent -= OnUnlinkUserWithOpenIdSuccess;
            GameSDKCallbackListener.OnUnlinkUserWithOpenIdFailEvent -= OnUnlinkUserWithOpenIdFailed;
        }

        public static void OnGetUserData()
        {
            if(mGetUserDataSuccessCallback != null)
            {
                mGetUserDataSuccessCallback();
            }
            ResetGetUserDataCallback();
        }

        public static void OnGetUserDataFailed(int responseCode)
        {
            if(mGetUserDataFailedCallback != null)
            {
                mGetUserDataFailedCallback(responseCode);
            }
            ResetGetUserDataCallback();
        }

        private static void ResetGetUserDataCallback()
        {
            mGetUserDataSuccessCallback = null;
            mGetUserDataFailedCallback = null;
            GameSDKCallbackListener.OnGetUserDataSuccessEvent -= OnGetUserData;
            GameSDKCallbackListener.OnGetUserDataFailEvent -= OnGetUserDataFailed;
        }

        public static void PostUserData(GameSdkNoneParamVoidCallback callback, string uid, string data, string properties, string name, string avatar, Dictionary<string, string> rankparams)
        {
            mPostUserDataCallback = callback;
            GameSDKCallbackListener.OnPostUserDataEvent += OnPostUserData;

            // 空字符串校验：如果 data 为空字符串，重置为空 JSON 格式字符串
            if (string.IsNullOrEmpty(data))
            {
                data = "{}"; // 空 JSON 格式字符串
            }

            if (string.IsNullOrEmpty(properties))
            {
                properties = "{}";// 空 JSON 格式字符串
            }
             
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    using (var javaRankParams = new AndroidJavaObject("java.util.TreeMap"))
                    {
                        foreach (var kvp in rankparams)
                        {
                            javaRankParams.Call<AndroidJavaObject>("put", kvp.Key, kvp.Value);
                        }
                        javaRemoteData.CallStatic("postUserData", uid, data, properties, name, avatar, javaRankParams);
                    }
                }
            }
            else
            {
                OnPostUserData();
            }
        }

        public static void OnPostUserData()
        {
            if(mPostUserDataCallback != null)
            {
                mPostUserDataCallback();
            }

            mPostUserDataCallback = null;
            GameSDKCallbackListener.OnPostUserDataEvent -= OnPostUserData;
        }

        public static void DeleteUserData(GameSdkNoneParamVoidCallback callback, GameSdkIntParamVoidCallback failedcallback, string uid)
        {
            mDeleteUserDataSuccessCallback = callback;
            mDeleteUserDataFailedCallback = failedcallback;
            GameSDKCallbackListener.OnDeleteUserDataSuccessEvent += OnDeleteUserData;
            GameSDKCallbackListener.OnDeleteUserDataFailEvent += OnDeleteUserDataFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("deleteUserData", uid);
                }
            }
            else
            {
                OnDeleteUserDataFailed(-1);
            }
        }

        public static void OnDeleteUserData()
        {
            if (mDeleteUserDataSuccessCallback != null)
            {
                mDeleteUserDataSuccessCallback();
            }
            ResetDeleteUserDataCallback();
        }

        public static void OnDeleteUserDataFailed(int responseCode)
        {
            if(mDeleteUserDataFailedCallback != null)
            {
                mDeleteUserDataFailedCallback(responseCode);
            }
            ResetDeleteUserDataCallback();
        }

        private static void ResetDeleteUserDataCallback()
        {
            mDeleteUserDataFailedCallback = null;
            mDeleteUserDataSuccessCallback = null;
            GameSDKCallbackListener.OnDeleteUserDataSuccessEvent -= OnDeleteUserData;
            GameSDKCallbackListener.OnDeleteUserDataFailEvent -= OnDeleteUserDataFailed;
        }

        public static string GetUid()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    return javaRemoteData.CallStatic<string>("getUid");
                }
            }
            return string.Empty;
        }

        public static int GetIntValue(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    return javaRemoteData.CallStatic<int>("getIntValue", key);
                }
            }
            return 0;
        }

        public static long GetLongValue(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    return javaRemoteData.CallStatic<long>("getLongValue", key);
                }
            }
            return 0L;
        }

        public static double GetDoubleValue(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    return javaRemoteData.CallStatic<double>("getDoubleValue", key);
                }
            }
            return 0.0;
        }

        public static string GetStringValue(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    return javaRemoteData.CallStatic<string>("getStringValue", key);
                }
            }
            return string.Empty;
        }

        public static bool GetBoolValue(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    return javaRemoteData.CallStatic<bool>("getBooleanValue", key);
                }
            }
            return false;
        }

        public static string GetProperty(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    return javaRemoteData.CallStatic<string>("getProperty", key);
                }
            }
            return string.Empty;
        }

        public static string GetMetaData(string key)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    return javaRemoteData.CallStatic<string>("getMetaData", key);
                }
            }
            return string.Empty;
        }

        public static string GetOpenId(int loginType)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    return javaRemoteData.CallStatic<string>("getOpenId", loginType);
                }
            }
            return string.Empty;
        }

        public static void GetRankData(GameSdkStringParamVoidCallback callback, GameSdkIntParamVoidCallback failedcallback, int page, int pageSize, bool isLocal)
        {
            PrepareRankDataCallbacks(callback, failedcallback);

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getRankData", page, pageSize, isLocal);
                }
            }
            else
            {
                OnGetRankDataFailed(-1);
            }
        }

        public static void GetRankData(GameSdkStringParamVoidCallback callback, GameSdkIntParamVoidCallback failedcallback, int page, int pageSize, string leaderboardCode, bool isLocal)
        {
            PrepareRankDataCallbacks(callback, failedcallback);

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getRankData", page, pageSize, leaderboardCode, isLocal);
                }
            }
            else
            {
                OnGetRankDataFailed(-1);
            }
        }

        public static void GetRankData(GameSdkStringParamVoidCallback callback, GameSdkIntParamVoidCallback failedcallback, int page, int pageSize, string leaderboardCode, bool isLocal, int leaderboardType, bool includeGroupInfo)
        {
            PrepareRankDataCallbacks(callback, failedcallback);

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getRankData", page, pageSize, leaderboardCode, isLocal, leaderboardType, includeGroupInfo);
                }
            }
            else
            {
                OnGetRankDataFailed(-1);
            }
        }

        private static void PrepareRankDataCallbacks(GameSdkStringParamVoidCallback callback, GameSdkIntParamVoidCallback failedcallback)
        {
            mGetRankDataSuccessCallback = callback;
            mGetRankDataFailedCallback = failedcallback;
            GameSDKCallbackListener.OnGetRankDataSuccessEvent += OnGetRankData;
            GameSDKCallbackListener.OnGetRankDataFailEvent += OnGetRankDataFailed;
        }

        public static void OnGetRankData(string rankData)
        {
            if(mGetRankDataSuccessCallback != null)
            {
                mGetRankDataSuccessCallback(rankData);
            }
            ResetGetRankDataCallback();
        }

        public static void OnGetRankDataFailed(int responseCode)
        {
            if(mGetRankDataFailedCallback != null)
            {
                mGetRankDataFailedCallback(responseCode);
            }
            ResetGetRankDataCallback();
        }

        private static void ResetGetRankDataCallback()
        {
            mGetRankDataFailedCallback = null;
            mGetRankDataSuccessCallback = null;
            GameSDKCallbackListener.OnGetRankDataSuccessEvent -= OnGetRankData;
            GameSDKCallbackListener.OnGetRankDataFailEvent -= OnGetRankDataFailed;
        }

        public static void GetEventStatus(GameSdkStringParamVoidCallback callback, GameSdkIntParamVoidCallback failedcallback)
        {
            mGetEventStatusSuccessCallback = callback;
            mGetEventStatusFailedCallback = failedcallback;
            GameSDKCallbackListener.OnGetEventStatusSuccessEvent += OnGetEventStatus;
            GameSDKCallbackListener.OnGetEventStatusFailEvent += OnGetEventStatusFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getEventStatus");
                }
            }
            else
            {
                OnGetEventStatusFailed(-1);
            }
        }

        public static void OnGetEventStatus(string eventStatus)
        {
            if(mGetEventStatusSuccessCallback != null)
            {
                mGetEventStatusSuccessCallback(eventStatus);
            }
            ResetGetEventStatusCallback();
        }

        public static void OnGetEventStatusFailed(int responseCode)
        {
            if(mGetEventStatusFailedCallback != null)
            {
                mGetEventStatusFailedCallback(responseCode);
            }
            ResetGetEventStatusCallback();
        }

        private static void ResetGetEventStatusCallback()
        {
            mGetEventStatusFailedCallback = null;
            mGetEventStatusSuccessCallback = null;
            GameSDKCallbackListener.OnGetEventStatusSuccessEvent -= OnGetEventStatus;
            GameSDKCallbackListener.OnGetEventStatusFailEvent -= OnGetEventStatusFailed;
        }

        public static void GetServerTime(GameSdkLongParamVoidCallback callback, GameSdkIntParamVoidCallback failedcallback)
        {
            mGetServerTimeSuccessCallback = callback;
            mGetServerTimeFailedCallback = failedcallback;
            GameSDKCallbackListener.OnGetServerTimeSuccessEvent += OnGetServerTime;
            GameSDKCallbackListener.OnGetServerTimeFailEvent += OnGetServerTimeFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getServerTime");
                }
            }
            else
            {
                OnGetServerTimeFailed(-1);
            }
        }

        public static void OnGetServerTime(long unixTimeSecond)
        {
            mGetServerTimeSuccessCallback?.Invoke(unixTimeSecond);
            ResetGetServerTimeCallback();
        }

        public static void OnGetServerTimeFailed(int responseCode)
        {
            mGetServerTimeFailedCallback?.Invoke(responseCode);
            ResetGetServerTimeCallback();
        }

        private static void ResetGetServerTimeCallback()
        {
            mGetServerTimeFailedCallback = null;
            mGetServerTimeSuccessCallback = null;
            GameSDKCallbackListener.OnGetServerTimeSuccessEvent -= OnGetServerTime;
            GameSDKCallbackListener.OnGetServerTimeFailEvent -= OnGetServerTimeFailed;
        }
        // Ext metadata APIs
        public static void GetPlayerExtMetadata(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string userId, string dataKey)
        {
            mGetPlayerExtMetadataSuccessCallback = successCallback;
            mGetPlayerExtMetadataFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGetPlayerExtMetadataSuccessEvent += OnGetPlayerExtMetadataSuccess;
            GameSDKCallbackListener.OnGetPlayerExtMetadataFailEvent += OnGetPlayerExtMetadataFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getPlayerExtMetadata", userId, dataKey);
                }
            }
            else
            {
                OnGetPlayerExtMetadataFailed(-1);
            }
        }

        private static void OnGetPlayerExtMetadataSuccess(string data)
        {
            mGetPlayerExtMetadataSuccessCallback?.Invoke(data);
            ResetGetPlayerExtMetadataCallbacks();
        }

        private static void OnGetPlayerExtMetadataFailed(int errorCode)
        {
            mGetPlayerExtMetadataFailedCallback?.Invoke(errorCode);
            ResetGetPlayerExtMetadataCallbacks();
        }

        private static void ResetGetPlayerExtMetadataCallbacks()
        {
            mGetPlayerExtMetadataSuccessCallback = null;
            mGetPlayerExtMetadataFailedCallback = null;
            GameSDKCallbackListener.OnGetPlayerExtMetadataSuccessEvent -= OnGetPlayerExtMetadataSuccess;
            GameSDKCallbackListener.OnGetPlayerExtMetadataFailEvent -= OnGetPlayerExtMetadataFailed;
        }

        public static void SavePlayerExtMetadata(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string userId, string dataKey, string dataValue, long expireTimestamp, int expireDays)
        {
            mSavePlayerExtMetadataSuccessCallback = successCallback;
            mSavePlayerExtMetadataFailedCallback = failedCallback;
            GameSDKCallbackListener.OnSavePlayerExtMetadataSuccessEvent += OnSavePlayerExtMetadataSuccess;
            GameSDKCallbackListener.OnSavePlayerExtMetadataFailEvent += OnSavePlayerExtMetadataFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("savePlayerExtMetadata", userId, dataKey, dataValue, expireTimestamp, expireDays);
                }
            }
            else
            {
                OnSavePlayerExtMetadataFailed(-1);
            }
        }

        private static void OnSavePlayerExtMetadataSuccess(string data)
        {
            mSavePlayerExtMetadataSuccessCallback?.Invoke(data);
            ResetSavePlayerExtMetadataCallbacks();
        }

        private static void OnSavePlayerExtMetadataFailed(int errorCode)
        {
            mSavePlayerExtMetadataFailedCallback?.Invoke(errorCode);
            ResetSavePlayerExtMetadataCallbacks();
        }

        private static void ResetSavePlayerExtMetadataCallbacks()
        {
            mSavePlayerExtMetadataSuccessCallback = null;
            mSavePlayerExtMetadataFailedCallback = null;
            GameSDKCallbackListener.OnSavePlayerExtMetadataSuccessEvent -= OnSavePlayerExtMetadataSuccess;
            GameSDKCallbackListener.OnSavePlayerExtMetadataFailEvent -= OnSavePlayerExtMetadataFailed;
        }

        public static void BatchGetPlayerExtMetadata(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string userId, string[] dataKeys)
        {
            mBatchGetPlayerExtMetadataSuccessCallback = successCallback;
            mBatchGetPlayerExtMetadataFailedCallback = failedCallback;
            GameSDKCallbackListener.OnBatchGetPlayerExtMetadataSuccessEvent += OnBatchGetPlayerExtMetadataSuccess;
            GameSDKCallbackListener.OnBatchGetPlayerExtMetadataFailEvent += OnBatchGetPlayerExtMetadataFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("batchGetPlayerExtMetadata", userId, dataKeys);
                }
            }
            else
            {
                OnBatchGetPlayerExtMetadataFailed(-1);
            }
        }

        private static void OnBatchGetPlayerExtMetadataSuccess(string data)
        {
            mBatchGetPlayerExtMetadataSuccessCallback?.Invoke(data);
            ResetBatchGetPlayerExtMetadataCallbacks();
        }

        private static void OnBatchGetPlayerExtMetadataFailed(int errorCode)
        {
            mBatchGetPlayerExtMetadataFailedCallback?.Invoke(errorCode);
            ResetBatchGetPlayerExtMetadataCallbacks();
        }

        private static void ResetBatchGetPlayerExtMetadataCallbacks()
        {
            mBatchGetPlayerExtMetadataSuccessCallback = null;
            mBatchGetPlayerExtMetadataFailedCallback = null;
            GameSDKCallbackListener.OnBatchGetPlayerExtMetadataSuccessEvent -= OnBatchGetPlayerExtMetadataSuccess;
            GameSDKCallbackListener.OnBatchGetPlayerExtMetadataFailEvent -= OnBatchGetPlayerExtMetadataFailed;
        }

        public static void BatchSavePlayerExtMetadata(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string userId, string[] dataKeys, string[] dataValues, long[] expireTimestamps, int[] expireDays)
        {
            mBatchSavePlayerExtMetadataSuccessCallback = successCallback;
            mBatchSavePlayerExtMetadataFailedCallback = failedCallback;
            GameSDKCallbackListener.OnBatchSavePlayerExtMetadataSuccessEvent += OnBatchSavePlayerExtMetadataSuccess;
            GameSDKCallbackListener.OnBatchSavePlayerExtMetadataFailEvent += OnBatchSavePlayerExtMetadataFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("batchSavePlayerExtMetadata", userId, dataKeys, dataValues, expireTimestamps, expireDays);
                }
            }
            else
            {
                OnBatchSavePlayerExtMetadataFailed(-1);
            }
        }

        private static void OnBatchSavePlayerExtMetadataSuccess(string data)
        {
            mBatchSavePlayerExtMetadataSuccessCallback?.Invoke(data);
            ResetBatchSavePlayerExtMetadataCallbacks();
        }

        private static void OnBatchSavePlayerExtMetadataFailed(int errorCode)
        {
            mBatchSavePlayerExtMetadataFailedCallback?.Invoke(errorCode);
            ResetBatchSavePlayerExtMetadataCallbacks();
        }

        private static void ResetBatchSavePlayerExtMetadataCallbacks()
        {
            mBatchSavePlayerExtMetadataSuccessCallback = null;
            mBatchSavePlayerExtMetadataFailedCallback = null;
            GameSDKCallbackListener.OnBatchSavePlayerExtMetadataSuccessEvent -= OnBatchSavePlayerExtMetadataSuccess;
            GameSDKCallbackListener.OnBatchSavePlayerExtMetadataFailEvent -= OnBatchSavePlayerExtMetadataFailed;
        }

        public static void GetGroupExtMetadata(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string groupId, string dataKey)
        {
            mGetGroupExtMetadataSuccessCallback = successCallback;
            mGetGroupExtMetadataFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGetGroupExtMetadataSuccessEvent += OnGetGroupExtMetadataSuccess;
            GameSDKCallbackListener.OnGetGroupExtMetadataFailEvent += OnGetGroupExtMetadataFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getGroupExtMetadata", groupId, dataKey);
                }
            }
            else
            {
                OnGetGroupExtMetadataFailed(-1);
            }
        }

        private static void OnGetGroupExtMetadataSuccess(string data)
        {
            mGetGroupExtMetadataSuccessCallback?.Invoke(data);
            ResetGetGroupExtMetadataCallbacks();
        }

        private static void OnGetGroupExtMetadataFailed(int errorCode)
        {
            mGetGroupExtMetadataFailedCallback?.Invoke(errorCode);
            ResetGetGroupExtMetadataCallbacks();
        }

        private static void ResetGetGroupExtMetadataCallbacks()
        {
            mGetGroupExtMetadataSuccessCallback = null;
            mGetGroupExtMetadataFailedCallback = null;
            GameSDKCallbackListener.OnGetGroupExtMetadataSuccessEvent -= OnGetGroupExtMetadataSuccess;
            GameSDKCallbackListener.OnGetGroupExtMetadataFailEvent -= OnGetGroupExtMetadataFailed;
        }

        public static void SaveGroupExtMetadata(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string groupId, string dataKey, string dataValue, long expireTimestamp, int expireDays)
        {
            mSaveGroupExtMetadataSuccessCallback = successCallback;
            mSaveGroupExtMetadataFailedCallback = failedCallback;
            GameSDKCallbackListener.OnSaveGroupExtMetadataSuccessEvent += OnSaveGroupExtMetadataSuccess;
            GameSDKCallbackListener.OnSaveGroupExtMetadataFailEvent += OnSaveGroupExtMetadataFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("saveGroupExtMetadata", groupId, dataKey, dataValue, expireTimestamp, expireDays);
                }
            }
            else
            {
                OnSaveGroupExtMetadataFailed(-1);
            }
        }

        private static void OnSaveGroupExtMetadataSuccess(string data)
        {
            mSaveGroupExtMetadataSuccessCallback?.Invoke(data);
            ResetSaveGroupExtMetadataCallbacks();
        }

        private static void OnSaveGroupExtMetadataFailed(int errorCode)
        {
            mSaveGroupExtMetadataFailedCallback?.Invoke(errorCode);
            ResetSaveGroupExtMetadataCallbacks();
        }

        private static void ResetSaveGroupExtMetadataCallbacks()
        {
            mSaveGroupExtMetadataSuccessCallback = null;
            mSaveGroupExtMetadataFailedCallback = null;
            GameSDKCallbackListener.OnSaveGroupExtMetadataSuccessEvent -= OnSaveGroupExtMetadataSuccess;
            GameSDKCallbackListener.OnSaveGroupExtMetadataFailEvent -= OnSaveGroupExtMetadataFailed;
        }

        public static void BatchGetGroupExtMetadata(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string groupId, string[] dataKeys)
        {
            mBatchGetGroupExtMetadataSuccessCallback = successCallback;
            mBatchGetGroupExtMetadataFailedCallback = failedCallback;
            GameSDKCallbackListener.OnBatchGetGroupExtMetadataSuccessEvent += OnBatchGetGroupExtMetadataSuccess;
            GameSDKCallbackListener.OnBatchGetGroupExtMetadataFailEvent += OnBatchGetGroupExtMetadataFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("batchGetGroupExtMetadata", groupId, dataKeys);
                }
            }
            else
            {
                OnBatchGetGroupExtMetadataFailed(-1);
            }
        }

        private static void OnBatchGetGroupExtMetadataSuccess(string data)
        {
            mBatchGetGroupExtMetadataSuccessCallback?.Invoke(data);
            ResetBatchGetGroupExtMetadataCallbacks();
        }

        private static void OnBatchGetGroupExtMetadataFailed(int errorCode)
        {
            mBatchGetGroupExtMetadataFailedCallback?.Invoke(errorCode);
            ResetBatchGetGroupExtMetadataCallbacks();
        }

        private static void ResetBatchGetGroupExtMetadataCallbacks()
        {
            mBatchGetGroupExtMetadataSuccessCallback = null;
            mBatchGetGroupExtMetadataFailedCallback = null;
            GameSDKCallbackListener.OnBatchGetGroupExtMetadataSuccessEvent -= OnBatchGetGroupExtMetadataSuccess;
            GameSDKCallbackListener.OnBatchGetGroupExtMetadataFailEvent -= OnBatchGetGroupExtMetadataFailed;
        }

        public static void BatchSaveGroupExtMetadata(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string groupId, IList<ExtMetadataItem> metadataList)
        {
            mBatchSaveGroupExtMetadataSuccessCallback = successCallback;
            mBatchSaveGroupExtMetadataFailedCallback = failedCallback;
            GameSDKCallbackListener.OnBatchSaveGroupExtMetadataSuccessEvent += OnBatchSaveGroupExtMetadataSuccess;
            GameSDKCallbackListener.OnBatchSaveGroupExtMetadataFailEvent += OnBatchSaveGroupExtMetadataFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    string[] dataKeys = null;
                    string[] dataValues = null;
                    long[] expireTimestamps = null;
                    int[] expireDays = null;

                    if (metadataList != null)
                    {
                        int size = metadataList.Count;
                        dataKeys = new string[size];
                        dataValues = new string[size];
                        long[] tempExpireTimestamps = new long[size];
                        int[] tempExpireDays = new int[size];
                        bool hasExpireTimestamp = false;
                        bool hasExpireDays = false;

                        for (int i = 0; i < size; i++)
                        {
                            var item = metadataList[i];
                            if (item == null)
                            {
                                dataKeys[i] = null;
                                dataValues[i] = null;
                                tempExpireTimestamps[i] = 0L;
                                tempExpireDays[i] = 0;
                                continue;
                            }

                            dataKeys[i] = item.DataKey;
                            dataValues[i] = item.DataValue;
                            tempExpireTimestamps[i] = item.ExpireTimestamp > 0 ? item.ExpireTimestamp : 0L;
                            tempExpireDays[i] = item.ExpireDays > 0 ? item.ExpireDays : 0;
                            if (tempExpireTimestamps[i] > 0)
                            {
                                hasExpireTimestamp = true;
                            }
                            if (tempExpireDays[i] > 0)
                            {
                                hasExpireDays = true;
                            }
                        }

                        if (hasExpireTimestamp)
                        {
                            expireTimestamps = tempExpireTimestamps;
                        }
                        if (hasExpireDays)
                        {
                            expireDays = tempExpireDays;
                        }
                    }

                    javaRemoteData.CallStatic("batchSaveGroupExtMetadata", groupId, dataKeys, dataValues, expireTimestamps, expireDays);
                }
            }
            else
            {
                OnBatchSaveGroupExtMetadataFailed(-1);
            }
        }

        private static void OnBatchSaveGroupExtMetadataSuccess(string data)
        {
            mBatchSaveGroupExtMetadataSuccessCallback?.Invoke(data);
            ResetBatchSaveGroupExtMetadataCallbacks();
        }

        private static void OnBatchSaveGroupExtMetadataFailed(int errorCode)
        {
            mBatchSaveGroupExtMetadataFailedCallback?.Invoke(errorCode);
            ResetBatchSaveGroupExtMetadataCallbacks();
        }

        private static void ResetBatchSaveGroupExtMetadataCallbacks()
        {
            mBatchSaveGroupExtMetadataSuccessCallback = null;
            mBatchSaveGroupExtMetadataFailedCallback = null;
            GameSDKCallbackListener.OnBatchSaveGroupExtMetadataSuccessEvent -= OnBatchSaveGroupExtMetadataSuccess;
            GameSDKCallbackListener.OnBatchSaveGroupExtMetadataFailEvent -= OnBatchSaveGroupExtMetadataFailed;
        }

        // Group APIs
        public static void GetGroupConfig(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback)
        {
            mGetGroupConfigSuccessCallback = successCallback;
            mGetGroupConfigFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGetGroupConfigSuccessEvent += OnGetGroupConfigSuccess;
            GameSDKCallbackListener.OnGetGroupConfigFailEvent += OnGetGroupConfigFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getGroupConfig");
                }
            }
            else
            {
                OnGetGroupConfigFailed(-1);
            }
        }

        private static void OnGetGroupConfigSuccess(string data)
        {
            mGetGroupConfigSuccessCallback?.Invoke(data);
            ResetGetGroupConfigCallbacks();
        }

        private static void OnGetGroupConfigFailed(int errorCode)
        {
            mGetGroupConfigFailedCallback?.Invoke(errorCode);
            ResetGetGroupConfigCallbacks();
        }

        private static void ResetGetGroupConfigCallbacks()
        {
            mGetGroupConfigSuccessCallback = null;
            mGetGroupConfigFailedCallback = null;
            GameSDKCallbackListener.OnGetGroupConfigSuccessEvent -= OnGetGroupConfigSuccess;
            GameSDKCallbackListener.OnGetGroupConfigFailEvent -= OnGetGroupConfigFailed;
        }


        public static void GetGroupByUserId(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string userId)
        {
            mGetGroupByUserIdSuccessCallback = successCallback;
            mGetGroupByUserIdFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGetGroupByUserIdSuccessEvent += OnGetGroupByUserIdSuccess;
            GameSDKCallbackListener.OnGetGroupByUserIdFailEvent += OnGetGroupByUserIdFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getGroupByUserId", userId);
                }
            }
            else
            {
                OnGetGroupByUserIdFailed(-1);
            }
        }

        private static void OnGetGroupByUserIdSuccess(string data)
        {
            mGetGroupByUserIdSuccessCallback?.Invoke(data);
            ResetGetGroupByUserIdCallbacks();
        }

        private static void OnGetGroupByUserIdFailed(int errorCode)
        {
            mGetGroupByUserIdFailedCallback?.Invoke(errorCode);
            ResetGetGroupByUserIdCallbacks();
        }

        private static void ResetGetGroupByUserIdCallbacks()
        {
            mGetGroupByUserIdSuccessCallback = null;
            mGetGroupByUserIdFailedCallback = null;
            GameSDKCallbackListener.OnGetGroupByUserIdSuccessEvent -= OnGetGroupByUserIdSuccess;
            GameSDKCallbackListener.OnGetGroupByUserIdFailEvent -= OnGetGroupByUserIdFailed;
        }


        public static void CreateGroup(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string creatorUserId, string logo, string name, string description, int groupStatus, string requiredConditionsJson)
        {
            mCreateGroupSuccessCallback = successCallback;
            mCreateGroupFailedCallback = failedCallback;
            GameSDKCallbackListener.OnCreateGroupSuccessEvent += OnCreateGroupSuccess;
            GameSDKCallbackListener.OnCreateGroupFailEvent += OnCreateGroupFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("createGroup", creatorUserId, logo, name, description, groupStatus, requiredConditionsJson);
                }
            }
            else
            {
                OnCreateGroupFailed(-1);
            }
        }

        private static void OnCreateGroupSuccess(string data)
        {
            mCreateGroupSuccessCallback?.Invoke(data);
            ResetCreateGroupCallbacks();
        }

        private static void OnCreateGroupFailed(int errorCode)
        {
            mCreateGroupFailedCallback?.Invoke(errorCode);
            ResetCreateGroupCallbacks();
        }

        private static void ResetCreateGroupCallbacks()
        {
            mCreateGroupSuccessCallback = null;
            mCreateGroupFailedCallback = null;
            GameSDKCallbackListener.OnCreateGroupSuccessEvent -= OnCreateGroupSuccess;
            GameSDKCallbackListener.OnCreateGroupFailEvent -= OnCreateGroupFailed;
        }


        public static void EditGroup(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string editorUserId, string groupId, string logo, string name, string description, int groupStatus, string requiredConditionsJson)
        {
            mEditGroupSuccessCallback = successCallback;
            mEditGroupFailedCallback = failedCallback;
            GameSDKCallbackListener.OnEditGroupSuccessEvent += OnEditGroupSuccess;
            GameSDKCallbackListener.OnEditGroupFailEvent += OnEditGroupFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("editGroup", editorUserId, groupId, logo, name, description, groupStatus, requiredConditionsJson);
                }
            }
            else
            {
                OnEditGroupFailed(-1);
            }
        }

        private static void OnEditGroupSuccess(string data)
        {
            mEditGroupSuccessCallback?.Invoke(data);
            ResetEditGroupCallbacks();
        }

        private static void OnEditGroupFailed(int errorCode)
        {
            mEditGroupFailedCallback?.Invoke(errorCode);
            ResetEditGroupCallbacks();
        }

        private static void ResetEditGroupCallbacks()
        {
            mEditGroupSuccessCallback = null;
            mEditGroupFailedCallback = null;
            GameSDKCallbackListener.OnEditGroupSuccessEvent -= OnEditGroupSuccess;
            GameSDKCallbackListener.OnEditGroupFailEvent -= OnEditGroupFailed;
        }


        public static void RecommendGroups(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string userId)
        {
            mRecommendGroupsSuccessCallback = successCallback;
            mRecommendGroupsFailedCallback = failedCallback;
            GameSDKCallbackListener.OnRecommendGroupsSuccessEvent += OnRecommendGroupsSuccess;
            GameSDKCallbackListener.OnRecommendGroupsFailEvent += OnRecommendGroupsFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("recommendGroups", userId);
                }
            }
            else
            {
                OnRecommendGroupsFailed(-1);
            }
        }

        private static void OnRecommendGroupsSuccess(string data)
        {
            mRecommendGroupsSuccessCallback?.Invoke(data);
            ResetRecommendGroupsCallbacks();
        }

        private static void OnRecommendGroupsFailed(int errorCode)
        {
            mRecommendGroupsFailedCallback?.Invoke(errorCode);
            ResetRecommendGroupsCallbacks();
        }

        private static void ResetRecommendGroupsCallbacks()
        {
            mRecommendGroupsSuccessCallback = null;
            mRecommendGroupsFailedCallback = null;
            GameSDKCallbackListener.OnRecommendGroupsSuccessEvent -= OnRecommendGroupsSuccess;
            GameSDKCallbackListener.OnRecommendGroupsFailEvent -= OnRecommendGroupsFailed;
        }


        public static void SearchGroups(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string groupId, string groupName)
        {
            mSearchGroupsSuccessCallback = successCallback;
            mSearchGroupsFailedCallback = failedCallback;
            GameSDKCallbackListener.OnSearchGroupsSuccessEvent += OnSearchGroupsSuccess;
            GameSDKCallbackListener.OnSearchGroupsFailEvent += OnSearchGroupsFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("searchGroups", groupId, groupName);
                }
            }
            else
            {
                OnSearchGroupsFailed(-1);
            }
        }

        private static void OnSearchGroupsSuccess(string data)
        {
            mSearchGroupsSuccessCallback?.Invoke(data);
            ResetSearchGroupsCallbacks();
        }

        private static void OnSearchGroupsFailed(int errorCode)
        {
            mSearchGroupsFailedCallback?.Invoke(errorCode);
            ResetSearchGroupsCallbacks();
        }

        private static void ResetSearchGroupsCallbacks()
        {
            mSearchGroupsSuccessCallback = null;
            mSearchGroupsFailedCallback = null;
            GameSDKCallbackListener.OnSearchGroupsSuccessEvent -= OnSearchGroupsSuccess;
            GameSDKCallbackListener.OnSearchGroupsFailEvent -= OnSearchGroupsFailed;
        }


        public static void JoinGroup(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string userId, string groupId)
        {
            mJoinGroupSuccessCallback = successCallback;
            mJoinGroupFailedCallback = failedCallback;
            GameSDKCallbackListener.OnJoinGroupSuccessEvent += OnJoinGroupSuccess;
            GameSDKCallbackListener.OnJoinGroupFailEvent += OnJoinGroupFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("joinGroup", userId, groupId);
                }
            }
            else
            {
                OnJoinGroupFailed(-1);
            }
        }

        private static void OnJoinGroupSuccess(string data)
        {
            mJoinGroupSuccessCallback?.Invoke(data);
            ResetJoinGroupCallbacks();
        }

        private static void OnJoinGroupFailed(int errorCode)
        {
            mJoinGroupFailedCallback?.Invoke(errorCode);
            ResetJoinGroupCallbacks();
        }

        private static void ResetJoinGroupCallbacks()
        {
            mJoinGroupSuccessCallback = null;
            mJoinGroupFailedCallback = null;
            GameSDKCallbackListener.OnJoinGroupSuccessEvent -= OnJoinGroupSuccess;
            GameSDKCallbackListener.OnJoinGroupFailEvent -= OnJoinGroupFailed;
        }


        public static void LeaveGroup(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string userId, string groupId)
        {
            mLeaveGroupSuccessCallback = successCallback;
            mLeaveGroupFailedCallback = failedCallback;
            GameSDKCallbackListener.OnLeaveGroupSuccessEvent += OnLeaveGroupSuccess;
            GameSDKCallbackListener.OnLeaveGroupFailEvent += OnLeaveGroupFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("leaveGroup", userId, groupId);
                }
            }
            else
            {
                OnLeaveGroupFailed(-1);
            }
        }

        private static void OnLeaveGroupSuccess(string data)
        {
            mLeaveGroupSuccessCallback?.Invoke(data);
            ResetLeaveGroupCallbacks();
        }

        private static void OnLeaveGroupFailed(int errorCode)
        {
            mLeaveGroupFailedCallback?.Invoke(errorCode);
            ResetLeaveGroupCallbacks();
        }

        private static void ResetLeaveGroupCallbacks()
        {
            mLeaveGroupSuccessCallback = null;
            mLeaveGroupFailedCallback = null;
            GameSDKCallbackListener.OnLeaveGroupSuccessEvent -= OnLeaveGroupSuccess;
            GameSDKCallbackListener.OnLeaveGroupFailEvent -= OnLeaveGroupFailed;
        }


        public static void KickPlayer(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string operatorId, string playerId, string groupId)
        {
            mKickPlayerSuccessCallback = successCallback;
            mKickPlayerFailedCallback = failedCallback;
            GameSDKCallbackListener.OnKickPlayerSuccessEvent += OnKickPlayerSuccess;
            GameSDKCallbackListener.OnKickPlayerFailEvent += OnKickPlayerFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("kickPlayer", operatorId, playerId, groupId);
                }
            }
            else
            {
                OnKickPlayerFailed(-1);
            }
        }

        private static void OnKickPlayerSuccess(string data)
        {
            mKickPlayerSuccessCallback?.Invoke(data);
            ResetKickPlayerCallbacks();
        }

        private static void OnKickPlayerFailed(int errorCode)
        {
            mKickPlayerFailedCallback?.Invoke(errorCode);
            ResetKickPlayerCallbacks();
        }

        private static void ResetKickPlayerCallbacks()
        {
            mKickPlayerSuccessCallback = null;
            mKickPlayerFailedCallback = null;
            GameSDKCallbackListener.OnKickPlayerSuccessEvent -= OnKickPlayerSuccess;
            GameSDKCallbackListener.OnKickPlayerFailEvent -= OnKickPlayerFailed;
        }


        public static void GetGroupMembers(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string groupId)
        {
            mGetGroupMembersSuccessCallback = successCallback;
            mGetGroupMembersFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGetGroupMembersSuccessEvent += OnGetGroupMembersSuccess;
            GameSDKCallbackListener.OnGetGroupMembersFailEvent += OnGetGroupMembersFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getGroupMembers", groupId);
                }
            }
            else
            {
                OnGetGroupMembersFailed(-1);
            }
        }

        private static void OnGetGroupMembersSuccess(string data)
        {
            mGetGroupMembersSuccessCallback?.Invoke(data);
            ResetGetGroupMembersCallbacks();
        }

        private static void OnGetGroupMembersFailed(int errorCode)
        {
            mGetGroupMembersFailedCallback?.Invoke(errorCode);
            ResetGetGroupMembersCallbacks();
        }

        private static void ResetGetGroupMembersCallbacks()
        {
            mGetGroupMembersSuccessCallback = null;
            mGetGroupMembersFailedCallback = null;
            GameSDKCallbackListener.OnGetGroupMembersSuccessEvent -= OnGetGroupMembersSuccess;
            GameSDKCallbackListener.OnGetGroupMembersFailEvent -= OnGetGroupMembersFailed;
        }

        public static void GetGroupMembersWithMetadata(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string groupId, string[] metadataKeys, string[] extMetadataKeys)
        {
            mGetGroupMembersWithMetadataSuccessCallback = successCallback;
            mGetGroupMembersWithMetadataFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGetGroupMembersWithMetadataSuccessEvent += OnGetGroupMembersWithMetadataSuccess;
            GameSDKCallbackListener.OnGetGroupMembersWithMetadataFailEvent += OnGetGroupMembersWithMetadataFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getGroupMembersWithMetadata", groupId, metadataKeys, extMetadataKeys);
                }
            }
            else
            {
                OnGetGroupMembersWithMetadataFailed(-1);
            }
        }

        private static void OnGetGroupMembersWithMetadataSuccess(string data)
        {
            mGetGroupMembersWithMetadataSuccessCallback?.Invoke(data);
            ResetGetGroupMembersWithMetadataCallbacks();
        }

        private static void OnGetGroupMembersWithMetadataFailed(int errorCode)
        {
            mGetGroupMembersWithMetadataFailedCallback?.Invoke(errorCode);
            ResetGetGroupMembersWithMetadataCallbacks();
        }

        private static void ResetGetGroupMembersWithMetadataCallbacks()
        {
            mGetGroupMembersWithMetadataSuccessCallback = null;
            mGetGroupMembersWithMetadataFailedCallback = null;
            GameSDKCallbackListener.OnGetGroupMembersWithMetadataSuccessEvent -= OnGetGroupMembersWithMetadataSuccess;
            GameSDKCallbackListener.OnGetGroupMembersWithMetadataFailEvent -= OnGetGroupMembersWithMetadataFailed;
        }


        public static void GetApplyList(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string groupId, string userId, int limit, int status)
        {
            mGetApplyListSuccessCallback = successCallback;
            mGetApplyListFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGetApplyListSuccessEvent += OnGetApplyListSuccess;
            GameSDKCallbackListener.OnGetApplyListFailEvent += OnGetApplyListFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getApplyList", groupId, userId, limit, status);
                }
            }
            else
            {
                OnGetApplyListFailed(-1);
            }
        }

        private static void OnGetApplyListSuccess(string data)
        {
            mGetApplyListSuccessCallback?.Invoke(data);
            ResetGetApplyListCallbacks();
        }

        private static void OnGetApplyListFailed(int errorCode)
        {
            mGetApplyListFailedCallback?.Invoke(errorCode);
            ResetGetApplyListCallbacks();
        }

        private static void ResetGetApplyListCallbacks()
        {
            mGetApplyListSuccessCallback = null;
            mGetApplyListFailedCallback = null;
            GameSDKCallbackListener.OnGetApplyListSuccessEvent -= OnGetApplyListSuccess;
            GameSDKCallbackListener.OnGetApplyListFailEvent -= OnGetApplyListFailed;
        }


        public static void ApproveApplication(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string operatorId, string groupId, string applicantId, long applicationId)
        {
            mApproveApplicationSuccessCallback = successCallback;
            mApproveApplicationFailedCallback = failedCallback;
            GameSDKCallbackListener.OnApproveApplicationSuccessEvent += OnApproveApplicationSuccess;
            GameSDKCallbackListener.OnApproveApplicationFailEvent += OnApproveApplicationFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("approveApplication", operatorId, groupId, applicantId, applicationId);
                }
            }
            else
            {
                OnApproveApplicationFailed(-1);
            }
        }

        private static void OnApproveApplicationSuccess(string data)
        {
            mApproveApplicationSuccessCallback?.Invoke(data);
            ResetApproveApplicationCallbacks();
        }

        private static void OnApproveApplicationFailed(int errorCode)
        {
            mApproveApplicationFailedCallback?.Invoke(errorCode);
            ResetApproveApplicationCallbacks();
        }

        private static void ResetApproveApplicationCallbacks()
        {
            mApproveApplicationSuccessCallback = null;
            mApproveApplicationFailedCallback = null;
            GameSDKCallbackListener.OnApproveApplicationSuccessEvent -= OnApproveApplicationSuccess;
            GameSDKCallbackListener.OnApproveApplicationFailEvent -= OnApproveApplicationFailed;
        }


        public static void RejectApplication(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string operatorId, string groupId, string applicantId, long applicationId)
        {
            mRejectApplicationSuccessCallback = successCallback;
            mRejectApplicationFailedCallback = failedCallback;
            GameSDKCallbackListener.OnRejectApplicationSuccessEvent += OnRejectApplicationSuccess;
            GameSDKCallbackListener.OnRejectApplicationFailEvent += OnRejectApplicationFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("rejectApplication", operatorId, groupId, applicantId, applicationId);
                }
            }
            else
            {
                OnRejectApplicationFailed(-1);
            }
        }

        private static void OnRejectApplicationSuccess(string data)
        {
            mRejectApplicationSuccessCallback?.Invoke(data);
            ResetRejectApplicationCallbacks();
        }

        private static void OnRejectApplicationFailed(int errorCode)
        {
            mRejectApplicationFailedCallback?.Invoke(errorCode);
            ResetRejectApplicationCallbacks();
        }

        private static void ResetRejectApplicationCallbacks()
        {
            mRejectApplicationSuccessCallback = null;
            mRejectApplicationFailedCallback = null;
            GameSDKCallbackListener.OnRejectApplicationSuccessEvent -= OnRejectApplicationSuccess;
            GameSDKCallbackListener.OnRejectApplicationFailEvent -= OnRejectApplicationFailed;
        }


        public static void AssignRole(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string groupId, string handlerUserId, string targetUserId, string roleCode)
        {
            mAssignRoleSuccessCallback = successCallback;
            mAssignRoleFailedCallback = failedCallback;
            GameSDKCallbackListener.OnAssignRoleSuccessEvent += OnAssignRoleSuccess;
            GameSDKCallbackListener.OnAssignRoleFailEvent += OnAssignRoleFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("assignRole", groupId, handlerUserId, targetUserId, roleCode);
                }
            }
            else
            {
                OnAssignRoleFailed(-1);
            }
        }

        private static void OnAssignRoleSuccess(string data)
        {
            mAssignRoleSuccessCallback?.Invoke(data);
            ResetAssignRoleCallbacks();
        }

        private static void OnAssignRoleFailed(int errorCode)
        {
            mAssignRoleFailedCallback?.Invoke(errorCode);
            ResetAssignRoleCallbacks();
        }

        private static void ResetAssignRoleCallbacks()
        {
            mAssignRoleSuccessCallback = null;
            mAssignRoleFailedCallback = null;
            GameSDKCallbackListener.OnAssignRoleSuccessEvent -= OnAssignRoleSuccess;
            GameSDKCallbackListener.OnAssignRoleFailEvent -= OnAssignRoleFailed;
        }


        public static void DeleteRole(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string groupId, string handlerUserId, string targetUserId)
        {
            mDeleteRoleSuccessCallback = successCallback;
            mDeleteRoleFailedCallback = failedCallback;
            GameSDKCallbackListener.OnDeleteRoleSuccessEvent += OnDeleteRoleSuccess;
            GameSDKCallbackListener.OnDeleteRoleFailEvent += OnDeleteRoleFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("deleteRole", groupId, handlerUserId, targetUserId);
                }
            }
            else
            {
                OnDeleteRoleFailed(-1);
            }
        }

        private static void OnDeleteRoleSuccess(string data)
        {
            mDeleteRoleSuccessCallback?.Invoke(data);
            ResetDeleteRoleCallbacks();
        }

        private static void OnDeleteRoleFailed(int errorCode)
        {
            mDeleteRoleFailedCallback?.Invoke(errorCode);
            ResetDeleteRoleCallbacks();
        }

        private static void ResetDeleteRoleCallbacks()
        {
            mDeleteRoleSuccessCallback = null;
            mDeleteRoleFailedCallback = null;
            GameSDKCallbackListener.OnDeleteRoleSuccessEvent -= OnDeleteRoleSuccess;
            GameSDKCallbackListener.OnDeleteRoleFailEvent -= OnDeleteRoleFailed;
        }


        public static void TransferRole(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string groupId, string handlerUserId, string targetUserId, string roleCode)
        {
            mTransferRoleSuccessCallback = successCallback;
            mTransferRoleFailedCallback = failedCallback;
            GameSDKCallbackListener.OnTransferRoleSuccessEvent += OnTransferRoleSuccess;
            GameSDKCallbackListener.OnTransferRoleFailEvent += OnTransferRoleFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("transferRole", groupId, handlerUserId, targetUserId, roleCode);
                }
            }
            else
            {
                OnTransferRoleFailed(-1);
            }
        }

        private static void OnTransferRoleSuccess(string data)
        {
            mTransferRoleSuccessCallback?.Invoke(data);
            ResetTransferRoleCallbacks();
        }

        private static void OnTransferRoleFailed(int errorCode)
        {
            mTransferRoleFailedCallback?.Invoke(errorCode);
            ResetTransferRoleCallbacks();
        }

        private static void ResetTransferRoleCallbacks()
        {
            mTransferRoleSuccessCallback = null;
            mTransferRoleFailedCallback = null;
            GameSDKCallbackListener.OnTransferRoleSuccessEvent -= OnTransferRoleSuccess;
            GameSDKCallbackListener.OnTransferRoleFailEvent -= OnTransferRoleFailed;
        }


        // Group Chat APIs
        public static void ConnectToGroupChat(GameSdkNoneParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, GameSdkStringParamVoidCallback messageCallback, string groupId, string userId, bool fetchMessages = true)
        {
            mGroupChatConnectSuccessCallback = successCallback;
            mGroupChatConnectFailedCallback = failedCallback;
            mGroupChatMessageCallback = messageCallback;

            GameSDKCallbackListener.OnGroupChatConnectSuccessEvent += OnGroupChatConnectSuccess;
            GameSDKCallbackListener.OnGroupChatConnectFailEvent += OnGroupChatConnectFailed;

            GameSDKCallbackListener.OnGroupChatMessageReceivedEvent -= OnGroupChatMessageReceived;
            if (messageCallback != null)
            {
                GameSDKCallbackListener.OnGroupChatMessageReceivedEvent += OnGroupChatMessageReceived;
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("connectToGroupChat", groupId, userId, fetchMessages);
                }
            }
            else
            {
                OnGroupChatConnectFailed(-1);
            }
        }

        private static void OnGroupChatConnectSuccess()
        {
            mGroupChatConnectSuccessCallback?.Invoke();
            ResetGroupChatConnectCallbacks();
        }

        private static void OnGroupChatConnectFailed(int errorCode)
        {
            mGroupChatConnectFailedCallback?.Invoke(errorCode);
            ResetGroupChatConnectCallbacks();
        }

        private static void ResetGroupChatConnectCallbacks()
        {
            mGroupChatConnectSuccessCallback = null;
            mGroupChatConnectFailedCallback = null;
            GameSDKCallbackListener.OnGroupChatConnectSuccessEvent -= OnGroupChatConnectSuccess;
            GameSDKCallbackListener.OnGroupChatConnectFailEvent -= OnGroupChatConnectFailed;
        }

        private static void OnGroupChatMessageReceived(string message)
        {
            mGroupChatMessageCallback?.Invoke(message);
        }

        private static void ResetGroupChatMessageCallback()
        {
            GameSDKCallbackListener.OnGroupChatMessageReceivedEvent -= OnGroupChatMessageReceived;
            mGroupChatMessageCallback = null;
        }

        public static void SendGroupChatMessage(GameSdkNoneParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string jsonMessage)
        {
            mGroupChatMessageSentSuccessCallback = successCallback;
            mGroupChatMessageSentFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGroupChatMessageSentSuccessEvent += OnGroupChatMessageSentSuccess;
            GameSDKCallbackListener.OnGroupChatMessageSentFailEvent += OnGroupChatMessageSentFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("sendGroupChatMessage", jsonMessage);
                }
            }
            else
            {
                OnGroupChatMessageSentFailed(-1);
            }
        }

        private static void OnGroupChatMessageSentSuccess()
        {
            mGroupChatMessageSentSuccessCallback?.Invoke();
            ResetGroupChatMessageSentCallbacks();
        }

        private static void OnGroupChatMessageSentFailed(int errorCode)
        {
            mGroupChatMessageSentFailedCallback?.Invoke(errorCode);
            ResetGroupChatMessageSentCallbacks();
        }

        private static void ResetGroupChatMessageSentCallbacks()
        {
            mGroupChatMessageSentSuccessCallback = null;
            mGroupChatMessageSentFailedCallback = null;
            GameSDKCallbackListener.OnGroupChatMessageSentSuccessEvent -= OnGroupChatMessageSentSuccess;
            GameSDKCallbackListener.OnGroupChatMessageSentFailEvent -= OnGroupChatMessageSentFailed;
        }

        public static void DisconnectFromGroupChat()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("disconnectFromGroupChat");
                }
            }

            ResetGroupChatMessageCallback();
            ResetGetGroupChatMessagesByTypeCallbacks();
        }

        public static void ClearGroupChatCredentials()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("clearGroupChatCredentials");
                }
            }

            ResetGroupChatMessageCallback();
            ResetGetGroupChatMessagesByTypeCallbacks();
        }

        public static void GetGroupChatMessagesByType(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, int messageType, int limit)
        {
            mGetGroupChatMessagesByTypeSuccessCallbacks[messageType] = successCallback;
            mGetGroupChatMessagesByTypeFailedCallbacks[messageType] = failedCallback;
            EnsureGetGroupChatMessagesByTypeCallbacksRegistered();

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getGroupChatMessagesByType", messageType, limit);
                }
            }
            else
            {
                OnGetGroupChatMessagesByTypeFailed(messageType);
            }
        }

        private static void OnGetGroupChatMessagesByTypeSuccess(string data, int messageType)
        {
            if (mGetGroupChatMessagesByTypeSuccessCallbacks.TryGetValue(messageType, out GameSdkStringParamVoidCallback callback))
            {
                callback?.Invoke(data);
            }
            ResetGetGroupChatMessagesByTypeCallback(messageType);
        }

        private static void OnGetGroupChatMessagesByTypeFailed(int messageType)
        {
            if (mGetGroupChatMessagesByTypeFailedCallbacks.TryGetValue(messageType, out GameSdkIntParamVoidCallback callback))
            {
                callback?.Invoke(-1);
            }
            ResetGetGroupChatMessagesByTypeCallback(messageType);
        }

        private static void EnsureGetGroupChatMessagesByTypeCallbacksRegistered()
        {
            if (mGetGroupChatMessagesByTypeCallbacksRegistered)
            {
                return;
            }

            mGetGroupChatMessagesByTypeCallbacksRegistered = true;
            GameSDKCallbackListener.OnGetGroupChatMessagesByTypeSuccessEvent += OnGetGroupChatMessagesByTypeSuccess;
            GameSDKCallbackListener.OnGetGroupChatMessagesByTypeFailEvent += OnGetGroupChatMessagesByTypeFailed;
        }

        private static void ResetGetGroupChatMessagesByTypeCallback(int messageType)
        {
            mGetGroupChatMessagesByTypeSuccessCallbacks.Remove(messageType);
            mGetGroupChatMessagesByTypeFailedCallbacks.Remove(messageType);

            if (mGetGroupChatMessagesByTypeSuccessCallbacks.Count == 0 && mGetGroupChatMessagesByTypeFailedCallbacks.Count == 0)
            {
                ResetGetGroupChatMessagesByTypeCallbacks();
            }
        }

        private static void ResetGetGroupChatMessagesByTypeCallbacks()
        {
            mGetGroupChatMessagesByTypeSuccessCallbacks.Clear();
            mGetGroupChatMessagesByTypeFailedCallbacks.Clear();
            if (!mGetGroupChatMessagesByTypeCallbacksRegistered)
            {
                return;
            }

            mGetGroupChatMessagesByTypeCallbacksRegistered = false;
            GameSDKCallbackListener.OnGetGroupChatMessagesByTypeSuccessEvent -= OnGetGroupChatMessagesByTypeSuccess;
            GameSDKCallbackListener.OnGetGroupChatMessagesByTypeFailEvent -= OnGetGroupChatMessagesByTypeFailed;
        }

        // UGP APIs
        public static void GetUGPPropertyDetailsById(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string propertyId)
        {
            mGetUGPPropertyByIdSuccessCallback = successCallback;
            mGetUGPPropertyByIdFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGetUGPPropertyByIdSuccessEvent += OnGetUGPPropertyByIdSuccess;
            GameSDKCallbackListener.OnGetUGPPropertyByIdFailEvent += OnGetUGPPropertyByIdFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getUGPPropertyDetailsById", propertyId);
                }
            }
            else
            {
                OnGetUGPPropertyByIdFailed(-1);
            }
        }

        private static void OnGetUGPPropertyByIdSuccess(string data)
        {
            mGetUGPPropertyByIdSuccessCallback?.Invoke(data);
            ResetGetUGPPropertyByIdCallbacks();
        }

        private static void OnGetUGPPropertyByIdFailed(int errorCode)
        {
            mGetUGPPropertyByIdFailedCallback?.Invoke(errorCode);
            ResetGetUGPPropertyByIdCallbacks();
        }

        private static void ResetGetUGPPropertyByIdCallbacks()
        {
            mGetUGPPropertyByIdSuccessCallback = null;
            mGetUGPPropertyByIdFailedCallback = null;
            GameSDKCallbackListener.OnGetUGPPropertyByIdSuccessEvent -= OnGetUGPPropertyByIdSuccess;
            GameSDKCallbackListener.OnGetUGPPropertyByIdFailEvent -= OnGetUGPPropertyByIdFailed;
        }


        public static void CreateUGPProperty(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string createParamsJson)
        {
            mCreateUGPPropertySuccessCallback = successCallback;
            mCreateUGPPropertyFailedCallback = failedCallback;
            GameSDKCallbackListener.OnCreateUGPPropertySuccessEvent += OnCreateUGPPropertySuccess;
            GameSDKCallbackListener.OnCreateUGPPropertyFailEvent += OnCreateUGPPropertyFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("createUGPProperty", createParamsJson);
                }
            }
            else
            {
                OnCreateUGPPropertyFailed(-1);
            }
        }

        private static void OnCreateUGPPropertySuccess(string data)
        {
            mCreateUGPPropertySuccessCallback?.Invoke(data);
            ResetCreateUGPPropertyCallbacks();
        }

        private static void OnCreateUGPPropertyFailed(int errorCode)
        {
            mCreateUGPPropertyFailedCallback?.Invoke(errorCode);
            ResetCreateUGPPropertyCallbacks();
        }

        private static void ResetCreateUGPPropertyCallbacks()
        {
            mCreateUGPPropertySuccessCallback = null;
            mCreateUGPPropertyFailedCallback = null;
            GameSDKCallbackListener.OnCreateUGPPropertySuccessEvent -= OnCreateUGPPropertySuccess;
            GameSDKCallbackListener.OnCreateUGPPropertyFailEvent -= OnCreateUGPPropertyFailed;
        }


        public static void ClaimUGPProperty(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string propertyId, string userId)
        {
            mClaimUGPPropertySuccessCallback = successCallback;
            mClaimUGPPropertyFailedCallback = failedCallback;
            GameSDKCallbackListener.OnClaimUGPPropertySuccessEvent += OnClaimUGPPropertySuccess;
            GameSDKCallbackListener.OnClaimUGPPropertyFailEvent += OnClaimUGPPropertyFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("claimUGPProperty", propertyId, userId);
                }
            }
            else
            {
                OnClaimUGPPropertyFailed(-1);
            }
        }

        private static void OnClaimUGPPropertySuccess(string data)
        {
            mClaimUGPPropertySuccessCallback?.Invoke(data);
            ResetClaimUGPPropertyCallbacks();
        }

        private static void OnClaimUGPPropertyFailed(int errorCode)
        {
            mClaimUGPPropertyFailedCallback?.Invoke(errorCode);
            ResetClaimUGPPropertyCallbacks();
        }

        private static void ResetClaimUGPPropertyCallbacks()
        {
            mClaimUGPPropertySuccessCallback = null;
            mClaimUGPPropertyFailedCallback = null;
            GameSDKCallbackListener.OnClaimUGPPropertySuccessEvent -= OnClaimUGPPropertySuccess;
            GameSDKCallbackListener.OnClaimUGPPropertyFailEvent -= OnClaimUGPPropertyFailed;
        }


        public static void GetUGPPropertyClaims(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string propertyId, int page, int size)
        {
            mGetUGPPropertyClaimsSuccessCallback = successCallback;
            mGetUGPPropertyClaimsFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGetUGPPropertyClaimsSuccessEvent += OnGetUGPPropertyClaimsSuccess;
            GameSDKCallbackListener.OnGetUGPPropertyClaimsFailEvent += OnGetUGPPropertyClaimsFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getUGPPropertyClaims", propertyId, page, size);
                }
            }
            else
            {
                OnGetUGPPropertyClaimsFailed(-1);
            }
        }

        private static void OnGetUGPPropertyClaimsSuccess(string data)
        {
            mGetUGPPropertyClaimsSuccessCallback?.Invoke(data);
            ResetGetUGPPropertyClaimsCallbacks();
        }

        private static void OnGetUGPPropertyClaimsFailed(int errorCode)
        {
            mGetUGPPropertyClaimsFailedCallback?.Invoke(errorCode);
            ResetGetUGPPropertyClaimsCallbacks();
        }

        private static void ResetGetUGPPropertyClaimsCallbacks()
        {
            mGetUGPPropertyClaimsSuccessCallback = null;
            mGetUGPPropertyClaimsFailedCallback = null;
            GameSDKCallbackListener.OnGetUGPPropertyClaimsSuccessEvent -= OnGetUGPPropertyClaimsSuccess;
            GameSDKCallbackListener.OnGetUGPPropertyClaimsFailEvent -= OnGetUGPPropertyClaimsFailed;
        }


        public static void CheckIfUGPPropertyClaimed(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string propertyId, string userId)
        {
            mCheckUGPPropertyClaimedSuccessCallback = successCallback;
            mCheckUGPPropertyClaimedFailedCallback = failedCallback;
            GameSDKCallbackListener.OnCheckUGPPropertyClaimedSuccessEvent += OnCheckUGPPropertyClaimedSuccess;
            GameSDKCallbackListener.OnCheckUGPPropertyClaimedFailEvent += OnCheckUGPPropertyClaimedFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("checkIfUGPPropertyClaimed", propertyId, userId);
                }
            }
            else
            {
                OnCheckUGPPropertyClaimedFailed(-1);
            }
        }

        private static void OnCheckUGPPropertyClaimedSuccess(string data)
        {
            mCheckUGPPropertyClaimedSuccessCallback?.Invoke(data);
            ResetCheckUGPPropertyClaimedCallbacks();
        }

        private static void OnCheckUGPPropertyClaimedFailed(int errorCode)
        {
            mCheckUGPPropertyClaimedFailedCallback?.Invoke(errorCode);
            ResetCheckUGPPropertyClaimedCallbacks();
        }

        private static void ResetCheckUGPPropertyClaimedCallbacks()
        {
            mCheckUGPPropertyClaimedSuccessCallback = null;
            mCheckUGPPropertyClaimedFailedCallback = null;
            GameSDKCallbackListener.OnCheckUGPPropertyClaimedSuccessEvent -= OnCheckUGPPropertyClaimedSuccess;
            GameSDKCallbackListener.OnCheckUGPPropertyClaimedFailEvent -= OnCheckUGPPropertyClaimedFailed;
        }


        public static void GetUGPPropertiesByGroup(GameSdkStringParamVoidCallback successCallback, GameSdkIntParamVoidCallback failedCallback, string groupId, int status, int isDesc, int limit)
        {
            mGetUGPPropertiesByGroupSuccessCallback = successCallback;
            mGetUGPPropertiesByGroupFailedCallback = failedCallback;
            GameSDKCallbackListener.OnGetUGPPropertiesByGroupSuccessEvent += OnGetUGPPropertiesByGroupSuccess;
            GameSDKCallbackListener.OnGetUGPPropertiesByGroupFailEvent += OnGetUGPPropertiesByGroupFailed;

            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaRemoteData = new AndroidJavaClass(androidClassPath))
                {
                    javaRemoteData.CallStatic("getUGPPropertiesByGroup", groupId, status, isDesc, limit);
                }
            }
            else
            {
                OnGetUGPPropertiesByGroupFailed(-1);
            }
        }

        private static void OnGetUGPPropertiesByGroupSuccess(string data)
        {
            mGetUGPPropertiesByGroupSuccessCallback?.Invoke(data);
            ResetGetUGPPropertiesByGroupCallbacks();
        }

        private static void OnGetUGPPropertiesByGroupFailed(int errorCode)
        {
            mGetUGPPropertiesByGroupFailedCallback?.Invoke(errorCode);
            ResetGetUGPPropertiesByGroupCallbacks();
        }

        private static void ResetGetUGPPropertiesByGroupCallbacks()
        {
            mGetUGPPropertiesByGroupSuccessCallback = null;
            mGetUGPPropertiesByGroupFailedCallback = null;
            GameSDKCallbackListener.OnGetUGPPropertiesByGroupSuccessEvent -= OnGetUGPPropertiesByGroupSuccess;
            GameSDKCallbackListener.OnGetUGPPropertiesByGroupFailEvent -= OnGetUGPPropertiesByGroupFailed;
        }


    }
}
