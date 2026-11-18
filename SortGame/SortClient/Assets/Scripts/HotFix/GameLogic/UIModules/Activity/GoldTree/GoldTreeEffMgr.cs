using System;
using System.Collections;
using System.Collections.Generic;
using Spine;
using TileGameCore;
using FishTilePlay;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using DG.Tweening;
using FishFramework;

public class GoldTreeEffMgr : MonoBehaviour
{
    private const float MoveSpeed = 2200f;
    /// <summary>松开收集后，棋盘 GoldenTree 停留与首次 UI 飞行开始的等待时间。</summary>
    public const float CollectFlyStartDelay = 0.2f;
    /// <summary>x2 时第二次飞行动效相对第一次「开始飞出」的间隔。</summary>
    private const float X2SecondMoveInterval = 0.2f;
    private const float NormalMoveStartDelay = CollectFlyStartDelay;

    private static GoldTreeEffMgr instance;

    [SerializeField]
    private GameObject moveEffOri;
    [SerializeField]
    private GameObject arriveEffOri;

    [SerializeField]
    private GameObject effPoolNode;

    List<GameObject> moveEffList = new();

    List<GameObject> arriveEffList = new();

    [SerializeField]
    Transform dstObj;


    private void Awake()
    {
        EnsureDstObj();
    }

    private void Start()
    {
        moveEffList.Add(moveEffOri);
        arriveEffList.Add(arriveEffOri);
        instance = this;
        EnsureDstObj();
    }

    void EnsureDstObj()
    {
        if (dstObj != null)
            return;

        var icon = transform.parent != null
            ? transform.parent.Find("goldTreeIcon")
            : null;
        if (icon == null)
        {
            var goldTreeNode = GameObject.Find("goldTreeNode");
            if (goldTreeNode != null)
                icon = goldTreeNode.transform.Find("goldTreeIcon");
        }

        if (icon != null)
            dstObj = icon;
        else
            Debug.LogError("[GoldTree] GoldTreeEffMgr.dstObj is null. Wire goldTreeIcon under goldTreeNode on UI Game.");
    }

    //在已有池子中获得对应特效
    GameObject GetEffFromPool(int type)
    {
        GameObject oriObj = arriveEffOri;
        List<GameObject> poolList = arriveEffList;
        if (type == 1)
        {
            oriObj = moveEffOri;
            poolList = moveEffList;
        }
        else if (type == 2)
        {
            oriObj = arriveEffOri;
            poolList = arriveEffList;
        }

        if (poolList.Count == 0)
        {
            GameObject obj = GameObject.Instantiate(oriObj);
            return obj;
        }
        else
        {
            GameObject retObj = poolList[0];
            poolList.RemoveAt(0);
            return retObj;
        }
    }

    private void PlayMoveEff(
        TileActor tile,
        bool fireCollectEvent = true,
        Vector3? overrideStartWorldPos = null,
        Action onFullyComplete = null,
        float spawnDelay = NormalMoveStartDelay)
    {
        if (spawnDelay > 0f)
        {
            Vector3 delayedStartWorldPos = overrideStartWorldPos ?? tile.transform.position;
            DOVirtual.DelayedCall(spawnDelay, () =>
            {
                if (this == null)
                    return;

                PlayMoveEff(tile, fireCollectEvent, delayedStartWorldPos, onFullyComplete, 0f);
            }).SetLink(gameObject);
            return;
        }

        var moveEff = GetEffFromPool(1);
        moveEff.SetParent(transform);
        moveEff.SetActive(true);
        moveEff.InitLocalTransform();

        moveEff.transform.position = overrideStartWorldPos ?? tile.transform.position;

        /*
        1.moveEff是一个UI上的移动特效.
        2.moveEffect 在UI上转一圈同时沿着贝塞尔曲线直达dstObj
        3.到达dstObj的时候，角度回到起点状态.
        4.动画结束隐藏moveeff
        5.开始播放到达动画.
        */

        // 获取 RectTransform 组件
        RectTransform moveEffRect = moveEff.GetComponent<RectTransform>();
        var ancP = moveEffRect.anchoredPosition3D;
        ancP.z = 0;
        moveEffRect.anchoredPosition3D = ancP;

        RectTransform dstRect = dstObj.GetComponent<RectTransform>();

        // 记录起始位置和角度
        Vector3 startPos = moveEffRect.anchoredPosition;
        Vector3 startRotation = moveEffRect.eulerAngles;

        // 将目标位置从世界坐标转换为相对于 moveEff 父节点的本地坐标
        Vector3 endWorldPos = dstRect.position;
        var hud = dstObj.GetComponentInParent<UIGoldTreeGame>();
        if (hud != null)
            endWorldPos = hud.GetShownIconWorldPos(dstObj);
        Vector3 endLocalPos = moveEffRect.parent.InverseTransformPoint(endWorldPos);

        // 创建贝塞尔曲线控制点（偏右的弧形曲线）
        Vector3 midPoint = Vector3.Lerp(startPos, endLocalPos, 0.3f);
        Vector3 controlPoint = midPoint + Vector3.up * 20f + Vector3.right * 140f;


        float distance = Vector3.Distance(startPos, endLocalPos);

        // 动画时长
        float duration = distance / MoveSpeed;

        // 创建动画序列
        DG.Tweening.Sequence moveSequence = DOTween.Sequence();

        // 同时执行旋转和移动动画（生成后立即飞出）
        Vector3 startWorldPos = moveEffRect.position;
        Vector3 controlWorldPos = moveEffRect.parent.TransformPoint(controlPoint);

        Vector3[] worldPath = new Vector3[]
        {
            startWorldPos,
            controlWorldPos,
            endWorldPos
        };

        float targetRot = 720f;
        if (distance < 500)
        {
            targetRot = 360f;
        }

        moveSequence.Append(moveEffRect.DORotate(startRotation + Vector3.forward * targetRot, duration, DG.Tweening.RotateMode.FastBeyond360)
            .SetEase(DG.Tweening.Ease.Linear));

        moveSequence.Join(moveEffRect.DOPath(worldPath, duration, DG.Tweening.PathType.CatmullRom)
            .SetEase(DG.Tweening.Ease.Linear)
            .OnComplete(() =>
            {
                TileGameCore.AudioController.PlaySound(
                    TileGameCore.AudioController.AudioClips.goldenTileCollect, 1f, 1f);
            }));

        // 缩小动画在下半程开始（动画进行到一半时开始缩小）

        float scaleTimePer = 0.6f;
        moveSequence.Insert(duration * scaleTimePer, moveEffRect.DOScale(0.5f, duration * (1 - scaleTimePer))
            .SetEase(DG.Tweening.Ease.InOutQuad));

        // // 2. 到达目标位置后，角度回到起点状态
        // moveSequence.Append(moveEffRect.DORotate(startRotation, 0.2f)
        //     .SetEase(DG.Tweening.Ease.OutQuad));

        // 3. 动画结束隐藏moveEff
        moveSequence.AppendCallback(() =>
        {
            moveEff.SetActive(false);
            // 将特效放回池子
            moveEffList.Add(moveEff);
        });

        // 4. 开始播放到达动画
        moveSequence.AppendCallback(() =>
        {
            if (fireCollectEvent)
            {
                GameEvent.Send((int)EventNameDef.GoldTreeCollected);
                PlayArriveEff();
            }

            onFullyComplete?.Invoke();
        });

        moveSequence.SetLink(moveEff, LinkBehaviour.KillOnDestroy);
        moveSequence.Play();
    }

