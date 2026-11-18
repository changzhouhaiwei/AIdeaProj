using System;
using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class NotificationSdk : MonoBehaviour
    {
        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.notification.local.NotificationSdk";

        /// <summary>
        /// 初始化通知SDK
        /// </summary>
        public static void InitNotificationSdk()
        {
            if(Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    using (var javaNotification = new AndroidJavaClass(androidClassPath))
                    {
                        javaNotification.CallStatic("init", currentActivity);
                    }
                }
            }
        }

        /// <summary>
        /// 发送报警提醒通知
        /// </summary>
        /// <param name="hours">指定发送提醒的时间点数组，以小时为单位</param>
        /// <param name="beginAfterDays">提醒开始生效的天数，从当前时间起算</param>
        /// <param name="cancelAfterDays">提醒自动取消的天数，从当前时间起算</param>
        /// <param name="style">提醒通知的样式，可选值为1、2</param>
        /// <param name="requestCode">通知的请求码，用于标识不同的通知</param>
        /// <param name="actionName">提醒通知的动作名称</param>
        /// <param name="nameSpace">提醒通知的命名空间</param>
        public static void SendAlarmRemind(int[] hours, int beginAfterDays, int cancelAfterDays, 
                                          int style, int requestCode, string actionName, string nameSpace)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaNotification = new AndroidJavaClass(androidClassPath))
                {
                    javaNotification.CallStatic("sendAlarmRemind", hours, beginAfterDays, 
                                               cancelAfterDays, style, requestCode, actionName, nameSpace);
                }
            }
        }

        /// <summary>
        /// 发送自定义样式的提醒通知
        /// </summary>
        /// <param name="hours">指定发送提醒的时间点数组，以小时为单位</param>
        /// <param name="beginAfterDays">提醒开始生效的天数，从当前时间起算</param>
        /// <param name="cancelAfterDays">提醒自动取消的天数，从当前时间起算</param>
        /// <param name="requestCode">通知的请求码，用于标识不同的通知</param>
        /// <param name="layoutResId">自定义布局文件的资源ID</param>
        /// <param name="notifyBgResIdsArray">自定义通知背景的资源ID数组</param>
        /// <param name="notifyTitleResIdsArray">自定义通知标题的资源ID数组</param>
        /// <param name="notifyDescResIdsArray">自定义通知描述的资源ID数组</param>
        /// <param name="actionName">提醒通知的动作名称</param>
        /// <param name="nameSpace">提醒通知的命名空间</param>
        public static void SendCustomAlarmRemind(int[] hours, int beginAfterDays, int cancelAfterDays, 
                                               int requestCode, int layoutResId, int[] notifyBgResIdsArray, 
                                               int[] notifyTitleResIdsArray, int[] notifyDescResIdsArray, 
                                               string actionName, string nameSpace)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaNotification = new AndroidJavaClass(androidClassPath))
                {
                    javaNotification.CallStatic("sendCustomAlarmRemind", hours, beginAfterDays, 
                                               cancelAfterDays, requestCode, layoutResId, 
                                               notifyBgResIdsArray, notifyTitleResIdsArray, 
                                               notifyDescResIdsArray, actionName, nameSpace);
                }
            }
        }

        /// <summary>
        /// 取消提醒通知
        /// </summary>
        /// <param name="requestCode">提醒通知的请求码</param>
        public static void CancelAlarmRemind(int requestCode)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaNotification = new AndroidJavaClass(androidClassPath))
                {
                    javaNotification.CallStatic("cancelAlarmRemind", requestCode);
                }
            }
        }
    }
}