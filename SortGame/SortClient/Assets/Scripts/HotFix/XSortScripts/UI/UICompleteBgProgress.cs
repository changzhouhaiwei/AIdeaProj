using System;
using cfg.Game;
using Core.Tools.MethodExtension;
using DG.Tweening;
using FishFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishXSortPlay
{
    /// <summary>
    /// 结算页：展示当前背景段到下一张新背景解锁的进度（仅展示，满条无额外逻辑）。
    /// </summary>
    public class UICompleteBgProgress : GPViewBase
    {
        [SerializeField] Image barComp;
        [SerializeField] TextMeshProUGUI progressUILb;
        [SerializeField] GameObject proBgNode;
        [SerializeField] Image previewBgImage;

        public void ResetForDelayedPlay()
        {
            if (barComp != null)
            {
                DOTween.Kill(barComp);
            }

            SetVisible(false);
        }

        public void SetInitBar(Action onComplete = null)
        {
            // 与 UICompletePerNode 一致：非 StartScene 流程不强制隐藏，方便编辑器直进 GameScene 验证
            if (GPUserDataManager.Instance != null && GPUserDataManager.Instance.GetBInfinitMode())
            {
                SetVisible(false);
                onComplete?.Invoke();
                return;
            }

            if (!TryGetProgress(out int curCnt, out int maxCnt, out GameBgCfg nextCfg))
            {
                SetVisible(false);
                onComplete?.Invoke();
                return;
            }

            SetVisible(true);
            RefreshPreviewBg(nextCfg);

            if (barComp != null)
            {
                DOTween.Kill(barComp);
                barComp.fillAmount = maxCnt > 0 ? (curCnt - 1) / (float)maxCnt : 0f;
                float dstFa = maxCnt > 0 ? (float)curCnt / maxCnt : 0f;
                barComp.DOFillAmount(dstFa, 1.0f).OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }

            RefreshProgressLabel(curCnt, maxCnt);
        }

        bool TryGetProgress(out int curCnt, out int maxCnt, out GameBgCfg nextCfg)
        {
            curCnt = 0;
            maxCnt = 0;
            nextCfg = null;

            if (ConfigHelper.Tables?.TbGameBgShow == null)
            {
                return false;
            }

            var list = ConfigHelper.Tables.TbGameBgShow.DataList;
            if (list == null || list.Count == 0)
            {
                return false;
            }

            // 关卡 ID 从 1 开始，与 game_tbgamebgshow.startLevel 对齐
            int levelId = GameDirector.CompletedLevelIndex() + 1;

            GameBgCfg currentCfg = null;
            int currentIdx = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var cfg = list[i];
                if (levelId >= cfg.StartLevel && levelId <= cfg.EndLevel)
                {
                    currentCfg = cfg;
                    currentIdx = i;
                    break;
                }
            }

            if (currentCfg == null || currentIdx < 0 || currentIdx >= list.Count - 1)
            {
                return false;
            }

            nextCfg = list[currentIdx + 1];
            maxCnt = nextCfg.StartLevel - currentCfg.StartLevel;
            if (maxCnt <= 0)
            {
                return false;
            }

            curCnt = levelId - currentCfg.StartLevel + 1;
            curCnt = Mathf.Clamp(curCnt, 0, maxCnt);
            return true;
        }

        void RefreshPreviewBg(GameBgCfg nextCfg)
        {
            if (previewBgImage == null || nextCfg == null)
            {
                return;
            }

            previewBgImage.sprite = LoadBgSprite("SceneBg", "bg_" + nextCfg.Path);
        }

        void RefreshProgressLabel(int current, int total)
        {
            if (progressUILb == null)
            {
                return;
            }

            progressUILb.SetActiveEx(true);
            progressUILb.text = $"{current}/{total}";
        }

        void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (proBgNode != null)
            {
                proBgNode.SetActive(visible);
            }
        }
    }
}
