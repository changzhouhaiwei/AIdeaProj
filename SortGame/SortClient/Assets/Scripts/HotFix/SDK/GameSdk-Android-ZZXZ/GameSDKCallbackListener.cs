using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.Text;
using UnityEngine;


namespace EwingSoft.GameSDK
{
    public delegate void GameSdkNoneParamVoidCallback();
    public delegate void GameSdkIntParamVoidCallback(int param1);
    public delegate void GameSdkBoolParamVoidCallback(bool param1);
    public delegate void GameSdkAdPaidCallback(string adPlatform, string adId, string adType, string adTypeDisplayed, string adNetwork, double adMoney, string currency);
    public delegate void GameSdkStringParamVoidCallback(string param1);
    public delegate void GameSdkLongParamVoidCallback(long param1);
    public delegate void GameSdkStringStringParamVoidCallback(string param1, string param2);
    public delegate void GameSdkStringStringStringParamVoidCallback(string param1, string param2, string param3);
    public delegate void GameSdkStringIntParamVoidCallback(string param1, int param2);
    public delegate void GameSdkStringIntIntParamVoidCallback(string param1, int param2, int param3);
    public delegate void GameSdkBoolIntParamVoidCallback(bool param1, int param2);
    public delegate void GameSdkBoolIntStringParamVoidCallback(bool param1, int param2, string param3);
    public delegate void GameSdkUserProfileScoreParamVoidCallback(double adScore, double payScore, string window);
    public delegate void GameSdkPADAssetStatusUpdatedCallback(string assetName, int assetStatus, int errorCode, long totalBytes, long bytesDownloaded, int transferProgressPercentage);

    public class GameSDKCallbackListener : MonoBehaviour
    {
        private static bool debugMode;
        //Ad SDK Callback
        public static event GameSdkNoneParamVoidCallback OnAdInitializedEvent;
        public static event GameSdkNoneParamVoidCallback OnRewardAdLoadSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnRewardAdLoadFailEvent;
        public static event GameSdkNoneParamVoidCallback OnRewardAdViewedEvent;
        public static event GameSdkNoneParamVoidCallback OnRewardAdCanceledEvent;
        public static event GameSdkStringParamVoidCallback OnRewardAdClickedEvent;
        public static event GameSdkStringParamVoidCallback OnRewardAdImpressionEvent;
        public static event GameSdkNoneParamVoidCallback OnFullAdLoadSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnFullAdLoadFailEvent;
        public static event GameSdkIntParamVoidCallback OnFullAdClosedEvent;
        public static event GameSdkStringParamVoidCallback OnFullAdImpressionEvent;
        public static event GameSdkStringParamVoidCallback OnFullAdClickedEvent;
        public static event GameSdkNoneParamVoidCallback OnOpenAdLoadSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnOpenAdLoadFailEvent;
        public static event GameSdkNoneParamVoidCallback OnOpenAdClosedEvent;
        public static event GameSdkNoneParamVoidCallback OnBannerAdLoadSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnBannerAdLoadFailEvent;
        public static event GameSdkStringParamVoidCallback OnBannerAdClickedEvent;
        public static event GameSdkStringParamVoidCallback OnBannerAdImpressionEvent;
        public static event GameSdkNoneParamVoidCallback OnBannerAdShownEvent;
        public static event GameSdkAdPaidCallback OnAdPaidEvent;
        //Firebase SDK Callback
        public static event GameSdkBoolParamVoidCallback OnRemoteConfigLoadedEvent;
        //CMP SDK Callback
        public static event GameSdkBoolIntParamVoidCallback OnCMPFinishedEvent;
        //Billing SDK Callback
        public static event GameSdkNoneParamVoidCallback OnQueryProductDetailsSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnQueryProductDetailsFailEvent;
        public static event GameSdkStringStringStringParamVoidCallback OnPurchaseSuccessEvent;
        public static event GameSdkStringIntParamVoidCallback OnPurchaseFailEvent;
        public static event GameSdkStringStringStringParamVoidCallback OnRestoreSuccessEvent;
        public static event GameSdkStringIntParamVoidCallback OnRestoreFailEvent;
        public static event GameSdkStringParamVoidCallback OnConsumeSuccessEvent;
        public static event GameSdkStringIntParamVoidCallback OnConsumeFailEvent;
        // RemoteData SDK Callback
        public static event GameSdkNoneParamVoidCallback OnGetUserDataSuccessEvent;
        public static event GameSdkIntParamVoidCallback  OnGetUserDataFailEvent;
        public static event GameSdkNoneParamVoidCallback OnPostUserDataEvent;
        public static event GameSdkNoneParamVoidCallback OnDeleteUserDataSuccessEvent;
        public static event GameSdkIntParamVoidCallback  OnDeleteUserDataFailEvent;
        public static event GameSdkUserProfileScoreParamVoidCallback OnQueryUserProfileScoreSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnQueryUserProfileScoreFailEvent;
        public static event GameSdkStringParamVoidCallback OnGetRankDataSuccessEvent;
        public static event GameSdkIntParamVoidCallback  OnGetRankDataFailEvent;
        public static event GameSdkStringParamVoidCallback OnGetEventStatusSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGetEventStatusFailEvent;
        public static event GameSdkLongParamVoidCallback OnGetServerTimeSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGetServerTimeFailEvent;
        public static event GameSdkNoneParamVoidCallback OnLinkUserWithOpenIdSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnLinkUserWithOpenIdFailEvent;
        public static event GameSdkNoneParamVoidCallback OnUnlinkUserWithOpenIdSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnUnlinkUserWithOpenIdFailEvent;
        public static event GameSdkStringParamVoidCallback OnGetPlayerExtMetadataSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGetPlayerExtMetadataFailEvent;
        public static event GameSdkStringParamVoidCallback OnSavePlayerExtMetadataSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnSavePlayerExtMetadataFailEvent;
        public static event GameSdkStringParamVoidCallback OnBatchGetPlayerExtMetadataSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnBatchGetPlayerExtMetadataFailEvent;
        public static event GameSdkStringParamVoidCallback OnBatchSavePlayerExtMetadataSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnBatchSavePlayerExtMetadataFailEvent;
        public static event GameSdkStringParamVoidCallback OnGetGroupExtMetadataSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGetGroupExtMetadataFailEvent;
        public static event GameSdkStringParamVoidCallback OnSaveGroupExtMetadataSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnSaveGroupExtMetadataFailEvent;
        public static event GameSdkStringParamVoidCallback OnBatchGetGroupExtMetadataSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnBatchGetGroupExtMetadataFailEvent;
        public static event GameSdkStringParamVoidCallback OnBatchSaveGroupExtMetadataSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnBatchSaveGroupExtMetadataFailEvent;
        public static event GameSdkStringParamVoidCallback OnGetGroupConfigSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGetGroupConfigFailEvent;
        public static event GameSdkStringParamVoidCallback OnGetGroupByUserIdSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGetGroupByUserIdFailEvent;
        public static event GameSdkStringParamVoidCallback OnCreateGroupSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnCreateGroupFailEvent;
        public static event GameSdkStringParamVoidCallback OnEditGroupSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnEditGroupFailEvent;
        public static event GameSdkStringParamVoidCallback OnRecommendGroupsSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnRecommendGroupsFailEvent;
        public static event GameSdkStringParamVoidCallback OnSearchGroupsSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnSearchGroupsFailEvent;
        public static event GameSdkStringParamVoidCallback OnJoinGroupSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnJoinGroupFailEvent;
        public static event GameSdkStringParamVoidCallback OnLeaveGroupSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnLeaveGroupFailEvent;
        public static event GameSdkStringParamVoidCallback OnKickPlayerSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnKickPlayerFailEvent;
        public static event GameSdkStringParamVoidCallback OnGetGroupMembersSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGetGroupMembersFailEvent;
        public static event GameSdkStringParamVoidCallback OnGetGroupMembersWithMetadataSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGetGroupMembersWithMetadataFailEvent;
        public static event GameSdkStringParamVoidCallback OnGetApplyListSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGetApplyListFailEvent;
        public static event GameSdkStringParamVoidCallback OnApproveApplicationSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnApproveApplicationFailEvent;
        public static event GameSdkStringParamVoidCallback OnRejectApplicationSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnRejectApplicationFailEvent;
        public static event GameSdkStringParamVoidCallback OnAssignRoleSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnAssignRoleFailEvent;
        public static event GameSdkStringParamVoidCallback OnDeleteRoleSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnDeleteRoleFailEvent;
        public static event GameSdkStringParamVoidCallback OnTransferRoleSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnTransferRoleFailEvent;
        public static event GameSdkNoneParamVoidCallback OnGroupChatConnectSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGroupChatConnectFailEvent;
        public static event GameSdkStringParamVoidCallback OnGroupChatMessageReceivedEvent;
        public static event GameSdkNoneParamVoidCallback OnGroupChatMessageSentSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGroupChatMessageSentFailEvent;
        public static event GameSdkStringIntParamVoidCallback OnGetGroupChatMessagesByTypeSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGetGroupChatMessagesByTypeFailEvent;
        public static event GameSdkStringParamVoidCallback OnGetUGPPropertyByIdSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGetUGPPropertyByIdFailEvent;
        public static event GameSdkStringParamVoidCallback OnCreateUGPPropertySuccessEvent;
        public static event GameSdkIntParamVoidCallback OnCreateUGPPropertyFailEvent;
        public static event GameSdkStringParamVoidCallback OnClaimUGPPropertySuccessEvent;
        public static event GameSdkIntParamVoidCallback OnClaimUGPPropertyFailEvent;
        public static event GameSdkStringParamVoidCallback OnGetUGPPropertyClaimsSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGetUGPPropertyClaimsFailEvent;
        public static event GameSdkStringParamVoidCallback OnCheckUGPPropertyClaimedSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnCheckUGPPropertyClaimedFailEvent;
        public static event GameSdkStringParamVoidCallback OnGetUGPPropertiesByGroupSuccessEvent;
        public static event GameSdkIntParamVoidCallback OnGetUGPPropertiesByGroupFailEvent;
        // Login SDK Callback
        public static event GameSdkStringIntIntParamVoidCallback OnLoginedEvent;
        // Shortcut SDK Callback
        // PAD SDK Callback
        public static event GameSdkPADAssetStatusUpdatedCallback OnAssetStatusUpdatedEvent;
        public static event GameSdkStringParamVoidCallback OnGetAssetStatusFailedEvent;
        public static event GameSdkStringStringParamVoidCallback OnGetAssetPathSuccessEvent;
        public static event GameSdkStringParamVoidCallback OnGetAssetPathFailedEvent;


