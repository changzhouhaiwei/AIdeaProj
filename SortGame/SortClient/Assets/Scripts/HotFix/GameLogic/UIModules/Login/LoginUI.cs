using UnityEngine.UI;
using UnityEngine;
using FishFramework;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Spine.Unity;

public class LoginUI : LoginUIBase
{
    private const string LoadingZzNodeName = "SkeletonGraphic (Loading_zz)";
    private const string LoadingZzLoopAnim = "Loading_zz";

    private Tween loadingSpinTween;
    private SkeletonGraphic loadingZzSpine;

    protected override void OnCreating()
    {
        var loadingIcon = viewBehaviour != null ? viewBehaviour.transform.Find("loading") : null;
        loadingSpinTween = LoadingIconSpin.Start(loadingIcon);
        PlayLoadingZzLoop();
    }

    protected override void OnOpen()
    {
        GameBAData.ExcuteLoadingEvent10010();
        InitUI();
    }

    float loadingDuration = 3f;

    private const int LowMemoryThresholdMb = 4096;
    private const float BaseSdkDedicatedDelay = 1.5f;

    // 初始化削峰：把各 SDK 初始化按总时长均匀摊到多帧，避免一帧内集中初始化导致 ANR
    private float initSpreadDuration = 2f;

    private bool m_loadingVisualDone;
    // Android 进游戏必须等 CheckInitProc 跑完 StartCMPSDK。低内存机多等 1.5 秒后，3 秒加载条会先切场景，协程被拆掉，广告不再初始化。
    private bool m_sdkChainReady;
#if UNITY_IOS && !UNITY_EDITOR
    private bool m_iosRcGateReady;
#endif

    private static bool IsLowMemoryDevice =>
        SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < LowMemoryThresholdMb;

    private void InitUI()
    {
        GameModule.Setting.SetFrameRate();

        ShowQuate();

        CheckInitResolution();

        Utility.DGDelayedCall(0.2f, () =>
        {
            UIGameFunc.CheckShowPrivacyDlg(() =>
            {
                panelBehaviour.StartCoroutine(CheckInitProc());
            });
        });
    }

    IEnumerator CheckInitProc()
    {
        // loading 进度条尽早启动，削峰期间用户可见进度反馈
        AnimateLoadingBar();

        if (IsLowMemoryDevice)
        {
            // BaseSdk 已在 GameApp 中启动，低端机额外等待其初始化完成后再进入后续 SDK 链
            yield return new WaitForSecondsRealtime(BaseSdkDedicatedDelay);
        }

        // BaseSdk、数数已在 GameApp.CorStartLogic 提前初始化
        // 顺序原则：广告+CMP 链尽量靠前（CMP 完成后才触发 AdSdk.InitAds，越早越好），
        // Remote Config 紧随其后，轻量/非关键项放后面
        var initSteps = new List<System.Action>
        {
            () => GPSDKMgr.Instance.InitAdSdk(),
            () => GPSDKMgr.Instance.InitCMPSDK(),
            () => GPSDKMgr.Instance.InitRemoteDataSdk(),
            () => GPSDKMgr.Instance.InitNoticationSDK(),
            () => GPSDKMgr.Instance.InitBillingSdk(),
#if UNITY_IOS && !UNITY_EDITOR
            () => GPSDKMgr.Instance.QueryCurrencyInfo(),
#endif
        };

        // 总时长按步数均分，得到每步之间的间隔
        float stepInterval = Mathf.Max(0.001f, initSpreadDuration / initSteps.Count);

        foreach (var step in initSteps)
        {
            step();
            yield return new WaitForSecondsRealtime(stepInterval);
        }

#if UNITY_IOS && !UNITY_EDITOR
        // 首启进关依赖 IsUnderReviewIOS：等 RC 成功/失败回调（或超时按提审）后再放行进游戏
        yield return GPSDKMgr.Instance.WaitForRemoteConfigResolved();
        m_iosRcGateReady = true;
        Debug.Log(
            $"[LoginUI][RCDiag] RC gate open. visualDone={m_loadingVisualDone} " +
            $"IsUnderReviewIOS={GPSDKMgr.Instance.IsUnderReviewIOS}");
        StartGameLogin();
        GPSDKMgr.Instance.StartCMPSDK();
        TryEnterGame();
#else
        StartGameLogin();
        GPSDKMgr.Instance.StartCMPSDK();
        m_sdkChainReady = true;
        TryEnterGame();
#endif
    }

