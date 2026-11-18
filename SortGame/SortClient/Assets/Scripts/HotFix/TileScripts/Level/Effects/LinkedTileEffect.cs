using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using TileGameCore;
using System;

namespace FishTilePlay
{
    public class LinkedTileEffect : TileEffect
    {
        [SerializeField] SpriteRenderer linkSpriteRenderer;
        [SerializeField] Particle pickUpParticle;

        private TileActor secondLinkedTile;

        private static int particleHash;

        public override void Init()
        {
            particleHash = ParticlesDirector.RegisterParticle(pickUpParticle);
        }

        public override void OnCreated(TileActor tileBehavior)
        {
            if (tileBehavior == secondLinkedTile)
                return;

            List<TileActor> neighbourTiles = LevelDirector.GetNeighbourTiles(linkedTile.ElementPosition);

            if (neighbourTiles.IsNullOrEmpty())
            {
                DisableEffect();

                return;
            }

            secondLinkedTile = neighbourTiles.GetRandomItem();
            secondLinkedTile.ApplyEffect(this);

            Vector3 direction = secondLinkedTile.transform.position - linkedTile.transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Round the angle to the nearest multiple of 90
            float roundedAngle = Mathf.Round(angle / 90) * -90;

            linkSpriteRenderer.transform.rotation = Quaternion.Euler(0, 0, roundedAngle);
            linkSpriteRenderer.transform.position = Vector3.Lerp(secondLinkedTile.IconSpriteRenderer.transform.position, linkedTile.IconSpriteRenderer.transform.position, 0.5f);

            linkSpriteRenderer.sortingOrder = Mathf.Max(secondLinkedTile.IconSpriteRenderer.sortingOrder, linkedTile.IconSpriteRenderer.sortingOrder) + 1;

            linkSpriteRenderer.transform.localScale = Vector3.zero;
            linkSpriteRenderer.transform.DOScale(1.0f, 0.3f).SetEase(DG.Tweening.Ease.OutBack);
        }

        public override void OnSortingOrderChanged(int order)
        {
            if (secondLinkedTile != null && linkedTile != null)
            {
                linkSpriteRenderer.sortingOrder = Mathf.Max(secondLinkedTile.IconSpriteRenderer.sortingOrder, linkedTile.IconSpriteRenderer.sortingOrder) + 1;
            }
        }

        public override void OnDisabled(TileActor tileBehavior)
        {
            if (tileBehavior == linkedTile)
            {
                if (secondLinkedTile != null)
                {
                    secondLinkedTile.DisableEffect();
                }
            }
            else
            {
                if (linkedTile != null)
                {
                    linkedTile.DisableEffect();
                }
            }
        }

        public override bool BeforeTileSubmitted()
        {
            if (LevelDirector.Dock.CountTiles(linkedTile) == 2)
            {
                LevelDirector.SubmitElement(linkedTile);

                return false;
            }
            else if (LevelDirector.Dock.CountTiles(secondLinkedTile) == 2)
            {
                LevelDirector.SubmitElement(secondLinkedTile);

                return false;
            }


            return true;
        }

        public override void OnTileSubmitted()
        {
            if (!linkedTile.IsSubmitted)
            {
                if (particleHash != -1)
                    ParticlesDirector.PlayParticle(particleHash).SetPosition(linkedTile.transform.position);

                TileGameCore.Tween.NextFrame(() =>
                {
                    if (GameDirector.IsGameActive)
                    {
                        LevelDirector.SubmitElement(linkedTile);
                    }
                });
            }

            if (!secondLinkedTile.IsSubmitted)
            {
                if (particleHash != -1)
                    ParticlesDirector.PlayParticle(particleHash).SetPosition(secondLinkedTile.transform.position);

                TileGameCore.Tween.NextFrame(() =>
                {
                    if (GameDirector.IsGameActive)
                    {
                        LevelDirector.SubmitElement(secondLinkedTile);
                    }
                });
            }

            linkedTile.DisableEffect();

            AudioController.PlaySound(AudioController.AudioClips.rubberSound, volumePercentage: 0.5f);
        }

        public override bool IsClickAllowed()
        {
            return linkedTile.IsClickable && secondLinkedTile.IsClickable;
        }

        /// <summary>
        /// 获取Link效果的序列化数据
        /// </summary>
        /// <returns>包含连接信息的JSON数据</returns>
        public override string GetEffectData()
        {
            if (secondLinkedTile == null) return "";

            var data = new LinkEffectModel
            {
                secondLinkedTileLayer = secondLinkedTile.ElementPosition.LayerId,
                secondLinkedTileX = secondLinkedTile.ElementPosition.X,
                secondLinkedTileY = secondLinkedTile.ElementPosition.Y
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
            var allTiles = LevelDirector.LevelGenService.ActTiles;
            foreach (var tile in allTiles)
            {
                if (tile.ElementPosition.LayerId == effectData.secondLinkedTileLayer &&
                    tile.ElementPosition.X == effectData.secondLinkedTileX &&
                    tile.ElementPosition.Y == effectData.secondLinkedTileY)
                {
                    secondLinkedTile = tile;
                    break;
                }
            }

            if (secondLinkedTile != null)
            {
                // 重新设置连接视觉效果
                Vector3 direction = secondLinkedTile.transform.position - linkedTile.transform.position;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float roundedAngle = Mathf.Round(angle / 90) * -90;

                linkSpriteRenderer.transform.rotation = Quaternion.Euler(0, 0, roundedAngle);
                linkSpriteRenderer.transform.position = Vector3.Lerp(secondLinkedTile.IconSpriteRenderer.transform.position, linkedTile.IconSpriteRenderer.transform.position, 0.5f);
                linkSpriteRenderer.sortingOrder = Mathf.Max(secondLinkedTile.IconSpriteRenderer.sortingOrder, linkedTile.IconSpriteRenderer.sortingOrder) + 1;
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
                    TileGameCore.Tween.DelayedCall(0.1f, () =>
                    {
                        if (linkedTile != null && linkedTile.Effect != null)
                        {
                            RestoreLinkConnection(effectData);
                        }
                    });
                }
                else
                {
                    // 超过最大重试次数，记录错误并禁用效果
                    Debug.LogWarning($"Failed to restore Link connection after {maxRetryCount} retries. Target tile at ({effectData.secondLinkedTileLayer}, {effectData.secondLinkedTileX}, {effectData.secondLinkedTileY}) not found.");
                    DisableEffect();
                }
            }
        }

        [System.Serializable]
        private struct LinkEffectModel
        {
            public int secondLinkedTileLayer;
            public int secondLinkedTileX;
            public int secondLinkedTileY;
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