        private static StringBuilder rankDataBuilder = new StringBuilder();
        private static StringBuilder eventStatusBuilder = new StringBuilder();
        private static StringBuilder groupConfigBuilder = new StringBuilder();
        private static StringBuilder groupByUserIdBuilder = new StringBuilder();
        private static StringBuilder createGroupBuilder = new StringBuilder();
        private static StringBuilder editGroupBuilder = new StringBuilder();
        private static StringBuilder recommendGroupsBuilder = new StringBuilder();
        private static StringBuilder searchGroupsBuilder = new StringBuilder();
        private static StringBuilder joinGroupBuilder = new StringBuilder();
        private static StringBuilder leaveGroupBuilder = new StringBuilder();
        private static StringBuilder kickPlayerBuilder = new StringBuilder();
        private static StringBuilder groupMembersBuilder = new StringBuilder();
        private static StringBuilder groupMembersWithMetadataBuilder = new StringBuilder();
        private static StringBuilder getPlayerExtMetadataBuilder = new StringBuilder();
        private static StringBuilder savePlayerExtMetadataBuilder = new StringBuilder();
        private static StringBuilder batchGetPlayerExtMetadataBuilder = new StringBuilder();
        private static StringBuilder batchSavePlayerExtMetadataBuilder = new StringBuilder();
        private static StringBuilder getGroupExtMetadataBuilder = new StringBuilder();
        private static StringBuilder saveGroupExtMetadataBuilder = new StringBuilder();
        private static StringBuilder batchGetGroupExtMetadataBuilder = new StringBuilder();
        private static StringBuilder batchSaveGroupExtMetadataBuilder = new StringBuilder();
        private static StringBuilder applyListBuilder = new StringBuilder();
        private static StringBuilder approveApplicationBuilder = new StringBuilder();
        private static StringBuilder rejectApplicationBuilder = new StringBuilder();
        private static StringBuilder assignRoleBuilder = new StringBuilder();
        private static StringBuilder deleteRoleBuilder = new StringBuilder();
        private static StringBuilder transferRoleBuilder = new StringBuilder();
        private static Dictionary<int, StringBuilder> groupChatMessagesByTypeBuilders = new Dictionary<int, StringBuilder>();
        private static StringBuilder groupChatMessageBuilder = new StringBuilder();
        private static StringBuilder ugpPropertyByIdBuilder = new StringBuilder();
        private static StringBuilder createUGPPropertyBuilder = new StringBuilder();
        private static StringBuilder claimUGPPropertyBuilder = new StringBuilder();
        private static StringBuilder ugpPropertyClaimsBuilder = new StringBuilder();
        private static StringBuilder checkUGPPropertyClaimedBuilder = new StringBuilder();
        private static StringBuilder ugpPropertiesByGroupBuilder = new StringBuilder();

        private static bool TryParseStatusMessage(string status, out bool isSuccess, out int errorCode)
        {
            isSuccess = false;
            errorCode = 0;
            if (string.IsNullOrEmpty(status))
            {
                return false;
            }

            string[] parts = status.Split('|');
            if (parts.Length != 2)
            {
                return false;
            }

            return bool.TryParse(parts[0], out isSuccess) && int.TryParse(parts[1], out errorCode);
        }

        private static void AppendChunk(StringBuilder builder, string chunk, System.Action<string> onCompleted)
        {
            if (chunk == "_SEND_END_")
            {
                string payload = builder.ToString();
                builder.Clear();
                onCompleted?.Invoke(payload);
            }
            else
            {
                builder.Append(chunk);
            }
        }

        private static bool TryParseMessageTypeChunk(string chunkMessage, out int messageType, out string chunk)
        {
            messageType = 0;
            chunk = string.Empty;
            if (string.IsNullOrEmpty(chunkMessage))
            {
                return false;
            }

            int separatorIndex = chunkMessage.IndexOf('|');
            if (separatorIndex <= 0 || separatorIndex == chunkMessage.Length - 1)
            {
                return false;
            }

            chunk = chunkMessage.Substring(separatorIndex + 1);
            return int.TryParse(chunkMessage.Substring(0, separatorIndex), out messageType);
        }

        private static void AppendGroupChatMessagesByTypeChunk(int messageType, string chunk, System.Action<string, int> onCompleted)
        {
            if (!groupChatMessagesByTypeBuilders.TryGetValue(messageType, out StringBuilder builder))
            {
                builder = new StringBuilder();
                groupChatMessagesByTypeBuilders[messageType] = builder;
            }

            if (chunk == "_SEND_END_")
            {
                string payload = builder.ToString();
                groupChatMessagesByTypeBuilders.Remove(messageType);
                onCompleted?.Invoke(payload, messageType);
                return;
            }

            builder.Append(chunk);
        }

