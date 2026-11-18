using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
// using UnityEditor.Playables;
using UnityEngine;
using DG.Tweening;
using FishTilePlay;
using FishFramework;
using TileGameCore;
using Spine.Unity;
using TMPro;
using cfg.Game;

public class EncouragePlayEffect : MonoBehaviour
{
    private const float ChallengeIdleThresholdSeconds = 2f;

    [SerializeField] GameObject godHandEffect;//神之一手
    [SerializeField] GameObject challengeEffect;//挑战来了
    [SerializeField] GameObject sideEffect;//五次消除来一下.
    [SerializeField] float sideEffectShowDelay = 0.8f;

    [SerializeField] private SkeletonGraphic ghSpineSkel;
    [SerializeField] private SkeletonGraphic clgSpineSkel;

    [SerializeField] private TextMeshProUGUI ghText;
    [SerializeField] private Animator challengeAnimator;

    private bool m_isInChallenge;
    private bool m_hasPendingMatch; // 标记是否有待处理的消除
    private float m_lastGodHandTime = -1f; // 上次触发神之一手的时间
    private float delay = 25f;

    private int m_eliminationCountForSideEffect;
    private const int SideEffectEveryNMatches = 5;
    private const string ThemeCombo5Slot = "themeCombo5";
    private Coroutine m_challengeDelayCoroutine;
    private readonly List<GameObject> m_sideEffectVariants = new();
    private GameObject m_combo5Effect;
    private int m_combo5ThemeId = int.MinValue;

    void Start()
    {
        godHandEffect?.SetActive(false);
        challengeEffect?.SetActive(false);
        ApplyTheme();
        HideSideEffect();

        // 订阅消除事件，用于检测成功消除
        DockActor.MatchCombined += OnMatchCombined;

        // 订阅棋子提交事件，在玩家提交棋子后检查挑战动效
        DockActor.ElementAdded += OnElementAdded;

        GameEvent.AddEventListener((int)EventNameDef.LoadLevelAnimFinish, OnLevelStartedClearSideEffect);
        GameEvent.AddEventListener((int)EventNameDef.LoadSaveLevelAnimFinish, OnLevelStartedClearSideEffect);
        GameEvent.AddEventListener((int)EventNameDef.EncouragePlayEffectsHide, OnGameExitOrReturnHideEffects);
    }

    void OnDestroy()
    {
        // 取消订阅
        DockActor.MatchCombined -= OnMatchCombined;
        DockActor.ElementAdded -= OnElementAdded;

        GameEvent.RemoveEventListener((int)EventNameDef.LoadLevelAnimFinish, OnLevelStartedClearSideEffect);
        GameEvent.RemoveEventListener((int)EventNameDef.LoadSaveLevelAnimFinish, OnLevelStartedClearSideEffect);
        GameEvent.RemoveEventListener((int)EventNameDef.EncouragePlayEffectsHide, OnGameExitOrReturnHideEffects);
    }

    /// <summary>游戏结束、回主菜单、重开当前关等：停止相关 Tween，逻辑复位，隐藏本物体下所有子节点。</summary>
    private void OnGameExitOrReturnHideEffects()
    {
        DOTween.Kill(this);
        FishFramework.Utility.DGDelayedCall(2f, PauseAndHideAllChildNodes, this);
    }

    private void PauseAndHideAllChildNodes()
    {
        DOTween.Kill(this);
        if (m_challengeDelayCoroutine != null)
        {
            StopCoroutine(m_challengeDelayCoroutine);
            m_challengeDelayCoroutine = null;
        }
        m_isInChallenge = false;
        m_hasPendingMatch = false;
        m_eliminationCountForSideEffect = 0;

        godHandEffect?.SetActive(false);
        challengeEffect?.SetActive(false);
        HideSideEffect();
        

        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
    }

