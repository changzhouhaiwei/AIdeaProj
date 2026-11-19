using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishXSortPlay
{
    public class ImpackEffectPool : MonoBehaviour
    {
        [SerializeField] ParticleSystem effImpactOri;

        readonly List<GameObject> pooledInstances = new List<GameObject>();

        /// <summary> 胜利/失败等结算界面展示时抑制新的撞击特效（并隐藏已在播的实例）。 </summary>
        bool playbackLocked;

        void Awake()
        {
            if (effImpactOri != null)
                effImpactOri.gameObject.SetActive(false);
        }

        public void ResetPlaybackAfterSettlement()
        {
            playbackLocked = false;
        }

        public void HideAllForSettlement()
        {
            playbackLocked = true;
            StopAllCoroutines();
            if (effImpactOri != null)
                effImpactOri.gameObject.SetActive(false);
            for (int i = 0; i < pooledInstances.Count; i++)
            {
                var go = pooledInstances[i];
                if (go == null) continue;
                var ps = go.GetComponent<ParticleSystem>() ?? go.GetComponentInChildren<ParticleSystem>(true);
                if (ps != null)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Clear(true);
                }
                go.SetActive(false);
            }
        }

        /// <summary> 在指定世界坐标播放落地撞击特效；结束后隐藏并回收到池中，不足则 Instantiate。 </summary>
        public void PlayAt(Vector3 worldPosition)
        {
            if (playbackLocked || effImpactOri == null)
                return;

            var go = RentInstance();
            if (go == null)
                return;

            go.transform.position = worldPosition;

            var ps = go.GetComponent<ParticleSystem>() ?? go.GetComponentInChildren<ParticleSystem>(true);
            if (ps == null)
            {
                go.SetActive(false);
                return;
            }

            ps.Clear(true);
            ps.Simulate(0, true, true);
            ps.Play();

            StartCoroutine(ReturnWhenFinished(go, ps));
        }

        GameObject RentInstance()
        {
            for (int i = 0; i < pooledInstances.Count; i++)
            {
                var go = pooledInstances[i];
                if (go != null && !go.activeSelf)
                {
                    go.SetActive(true);
                    return go;
                }
            }

            var clone = Instantiate(effImpactOri.gameObject, transform);
            pooledInstances.Add(clone);
            clone.SetActive(true);
            return clone;
        }

        static IEnumerator ReturnWhenFinished(GameObject go, ParticleSystem ps)
        {
            yield return null;
            while (go != null && ps != null && ps.IsAlive(true))
                yield return null;

            if (go != null)
                go.SetActive(false);
        }
    }
}