        private static void HandleStatusMessage(string tag, string status, System.Action onSuccess, System.Action<int> onFail)
        {
            if (TryParseStatusMessage(status, out bool isSuccess, out int errorCode))
            {
                if (debugMode)
                {
                    Debug.Log($"-------{tag}: isSuccess is {isSuccess}, errorCode is {errorCode}-------");
                }

                if (isSuccess)
                {
                    onSuccess?.Invoke();
                }
                else
                {
                    onFail?.Invoke(errorCode);
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning($"{tag}: Invalid status format.");
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            debugMode = false;
            if (debugMode)
            {
                Debug.Log("Into GameSDKCallbackListener!");
            }
        }

        #region AdSDK Callback
        
        /// <summary>
        /// 处理广告初始化成功的事件。
        /// 当广告初始化成功时，记录日志（如果启用了调试模式），并触发 OnAdInitializedEvent 事件。
        /// </summary>
        private void OnAdInitialized()
        {
            if (debugMode)
            {
                Debug.Log("-------AdMob initialize succeed-------");
            }
            OnAdInitializedEvent?.Invoke();
        }

        /// <summary>
        /// 处理广告支付事件。
        /// 解析支付信息字符串，验证格式并提取广告平台、广告ID、广告类型、实际展示类型、广告网络、收入金额和货币单位。
        /// 如果解析成功，记录日志（如果启用了调试模式），并触发 OnAdPaidEvent 事件。
        /// 如果解析失败，记录警告日志。
        /// </summary>
        /// <param name="paidMessage">包含支付信息的字符串，格式为 "adPlatform|adId|adType|adTypeDisplayed|adNetwork|adMoney|currency"。</param>
        private void OnAdPaid(string paidMessage)
        {
            string[] parts = paidMessage.Split('|');

            // 验证支付信息格式是否正确（期望7个部分），并尝试解析收入金额
            if (parts.Length != 7)
            {
                if (debugMode)
                {
                    Debug.LogWarning("OnAdPaid: Invalid message format, expected 7 parts");
                }
                return;
            }

            // 使用 InvariantCulture 确保小数点始终被正确解析，避免不同地区设置导致的解析问题
            if (!double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out double adMoney))
            {
                if (debugMode)
                {
                    Debug.LogWarning("OnAdPaid: Invalid adMoney value");
                }
                return;
            }

            if (debugMode)
            {
                Debug.Log($"-------onAdPaid: adPlatform is {parts[0]}, adId is {parts[1]}, adType is {parts[2]}, adTypeDisplayed is {parts[3]}, adNetwork is {parts[4]}, adMoney is {adMoney}, currency is {parts[6]}-------");
            }

            OnAdPaidEvent?.Invoke(parts[0], parts[1], parts[2], parts[3], parts[4], adMoney, parts[6]);
        }

        /// <summary>
        /// 处理奖励广告加载成功的事件。
        /// 记录日志（如果启用了调试模式），并触发 OnRewardAdLoadSuccessEvent 事件。
        /// </summary>
        private void OnRewardAdLoaded()
        {
            if (debugMode) { Debug.Log("-------OnRewardAdLoaded-------"); }
            OnRewardAdLoadSuccessEvent?.Invoke();
        }

        /// <summary>
        /// 处理奖励广告加载失败的事件。
        /// 解析错误代码字符串，验证格式并提取错误代码。
        /// 如果解析成功，记录日志（如果启用了调试模式），并触发 OnRewardAdLoadFailEvent 事件。
        /// 如果解析失败，记录警告日志。
        /// </summary>
        /// <param name="errorCodeMessage">包含错误代码的字符串。</param>
        private void OnRewardAdLoadFailed(string errorCodeMessage)
        {
            if (int.TryParse(errorCodeMessage, out int errorCode))
            {
                if (debugMode) { Debug.Log("-------OnRewardAdLoadFailed. ErrorCode is " + errorCode + "-------"); }
                OnRewardAdLoadFailEvent?.Invoke(errorCode);
            }
            else
            {
                if (debugMode) { Debug.LogWarning("OnRewardAdLoadFailed: Invalid error code message: " + errorCodeMessage); }
            }
        }

        /// <summary>
        /// 处理奖励广告被观看的事件。
        /// 记录日志（如果启用了调试模式），并触发 OnRewardAdViewedEvent 事件。
        /// </summary>
        private void OnRewardAdViewed()
        {
            if (debugMode) { Debug.Log("-------OnRewardAdViewed-------"); }
            OnRewardAdViewedEvent?.Invoke();
        }

        /// <summary>
        /// 处理奖励广告被取消的事件。
        /// 记录日志（如果启用了调试模式），并触发 OnRewardAdCanceledEvent 事件。
        /// </summary>
        private void OnRewardAdCanceled()
        {
            if (debugMode) { Debug.Log("-------OnRewardAdCanceled-------"); }
            OnRewardAdCanceledEvent?.Invoke();
        }

        private void OnRewardAdClicked(string adSource){
            if(debugMode){ Debug.Log("-------OnRewardAdClicked-------"); }
            OnRewardAdClickedEvent?.Invoke(adSource);
        }

        private void OnRewardAdImpression(string adSource){
            if(debugMode){ Debug.Log("-------OnRewardAdImpression-------"); }
            OnRewardAdImpressionEvent?.Invoke(adSource);
        }

        /// <summary>
        /// 处理全屏广告加载成功的事件。
        /// 记录日志（如果启用了调试模式），并触发 OnFullAdLoadSuccessEvent 事件。
        /// </summary>
        private void OnFullAdLoaded()
        {
            if (debugMode) { Debug.Log("-------OnFullAdLoaded-------"); }
            OnFullAdLoadSuccessEvent?.Invoke();
        }

        /// <summary>
        /// 处理全屏广告加载失败的事件。
        /// 解析错误代码字符串，验证格式并提取错误代码。
        /// 如果解析成功，记录日志（如果启用了调试模式），并触发 OnFullAdLoadFailEvent 事件。
        /// 如果解析失败，记录警告日志。
        /// </summary>
        /// <param name="errorCodeMessage">包含错误代码的字符串。</param>
        private void OnFullAdLoadFailed(string errorCodeMessage)
        {
            if(int.TryParse(errorCodeMessage, out int errorCode))
            {
                if (debugMode) { Debug.Log("-------OnFullAdLoadFailed-------"); }
                OnFullAdLoadFailEvent?.Invoke(errorCode);
            }
            else
            {
                if (debugMode) { Debug.LogWarning("OnFullAdLoadFailed: Invalid error code message: " + errorCodeMessage); }
            }
        }

        /// <summary>
        /// 处理全屏广告关闭的事件。
        /// 解析错误代码字符串，验证格式并提取错误代码。
        /// 如果解析成功，记录日志（如果启用了调试模式），并触发 OnFullAdClosedEvent 事件。
        /// 如果解析失败，记录警告日志。
        /// </summary>
        /// <param name="errorCodeMessage">包含错误代码的字符串。</param>
        private void OnFullAdClosed(string errorCodeMessage)
        {
            if (int.TryParse(errorCodeMessage, out int errorCode))
            {
                if (debugMode) { Debug.Log("-------OnFullAdClosed. ErrorCode is " + errorCode + "-------"); }
                OnFullAdClosedEvent?.Invoke(errorCode);
            }
            else
            {
                if (debugMode) { Debug.LogWarning("OnFullAdClosed: Invalid error code message: " + errorCodeMessage); }
            }
        }

        private void OnFullAdClicked(string adSource){
            if(debugMode){ Debug.Log("-------onFullAdClicked-------"); }
            OnFullAdClickedEvent?.Invoke(adSource);
        }

        private void OnFullAdImpression(string adSource){
            if(debugMode){ Debug.Log("-------onFullAdImpression-------"); }
            OnFullAdImpressionEvent?.Invoke(adSource);
        }

        /// <summary>
        /// 处理开屏广告加载成功的事件。
        /// 记录日志（如果启用了调试模式），并触发 OnOpenAdLoadSuccessEvent 事件。
        /// </summary>
        private void OnOpenAdLoaded()
        {
            if (debugMode) { Debug.Log("-------OnOpenAdLoaded-------"); }
            OnOpenAdLoadSuccessEvent?.Invoke();
        }

        /// <summary>
        /// 处理开屏广告加载失败的事件。
        /// 解析错误代码字符串，验证格式并提取错误代码。
        /// 如果解析成功，记录日志（如果启用了调试模式），并触发 OnOpenAdLoadFailEvent 事件。
        /// 如果解析失败，记录警告日志。
        /// </summary>
        /// <param name="errorCodeMessage">包含错误代码的字符串。</param>
        private void OnOpenAdLoadFailed(string errorCodeMessage)
        {
            if (int.TryParse(errorCodeMessage, out int errorCode))
            {
                if (debugMode) { Debug.Log("-------OnOpenAdLoadFailed. ErrorCode is " + errorCode + "-------"); }
                OnOpenAdLoadFailEvent?.Invoke(errorCode);
            }
            else
            {
                if (debugMode) { Debug.LogWarning("OnOpenAdLoadFailed: Invalid error code message: " + errorCodeMessage); }
            }
        }

        /// <summary>
        /// 处理开屏广告关闭的事件。
        /// 记录日志（如果启用了调试模式），并触发 OnOpenAdClosedEvent 事件。
        /// </summary>
        private void OnOpenAdClosed()
        {
            if (debugMode) { Debug.Log("-------OnOpenAdClosed-------"); }
            OnOpenAdClosedEvent?.Invoke();
        }

        /// <summary>
        /// 处理横幅广告加载成功的事件。
        /// 记录日志（如果启用了调试模式），并触发 OnBannerAdLoadSuccessEvent 事件。
        /// </summary>
        private void OnBannerAdLoaded()
        {
            if (debugMode) { Debug.Log("-------OnBannerAdLoaded-------"); }
            OnBannerAdLoadSuccessEvent?.Invoke();
        }

        /// <summary>
        /// 处理横幅广告加载失败的事件。
        /// 解析错误代码字符串，验证格式并提取错误代码。
        /// 如果解析成功，记录日志（如果启用了调试模式），并触发 OnBannerAdLoadFailEvent 事件。
        /// 如果解析失败，记录警告日志。
        /// </summary>
        /// <param name="errorCodeMessage">包含错误代码的字符串。</param>
        private void OnBannerAdLoadFailed(string errorCodeMessage)
        {
            if (debugMode) { Debug.Log("-------OnBannerAdLoadFailed. ErrorCode is " + errorCodeMessage + "-------"); }
            
            if (int.TryParse(errorCodeMessage, out int errorCode))
            {
                OnBannerAdLoadFailEvent?.Invoke(errorCode);
            }
            else
            {
                if (debugMode) { Debug.LogWarning("OnBannerAdLoadFailed. Invalid error code message: " + errorCodeMessage); }
            }
        }

        /// <summary>
        /// 处理横幅广告被点击的事件。
        /// 记录日志（如果启用了调试模式），并触发 OnBannerAdClickedEvent 事件。
        /// </summary>
        private void OnBannerAdClicked(string adSource)
        {
            if (debugMode)
            {
                Debug.Log("-------OnBannerAdClicked-------");
            }
            OnBannerAdClickedEvent?.Invoke(adSource);
        }

        private void OnBannerAdImpression(string adSource){
            if(debugMode){
                Debug.Log("-------OnBannerAdImpression-------");
            }
            OnBannerAdImpressionEvent?.Invoke(adSource);
        }

        /// <summary>
        /// 处理横幅广告显示的事件。
        /// 记录日志（如果启用了调试模式），并触发 OnBannerAdShownEvent 事件。
        /// </summary>
        private void OnBannerAdShown()
        {
            if (debugMode)
            {
                Debug.Log("-------OnBannerAdShown-------");
            }
            OnBannerAdShownEvent?.Invoke();
        }
        #endregion

        #region FirebaseSDK Callback
        private void OnRemoteConfigLoaded(string isSuccess)
        {
            if(bool.TryParse(isSuccess, out bool success))
            {
                Debug.Log("OnRemoteConfigLoaded " + success);
                OnRemoteConfigLoadedEvent?.Invoke(success);
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning("Invalid error code message: " + isSuccess);
                }
            }

        }
        #endregion

