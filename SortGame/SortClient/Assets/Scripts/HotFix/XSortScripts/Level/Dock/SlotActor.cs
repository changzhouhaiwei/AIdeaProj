using System.Collections;
using UnityEngine;
using XSortGameCore;
using DG.Tweening;
using System;
using FishFramework;

namespace FishXSortPlay
{
    public class SlotActor : MonoBehaviour //, ITappable
    {
        [SerializeField] Vector3 offset;

        [Space]
        [SerializeField] Transform visuals;
        [SerializeField] SpriteRenderer backImage;
        [SerializeField] SpriteRenderer outlineImage;
        // [SerializeField] bool isAdUnit; //移除七格广告开启功能.
        // [SerializeField] GameObject adIcon;

        private Transform SlotablePos { get; set; }

        public Vector3 Scale { get; private set; }
        public Vector3 Rotation { get; private set; }
        public Vector3 Position => transform.position;

        public SlotCase SlotCase { get; private set; }

        public bool IsOccupied => SlotCase != null;

        /// <summary> 接棋锚点世界坐标（与 SubmitMove/ShiftMove 目标一致）。 </summary>
        public Vector3 SlotAnchorWorldPosition => SlotablePos.position;

        private TweenClip colorCase;

        private int spriteSortOrder;

        private bool isTemp = false;
        private Sprite cachedDefaultBackSprite;

        // private bool hasAddEvent = false;

        Coroutine stickCoroutine;

        private static XSortGameModel XSortGameModel;

        private void Awake()
        {
            SlotablePos = new GameObject("Slotable Position").transform;
            SlotablePos.SetParent(transform);
            SlotablePos.position = transform.position + offset;
            // 为预置的slots设置默认的Scale，确保大小一致性
            Scale = Vector3.one * 0.84f; // 使用固定值0.8，与动态创建的slots保持一致
            // AddEvent();
        }

        void AddEvent()
        {
            // if (isAdUnit && !hasAddEvent)
            // {
            //     hasAddEvent = true;
            //     GameEvent.AddEventListener((int)EventNameDef.ADAddSlot7Suc, OnADAdd7SlotSuc);
            // }
        }
        
        void OnEnable()
        {
            AddEvent();
        }

        void OnDestroy()
        {
            // if (hasAddEvent)
            //     GameEvent.RemoveEventListener((int)EventNameDef.ADAddSlot7Suc, OnADAdd7SlotSuc);
        }

        public static SlotActor GetTempSlot(SlotActor lastSlot, SlotActor prevLastSlot)
        {
            var tempSlot = new GameObject("Temp Slot").AddComponent<SlotActor>();

            tempSlot.SlotablePos = new GameObject("Slotable Position").transform;
            tempSlot.SlotablePos.SetParent(tempSlot.transform);
            tempSlot.SlotablePos.position = lastSlot.SlotablePos.position + (lastSlot.SlotablePos.position - prevLastSlot.SlotablePos.position);
            tempSlot.Scale = Vector3.one;

            tempSlot.spriteSortOrder = lastSlot.spriteSortOrder + 5;

            tempSlot.isTemp = true;

            return tempSlot;
        }

        public void Init(int spriteSortOrder, Vector3 position, Vector3 scale)
        {
            transform.position = position;
            var smallerSize = scale.x > scale.y ? scale.y : scale.x;
            visuals.localScale = Vector3.one * smallerSize;

            // 使用传入的scale参数而不是transform.localScale，确保大小一致性
            Scale = scale;
            Rotation = transform.eulerAngles;

            ShowOrHideOutline(false);

            this.spriteSortOrder = spriteSortOrder * 5 + 10;

            // outlineImage.spriteSortOr
        }

        public void ApplySlotUi(Sprite spriteOrNull)
        {
            if (backImage == null)
            {
                return;
            }

            if (cachedDefaultBackSprite == null)
            {
                cachedDefaultBackSprite = backImage.sprite;
            }

            backImage.sprite = spriteOrNull != null ? spriteOrNull : cachedDefaultBackSprite;
        }

        public void Assign(SlotCase slotCase, bool instant = false)
        {
            SlotCase = slotCase;
            GameDirector.RecordLastPlayTime();
            if (isTemp)
            {
                SlotCase.SubmitMove(SlotablePos.position, Scale, Rotation, spriteSortOrder, instant);
            }
            else
            {
                SlotCase.SubmitMove(SlotablePos, Scale, Rotation, spriteSortOrder, instant);
            }
        }

