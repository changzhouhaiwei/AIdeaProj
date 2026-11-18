using UnityEngine;

namespace EwingSoft.GameSDK
{
    public class ShortcutSdk : MonoBehaviour
    {
        private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
        private const string androidClassPath = "com.zzxz.gamesdk.core.shortcut.ShortcutSdk";

        /// <summary>
        /// 初始化快捷方式SDK
        /// </summary>
        public static void InitShortcutSdk()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaUnityPlayer = new AndroidJavaClass(unityPlayerClassPath))
                {
                    var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    using (var javaShortcut = new AndroidJavaClass(androidClassPath))
                    {
                        javaShortcut.CallStatic("init", currentActivity);
                    }
                }
            }
        }

        /// <summary>
        /// 发布或更新一个动态快捷方式
        /// </summary>
        /// <param name="shortcutId">快捷方式唯一ID</param>
        /// <param name="iconResId">快捷方式图标资源ID</param>
        /// <param name="shortLabelResIds">短文案资源ID数组</param>
        /// <param name="longLabelResIds">长文案资源ID数组，传null时复用短文案</param>
        /// <param name="initialState">初始显示文案下标</param>
        /// <param name="rank">排序值，通常越小越靠前</param>
        /// <returns>true表示发布或更新成功，否则失败</returns>
        public static bool Publish(string shortcutId,
                                   int iconResId,
                                   int[] shortLabelResIds,
                                   int[] longLabelResIds,
                                   int initialState,
                                   int rank)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaShortcut = new AndroidJavaClass(androidClassPath))
                {
                    return javaShortcut.CallStatic<bool>("publish", shortcutId, iconResId,
                        shortLabelResIds, longLabelResIds, initialState, rank);
                }
            }
            return false;
        }

        /// <summary>
        /// 更新已发布快捷方式的显示状态
        /// </summary>
        /// <param name="shortcutId">快捷方式唯一ID</param>
        /// <param name="state">目标显示文案下标</param>
        /// <returns>true表示更新成功，否则失败</returns>
        public static bool UpdateState(string shortcutId, int state)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaShortcut = new AndroidJavaClass(androidClassPath))
                {
                    return javaShortcut.CallStatic<bool>("updateState", shortcutId, state);
                }
            }
            return false;
        }

        /// <summary>
        /// 移除一个动态快捷方式
        /// </summary>
        /// <param name="shortcutId">快捷方式唯一ID</param>
        /// <returns>true表示移除成功，否则失败</returns>
        public static bool Remove(string shortcutId)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaShortcut = new AndroidJavaClass(androidClassPath))
                {
                    return javaShortcut.CallStatic<bool>("remove", shortcutId);
                }
            }
            return false;
        }

        /// <summary>
        /// 获取并清除当前待处理的快捷方式消息
        /// </summary>
        /// <returns>当前待处理消息，格式为shortcutId|state；没有待处理事件时返回空字符串</returns>
        public static string ConsumePendingAction()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                using (var javaShortcut = new AndroidJavaClass(androidClassPath))
                {
                    string shortcutMessage = javaShortcut.CallStatic<string>("consumePendingAction");
                    return shortcutMessage ?? string.Empty;
                }
            }
            return string.Empty;
        }

    }
}