        #region CMPSDK Callback
        private void OnCMPFinished(string cmpMessage)
        {
            string[] parts = cmpMessage.Split('|');

            if(parts.Length == 2 && bool.TryParse(parts[0], out bool canSet) && int.TryParse(parts[1], out int errorCode))
            {
                if (debugMode) { Debug.Log("-------onCMPFinished-------"); }
                OnCMPFinishedEvent?.Invoke(canSet, errorCode);
            }
            else
            {
                if (debugMode) { Debug.LogWarning("onCMPFinished: Invalid error code message: " + cmpMessage); }
            }
        }
        #endregion

        #region BillingSDK Callback
        /// <summary>
        /// 处理查询商品详情成功的回调函数。
        /// 当查询商品详情成功时调用，触发相关事件并记录调试日志（如果启用了调试模式）。
        /// </summary>
        private void OnQueryProductDetailsSuccess()
        {
            // 如果启用了调试模式，记录查询商品详情成功的日志信息
            if (debugMode)
            {
                Debug.Log("-------Query Product Details Success-------");
            }
            // 触发查询商品详情成功的事件
            OnQueryProductDetailsSuccessEvent?.Invoke();
        }

        /// <summary>
        /// 处理查询商品详情失败的回调函数。
        /// 当查询商品详情失败时调用，解析错误码并触发相关事件，同时记录调试日志（如果启用了调试模式）。
        /// </summary>
        /// <param name="errorCodeMessage">错误码信息，可能为字符串形式的数字。</param>
        private void OnQueryProductDetailsFailed(string errorCodeMessage)
        {
            // 尝试将错误码信息解析为整数
            if (int.TryParse(errorCodeMessage, out int errorCode))
            {
                // 如果启用了调试模式，记录查询商品详情失败的日志信息
                if (debugMode)
                {
                    Debug.Log("-------Query Product Details Failed. ErrorCode is " + errorCodeMessage + "-------");
                }
                // 触发查询商品详情失败的事件，并传递错误码
                OnQueryProductDetailsFailEvent?.Invoke(errorCode);
            }
            else
            {
                // 如果错误码信息无效，记录警告日志
                if (debugMode)
                {
                    Debug.LogWarning("Invalid error code message: " + errorCodeMessage);
                }
            }
        }

        /// <summary>
        /// 处理购买成功的回调函数。
        /// 当购买成功时调用，解析购买信息并触发相关事件，同时记录调试日志（如果启用了调试模式）。
        /// </summary>
        /// <param name="purchaseInfo">购买信息，格式为"productId|orderId|transactionId"。</param>
        private void OnHandlePurchaseSuccess(string purchaseInfo)
        {
            // 按照指定分隔符解析购买信息
            string[] parts = purchaseInfo.Split('|');

            // 验证解析结果是否符合预期格式
            if (parts.Length == 3)
            {
                // 如果启用了调试模式，记录购买成功的日志信息
                if (debugMode)
                {
                    Debug.Log($"-------OnHandlePurchaseSuccess: productId is {parts[0]}, orderId is {parts[1]}, transactionId is {parts[2]}-------");
                }
                // 触发购买成功的事件，并传递商品ID和订单ID
                OnPurchaseSuccessEvent?.Invoke(parts[0], parts[1], parts[2]);
            }
            else
            {
                // 如果购买信息格式无效，记录警告日志
                if (debugMode)
                {
                    Debug.LogWarning("Invalid purchaseInfo format.");
                }
            }
        }