    private void PlayArriveEff()
    {

        var arriveEff = instance.GetEffFromPool(2);

        arriveEff.SetParent(transform);
        arriveEff.SetActive(true);
        arriveEff.InitLocalTransform();

        // 使用 RectTransform 设置位置
        RectTransform arriveEffRect = arriveEff.GetComponent<RectTransform>();
        RectTransform dstRect = dstObj.GetComponent<RectTransform>();

        // 将目标位置从世界坐标转换为相对于 arriveEff 父节点的本地坐标
        Vector3 dstWorldPos = dstRect.position;
        Vector3 dstLocalPos = arriveEffRect.parent.InverseTransformPoint(dstWorldPos);
        arriveEffRect.anchoredPosition = dstLocalPos;

        var comp = arriveEff.GetComponent<UnityEngine.Animation>();
        // comp.Play();

        DG.Tweening.Sequence moveSequence = DOTween.Sequence();
        moveSequence.AppendInterval(1.4f);
        moveSequence.AppendCallback(() =>
        {
            arriveEff.SetActiveEx(false);
            arriveEffList.Add(arriveEff);
        });
        moveSequence.SetLink(arriveEff, LinkBehaviour.KillOnDestroy);
        moveSequence.Play();
    }

    public static void ExcuteMoveEff(TileActor tile, bool isX2 = false)
    {
        if (instance == null)
        {
            Debug.LogError("[GoldTree] GoldTreeEffMgr instance is null. Add GoldTreeEffectNode to UI Game prefab.");
            GameEvent.Send((int)EventNameDef.GoldTreeCollected);
            return;
        }

        if (instance.dstObj == null)
        {
            instance.EnsureDstObj();
            if (instance.dstObj == null)
            {
                Debug.LogError("[GoldTree] Cannot play collect fly effect: dstObj is null.");
                GameEvent.Send((int)EventNameDef.GoldTreeCollected);
                return;
            }
        }

        UIController.GetPage<UIGame>()?.ShowGoldTreeForCollect(isX2 ? 2 : 1);

        Vector3 startWorldPos = tile.transform.position;
        if (isX2)
        {
            // 第一次：CollectFlyStartDelay 后才生成并飞出；第二次：第一次飞出 X2SecondMoveInterval 后再生成并飞出。
            instance.PlayMoveEff(tile, true, startWorldPos, null, NormalMoveStartDelay);
            DOVirtual.DelayedCall(NormalMoveStartDelay + X2SecondMoveInterval, () =>
            {
                if (instance != null)
                {
                    instance.PlayMoveEff(tile, true, startWorldPos, null, 0f);
                }
            }).SetLink(instance.gameObject);
        }
        else
        {
            instance.PlayMoveEff(tile);
        }
    }

    public static void ExcuteArriveEff()
    {
        if (instance != null)
        {
            instance.PlayArriveEff();
        }
    }


}
