using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FishFramework
{
    /// <summary>
    /// 实用函数集。
    /// </summary>
    public static partial class Utility
    {
        /// <summary>
        /// 清除所有子节点
        /// </summary>
        public static void ClearChild(Transform go)
        {
            if (go == null) return;
            for (int i = go.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(go.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 清理内存
        /// </summary>
        public static void ClearMemory()
        {
            GC.Collect();
            Resources.UnloadUnusedAssets();
        }

        public static IEnumerator InvokeRealtimeCoroutine(Action action, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action?.Invoke();
        }

        public static T FindInParents<T>(GameObject go) where T : Component
        {
            if (go == null) return null;
            var comp = go.GetComponent<T>();

            if (comp != null)
                return comp;

            Transform t = go.transform.parent;
            while (t != null && comp == null)
            {
                comp = t.gameObject.GetComponent<T>();
                t = t.parent;
            }

            return comp;
        }


        // 判断LayerMask里面是否包含你想要的Layer
        public static bool IsInLayerMask(GameObject obj, LayerMask layerMask)
        {
            // 根据Layer数值进行移位获得用于运算的Mask值
            int objLayerMask = 1 << obj.layer;
            return (layerMask.value & objLayerMask) > 0;
        }

        public static List<int> GetRandomSequence(int n, int seed)
        {
            List<int> numbers = new List<int>();
            for (int i = 1; i <= n; i++)
                numbers.Add(i);

            System.Random rand = new System.Random(seed);

            // Fisher–Yates 洗牌算法
            for (int i = numbers.Count - 1; i > 0; i--)
            {
                int j = rand.Next(0, i + 1);
                (numbers[i], numbers[j]) = (numbers[j], numbers[i]);
            }

            return numbers;
        }


        public static void DGDelayedCall(float delayTime, Action action)
        {
            var oneSeq = DOTween.Sequence();
            oneSeq.AppendInterval(delayTime);
            oneSeq.AppendCallback(() =>
            {
                action?.Invoke();
            });
            oneSeq.OnComplete(() => oneSeq.Kill()); // 自动释放
        }

        /// <summary>
        /// 延迟调用，可以指定target用于后续终止动画
        /// </summary>
        public static void DGDelayedCall(float delayTime, Action action, Object target)
        {
            var oneSeq = DOTween.Sequence();
            if (target != null)
            {
                oneSeq.SetTarget(target); // 设置target，方便后续通过DOTween.Kill(target)终止
            }
            oneSeq.AppendInterval(delayTime);
            oneSeq.AppendCallback(() =>
            {
                action?.Invoke();
            });
            oneSeq.OnComplete(() => oneSeq.Kill()); // 自动释放
        }
    }
}