        public void AssingFast(SlotCase slotCase)
        {
            SlotCase = slotCase;

            slotCase.Behavior.SetSortingOrder(spriteSortOrder);

            SlotCase.ShiftMove(SlotablePos);
        }

        public void AssingWithoutMove(SlotCase slotCase)
        {
            SlotCase = slotCase;

            slotCase.Behavior.SetSortingOrder(spriteSortOrder);
        }

        /// <summary>失败结算：把占槽棋子压回本槽位低排序，避免飞入/长按高排序盖住 UI。</summary>
        public void RestoreOccupiedXSortSorting()
        {
            if (SlotCase?.Behavior == null)
                return;

            SlotCase.Behavior.SetSortingOrder(spriteSortOrder);
        }

        public void Move()
        {
            if (SlotCase != null)
            {
                SlotCase.ShiftMove(SlotablePos);

                SlotCase.Behavior.SetSortingOrder(spriteSortOrder);
            }
        }

        public void ChangePosition(Vector3 newPosition)
        {
            if (SlotCase != null && !SlotCase.IsMoving)
            {
                stickCoroutine = StartCoroutine(StickToSlotCoroutine());
            }
            transform.DOMove(newPosition, 0.1f).OnComplete(() =>
            {
                if (SlotCase != null && !SlotCase.IsMoving)
                {
                    StopCoroutine(stickCoroutine);
                    SlotCase.Behavior.Transform.position = SlotablePos.position;
                }
            });

        }


        public void RefreshAdShow()
        {
        }

        private IEnumerator StickToSlotCoroutine()
        {
            while (true)
            {
                yield return null;
                SlotCase.Behavior.Transform.position = SlotablePos.position;
            }
        }

        public SlotCase RemoveSlot()
        {
            var slotCase = SlotCase;
            SlotCase = null;
            return slotCase;
        }

        public void ShowOrHideOutline(bool b)
        {
            outlineImage.SetActiveEx(b);
            // visuals.SetActiveEx(!b);
        }

        public void ChangeColor(Color color)
        {
            // if (outlineImage == null || backImage == null) return;

            // colorCase.KillActive();

            // outlineImage.color = color;
            // outlineImage.transform.localScale = Vector3.zero;
            // outlineImage.gameObject.SetActive(true);

            // colorCase = outlineImage.DOScale(1, 0.15f).OnComplete(() =>
            // {
            //     backImage.color = color;
            //     outlineImage.gameObject.SetActive(false);
            // });
        }

        public void RestoreColor(Color color)
        {
            // if (outlineImage == null || backImage == null) return;

            // colorCase.KillActive();

            // outlineImage.color = backImage.color;
            // outlineImage.transform.localScale = Vector3.one;
            // outlineImage.gameObject.SetActive(true);

            // backImage.color = color;

            // colorCase = outlineImage.DOScale(0, 0.15f).OnComplete(() =>
            // {
            //     outlineImage.gameObject.SetActive(false);
            // });
        }

        public void ClearColor()
        {
            // colorCase.KillActive();
            // if (backImage != null) backImage.color = Color.white;

            // if (outlineImage != null) outlineImage.gameObject.SetActive(false);
        }

        public void Clear()
        {
            ClearColor();
            if (SlotCase != null) SlotCase.Clear();
            SlotCase = null;
        }

        private void OnDrawGizmos()
        {

        }

        public void OnObjectClicked(out bool isValid)
        {
            isValid = false;
        }

        private string addSlot7DockKey = "AddSlot7";
        void PlayAd()
        {
#if UNITY_EDITOR
            GameEvent.Send((int)EventNameDef.ADAddSlot7Suc);
#else
            if (GameData.BFromStartScene && PlatformMgr.GetUseSDK())
            {
                GameBAData.ExcuteAdClick(addSlot7DockKey);
                if (!AdsPlayStrategy.CheckRewardAdsPlay(addSlot7DockKey))
                {
                    return;
                }

                UIController.ShowPage<UIAdsLoading>();
                GPSDKMgr.Instance.PlayRewardedAd(addSlot7DockKey, 7);
            }
#endif
        }


        void OnADAdd7SlotSuc()
        {
        }


        public void OnObjectReleased()
        {

        }

        public void PlayLongPresssAnim()
        {
        }

        public void RecoverLongPressAnim(bool bResetTran = true)
        {

        }
    }
}