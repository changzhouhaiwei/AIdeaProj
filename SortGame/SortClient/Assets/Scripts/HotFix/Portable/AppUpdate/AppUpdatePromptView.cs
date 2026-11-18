using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Portable.AppUpdate
{
    /// <summary>
    /// Android 系统 AlertDialog。强更不可取消；软更可以点 Not now 或按返回键关掉。
    /// iOS 不弹。
    /// </summary>
    public sealed class AppUpdatePromptView : MonoBehaviour
    {
        const string HostName = "[AppUpdatePrompt]";

        static AppUpdatePromptView s_host;
        static readonly Queue<Action> s_mainThread = new Queue<Action>();

        static AppUpdatePrompt s_prompt;
        static Action s_onConfirm;
        static Action s_onSkip;
        static bool s_open;
        static int s_generation;
#if UNITY_ANDROID && !UNITY_EDITOR
        const int ButtonPositive = -1;
        static AndroidJavaObject s_dialog;
        static DialogClickProxy s_positiveProxy;
        static DialogClickProxy s_negativeProxy;
        static CancelProxy s_cancelProxy;
        static ViewClickProxy s_forceClickProxy;
#endif

        public static bool IsOpen => s_open;
        public static bool IsForce => s_prompt != null && s_prompt.ForceUpdate;
        public static AppUpdatePrompt Current => s_open ? s_prompt : null;

        public static void Show(AppUpdatePrompt prompt, Action onConfirm, Action onSkip)
        {
            if (prompt == null)
            {
                return;
            }

#if !UNITY_ANDROID || UNITY_EDITOR
            Debug.LogWarning("[AppUpdate] 系统弹窗只在 Android 真机显示");
            return;
#else
            s_prompt = prompt;
            s_onConfirm = onConfirm;
            s_onSkip = onSkip;
            s_open = true;
            int generation = ++s_generation;
            EnsureHost();
            RunOnUiThread(() =>
            {
                if (generation != s_generation || !s_open)
                {
                    return;
                }

                Present(prompt);
            });
#endif
        }

        public static void Close()
        {
            s_open = false;
            s_prompt = null;
            s_generation++;
#if UNITY_ANDROID && !UNITY_EDITOR
            RunOnUiThread(DismissOnUiThread);
#endif
        }

        internal static void Post(Action action)
        {
            if (action == null)
            {
                return;
            }

            lock (s_mainThread)
            {
                s_mainThread.Enqueue(action);
            }

            EnsureHost();
        }

        void OnApplicationPause(bool pause)
        {
            if (!pause)
            {
                ScheduleForceButtonReset();
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ScheduleForceButtonReset();
            }
        }

        static void ScheduleForceButtonReset()
        {
            if (!s_open || s_prompt == null || !s_prompt.ForceUpdate)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            RunOnUiThread(RebindForceButton, false);
#endif
        }

        void Update()
        {
            Action[] batch = null;
            lock (s_mainThread)
            {
                if (s_mainThread.Count == 0)
                {
                    return;
                }

                batch = s_mainThread.ToArray();
                s_mainThread.Clear();
            }

            for (int i = 0; i < batch.Length; i++)
            {
                batch[i]?.Invoke();
            }
        }

        static void EnsureHost()
        {
            if (s_host != null)
            {
                return;
            }

            var root = new GameObject(HostName);
            DontDestroyOnLoad(root);
            s_host = root.AddComponent<AppUpdatePromptView>();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static void RunOnUiThread(Action action, bool markClosedOnFailure = true)
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity == null)
                    {
                        if (markClosedOnFailure)
                        {
                            s_open = false;
                        }

                        Debug.LogWarning("[AppUpdate] 没有当前 Activity，系统弹窗未显示");
                        return;
                    }

                    activity.Call("runOnUiThread", new AndroidJavaRunnable(action));
                }
            }
            catch (Exception e)
            {
                if (markClosedOnFailure)
                {
                    s_open = false;
                }

                Debug.LogWarning("[AppUpdate] 系统弹窗失败 " + e.Message);
            }
        }

        static void Present(AppUpdatePrompt prompt)
        {
            DismissOnUiThread();
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity == null)
                    {
                        s_open = false;
                        return;
                    }

                    using (var builder = CreateBuilder(activity))
                    {
                        CallBuilder(builder, "setTitle", prompt.Title ?? string.Empty);
                        CallBuilder(builder, "setMessage", prompt.Message ?? string.Empty);
                        CallBuilder(builder, "setCancelable", !prompt.ForceUpdate);

                        s_positiveProxy = new DialogClickProxy(() => s_onConfirm?.Invoke());
                        CallBuilder(builder, "setPositiveButton", string.IsNullOrEmpty(prompt.ConfirmText) ? "Update Now" : prompt.ConfirmText, s_positiveProxy);

                        if (!prompt.ForceUpdate)
                        {
                            s_negativeProxy = new DialogClickProxy(() => s_onSkip?.Invoke());
                            s_cancelProxy = new CancelProxy(() => s_onSkip?.Invoke());
                            CallBuilder(builder, "setNegativeButton", string.IsNullOrEmpty(prompt.SkipText) ? "Maybe Later" : prompt.SkipText, s_negativeProxy);
                            CallBuilder(builder, "setOnCancelListener", s_cancelProxy);
                        }

                        var dialog = builder.Call<AndroidJavaObject>("create");
                        dialog.Call("setCanceledOnTouchOutside", false);
                        dialog.Call("show");
                        s_dialog = dialog;
                        Debug.Log("[AppUpdate] 系统弹窗已显示 force=" + prompt.ForceUpdate + " title=" + prompt.Title);
                        if (prompt.ForceUpdate)
                        {
                            // 系统按钮默认点了就会关掉。强更换成不关闭的点击。
                            s_forceClickProxy = new ViewClickProxy(
                                () => s_onConfirm?.Invoke(),
                                prompt.StoreUrl,
                                prompt.FallbackUrl);
                            using (var button = dialog.Call<AndroidJavaObject>("getButton", ButtonPositive))
                            {
                                button.Call("setOnClickListener", s_forceClickProxy);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                s_open = false;
                DismissOnUiThread();
                Debug.LogWarning("[AppUpdate] 系统弹窗失败 " + e.Message);
            }
        }

        static AndroidJavaObject CreateBuilder(AndroidJavaObject activity)
        {
            try
            {
                using (var style = new AndroidJavaClass("android.R$style"))
                {
                    int theme = style.GetStatic<int>("Theme_DeviceDefault_Dialog_Alert");
                    return new AndroidJavaObject("android.app.AlertDialog$Builder", activity, theme);
                }
            }
            catch (Exception)
            {
                return new AndroidJavaObject("android.app.AlertDialog$Builder", activity);
            }
        }

        static void CallBuilder(AndroidJavaObject builder, string method, params object[] args)
        {
            using (var returned = builder.Call<AndroidJavaObject>(method, args))
            {
            }
        }

        static void RebindForceButton()
        {
            if (!s_open || s_dialog == null || s_forceClickProxy == null)
            {
                return;
            }

            try
            {
                using (var button = s_dialog.Call<AndroidJavaObject>("getButton", ButtonPositive))
                {
                    if (button == null)
                    {
                        return;
                    }

                    button.Call("setEnabled", true);
                    button.Call("setClickable", true);
                    button.Call("setPressed", false);
                    try
                    {
                        button.Call("cancelPendingInputEvents");
                    }
                    catch (Exception)
                    {
                    }

                    button.Call("setOnClickListener", s_forceClickProxy);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AppUpdate] 强更按钮恢复失败 " + e.Message);
            }
        }

        static void DismissOnUiThread()
        {
            var dialog = s_dialog;
            s_dialog = null;
            if (dialog == null)
            {
                return;
            }

            try
            {
                dialog.Call("dismiss");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AppUpdate] 关闭系统弹窗失败 " + e.Message);
            }

            dialog.Dispose();
        }