    private void RestoreEncourageChildNodes()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(true);
    }

    private void OnLevelStartedClearSideEffect()
    {
        DOTween.Kill(this);
        if (m_challengeDelayCoroutine != null)
        {
            StopCoroutine(m_challengeDelayCoroutine);
            m_challengeDelayCoroutine = null;
        }
        m_eliminationCountForSideEffect = 0;
        m_isInChallenge = false;
        m_hasPendingMatch = false;

        RestoreEncourageChildNodes();

        godHandEffect?.SetActive(false);
        challengeEffect?.SetActive(false);
        ApplyTheme();
        HideSideEffect();
    }

    private void OnElementAdded(IDockable element)
    {
        // 玩家提交棋子后，延迟检查是否有消除（等待状态更新）
        FishFramework.Utility.DGDelayedCall(0.1f, () =>
        {
            // 检查备战席是否有匹配（3个相同类型的棋子）
            bool hasMatch = CheckIfHasMatch();

            if (!hasMatch)
            {
                // 无消除，直接调用检查挑战动效
                CheckShowChallengeEffect();
            }
            else
            {
                // 有消除，标记待处理，等待消除结束后调用
                m_hasPendingMatch = true;
            }
        });
    }

    private void OnMatchCombined(List<IDockable> matchedTiles)
    {
        // 当成功消除时，检查是否显示神之一手动效
        if (m_isInChallenge)
        {
            CheckShowGodHandEffect();
        }

        // 如果有待处理的匹配，在消除结束后调用检查挑战动效
        if (m_hasPendingMatch)
        {
            m_hasPendingMatch = false;
            // 消除结束后，延迟检查挑战动效（等待状态更新）
            FishFramework.Utility.DGDelayedCall(0.1f, () =>
            {
                CheckShowChallengeEffect();
            });
        }

        m_eliminationCountForSideEffect++;
        if (m_eliminationCountForSideEffect >= SideEffectEveryNMatches)
        {
            m_eliminationCountForSideEffect = 0;
            PlaySideEffect();
        }
    }

    // 检查备战席是否有匹配（3个相同类型的棋子）
    private bool CheckIfHasMatch()
    {
        var dock = LevelDirector.Dock;
        if (dock == null) return false;

        // 获取备战席中所有已占用的棋子
        var occupiedTiles = dock.GetOccupiedTileIndices();
        if (occupiedTiles.Count < 3) return false;

        // 按类型分组，检查是否有3个或以上相同类型的棋子
        var tileGroups = occupiedTiles.GroupBy(tile => tile.TileModel.TileIndex);
        foreach (var group in tileGroups)
        {
            if (group.Count() >= 3)
            {
                return true;
            }
        }

        return false;
    }

    // 检查是否有可能形成三消（包括备战席+场上的组合）
    private bool CheckIfHasPossibleMatch()
    {
        var dock = LevelDirector.Dock;
        if (dock == null) return false;

        var levelGenFunc = LevelDirector.LevelGenService;
        if (levelGenFunc == null) return false;

        // 获取备战席中所有已占用的棋子
        var occupiedTiles = dock.GetOccupiedTileIndices();
        if (occupiedTiles.Count == 0) return false;

        // 按类型分组备战席中的棋子
        var dockTileGroups = occupiedTiles.GroupBy(tile => tile.TileModel.TileIndex);

        // 获取场上所有可点击的棋子
        var clickableTiles = LevelDirector.GetClickableTiles();

        // 检查每种类型是否可能形成三消
        foreach (var dockGroup in dockTileGroups)
        {
            int dockCount = dockGroup.Count();
            int tileIndex = dockGroup.First().TileModel.TileIndex;

            // 如果备战席已经有3个或以上相同类型的棋子，可以直接三消
            if (dockCount >= 3)
            {
                return true;
            }

            // 如果备战席有2个相同类型的棋子，检查场上是否有1个相同类型的可点击棋子
            if (dockCount == 2)
            {
                int boardCount = clickableTiles.Count(tile =>
                    tile.TileModel != null &&
                    tile.TileModel.TileIndex == tileIndex &&
                    tile.IsClickable);

                if (boardCount >= 1)
                {
                    return true; // 备战席2个 + 场上1个 = 三消
                }
            }

            // 如果备战席有1个棋子，检查场上是否有2个相同类型的可点击棋子
            if (dockCount == 1)
            {
                int boardCount = clickableTiles.Count(tile =>
                    tile.TileModel != null &&
                    tile.TileModel.TileIndex == tileIndex &&
                    tile.IsClickable);

                if (boardCount >= 2)
                {
                    return true; // 备战席1个 + 场上2个 = 三消（需要提交2个棋子）
                }
            }
        }

        // 检查场上是否有3个相同类型的可点击棋子（不需要备战席）
        var boardTileGroups = clickableTiles
            .Where(tile => tile.TileModel != null)
            .GroupBy(tile => tile.TileModel.TileIndex);

        // foreach (var boardGroup in boardTileGroups)
        // {
        //     if (boardGroup.Count() >= 3)
        //     {
        //         return true; // 场上直接有3个相同类型的可点击棋子
        //     }
        // }

        return false; // 没有可能形成三消
    }

    //备战席<=3且场上无可消除棋子 
    //可操作位置加上已存在的棋子不可以消除
    public void CheckShowChallengeEffect()
    {
        if (!CanStartChallengeDelay())
        {
            return;
        }

        if (m_challengeDelayCoroutine == null)
        {
            m_challengeDelayCoroutine = StartCoroutine(DelayPlayChallengeEffect());
        }
    }

    private bool CanStartChallengeDelay()
    {
        if (m_isInChallenge)
            return false;

        float currentTime = Time.time;
        if (m_lastGodHandTime > 0 && currentTime - m_lastGodHandTime < delay)
            return false;

        var dock = LevelDirector.Dock;
        if (dock == null)
            return false;

        int availableSlots = DockActor.GetSlotsAvailable();
        if (availableSlots != 3)
            return false;

        bool hasPossibleMatch = CheckIfHasPossibleMatch();
        return !hasPossibleMatch;
    }

    private IEnumerator DelayPlayChallengeEffect()
    {
        float observedOperateTime = GameDirector.GetLastOperateTimeSinceLevelLoad();
        float stableStartTime = Time.time;

        while (true)
        {
            if (!CanStartChallengeDelay())
            {
                m_challengeDelayCoroutine = null;
                yield break;
            }

            float latestOperateTime = GameDirector.GetLastOperateTimeSinceLevelLoad();
            if (latestOperateTime > observedOperateTime)
            {
                observedOperateTime = latestOperateTime;
                stableStartTime = Time.time;
            }

            if (Time.time - stableStartTime >= ChallengeIdleThresholdSeconds)
            {
                break;
            }

            yield return null;
        }

        if (CanStartChallengeDelay())
        {
            m_isInChallenge = true;
            PlayChallengeEffect();
        }

        m_challengeDelayCoroutine = null;
    }

    void PlaySideEffect()
    {
        if (sideEffect == null)
            return;

        FishFramework.Utility.DGDelayedCall(sideEffectShowDelay, () =>
        {
            if (sideEffect == null)
                return;

            GameObject selectedEffect = SelectSideEffectVariant();
            if (selectedEffect == null)
                return;

            SetOnlyActiveSideEffectChild(selectedEffect);
            sideEffect.SetActive(true);
            RestartParticleSystems(selectedEffect.transform);

            TileBaseThemePack pack = ThemeUiApplier.Get();
            AudioClip comboSound = pack != null ? pack.combo5Sound : null;
            if (comboSound == null)
                comboSound = AudioController.AudioClips?.fiveCombo;
            ThemeUiApplier.PlaySound(comboSound);
            FishFramework.Utility.DGDelayedCall(2.0f, () =>
            {
                HideSideEffect();
            }, this);
        }, this);
    }

    public void ApplyTheme()
    {
        EnsureThemeCombo5Instance();
        SetOnlyActiveSideEffectChild(null);
    }

    void EnsureThemeCombo5Instance()
    {
        int themeId = LevelDirector.LoadedLevelTheme;
        if (m_combo5Effect != null && m_combo5ThemeId == themeId)
            return;

        ClearThemeCombo5();
        GameObject prefab = ResolveCombo5Prefab();
        if (prefab == null || sideEffect == null)
            return;

        GameObject inst = Instantiate(prefab, sideEffect.transform, false);
        inst.name = ThemeCombo5Slot;
        inst.SetActive(false);
        m_combo5Effect = inst;
        m_combo5ThemeId = themeId;
    }

    static GameObject ResolveCombo5Prefab()
    {
        TileBaseThemePack pack = ThemeUiApplier.Get();
        if (pack != null && pack.combo5Effect != null)
            return pack.combo5Effect;

        GameTileBaseControlCfg cfg = ConfigHelper.Tables?.TbGameTileBaseControlCfg?.GetOrDefault(LevelDirector.LoadedLevelTheme);
        string prefabName = cfg != null ? cfg.Combo5Effect : null;
        if (string.IsNullOrEmpty(prefabName))
            return null;

        return UILoadHelper.LoadAsset<GameObject>("Assets/Arts/NewEff/Prefabs/" + prefabName + ".prefab");
    }

    void ClearThemeCombo5()
    {
        m_combo5ThemeId = int.MinValue;
        if (m_combo5Effect != null)
        {
            Destroy(m_combo5Effect);
            m_combo5Effect = null;
            return;
        }

        GameObject existing = FindSideEffectChild(ThemeCombo5Slot);
        if (existing != null)
            Destroy(existing);
    }

    private GameObject SelectSideEffectVariant()
    {
        EnsureThemeCombo5Instance();
        if (m_combo5Effect != null)
            return m_combo5Effect;

        return SelectRandomSideEffectVariant();
    }

    GameObject FindSideEffectChild(string childName)
    {
        if (sideEffect == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        Transform found = sideEffect.transform.Find(childName);
        if (found != null)
        {
            return found.gameObject;
        }

        Transform root = sideEffect.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name == childName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private GameObject SelectRandomSideEffectVariant()
    {
        CacheSideEffectVariants();
        if (m_sideEffectVariants.Count == 0)
            return null;

        return m_sideEffectVariants[Random.Range(0, m_sideEffectVariants.Count)];
    }

    private void CacheSideEffectVariants()
    {
        if (sideEffect == null || m_sideEffectVariants.Count > 0)
            return;

        for (int i = 1; i <= 5; i++)
        {
            Transform variant = sideEffect.transform.Find($"Eff_{i:00}");
            if (variant != null)
                m_sideEffectVariants.Add(variant.gameObject);
        }
    }

    void SetOnlyActiveSideEffectChild(GameObject selected)
    {
        if (sideEffect == null)
        {
            return;
        }

        Transform root = sideEffect.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            GameObject child = root.GetChild(i).gameObject;
            bool on = selected != null && child == selected;
            child.SetActive(on);
            if (!on)
            {
                StopParticleSystems(child.transform);
            }
        }
    }

    private void HideSideEffect()
    {
        if (sideEffect == null)
            return;

        SetOnlyActiveSideEffectChild(null);
        sideEffect.SetActive(false);
    }

    static void StopParticleSystems(Transform root)
    {
        if (root == null)
        {
            return;
        }

        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles[i].Clear(true);
        }
    }

    private static void RestartParticleSystems(Transform root)
    {
        if (root == null)
            return;

        StopParticleSystems(root);
        foreach (ParticleSystem particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            particleSystem.Play(true);
        }
    }

    void PlayChallengeEffect()
    {
        // 先关再开，确保 Animator 从首帧重播（只驱动缩放/透明度，不驱动坐标）
        challengeEffect.SetActive(false);
        challengeEffect.SetActive(true);

        var challengeOpen = AudioController.AudioClips?.challengeOpen;
        if (challengeOpen != null)
            AudioController.PlaySound(challengeOpen);

        if (challengeAnimator != null)
        {
            challengeAnimator.Rebind();
            challengeAnimator.Play("GodHandTextEff_Anim", 0, 0f);
            challengeAnimator.Update(0f);
        }

        clgSpineSkel.AnimationState.ClearTrack(0); // 清理当前轨道
        clgSpineSkel.Skeleton.SetToSetupPose();    // 还原为初始姿势
        clgSpineSkel.Skeleton.ScaleX = -1f;         // 左下角水平翻转
        clgSpineSkel.Update(0);

        // 与 gh 共用 GodHands 资源；挑战侧播 GodHands（ATile2 无 Dog 套）
        var d = clgSpineSkel.AnimationState.SetAnimation(0, "GodHands_wenhao", false);
        d.TimeScale = 0.6f;

        FishFramework.Utility.DGDelayedCall(3.0f, () =>
        {
            challengeEffect.SetActive(false);
        });
    }

    //出现"挑战动效之后，玩家成功消除一次
    //和上方
    public void CheckShowGodHandEffect()
    {
        // 只有在挑战状态下才显示神之一手动效
        if (!m_isInChallenge)
        {
            return;
        }

        // 检查是否在5秒内触发过神之一手
        float currentTime = Time.time;
        if (m_lastGodHandTime > 0 && currentTime - m_lastGodHandTime < delay)
        {
            // 5秒内不再触发，只重置挑战状态
            m_isInChallenge = false;
            return;
        }

        // 播放神之一手动效
        var challengeOver = AudioController.AudioClips?.challengeOver;
        if (challengeOver != null)
            AudioController.PlaySound(challengeOver);

        PlayGodHandEffect();

        // 记录触发时间
        m_lastGodHandTime = currentTime;

        // 重置挑战状态
        m_isInChallenge = false;
    }


    void PlayGodHandEffect()
    {
        godHandEffect.SetActive(true);

        var str = I2LocalizeMgr.GetString("UI_BrilliantMove");
        str = str.Replace("\\n", "\n");

        ghText.text = str ;//I2LocalizeMgr.GetString("UI_BrilliantMove");

        ghSpineSkel.AnimationState.ClearTrack(0); // 清理当前轨道
        ghSpineSkel.Skeleton.SetToSetupPose();    // 还原为初始姿势
        ghSpineSkel.Update(0);

        var d = ghSpineSkel.AnimationState.SetAnimation(0, "GodHands", false);
        d.TimeScale = 0.6f;

        FishFramework.Utility.DGDelayedCall(3.0f, () =>
        {
            godHandEffect.SetActive(false);
        });

    }
}