        /// <summary>
        /// 处理购买失败的回调函数。
        /// 当购买失败时调用，解析失败信息并触发相关事件，同时记录调试日志（如果启用了调试模式）。
        /// </summary>
        /// <param name="purchaseFailInfo">购买失败信息，格式为"productId|errorCode"。</param>
        private void OnHandlePurchaseFailed(string purchaseFailInfo)
        {
            // 按照指定分隔符解析购买失败信息
            string[] parts = purchaseFailInfo.Split('|');

            // 验证解析结果是否符合预期格式，并尝试将错误码解析为整数
            if (parts.Length == 2 && int.TryParse(parts[1], out int errorCode))
            {
                // 如果启用了调试模式，记录购买失败的日志信息
                if (debugMode)
                {
                    Debug.Log($"-------OnHandlePurchaseFailed: productId is {parts[0]}, errorCode is {parts[1]}-------");
                }
                // 触发购买失败的事件，并传递商品ID和错误码
                OnPurchaseFailEvent?.Invoke(parts[0], errorCode);
            }
            else
            {
                // 如果购买失败信息格式无效，记录警告日志
                if (debugMode)
                {
                    Debug.LogWarning("Invalid purchaseFailInfo format.");
                }
            }
        }

        /// <summary>
        /// 处理恢复购买成功的回调函数。
        /// 当恢复购买成功时调用，解析恢复信息并触发相关事件，同时记录调试日志（如果启用了调试模式）。
        /// </summary>
        /// <param name="purchaseInfo">恢复购买信息，格式为"productId|orderId|transactionId"。</param>
        private void OnHandleRestoreSuccess(string purchaseInfo)
        {
            // 按照指定分隔符解析恢复购买信息
            string[] parts = purchaseInfo.Split('|');

            // 验证解析结果是否符合预期格式
            if (parts.Length == 3)
            {
                // 如果启用了调试模式，记录恢复购买成功的日志信息
                if (debugMode)
                {
                    Debug.Log($"-------OnHandleRestoreSuccess: productId is {parts[0]}, orderId is {parts[1]}, transactionId is {parts[2]}-------");
                }
                // 触发恢复购买成功的事件，并传递商品ID和订单ID
                OnRestoreSuccessEvent?.Invoke(parts[0], parts[1], parts[2]);
            }
            else
            {
                // 如果恢复购买信息格式无效，记录警告日志
                if (debugMode)
                {
                    Debug.LogWarning("Invalid purchaseInfo format.");
                }
            }
        }

        /// <summary>
        /// 处理恢复购买失败的回调函数。
        /// 当恢复购买失败时调用，解析失败信息并触发相关事件，同时记录调试日志（如果启用了调试模式）。
        /// </summary>
        /// <param name="purchaseFailInfo">恢复购买失败信息，格式为"productId|errorCode"。</param>
        private void OnHandleRestoreFailed(string purchaseFailInfo)
        {
            // 按照指定分隔符解析恢复购买失败信息
            string[] parts = purchaseFailInfo.Split('|');

            // 验证解析结果是否符合预期格式，并尝试将错误码解析为整数
            if (parts.Length == 2 && int.TryParse(parts[1], out int errorCode))
            {
                // 如果启用了调试模式，记录恢复购买失败的日志信息
                if (debugMode)
                {
                    Debug.Log($"-------OnHandleRestoreFailed: productId is {parts[0]}, errorCode is {parts[1]}-------");
                }
                // 触发恢复购买失败的事件，并传递商品ID和错误码
                OnRestoreFailEvent?.Invoke(parts[0], errorCode);
            }
            else
            {
                // 如果恢复购买失败信息格式无效，记录警告日志
                if (debugMode)
                {
                    Debug.LogWarning("Invalid purchaseFailInfo format.");
                }
            }
        }

        /// <summary>
        /// 处理消耗购买成功的回调函数。
        /// 当消耗购买成功时调用，触发相关事件并记录调试日志（如果启用了调试模式）。
        /// </summary>
        /// <param name="productId">商品ID。</param>
        private void OnHandleConsumePurchaseSuccess(string productId)
        {
            // 如果启用了调试模式，记录消耗购买成功的日志信息
            if (debugMode)
            {
                Debug.Log($"-------OnHandleConsumePurchaseSuccess: productId is {productId}-------");
            }
            // 触发消耗购买成功的事件，并传递商品ID
            OnConsumeSuccessEvent?.Invoke(productId);
        }

        /// <summary>
        /// 处理消耗购买失败的回调函数。
        /// 当消耗购买失败时调用，解析失败信息并触发相关事件，同时记录调试日志（如果启用了调试模式）。
        /// </summary>
        /// <param name="consumeFailInfo">消耗购买失败信息，格式为"productId|errorCode"。</param>
        private void OnHandleConsumePurchaseFailed(string consumeFailInfo)
        {
            // 按照指定分隔符解析消耗购买失败信息
            string[] parts = consumeFailInfo.Split('|');

            // 验证解析结果是否符合预期格式，并尝试将错误码解析为整数
            if (parts.Length == 2 && int.TryParse(parts[1], out int errorCode))
            {
                // 如果启用了调试模式，记录消耗购买失败的日志信息
                if (debugMode)
                {
                    Debug.Log($"-------OnHandleConsumePurchaseFailed: productId is {parts[0]}, errorCode is {parts[1]}-------");
                }
                // 触发消耗购买失败的事件，并传递商品ID和错误码
                OnConsumeFailEvent?.Invoke(parts[0], errorCode);
            }
            else
            {
                // 如果消耗购买失败信息格式无效，记录警告日志
                if (debugMode)
                {
                    Debug.LogWarning("OnHandleConsumePurchaseFailed: Invalid consumeFailInfo format.");
                }
            }
        }
        #endregion

        #region RemoteDataSDK Callback

        private void OnGetUserData(string getUserDataStatus)
        {
            string[] parts = getUserDataStatus.Split('|');

            if(parts.Length == 2 && bool.TryParse(parts[0], out bool isSuccess) && int.TryParse(parts[1], out int errorCode))
            {
                if(debugMode)
                {
                    Debug.Log($"-------OnGetUserData: isSuccess is {isSuccess}, errorCode is {errorCode}-------");
                }
                if (isSuccess)
                {
                    OnGetUserDataSuccessEvent?.Invoke();
                }
                else
                {
                    OnGetUserDataFailEvent?.Invoke(errorCode);
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning("OnGetUserData: Invalid getUserDataStatus format.");
                }
            }
        }

        private void OnPostUserData(string postUserDataStatus)
        {
            string[] parts = postUserDataStatus.Split('|');

            if (parts.Length == 2 && bool.TryParse(parts[0], out bool isSuccess) && int.TryParse(parts[1], out int errorCode))
            {
                Debug.Log($"[AdDiag] OnPostUserData success={isSuccess} errorCode={errorCode} raw={postUserDataStatus}");
                if (isSuccess)
                {
                    OnPostUserDataEvent?.Invoke();
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning("OnPostUserData: Invalid postUserDataStatus format.");
                }
            }
        }

        private void OnDeleteUserData(string deleteUserDataStatus)
        {
            string[] parts = deleteUserDataStatus.Split('|');

            if(parts.Length== 2 && bool.TryParse(parts[0], out bool isSuccess) && int.TryParse(parts[1], out int errorCode))
            {
                if (debugMode)
                {
                    Debug.Log($"-------OnDeleteUserData: isSuccess is {isSuccess}, errorCode is {errorCode}-------");
                }
                if (isSuccess)
                {
                    OnDeleteUserDataSuccessEvent?.Invoke();
                }
                else
                {
                    OnDeleteUserDataFailEvent?.Invoke(errorCode);
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning("Invalid deleteUserDataStatus format.");
                }
            }
        }

