using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using XSortGameCore;
using System;

namespace FishXSortPlay
{
    public class LinkedXSortEffect : XSortEffect
    {
        [SerializeField] SpriteRenderer linkSpriteRenderer;
        [SerializeField] Particle pickUpParticle;

        private XSortActor secondLinkedXSort;

        private static int particleHash;

        public override void Init()
        {
            particleHash = ParticlesDirector.RegisterParticle(pickUpParticle);
        }

        public override void OnCreated(XSortActor XSortBehavior)
        {
            if (XSortBehavior == secondLinkedXSort)
                return;

            List<XSortActor> neighbourXSorts = LevelDirector.GetNeighbourXSorts(linkedXSort.ElementPosition);

            if (neighbourXSorts.IsNullOrEmpty())
            {
                DisableEffect();

                return;
            }

            secondLinkedXSort = neighbourXSorts.GetRandomItem();
            secondLinkedXSort.ApplyEffect(this);

            Vector3 direction = secondLinkedXSort.transform.position - linkedXSort.transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Round the angle to the nearest multiple of 90
            float roundedAngle = Mathf.Round(angle / 90) * -90;

            linkSpriteRenderer.transform.rotation = Quaternion.Euler(0, 0, roundedAngle);
            linkSpriteRenderer.transform.position = Vector3.Lerp(secondLinkedXSort.IconSpriteRenderer.transform.position, linkedXSort.IconSpriteRenderer.transform.position, 0.5f);

            linkSpriteRenderer.sortingOrder = Mathf.Max(secondLinkedXSort.IconSpriteRenderer.sortingOrder, linkedXSort.IconSpriteRenderer.sortingOrder) + 1;

            linkSpriteRenderer.transform.localScale = Vector3.zero;
            linkSpriteRenderer.transform.DOScale(1.0f, 0.3f).SetEase(DG.Tweening.Ease.OutBack);
        }

        public override void OnSortingOrderChanged(int order)
        {
            if (secondLinkedXSort != null && linkedXSort != null)
            {
                linkSpriteRenderer.sortingOrder = Mathf.Max(secondLinkedXSort.IconSpriteRenderer.sortingOrder, linkedXSort.IconSpriteRenderer.sortingOrder) + 1;
            }
        }

        public override void OnDisabled(XSortActor XSortBehavior)
        {
            if (XSortBehavior == linkedXSort)
            {
                if (secondLinkedXSort != null)
                {
                    secondLinkedXSort.DisableEffect();
                }
            }
            else
            {
                if (linkedXSort != null)
                {
                    linkedXSort.DisableEffect();
                }
            }
        }

        public override bool BeforeXSortSubmitted()
        {
            if (LevelDirector.Dock.CountXSorts(linkedXSort) == 2)
            {
                LevelDirector.SubmitElement(linkedXSort);

                return false;
            }
            else if (LevelDirector.Dock.CountXSorts(secondLinkedXSort) == 2)
            {
                LevelDirector.SubmitElement(secondLinkedXSort);

                return false;
            }


            return true;
        }

        public override void OnXSortSubmitted()
        {
            if (!linkedXSort.IsSubmitted)
            {
                if (particleHash != -1)
                    ParticlesDirector.PlayParticle(particleHash).SetPosition(linkedXSort.transform.position);

                XSortGameCore.Tween.NextFrame(() =>
                {
                    if (GameDirector.IsGameActive)
                    {
                        LevelDirector.SubmitElement(linkedXSort);
                    }
                });
            }

            if (!secondLinkedXSort.IsSubmitted)
            {
                if (particleHash != -1)
                    ParticlesDirector.PlayParticle(particleHash).SetPosition(secondLinkedXSort.transform.position);

                XSortGameCore.Tween.NextFrame(() =>
                {
                    if (GameDirector.IsGameActive)
                    {
                        LevelDirector.SubmitElement(secondLinkedXSort);
                    }
                });
            }

            linkedXSort.DisableEffect();

            AudioController.PlaySound(AudioController.AudioClips.rubberSound, volumePercentage: 0.5f);
        }

        public override bool IsClickAllowed()
        {
            return linkedXSort.IsClickable && secondLinkedXSort.IsClickable;
        }

