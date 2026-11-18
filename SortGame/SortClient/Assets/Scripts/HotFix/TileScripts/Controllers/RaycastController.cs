using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TileGameCore;
using UnityEngine.Tilemaps;
using FishFramework;
using System.Collections.Generic;
// using CodiceApp.EventTracking.Plastic;

namespace FishTilePlay
{
    public class RaycastController : MonoBehaviour
    {
        private static bool isActive;

        public static bool IsActive => isActive;

        public static event SimpleCallback OnInputActivated;

        /// <summary>
        /// Esc按键（包括Android返回键）按下时触发的事件
        /// 订阅此事件可以自定义Esc按键的行为
        /// </summary>
        public static event SimpleCallback OnEscapePressed;

        public static ITappable m_curClickObject;

        public static float m_startTime;
        public void Init()
        {
            isActive = true;

            // 关闭多点触控
            DisableMultiTouch();
        }

        /*
            返回按钮
            现在的UI有两套
            UIPage一套
            UIPanel一套

            UIPanel的优先级更高,
            返回一次只会处理一次事件,不能同时触发两处.
            集成UIPage 或者 UIPanel, 需要提供返回的定制方法, 不要粗暴的关闭界面.
            
            每个界面单独写,不用全局处理了. 
        */

        private void CheckKeyBoard()
        {
            // 检测Esc按键（包括Android返回键）
            if (InputDirector.EscapeAction != null && InputDirector.EscapeAction.WasPressedThisFrame())
            {
                // 广告播放期间屏蔽返回键，避免误触导致流程异常
                if (GPSDKMgr.Instance != null && GPSDKMgr.Instance.IsAdPlaying)
                {
                    return;
                }

                if (TileAppUpdate.ConsumeEscape())
                {
                    return;
                }

                bool handled = false;

                // UIPanel的优先级更高，先检查UIPanel
                handled = GameModule.UI.HandleTopPanelEsc();

                // 如果UIPanel没有处理，再检查UIPage
                if (!handled)
                {
                    handled = TileGameCore.UIController.HandleTopPageEsc();
                }

                // 如果都没有处理，触发全局事件（允许外部自定义行为）
                if (!handled)
                {
                    OnEscapePressed?.Invoke();
                }
            }

#if UNITY_EDITOR
            // 使用新的Input System监听键盘按键
            if (InputDirector.F1Action != null && InputDirector.F1Action.WasPressedThisFrame())
            {

                int cnt = GPUserDataManager.Instance.GetPassLevelCnt();
                Debug.Log("cntttt= " + cnt);

                PlayerPrefs.Save();
                // Aler1tDlg.ShowDlg("F1");
                if (GameData.BFromStartScene)
                    GameModule.UI.OpenPanel<GMLevelChooseUI>();
            }

            if (InputDirector.F2Action != null && InputDirector.F2Action.WasPressedThisFrame())
            {
                if (Time.timeScale < 1)
                {
                    Time.timeScale = 1.0f;
                }
                else
                {
                    Time.timeScale = 0.1f;
                }
                // Alert1Dlg.ShowDlg("F2");
                // GameEvent.Send((int)EventNameDef.ReqQueryShopProductSuc);
                // GameModule.UI.OpenPanel<UIShopMain>();
                // GameEvent.Send<string>((int)EventNameDef.BuyProductSuccess, "1002");
                // GameEvent.Send<string>((int)EventNameDef.BuyProductSuccess, contentJson);
                // GameModule.UI.OpenPanel<CircleDailyAwardUI>();
                // GameModule.UI.OpenPanel<UIUserInfoDlg>();

                // var oneList = FishFramework.Utility.GetRandomSequence(10, 2);
                // Debug.Log(oneList.ToString());
                // string str = "";
                // foreach (var item in oneList)
                // {
                //     str += item + "_";
                //     // Debug.Log(item);
                // }

                // Debug.Log("strrr=== " + str);

            }

            if (InputDirector.F3Action != null && InputDirector.F3Action.WasPressedThisFrame())
            {
                // AudioController.SetMusicBOpen(false);
                GameDirector.levelController.OnMatchCompleted(true);
            }

            if (InputDirector.F4Action != null && InputDirector.F4Action.WasPressedThisFrame())
            {
                AudioController.SetMusicBOpen(true);
                // var userData = GPUserDataManager.Instance.UserData;
                // if (userData.IsWithinFirstDayWindow())
                // {
                //     GPUserDataManager.Instance.UserData.IncrementVideoPlayFirstDay();
                //     var v = GPUserDataManager.Instance.UserData.videoPlayCntFirstDay;
                //     Debug.Log("first day cnt===" + v);
                // }
                // else
                // {
                //     Debug.LogError("not today");
                // }

                // GameModule.UI.OpenPanel<UIRatesDlg>();
                GameModule.UI.OpenPanel<UIApplyNotificationDlg>();
            }

            if (InputDirector.F6Action != null && InputDirector.F6Action.WasPressedThisFrame())
            {
                GameModule.UI.OpenPanel<UIRatesDlg>();
            }

            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            {
                DebugRandomLongPressEffect();
            }

#endif
        }

#if UNITY_EDITOR
        static TileActor s_debugLongPressTile;