        private void OnQueryUserProfileScore(string status)
        {
            string[] parts = status.Split('|');

            if (parts.Length == 5
                && bool.TryParse(parts[0], out bool isSuccess)
                && int.TryParse(parts[1], out int responseCode)
                && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double adScore)
                && double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double payScore))
            {
                if (debugMode)
                {
                    Debug.Log($"-------OnQueryUserProfileScore: isSuccess is {isSuccess}, responseCode is {responseCode}-------");
                }
                if (isSuccess)
                {
                    OnQueryUserProfileScoreSuccessEvent?.Invoke(adScore, payScore, parts[4]);
                }
                else
                {
                    OnQueryUserProfileScoreFailEvent?.Invoke(responseCode);
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning("OnQueryUserProfileScore: Invalid status format.");
                }
                OnQueryUserProfileScoreFailEvent?.Invoke(-1);
            }
        }

        private void OnGetRankData(string getRankDataStatus)
        {
            string[] parts = getRankDataStatus.Split('|');

            if(parts.Length == 2 && bool.TryParse(parts[0], out bool isSuccess) && int.TryParse(parts[1], out int errorCode))
            {
                if (debugMode)
                {
                    Debug.Log($"-------OnGetRankData: isSuccess is {isSuccess}, errorCode is {errorCode}-------");
                }

                if (!isSuccess)
                {
                    OnGetRankDataFailEvent?.Invoke(errorCode);
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning("Invalid getRankDataStatus format.");
                }
            }
            
        }

        private void OnGetRankDataChunk(string rankData)
        {
            if (rankData.Equals("_SEND_END_"))
            {
                if (debugMode)
                {
                    Debug.Log("-------OnGetRankDataChunk:  rankData Receive Completed.-------");
                }
                string rankDataStr = rankDataBuilder.ToString();
                if (!string.IsNullOrEmpty(rankDataStr))
                {
                    // return rankDataStr
                    OnGetRankDataSuccessEvent?.Invoke(rankDataStr);
                    rankDataBuilder.Clear();
                }
                else
                {
                    if (debugMode)
                    {
                        Debug.LogWarning("rankDataStr is empty.");
                    }
                }
            }
            else
            {
                rankDataBuilder.Append(rankData);
            }
        }

        private void OnGetEventStatus(string eventStatus)
        {
            string[] parts = eventStatus.Split('|');

            if(parts.Length == 2 && bool.TryParse(parts[0], out bool isSuccess) && int.TryParse(parts[1], out int errorCode))
            {
                if(debugMode)
                {
                    Debug.Log($"-------OnGetEventStatus: isSuccess is {isSuccess}, errorCode is {errorCode}-------");
                }

                if (!isSuccess)
                {
                    OnGetEventStatusFailEvent?.Invoke(errorCode);
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning("Invalid eventStatus format.");
                }
            }
        }

        private void OnGetEventStatusChunk(string eventData)
        {
            if (eventData.Equals("_SEND_END_"))
            {
                if (debugMode)
                {
                    Debug.Log("-------OnGetEventStatusChunk:  eventData Receive Completed.-------");
                }
                string eventStatusStr = eventStatusBuilder.ToString();
                if (!string.IsNullOrEmpty(eventStatusStr))
                {
                    // return eventDataStr
                    OnGetEventStatusSuccessEvent?.Invoke(eventStatusStr);
                    eventStatusBuilder.Clear();
                }
                else
                {
                   if (debugMode)
                    {
                        Debug.LogWarning("eventStatusStr is empty.");
                    }
                }
            }
            else
            {
                eventStatusBuilder.Append(eventData);
            }
        }

        private void OnGetServerTime(string status)
        {
            string[] parts = status.Split('|');

            if(parts.Length == 3 &&
               bool.TryParse(parts[0], out bool isSuccess) &&
               int.TryParse(parts[1], out int errorCode) &&
               long.TryParse(parts[2], out long unixTimeSecond))
            {
                if(debugMode)
                {
                    Debug.Log($"-------OnGetServerTime: isSuccess is {isSuccess}, errorCode is {errorCode}, unixTimeSecond is {unixTimeSecond}-------");
                }

                if (isSuccess)
                {
                    OnGetServerTimeSuccessEvent?.Invoke(unixTimeSecond);
                }
                else
                {
                    OnGetServerTimeFailEvent?.Invoke(errorCode);
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning("Invalid getServerTime format.");
                }
            }
        }

        private void OnGetPlayerExtMetadata(string status)
        {
            HandleStatusMessage("OnGetPlayerExtMetadata", status, null, errorCode => OnGetPlayerExtMetadataFailEvent?.Invoke(errorCode));
        }

        private void OnGetPlayerExtMetadataChunk(string chunk)
        {
            AppendChunk(getPlayerExtMetadataBuilder, chunk, data =>
            {
                OnGetPlayerExtMetadataSuccessEvent?.Invoke(data);
            });
        }

        private void OnSavePlayerExtMetadata(string status)
        {
            HandleStatusMessage("OnSavePlayerExtMetadata", status, null, errorCode => OnSavePlayerExtMetadataFailEvent?.Invoke(errorCode));
        }

        private void OnSavePlayerExtMetadataChunk(string chunk)
        {
            AppendChunk(savePlayerExtMetadataBuilder, chunk, data =>
            {
                OnSavePlayerExtMetadataSuccessEvent?.Invoke(data);
            });
        }

        private void OnBatchGetPlayerExtMetadata(string status)
        {
            HandleStatusMessage("OnBatchGetPlayerExtMetadata", status, null, errorCode => OnBatchGetPlayerExtMetadataFailEvent?.Invoke(errorCode));
        }

        private void OnBatchGetPlayerExtMetadataChunk(string chunk)
        {
            AppendChunk(batchGetPlayerExtMetadataBuilder, chunk, data =>
            {
                OnBatchGetPlayerExtMetadataSuccessEvent?.Invoke(data);
            });
        }

        private void OnBatchSavePlayerExtMetadata(string status)
        {
            HandleStatusMessage("OnBatchSavePlayerExtMetadata", status, null, errorCode => OnBatchSavePlayerExtMetadataFailEvent?.Invoke(errorCode));
        }

        private void OnBatchSavePlayerExtMetadataChunk(string chunk)
        {
            AppendChunk(batchSavePlayerExtMetadataBuilder, chunk, data =>
            {
                OnBatchSavePlayerExtMetadataSuccessEvent?.Invoke(data);
            });
        }

        private void OnGetGroupExtMetadata(string status)
        {
            HandleStatusMessage("OnGetGroupExtMetadata", status, null, errorCode => OnGetGroupExtMetadataFailEvent?.Invoke(errorCode));
        }

        private void OnGetGroupExtMetadataChunk(string chunk)
        {
            AppendChunk(getGroupExtMetadataBuilder, chunk, data =>
            {
                OnGetGroupExtMetadataSuccessEvent?.Invoke(data);
            });
        }

        private void OnSaveGroupExtMetadata(string status)
        {
            HandleStatusMessage("OnSaveGroupExtMetadata", status, null, errorCode => OnSaveGroupExtMetadataFailEvent?.Invoke(errorCode));
        }

        private void OnSaveGroupExtMetadataChunk(string chunk)
        {
            AppendChunk(saveGroupExtMetadataBuilder, chunk, data =>
            {
                OnSaveGroupExtMetadataSuccessEvent?.Invoke(data);
            });
        }

        private void OnBatchGetGroupExtMetadata(string status)
        {
            HandleStatusMessage("OnBatchGetGroupExtMetadata", status, null, errorCode => OnBatchGetGroupExtMetadataFailEvent?.Invoke(errorCode));
        }

        private void OnBatchGetGroupExtMetadataChunk(string chunk)
        {
            AppendChunk(batchGetGroupExtMetadataBuilder, chunk, data =>
            {
                OnBatchGetGroupExtMetadataSuccessEvent?.Invoke(data);
            });
        }

        private void OnBatchSaveGroupExtMetadata(string status)
        {
            HandleStatusMessage("OnBatchSaveGroupExtMetadata", status, null, errorCode => OnBatchSaveGroupExtMetadataFailEvent?.Invoke(errorCode));
        }

        private void OnBatchSaveGroupExtMetadataChunk(string chunk)
        {
            AppendChunk(batchSaveGroupExtMetadataBuilder, chunk, data =>
            {
                OnBatchSaveGroupExtMetadataSuccessEvent?.Invoke(data);
            });
        }

        private void OnGetGroupConfig(string status)
        {
            HandleStatusMessage("OnGetGroupConfig", status, null, errorCode => OnGetGroupConfigFailEvent?.Invoke(errorCode));
        }

        private void OnGetGroupConfigChunk(string chunk)
        {
            AppendChunk(groupConfigBuilder, chunk, data =>
            {
                OnGetGroupConfigSuccessEvent?.Invoke(data);
            });
        }

        private void OnGetGroupByUserId(string status)
        {
            HandleStatusMessage("OnGetGroupByUserId", status, null, errorCode => OnGetGroupByUserIdFailEvent?.Invoke(errorCode));
        }

        private void OnGetGroupByUserIdChunk(string chunk)
        {
            AppendChunk(groupByUserIdBuilder, chunk, data =>
            {
                OnGetGroupByUserIdSuccessEvent?.Invoke(data);
            });
        }

        private void OnCreateGroup(string status)
        {
            HandleStatusMessage("OnCreateGroup", status, null, errorCode => OnCreateGroupFailEvent?.Invoke(errorCode));
        }

        private void OnCreateGroupChunk(string chunk)
        {
            AppendChunk(createGroupBuilder, chunk, data =>
            {
                OnCreateGroupSuccessEvent?.Invoke(data);
            });
        }

        private void OnEditGroup(string status)
        {
            HandleStatusMessage("OnEditGroup", status, null, errorCode => OnEditGroupFailEvent?.Invoke(errorCode));
        }

        private void OnEditGroupChunk(string chunk)
        {
            AppendChunk(editGroupBuilder, chunk, data =>
            {
                OnEditGroupSuccessEvent?.Invoke(data);
            });
        }

        private void OnRecommendGroups(string status)
        {
            HandleStatusMessage("OnRecommendGroups", status, null, errorCode => OnRecommendGroupsFailEvent?.Invoke(errorCode));
        }

        private void OnRecommendGroupsChunk(string chunk)
        {
            AppendChunk(recommendGroupsBuilder, chunk, data =>
            {
                OnRecommendGroupsSuccessEvent?.Invoke(data);
            });
        }

        private void OnSearchGroups(string status)
        {
            HandleStatusMessage("OnSearchGroups", status, null, errorCode => OnSearchGroupsFailEvent?.Invoke(errorCode));
        }

        private void OnSearchGroupsChunk(string chunk)
        {
            AppendChunk(searchGroupsBuilder, chunk, data =>
            {
                OnSearchGroupsSuccessEvent?.Invoke(data);
            });
        }

        private void OnJoinGroup(string status)
        {
            HandleStatusMessage("OnJoinGroup", status, null, errorCode => OnJoinGroupFailEvent?.Invoke(errorCode));
        }

        private void OnJoinGroupChunk(string chunk)
        {
            AppendChunk(joinGroupBuilder, chunk, data =>
            {
                OnJoinGroupSuccessEvent?.Invoke(data);
            });
        }

        private void OnLeaveGroup(string status)
        {
            HandleStatusMessage("OnLeaveGroup", status, null, errorCode => OnLeaveGroupFailEvent?.Invoke(errorCode));
        }

        private void OnLeaveGroupChunk(string chunk)
        {
            AppendChunk(leaveGroupBuilder, chunk, data =>
            {
                OnLeaveGroupSuccessEvent?.Invoke(data);
            });
        }

        private void OnKickPlayer(string status)
        {
            HandleStatusMessage("OnKickPlayer", status, null, errorCode => OnKickPlayerFailEvent?.Invoke(errorCode));
        }

        private void OnKickPlayerChunk(string chunk)
        {
            AppendChunk(kickPlayerBuilder, chunk, data =>
            {
                OnKickPlayerSuccessEvent?.Invoke(data);
            });
        }

        private void OnGetGroupMembers(string status)
        {
            HandleStatusMessage("OnGetGroupMembers", status, null, errorCode => OnGetGroupMembersFailEvent?.Invoke(errorCode));
        }

        private void OnGetGroupMembersChunk(string chunk)
        {
            AppendChunk(groupMembersBuilder, chunk, data =>
            {
                OnGetGroupMembersSuccessEvent?.Invoke(data);
            });
        }

        private void OnGetGroupMembersWithMetadata(string status)
        {
            HandleStatusMessage("OnGetGroupMembersWithMetadata", status, null, errorCode => OnGetGroupMembersWithMetadataFailEvent?.Invoke(errorCode));
        }

        private void OnGetGroupMembersWithMetadataChunk(string chunk)
        {
            AppendChunk(groupMembersWithMetadataBuilder, chunk, data =>
            {
                OnGetGroupMembersWithMetadataSuccessEvent?.Invoke(data);
            });
        }

        private void OnGetApplyList(string status)
        {
            HandleStatusMessage("OnGetApplyList", status, null, errorCode => OnGetApplyListFailEvent?.Invoke(errorCode));
        }

        private void OnGetApplyListChunk(string chunk)
        {
            AppendChunk(applyListBuilder, chunk, data =>
            {
                OnGetApplyListSuccessEvent?.Invoke(data);
            });
        }

        private void OnApproveApplication(string status)
        {
            HandleStatusMessage("OnApproveApplication", status, null, errorCode => OnApproveApplicationFailEvent?.Invoke(errorCode));
        }

        private void OnApproveApplicationChunk(string chunk)
        {
            AppendChunk(approveApplicationBuilder, chunk, data =>
            {
                OnApproveApplicationSuccessEvent?.Invoke(data);
            });
        }

        private void OnRejectApplication(string status)
        {
            HandleStatusMessage("OnRejectApplication", status, null, errorCode => OnRejectApplicationFailEvent?.Invoke(errorCode));
        }

        private void OnRejectApplicationChunk(string chunk)
        {
            AppendChunk(rejectApplicationBuilder, chunk, data =>
            {
                OnRejectApplicationSuccessEvent?.Invoke(data);
            });
        }

        private void OnAssignRole(string status)
        {
            HandleStatusMessage("OnAssignRole", status, null, errorCode => OnAssignRoleFailEvent?.Invoke(errorCode));
        }

        private void OnAssignRoleChunk(string chunk)
        {
            AppendChunk(assignRoleBuilder, chunk, data =>
            {
                OnAssignRoleSuccessEvent?.Invoke(data);
            });
        }

        private void OnDeleteRole(string status)
        {
            HandleStatusMessage("OnDeleteRole", status, null, errorCode => OnDeleteRoleFailEvent?.Invoke(errorCode));
        }

        private void OnDeleteRoleChunk(string chunk)
        {
            AppendChunk(deleteRoleBuilder, chunk, data =>
            {
                OnDeleteRoleSuccessEvent?.Invoke(data);
            });
        }

        private void OnTransferRole(string status)
        {
            HandleStatusMessage("OnTransferRole", status, null, errorCode => OnTransferRoleFailEvent?.Invoke(errorCode));
        }

        private void OnTransferRoleChunk(string chunk)
        {
            AppendChunk(transferRoleBuilder, chunk, data =>
            {
                OnTransferRoleSuccessEvent?.Invoke(data);
            });
        }

        private void OnGetGroupChatMessagesByType(string status)
        {
            HandleStatusMessage("OnGetGroupChatMessagesByType", status, null, messageType =>
            {
                groupChatMessagesByTypeBuilders.Remove(messageType);
                OnGetGroupChatMessagesByTypeFailEvent?.Invoke(messageType);
            });
        }

        private void OnGetGroupChatMessagesByTypeChunk(string chunk)
        {
            if (!TryParseMessageTypeChunk(chunk, out int messageType, out string payload))
            {
                if (debugMode)
                {
                    Debug.LogWarning("OnGetGroupChatMessagesByTypeChunk: Invalid chunk format.");
                }
                return;
            }

            AppendGroupChatMessagesByTypeChunk(messageType, payload, (data, completedMessageType) =>
            {
                OnGetGroupChatMessagesByTypeSuccessEvent?.Invoke(data, completedMessageType);
            });
        }

        private void OnGetUGPPropertyById(string status)
        {
            HandleStatusMessage("OnGetUGPPropertyById", status, null, errorCode => OnGetUGPPropertyByIdFailEvent?.Invoke(errorCode));
        }

        private void OnGetUGPPropertyByIdChunk(string chunk)
        {
            AppendChunk(ugpPropertyByIdBuilder, chunk, data =>
            {
                OnGetUGPPropertyByIdSuccessEvent?.Invoke(data);
            });
        }

        private void OnCreateUGPProperty(string status)
        {
            HandleStatusMessage("OnCreateUGPProperty", status, null, errorCode => OnCreateUGPPropertyFailEvent?.Invoke(errorCode));
        }

        private void OnCreateUGPPropertyChunk(string chunk)
        {
            AppendChunk(createUGPPropertyBuilder, chunk, data =>
            {
                OnCreateUGPPropertySuccessEvent?.Invoke(data);
            });
        }

        private void OnClaimUGPProperty(string status)
        {
            HandleStatusMessage("OnClaimUGPProperty", status, null, errorCode => OnClaimUGPPropertyFailEvent?.Invoke(errorCode));
        }

        private void OnClaimUGPPropertyChunk(string chunk)
        {
            AppendChunk(claimUGPPropertyBuilder, chunk, data =>
            {
                OnClaimUGPPropertySuccessEvent?.Invoke(data);
            });
        }

        private void OnGetUGPPropertyClaims(string status)
        {
            HandleStatusMessage("OnGetUGPPropertyClaims", status, null, errorCode => OnGetUGPPropertyClaimsFailEvent?.Invoke(errorCode));
        }

        private void OnGetUGPPropertyClaimsChunk(string chunk)
        {
            AppendChunk(ugpPropertyClaimsBuilder, chunk, data =>
            {
                OnGetUGPPropertyClaimsSuccessEvent?.Invoke(data);
            });
        }

        private void OnCheckIfUGPPropertyClaimed(string status)
        {
            HandleStatusMessage("OnCheckIfUGPPropertyClaimed", status, null, errorCode => OnCheckUGPPropertyClaimedFailEvent?.Invoke(errorCode));
        }

        private void OnCheckIfUGPPropertyClaimedChunk(string chunk)
        {
            AppendChunk(checkUGPPropertyClaimedBuilder, chunk, data =>
            {
                OnCheckUGPPropertyClaimedSuccessEvent?.Invoke(data);
            });
        }

        private void OnGetUGPPropertiesByGroup(string status)
        {
            HandleStatusMessage("OnGetUGPPropertiesByGroup", status, null, errorCode => OnGetUGPPropertiesByGroupFailEvent?.Invoke(errorCode));
        }

        private void OnGetUGPPropertiesByGroupChunk(string chunk)
        {
            AppendChunk(ugpPropertiesByGroupBuilder, chunk, data =>
            {
                OnGetUGPPropertiesByGroupSuccessEvent?.Invoke(data);
            });
        }

        private void OnLinkUserWithOpenId(string status)
        {
            HandleStatusMessage("OnLinkUserWithOpenId", status, () => OnLinkUserWithOpenIdSuccessEvent?.Invoke(), errorCode => OnLinkUserWithOpenIdFailEvent?.Invoke(errorCode));
        }

        private void OnUnlinkUserWithOpenId(string status)
        {
            HandleStatusMessage("OnUnlinkUserWithOpenId", status, () => OnUnlinkUserWithOpenIdSuccessEvent?.Invoke(), errorCode => OnUnlinkUserWithOpenIdFailEvent?.Invoke(errorCode));
        }

        private void OnGroupChatConnectStatus(string status)
        {
            HandleStatusMessage("OnGroupChatConnectStatus", status, () => OnGroupChatConnectSuccessEvent?.Invoke(), errorCode => OnGroupChatConnectFailEvent?.Invoke(errorCode));
        }

        private void OnGroupChatMessageSent(string status)
        {
            HandleStatusMessage("OnGroupChatMessageSent", status, () => OnGroupChatMessageSentSuccessEvent?.Invoke(), errorCode => OnGroupChatMessageSentFailEvent?.Invoke(errorCode));
        }

        private void OnGroupChatMessageReceivedChunk(string chunk)
        {
            AppendChunk(groupChatMessageBuilder, chunk, data =>
            {
                OnGroupChatMessageReceivedEvent?.Invoke(data);
            });
        }