    private void CheckInitResolution()
    {
        GameModule.Setting.SetDynamicResolution();
    }

    private void StartGameLogin()
    {
#if UNITY_EDITOR
        InitNonePlatform();
#else
        InitSDKPlatform();
#endif
    }

    private void ShowQuate()
    {
        int idx = 13 ;//Random.Range(1,13);

        GameData.Player.QuateId = idx;

        string text = I2LocalizeMgr.GetString("FamousQuotes" + idx);

        string[] parts = Regex.Split(text, "-+|—+"); // - 连续 或 — 连续 都能切

        if (parts.Length > 1)
        {
            quateDescLb.text = parts[0];
            quateNameLb.text = "—" + parts[1];
        }
    }

    //打的是无平台SDK的包
    private void InitNonePlatform()
    {
        Utility.DGDelayedCall(loadingDuration, EnterGame);
    }

    private void InitSDKPlatform()
    {
        GameBAData.ExcuteLoading("", "", "10012", "拉起SDK登录", true);
        GPSDKMgr.Instance.PostUserData(() =>
        {
            Debug.Log("俺的UID  =   " + GPUserDataManager.Instance.GetUID());
        });
    }

    IEnumerator InitSDKCor(float delay)
    {
        yield return new WaitForSeconds(delay);
        ProcLogin();
    }

    private void ProcLogin()
    {
        Debug.Log("Proc ==== Login登录");
    }

    private Sequence mSecureSequence;

    void AnimateLoadingBar()
    {
        loadingDuration = 3f;

        int outTime = 3;

        if (GPUserDataManager.HasLocalData() != true)
        {
            outTime = 4;
        }

        mSecureSequence = DOTween.Sequence().AppendInterval(outTime).AppendCallback(() =>
        {
            m_loadingVisualDone = true;
#if UNITY_IOS && !UNITY_EDITOR
            Debug.Log($"[LoginUI][RCDiag] loading visual done outTime={outTime}");
#endif
            TryEnterGame();
        });

#if UNITY_EDITOR
        loadingDuration = 0.5f;
#endif

        loadingBar.fillAmount = 0f;
        SetLoadingLb(0f);

        DOTween.To(() => loadingBar.fillAmount, x =>
            {
                loadingBar.fillAmount = x;
                SetLoadingLb(x);
            }, 1f, loadingDuration)
            .SetEase(Ease.Linear);
    }

    private void SetLoadingLb(float amount)
    {
        if (loadingLb == null)
        {
            return;
        }

        loadingLb.text = $"{Mathf.RoundToInt(amount * 100f)}%";
    }

    protected override void OnClicked(Button button)
    {
    }

    private bool hasEnterGame = false;

    /// <summary>
    /// Android：进度条计时结束后进游戏。
    /// iOS：还需 Remote Config 门闸打开（避免 loading 3~4s 定时器绕过 RC 等待）。
    /// </summary>
    private void TryEnterGame()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!m_loadingVisualDone || !m_iosRcGateReady)
        {
            Debug.Log(
                $"[LoginUI][RCDiag] TryEnterGame blocked visualDone={m_loadingVisualDone} rcReady={m_iosRcGateReady}");
            return;
        }
#else
        if (!m_loadingVisualDone || !m_sdkChainReady)
            return;
#endif
        EnterGame();
    }

    private void EnterGame()
    {
        if (hasEnterGame)
        {
            return;
        }

        hasEnterGame = true;
        Debug.Log("[LoginUI][RCDiag] EnterGame");

        SceneLoaderHelper.OpenSceneNew(GameSceneName.GameScene, () =>
        {
            GameModule.UI.DestroyAllPanel();
            GameBAData.ExcuteLoading("", "", "10029", "到达主界面", true);
        }, needLoading: true);
    }

    protected override void OnDestroyed()
    {
        if (GameModule.UI == null || !GameModule.UI.ExistPanel<LoadingUI>())
        {
            LoadingZzSpinePlayback.Release();
        }

        LoadingIconSpin.Stop(ref loadingSpinTween);
        mSecureSequence?.Kill();
        mSecureSequence = null;
    }

    private void PlayLoadingZzLoop()
    {
        if (loadingZzSpine == null && viewBehaviour != null)
        {
            var spineTransform = viewBehaviour.transform.Find(LoadingZzNodeName);
            loadingZzSpine = spineTransform != null ? spineTransform.GetComponent<SkeletonGraphic>() : null;
        }

        LoadingZzSpinePlayback.Show(loadingZzSpine, LoadingZzLoopAnim);
    }
}