        /// <summary>
        /// F10：随机棋子预览长按特效（改预制体参数后按 F10 即可看到）。
        /// </summary>
        private static void DebugRandomLongPressEffect()
        {
            if (s_debugLongPressTile != null)
            {
                s_debugLongPressTile.RecoverLongPressAnim(true);
                s_debugLongPressTile = null;
            }

            var levelGen = LevelDirector.LevelGenService;
            if (levelGen == null || levelGen.ActTiles == null || levelGen.ActTiles.Count == 0)
            {
                Debug.LogWarning("[RaycastController] F10: 没有可预览的棋子");
                return;
            }

            var candidates = new List<TileActor>(levelGen.ActTiles.Count);
            for (int i = 0; i < levelGen.ActTiles.Count; i++)
            {
                var tile = levelGen.ActTiles[i];
                if (tile == null || tile.IsSubmitted || !tile.gameObject.activeInHierarchy)
                    continue;
                candidates.Add(tile);
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning("[RaycastController] F10: 没有未提交的活跃棋子");
                return;
            }

            s_debugLongPressTile = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            s_debugLongPressTile.DebugReplayLongPressHintEff();
            Debug.Log($"[RaycastController] F10 long-press preview: {s_debugLongPressTile.name}");
        }
#endif

        private void Update()
        {
            CheckKeyBoard();
            if (!isActive || !LevelDirector.IsRaycastEnabled || UIController.IsPopupOpened) return;

            if (InputDirector.ClickAction.WasPressedThisFrame())
            {
                Ray ray = Camera.main.ScreenPointToRay(InputDirector.MousePosition);
                if (TryResolveClickTarget(ray, out RaycastHit hit, out ITappable clickableObject))
                {
                    TileActor resolvedTile = clickableObject as TileActor;
                    if (AwardColorEggCoverManager.TryHandleBlockedClick(hit.point, resolvedTile)
                        || BoxCoverManager.TryHandleBlockedClick(hit.point, resolvedTile))
                    {
                        ResetLongPress();
                        clickableObject = null;
                    }
                    else if (resolvedTile != null
                        && resolvedTile.IsClickable
                        && (AwardColorEggCoverManager.IsClickBlocked(resolvedTile, hit.point)
                            || BoxCoverManager.IsClickBlocked(resolvedTile, hit.point)))
                    {
                        ResetLongPress();
                        if (!AwardColorEggCoverManager.TryHandleBlockedClick(hit.point, resolvedTile))
                            BoxCoverManager.TryHandleBlockedClick(hit.point, resolvedTile);
                        clickableObject = null;
                    }

                    if (clickableObject != null)
                    {
                        if (LevelDirector.IsLevelLoaded)
                        {
                            if (m_curClickObject != clickableObject)
                            {
                                ResetLongPress();
                            }

                            if (m_curClickObject == null)
                            {
                                m_startTime = Time.realtimeSinceStartup;
                                clickableObject.OnObjectClicked(out bool isValid);

                                if (isValid)
                                {
                                    m_curClickObject = clickableObject;
                                }
                            }
                            else
                            {
                                ResetLongPress();
                            }
                        }
                    }
                }
            }
            else if (InputDirector.ClickAction.WasReleasedThisFrame())
            {
                Ray ray = Camera.main.ScreenPointToRay(InputDirector.MousePosition);
                if (TryResolveClickTarget(ray, out _, out ITappable clickableObject)
                    && clickableObject != null
                    && LevelDirector.IsLevelLoaded
                    && m_curClickObject == clickableObject)
                {
                    clickableObject.OnObjectReleased();
                    m_curClickObject = null;
                    return;
                }

                ResetLongPress();
            }
            // 检查触摸状态是否异常（如系统中断导致的状态不一致）
            else if (m_curClickObject != null && !InputDirector.ClickAction.IsPressed())
            {
                // 如果当前有点击对象但触摸状态显示未按下，说明状态异常
                ResetLongPress();
            }

            // 可选：使用 WasCompletedThisFrame 来检测完整的点击完成
            // if (InputDirector.ClickAction.WasCompletedThisFrame())
            // {
            //     // 检测到完整的点击动作完成（按下+释放）
            //     // 可以用于处理简单的点击逻辑，而不需要分别处理按下和释放
            // }
        }