#endregion

        #region LoginSDK Callback
        private void OnLogined(string loginMessage)
        {
            string[] parts = loginMessage.Split('|');

            if(parts.Length == 3 && int.TryParse(parts[1], out int loginType) && int.TryParse(parts[2], out int errorCode))
            {
                if (debugMode)
                {
                    Debug.Log($"-------OnLogined: openId is {parts[0]}, loginType is {loginType}, errorCode is {errorCode}-------");
                }

                OnLoginedEvent?.Invoke(parts[0], loginType, errorCode);
            }
        }

        #endregion

        #region ShortcutSDK Callback

        private void OnShortcutClicked(string shortcutMessage)
        {
            if (debugMode)
            {
                Debug.Log("--- OnShortcutClicked ignored. Use ShortcutSdk.ConsumePendingAction instead.");
            }
        }

        #endregion

        #region PADSDK Callback

        private void OnAssetStatusUpdated(string assetUpdatedStatus)
        {
            string[] parts = assetUpdatedStatus.Split('|');
            if(parts.Length == 6 
                && int.TryParse(parts[1], out int assetStatus) 
                && int.TryParse(parts[2], out int errorCode) 
                && long.TryParse(parts[3], out long totalBytes) 
                && long.TryParse(parts[4], out long bytesDownloaded) 
                && int.TryParse(parts[5], out int transferProgressPercentage)){
                if (debugMode)
                {
                    Debug.Log($"--- OnAssetStatusUpdated: The assetName is {parts[0]}, The assetStatus is {assetStatus}, The errorCode is {errorCode}, The totalBytes is {totalBytes}, The byteDownloaded is {bytesDownloaded}, The transferProgressPercentage is {transferProgressPercentage}"); 
                }
                OnAssetStatusUpdatedEvent.Invoke(parts[0], assetStatus, errorCode, totalBytes, bytesDownloaded, transferProgressPercentage);
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning("Invalid assetUpdatedStatus format. The assetUpdatedStatus message is " + assetUpdatedStatus);
                }
            }
        }

        private void OnGetAssetStatusFailed(string assetName)
        {
            if (debugMode)
            {
                Debug.Log("--- onGetAssetStatusFailed: The assetName is " + assetName);
            }
            OnGetAssetStatusFailedEvent.Invoke(assetName);
        }

        private void OnGetAssetPathSuccess(string assetPathMessgae)
        {
            string[] parts = assetPathMessgae.Split('|');
            if(parts.Length == 2)
            {
                if (debugMode)
                {
                    Debug.Log($"--- OnGetAssetPathSuccess: The assetName is {parts[0]}, The assetPath is {parts[1]}");
                }
                OnGetAssetPathSuccessEvent.Invoke(parts[0], parts[1]);
            }
        }

        private void OnGetAssetPathFailed(string assetName)
        {
            if (debugMode)
            {
                Debug.Log("--- onGetAssetPathFailed: The assetName is " + assetName);
            }
            OnGetAssetPathFailedEvent.Invoke(assetName);
        }
        #endregion

    }
}