        /// <summary>
        /// 获取Link效果的序列化数据
        /// </summary>
        /// <returns>包含连接信息的JSON数据</returns>
        public override string GetEffectData()
        {
            if (secondLinkedXSort == null) return "";

            var data = new LinkEffectModel
            {
                secondLinkedXSortLayer = secondLinkedXSort.ElementPosition.LayerId,
                secondLinkedXSortX = secondLinkedXSort.ElementPosition.X,
                secondLinkedXSortY = secondLinkedXSort.ElementPosition.Y
            };
            return JsonUtility.ToJson(data);
        }

        /// <summary>
        /// 从序列化数据恢复Link效果状态
        /// </summary>
        /// <param name="data">包含连接信息的JSON数据</param>
        public override void SetEffectData(string data)
        {
            if (string.IsNullOrEmpty(data)) return;

            try
            {
                var effectData = JsonUtility.FromJson<LinkEffectModel>(data);
                RestoreLinkConnection(effectData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to restore Link effect data: {e.Message}");
            }
        }

        /// <summary>
        /// 强制重新建立连接关系（用于延迟处理）
        /// </summary>
        public void ForceRestoreConnection()
        {
            var effectData = GetEffectData();
            if (!string.IsNullOrEmpty(effectData))
            {
                try
                {
                    var data = JsonUtility.FromJson<LinkEffectModel>(effectData);
                    retryCount = 0; // 重置重试计数
                    RestoreLinkConnection(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to force restore Link connection: {e.Message}");
                }
            }
        }

        private int retryCount = 0;
        private const int maxRetryCount = 10;

        /// <summary>
        /// 恢复Link连接关系，如果目标棋子还未恢复则延迟重试
        /// </summary>
        private void RestoreLinkConnection(LinkEffectModel effectData)
        {
            // 查找第二个连接的棋子
            var allXSorts = LevelDirector.LevelGenService.ActXSorts;
            foreach (var XSort in allXSorts)
            {
                if (XSort.ElementPosition.LayerId == effectData.secondLinkedXSortLayer &&
                    XSort.ElementPosition.X == effectData.secondLinkedXSortX &&
                    XSort.ElementPosition.Y == effectData.secondLinkedXSortY)
                {
                    secondLinkedXSort = XSort;
                    break;
                }
            }

            if (secondLinkedXSort != null)
            {
                // 重新设置连接视觉效果
                Vector3 direction = secondLinkedXSort.transform.position - linkedXSort.transform.position;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float roundedAngle = Mathf.Round(angle / 90) * -90;

                linkSpriteRenderer.transform.rotation = Quaternion.Euler(0, 0, roundedAngle);
                linkSpriteRenderer.transform.position = Vector3.Lerp(secondLinkedXSort.IconSpriteRenderer.transform.position, linkedXSort.IconSpriteRenderer.transform.position, 0.5f);
                linkSpriteRenderer.sortingOrder = Mathf.Max(secondLinkedXSort.IconSpriteRenderer.sortingOrder, linkedXSort.IconSpriteRenderer.sortingOrder) + 1;
                linkSpriteRenderer.transform.localScale = Vector3.one;
                
                // 重置重试计数
                retryCount = 0;
            }
            else
            {
                // 如果目标棋子还未恢复，延迟重试
                retryCount++;
                if (retryCount <= maxRetryCount)
                {
                    XSortGameCore.Tween.DelayedCall(0.1f, () =>
                    {
                        if (linkedXSort != null && linkedXSort.Effect != null)
                        {
                            RestoreLinkConnection(effectData);
                        }
                    });
                }
                else
                {
                    // 超过最大重试次数，记录错误并禁用效果
                    Debug.LogWarning($"Failed to restore Link connection after {maxRetryCount} retries. Target XSort at ({effectData.secondLinkedXSortLayer}, {effectData.secondLinkedXSortX}, {effectData.secondLinkedXSortY}) not found.");
                    DisableEffect();
                }
            }
        }

        [System.Serializable]
        private struct LinkEffectModel
        {
            public int secondLinkedXSortLayer;
            public int secondLinkedXSortX;
            public int secondLinkedXSortY;
        }

        public override bool IsShuffleAllowed()
        {
            return false;
        }

        public override void Clear()
        {

        }
    }
}