#endif

        sealed class DialogClickProxy : AndroidJavaProxy
        {
            readonly Action _action;

            public DialogClickProxy(Action action)
                : base("android.content.DialogInterface$OnClickListener")
            {
                _action = action;
            }

            [Preserve]
            public void onClick(AndroidJavaObject dialog, int which)
            {
                Post(_action);
            }
        }

        sealed class CancelProxy : AndroidJavaProxy
        {
            readonly Action _action;

            public CancelProxy(Action action)
                : base("android.content.DialogInterface$OnCancelListener")
            {
                _action = action;
            }

            [Preserve]
            public void onCancel(AndroidJavaObject dialog)
            {
                Post(_action);
            }
        }

        sealed class ViewClickProxy : AndroidJavaProxy
        {
            readonly Action _action;
            readonly string _storeUrl;
            readonly string _fallbackUrl;
            AndroidJavaRunnable _afterClick;

            public ViewClickProxy(Action action, string storeUrl, string fallbackUrl)
                : base("android.view.View$OnClickListener")
            {
                _action = action;
                _storeUrl = storeUrl ?? string.Empty;
                _fallbackUrl = fallbackUrl ?? string.Empty;
            }

            [Preserve]
            public void onClick(AndroidJavaObject view)
            {
                if (_afterClick == null)
                {
                    _afterClick = new AndroidJavaRunnable(LaunchAfterClick);
                }

                try
                {
                    view.Call("setPressed", false);
                    view.Call("post", _afterClick);
                }
                catch (Exception)
                {
                    LaunchAfterClick();
                }
            }

            [Preserve]
            void LaunchAfterClick()
            {
                bool opened = AppUpdateService.TryLaunchStoreFromUi(_storeUrl, _fallbackUrl);
                if (!opened)
                {
                    Debug.LogWarning("[AppUpdate] 点击线程跳转失败，交给主线程再试");
                }

                Post(_action);
            }
        }
    }
}