        /// <summary>
        /// 射线先打到蛋/箱时，优先把点击交给同射线下的棋子，
        /// 这样被盖住一部分的棋子能走常规「点了只抖、不上槽」逻辑。
        /// </summary>
        static bool TryResolveClickTarget(Ray ray, out RaycastHit hit, out ITappable target)
        {
            hit = default;
            target = null;
            RaycastHit[] hits = Physics.RaycastAll(ray);
            if (hits == null || hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            RaycastHit? tileHit = null;
            RaycastHit? otherHit = null;
            for (int i = 0; i < hits.Length; i++)
            {
                ITappable tappable = hits[i].transform.GetComponent<ITappable>();
                if (tappable == null)
                    continue;
                if (tappable is TileActor)
                {
                    tileHit = hits[i];
                    break;
                }
                if (otherHit == null)
                    otherHit = hits[i];
            }

            if (tileHit.HasValue)
            {
                hit = tileHit.Value;
                target = hit.transform.GetComponent<ITappable>();
                return true;
            }
            if (otherHit.HasValue)
            {
                hit = otherHit.Value;
                target = hit.transform.GetComponent<ITappable>();
                return true;
            }

            hit = hits[0];
            return false;
        }

        public static void ResetLongPress()
        {
            if (m_curClickObject == null) return;

            var beh = m_curClickObject as TileActor;
            if (beh != null && !beh.IsNull() && beh.gameObject && beh.gameObject.IsNull() != true)
            {
                // 已回池/未激活时不必再播恢复动画，只清会话
                if (beh.gameObject.activeInHierarchy && !beh.IsSubmitted)
                    m_curClickObject.RecoverLongPressAnim(true);
            }

            m_curClickObject = null;
        }

        public static void Disable()
        {
            isActive = false;
        }

        public static void Enable()
        {
            isActive = true;

            OnInputActivated?.Invoke();
        }

        public void ResetControl()
        {

        }

        /// <summary>
        /// 关闭多点触控
        /// </summary>
        private void DisableMultiTouch()
        {
            // 设置最大触控点数为1，禁用多点触控
            Input.multiTouchEnabled = false;

            Debug.Log("多点触控已禁用");
        }

        /// <summary>
        /// 启用多点触控
        /// </summary>
        public void EnableMultiTouch()
        {
            Input.multiTouchEnabled = true;

            Debug.Log("多点触控已启用");
        }

        /// <summary>
        /// 检查多点触控状态
        /// </summary>
        /// <returns>是否启用了多点触控</returns>
        public bool IsMultiTouchEnabled()
        {
            return Input.multiTouchEnabled;
        }
    }
}
