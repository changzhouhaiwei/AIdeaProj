using System;
using System.Collections.Generic;
using DG.Tweening;
using FishFramework;
using Spine.Unity;
using TileGameCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FishTilePlay
{
    public sealed class AwardColorEggCoverLotteryArgs
    {
        public AwardColorEggCoverReward[] Rewards;
        public int PopupType;
        public Action OnFinished;
    }

    public class UIAwardColorEggCoverLottery : UIPanel, IUIOpen<AwardColorEggCoverLotteryArgs>
    {
        public const string PrefabAssetPath = "Assets/CABRes/Prefabs/AwardColorEggCover/UIAwardColorEggCoverLottery.prefab";
        const int RewardAdType = 8;
        const string AdPlace = "AwardColorEggCoverPickAll";

        public override PanelLayer Layer { get; set; }
        protected override string PrefabPath => PrefabAssetPath;

        sealed class EggSlot
        {
            public Transform Root;
            public GameObject Whole;
            public GameObject Opened;
            public Image Item;
            public TextMeshProUGUI Count;
            public GameObject Lucky;
            public RectTransform WholeRect;
            public Transform GlowSet;
            public GameObject Glow;
            public Transform CelebrateSet;
            public GameObject Celebrate;
            public DG.Tweening.Tween IdleTween;
            public bool Idling;
        }

        readonly List<Button> eggButtons = new();
        readonly List<int> eggButtonSlots = new();
        readonly List<EggSlot> slots = new();
        AwardColorEggCoverReward[] rewards;
        Action onFinished;
        int popupType;
        int choiceState;
        const string HammerHitAnimation = "Hammer_Hit";
        const float HammerImpactTime = 0.73f;
        const float BrokenSoundDelay = 0.4f;
        const float HammerFlySpeed = 680f;
        const float HammerHoverGap = -20f;
        const float HammerLandDrop = 120f;
        RectTransform hammer;
        Vector3 hammerScale;
        Vector3 hammerIdleEuler;
        float hammerReach;
        SkeletonGraphic hammerSpine;
        Spine.TrackEntry hammerHit;
        GameObject glowTemplate;
        GameObject celebrateTemplate;
        const float OtherEggRevealDelay = 0.7f;
        const float ButtonPopDuration = 0.22f;
        const float ButtonSettleDuration = 0.12f;
        const float RewardDepartRise = 200f;
        const float RewardDepartDuration = 0.85f;
        readonly List<GameObject> rewardClones = new();
        DG.Tweening.Tween hammerTween;
        DG.Tweening.Tween brokenSoundTween;
        DG.Tweening.Tween otherRevealTween;
        DG.Tweening.Tween lockFadeTween;
        DG.Tweening.Tween rewardDepart;
        Button claimButton;
        Button pickAllButton;
        GameObject claimLock;
        GameObject pickAllLock;
        int selected = -1;
        bool broken;
        bool revealed;
        bool buttonsReady;
        bool buttonScalesCached;
        Vector3 claimButtonScale = Vector3.one;
        Vector3 pickAllButtonScale = Vector3.one;
        bool finished;
        bool waitingForAd;

        public static void Show(
            AwardColorEggCoverReward[] rewardList,
            int popupType,
            Action onFinished)
        {
            GameModule.UI.OpenPanel<UIAwardColorEggCoverLottery, AwardColorEggCoverLotteryArgs>(
                new AwardColorEggCoverLotteryArgs
                {
                    Rewards = rewardList,
                    PopupType = popupType,
                    OnFinished = onFinished,
                });
        }

        protected override void OnBindCompsAndEvents()
        {
            Transform content = panelBehaviour.transform.Find("Content");
            if (content == null)
                content = panelBehaviour.transform;

            claimButton = FindButton(content, "Claim");
            pickAllButton = FindButton(content, "PickAll");
            claimLock = FindLock(claimButton);
            pickAllLock = FindLock(pickAllButton);
            BindEvent(claimButton);
            BindEvent(pickAllButton);

            for (int i = 0; i < content.childCount; i++)
                CollectEgg(content.GetChild(i));

            BindHammer(content);
            glowTemplate = FindInactiveTemplate(content, "Glow_Eff");
            celebrateTemplate = FindInactiveTemplate(content, "Celebrate_Eff");
        }

        protected override void OnUnbindCompsAndEvents()
        {
            hammerTween?.Kill();
            brokenSoundTween?.Kill();
            brokenSoundTween = null;
            ClearHammerTrack();
            otherRevealTween?.Kill();
            lockFadeTween?.Kill();
            rewardDepart?.Kill();
            rewardDepart = null;
            ClearRewardClones();
            UnhookAd();
            UnbindEvent(claimButton);
            UnbindEvent(pickAllButton);
            for (int i = 0; i < slots.Count; i++)
                StopEggIdle(slots[i]);
            for (int i = 0; i < eggButtons.Count; i++)
                UnbindEvent(eggButtons[i]);
            eggButtons.Clear();
            eggButtonSlots.Clear();
            slots.Clear();
            claimButton = null;
            pickAllButton = null;
            claimLock = null;
            pickAllLock = null;
            glowTemplate = null;
            celebrateTemplate = null;
            hammerSpine = null;
            hammer = null;
        }

        public void OnOpen(AwardColorEggCoverLotteryArgs args)
        {
            rewards = args != null ? args.Rewards : null;
            onFinished = args != null ? args.OnFinished : null;
            popupType = args != null ? args.PopupType : 0;
            choiceState = 0;
            selected = -1;
            broken = false;
            revealed = false;
            buttonsReady = false;
            finished = false;
            waitingForAd = false;
            HideActionButtons();
            for (int i = 0; i < slots.Count; i++)
            {
                ResetSlot(slots[i], RewardAt(i));
                BeginEggIdle(slots[i], 1.4f + i * 1.2f);
            }
            BeginWander();
            ReportEggSmash(0, AwardColorEggCoverPending.FormatAllRewardIds(), 0);
        }

        protected override void OnClicked(Button button)
        {
            if (button == claimButton)
            {
                PlayButtonClick();
                ClaimSelected();
                return;
            }
            if (button == pickAllButton)
            {
                PlayButtonClick();
                PickAll();
                return;
            }
            for (int i = 0; i < eggButtons.Count; i++)
            {
                if (button == eggButtons[i])
                {
                    SelectEgg(eggButtonSlots[i]);
                    return;
                }
            }
        }

        static void PlayButtonClick()
        {
            if (AudioController.AudioClips != null && AudioController.AudioClips.buttonSound != null)
                AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }

        protected override void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
        {
        }

        protected override bool OnEscButtonPressed()
        {
            return true;
        }

        void ResetSlot(EggSlot slot, AwardColorEggCoverReward reward)
        {
            if (slot.Whole != null)
                slot.Whole.SetActive(true);
            if (slot.Opened != null)
                slot.Opened.SetActive(false);
            ClearSlotEffects(slot);
            if (slot.Item != null)
            {
                Sprite icon = LoadRewardSprite(reward);
                if (icon != null)
                    slot.Item.sprite = icon;
            }
            if (slot.Count != null)
            {
                slot.Count.text = reward.Count > 0 ? reward.CountLabel : "";
                slot.Count.gameObject.SetActive(false);
            }
            if (slot.Lucky != null)
                slot.Lucky.SetActive(false);
            StopEggIdle(slot);
            if (slot.WholeRect != null)
            {
                slot.WholeRect.DOKill();
                slot.WholeRect.localScale = Vector3.one;
            }
        }

        AwardColorEggCoverReward RewardAt(int index)
        {
            if (rewards != null && index >= 0 && index < rewards.Length)
                return rewards[index];
            return default;
        }

        void SelectEgg(int index)
        {
            if (finished || broken || index < 0 || index >= slots.Count)
                return;
            selected = index;
            broken = true;
            ReportEggSmash(1, AwardColorEggCoverPending.FormatRewardId(index, false), 0);
            PlaySmash(index);
        }

        void BeginWander()
        {
            if (hammer == null || slots.Count == 0)
                return;
            hammerTween?.Kill();
            ClearHammerTrack();
            hammer.gameObject.SetActive(true);
            hammer.SetAsLastSibling();
            hammer.localScale = hammerScale;
            hammer.localEulerAngles = hammerIdleEuler;
            HoldHammerPose();
            float shift = HammerHeadShiftX();
            float left = HammerAnchoredFor(EggCenterWorld(LeftEgg())).x + shift;
            float right = HammerAnchoredFor(EggCenterWorld(RightEgg())).x + shift + 100f;
            float y = HammerAnchoredFor(EggTopWorld(LeftEgg())).y + HammerHoverGap;
            hammer.anchoredPosition = new Vector2((left + right) * 0.5f, y);
            Sequence swing = DOTween.Sequence().SetUpdate(true).SetLink(hammer.gameObject);
            swing.Append(hammer.DOAnchorPosX(right, 0.73f).SetEase(Ease.InOutSine));
            swing.Append(hammer.DOAnchorPosX(left, 1.45f).SetEase(Ease.InOutSine));
            swing.SetLoops(-1, LoopType.Yoyo);
            hammerTween = swing;
        }

        RectTransform LeftEgg()
        {
            RectTransform left = slots[0].Root as RectTransform;
            for (int i = 1; i < slots.Count; i++)
            {
                RectTransform egg = slots[i].Root as RectTransform;
                if (egg.anchoredPosition.x < left.anchoredPosition.x)
                    left = egg;
            }
            return left;
        }

        RectTransform RightEgg()
        {
            RectTransform right = slots[0].Root as RectTransform;
            for (int i = 1; i < slots.Count; i++)
            {
                RectTransform egg = slots[i].Root as RectTransform;
                if (egg.anchoredPosition.x > right.anchoredPosition.x)
                    right = egg;
            }
            return right;
        }

        void PlaySmash(int index)
        {
            if (hammer == null || hammerSpine == null)
            {
                PlayBrokenSound();
                FinishSmash(index);
                return;
            }

            hammerTween?.Kill();
            ClearHammerTrack();
            hammer.gameObject.SetActive(true);
            hammer.SetAsLastSibling();
            hammer.localScale = hammerScale;
            HoldHammerPose();
            CacheHammerReach();
            RectTransform egg = slots[index].Root as RectTransform;
            Vector2 land = HammerAnchoredFor(EggTopWorld(egg));
            land.y -= hammerReach + HammerLandDrop;
            float duration = Mathf.Clamp(Vector2.Distance(hammer.anchoredPosition, land) / HammerFlySpeed, 0.3f, 0.72f);
            Sequence smash = DOTween.Sequence().SetUpdate(true).SetLink(hammer.gameObject);
            smash.Append(hammer.DOAnchorPos(land, duration).SetEase(Ease.InOutSine));
            smash.Join(hammer.DOLocalRotate(Vector3.zero, duration).SetEase(Ease.InOutSine));
            smash.AppendCallback(PlayHammerHit);
            smash.AppendInterval(HammerImpactTime);
            smash.AppendCallback(() => FinishSmash(index));
            hammerTween = smash;
        }

        void BindHammer(Transform content)
        {
            Transform search = content != null ? content : panelBehaviour.transform;
            SkeletonGraphic[] graphics = search.GetComponentsInChildren<SkeletonGraphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i].name.IndexOf("Hammer", StringComparison.Ordinal) < 0)
                    continue;
                hammerSpine = graphics[i];
                break;
            }
            if (hammerSpine == null && content != null)
            {
                graphics = panelBehaviour.transform.GetComponentsInChildren<SkeletonGraphic>(true);
                for (int i = 0; i < graphics.Length; i++)
                {
                    if (graphics[i].name.IndexOf("Hammer", StringComparison.Ordinal) < 0)
                        continue;
                    hammerSpine = graphics[i];
                    break;
                }
            }
            hammer = hammerSpine != null ? hammerSpine.rectTransform : null;
            if (hammer == null)
                return;
            hammerScale = hammer.localScale;
            hammerIdleEuler = hammer.localEulerAngles;
            hammerSpine.raycastTarget = false;
            HoldHammerPose();
        }

        Vector3 EggCenterWorld(RectTransform egg)
        {
            return egg.TransformPoint(egg.rect.center);
        }

        Vector3 EggTopWorld(RectTransform egg)
        {
            return egg.TransformPoint(new Vector3(egg.rect.center.x, egg.rect.yMax, 0f));
        }

        float HammerHeadShiftX()
        {
            if (hammer == null)
                return 0f;
            float headY = 114f * Mathf.Abs(hammer.localScale.y);
            if (hammerSpine != null)
            {
                Mesh mesh = hammerSpine.GetLastMesh();
                if (mesh != null && mesh.vertexCount > 0)
                    headY = mesh.bounds.max.y * 0.72f * Mathf.Abs(hammer.localScale.y);
            }
            float z = hammerIdleEuler.z;
            if (z > 180f)
                z -= 360f;
            return headY * Mathf.Sin(z * Mathf.Deg2Rad);
        }

        Vector2 HammerAnchoredFor(Vector3 world)
        {
            RectTransform parent = hammer.parent as RectTransform;
            Vector3 local = parent.InverseTransformPoint(world);
            Rect rect = parent.rect;
            Vector2 anchor = (hammer.anchorMin + hammer.anchorMax) * 0.5f;
            Vector2 anchorInParent = rect.min + Vector2.Scale(rect.size, anchor);
            return (Vector2)local - anchorInParent;
        }

        void CacheHammerReach()
        {
            hammerReach = 0f;
            if (hammerSpine == null || hammer == null)
                return;
            Mesh mesh = hammerSpine.GetLastMesh();
            if (mesh == null || mesh.vertexCount == 0)
                return;
            hammerReach = mesh.bounds.max.y * Mathf.Abs(hammer.localScale.y);
        }

        void HoldHammerPose()
        {
            if (hammerSpine == null)
                return;
            hammerSpine.UnscaledTime = true;
            hammerSpine.Initialize(false);
            if (hammerSpine.AnimationState != null)
                hammerSpine.AnimationState.ClearTracks();
            if (hammerSpine.Skeleton != null)
            {
                hammerSpine.Skeleton.SetToSetupPose();
                hammerSpine.Update(0f);
            }
        }

        void PlayHammerHit()
        {
            PlayBrokenSound();
            if (hammer != null)
                hammer.localRotation = Quaternion.identity;
            if (hammerSpine == null || hammerSpine.AnimationState == null)
                return;
            hammerSpine.UnscaledTime = true;
            if (hammerSpine.Skeleton == null || hammerSpine.Skeleton.Data.FindAnimation(HammerHitAnimation) == null)
                return;
            hammerSpine.AnimationState.ClearTracks();
            hammerHit = hammerSpine.AnimationState.SetAnimation(0, HammerHitAnimation, false);
            if (hammerHit != null)
                hammerHit.Complete += OnHammerHitComplete;
            hammerSpine.Update(0f);
        }

        void OnHammerHitComplete(Spine.TrackEntry entry)
        {
            hammerHit = null;
            if (hammer != null)
                hammer.gameObject.SetActive(false);
        }

        void ClearHammerTrack()
        {
            if (hammerHit == null)
                return;
            hammerHit.Complete -= OnHammerHitComplete;
            hammerHit = null;
        }

        void HideHammer()
        {
            ClearHammerTrack();
            if (hammerSpine != null && hammerSpine.AnimationState != null)
                hammerSpine.AnimationState.ClearTracks();
            if (hammer != null)
                hammer.gameObject.SetActive(false);
        }

        void FinishSmash(int index)
        {
            if (finished || index < 0 || index >= slots.Count)
                return;
            PlayBreakAnimation(slots[index]);
            int picked = index;
            otherRevealTween?.Kill();
            otherRevealTween = DOVirtual.DelayedCall(OtherEggRevealDelay, () =>
            {
                if (finished)
                    return;
                for (int i = 0; i < slots.Count; i++)
                {
                    if (i != picked)
                        Reveal(slots[i]);
                }
                revealed = true;
                PopInActionButtons();
            }).SetUpdate(true);
        }

        void PlayBreakAnimation(EggSlot slot)
        {
            AttachSelectedGlow(slot);
            AttachSelectedCelebrate(slot);
            if (slot == null || slot.WholeRect == null)
            {
                Reveal(slot);
                return;
            }
            StopEggIdle(slot);
            RectTransform egg = slot.WholeRect;
            egg.localScale = Vector3.one;
            egg.DOScale(1.2f, 0.15f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(egg.gameObject)
                .OnComplete(() =>
                {
                    if (!finished)
                        Reveal(slot);
                });
        }

        void CollectEgg(Transform node)
        {
            if (node.Find("Egg_all") != null && node.Find("Egg_Set") != null)
            {
                EggSlot slot = new EggSlot
                {
                    Root = node,
                    Whole = node.Find("Egg_all").gameObject,
                    Opened = node.Find("Egg_Set").gameObject,
                    Item = FindImage(node, "Egg_Set/Egg_item"),
                    Count = FindText(node, "Count"),
                    Lucky = FindLucky(node),
                    WholeRect = node.Find("Egg_all") as RectTransform,
                    GlowSet = node.Find("Egg_Set/GlowSet"),
                    CelebrateSet = node.Find("Egg_Set/Celebrate_Set"),
                };
                PivotEggToBottom(slot.WholeRect);
                slots.Add(slot);
                Button[] buttons = node.GetComponentsInChildren<Button>(true);
                for (int b = 0; b < buttons.Length; b++)
                {
                    eggButtons.Add(buttons[b]);
                    eggButtonSlots.Add(slots.Count - 1);
                    BindEvent(buttons[b]);
                }
                return;
            }

            for (int i = 0; i < node.childCount; i++)
                CollectEgg(node.GetChild(i));
        }

        static GameObject FindInactiveTemplate(Transform content, string name)
        {
            Transform found = FindDeep(content, name);
            if (found == null)
                return null;
            found.gameObject.SetActive(false);
            return found.gameObject;
        }

        static Transform FindDeep(Transform node, string name)
        {
            if (node.name == name)
                return node;
            for (int i = 0; i < node.childCount; i++)
            {
                Transform found = FindDeep(node.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        void AttachSelectedGlow(EggSlot slot)
        {
            if (slot == null)
                return;
            slot.Glow = AttachEffect(glowTemplate, slot.GlowSet, slot.Glow);
        }

        void AttachSelectedCelebrate(EggSlot slot)
        {
            if (slot == null)
                return;
            slot.Celebrate = AttachEffect(celebrateTemplate, slot.CelebrateSet, slot.Celebrate);
        }

        static GameObject AttachEffect(GameObject template, Transform parent, GameObject current)
        {
            if (template == null || parent == null || current != null)
                return current;
            GameObject effect = UnityEngine.Object.Instantiate(template, parent, false);
            effect.name = template.name;
            effect.SetActive(true);
            Transform effectTransform = effect.transform;
            effectTransform.localPosition = Vector3.zero;
            effectTransform.localRotation = Quaternion.identity;
            effectTransform.localScale = template.transform.localScale;
            return effect;
        }

        void ClearSlotEffects(EggSlot slot)
        {
            if (slot == null)
                return;
            slot.Glow = ClearEffect(slot.GlowSet, slot.Glow, glowTemplate);
            slot.Celebrate = ClearEffect(slot.CelebrateSet, slot.Celebrate, celebrateTemplate);
        }

        static GameObject ClearEffect(Transform parent, GameObject instance, GameObject template)
        {
            if (instance != null)
                UnityEngine.Object.Destroy(instance);
            if (parent == null)
                return null;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.gameObject != template)
                    UnityEngine.Object.Destroy(child.gameObject);
            }
            return null;
        }

        void PivotEggToBottom(RectTransform egg)
        {
            if (egg == null || egg.pivot.y <= 0.01f)
                return;
            Vector2 size = egg.rect.size;
            if (size.y <= 0f)
                size = egg.sizeDelta;
            Vector2 center = egg.anchoredPosition + Vector2.Scale(new Vector2(0.5f, 0.5f) - egg.pivot, size);
            Vector2 pivot = new Vector2(egg.pivot.x, 0f);
            egg.pivot = pivot;
            egg.anchoredPosition = center - Vector2.Scale(new Vector2(0.5f, 0.5f) - pivot, size);
        }

        void BeginEggIdle(EggSlot slot, float firstPause)
        {
            StopEggIdle(slot);
            if (slot.WholeRect == null)
                return;
            slot.Idling = true;
            ScheduleEggIdle(slot, firstPause);
        }

        void ScheduleEggIdle(EggSlot slot, float pause)
        {
            if (slot.WholeRect == null)
                return;
            float tilt = UnityEngine.Random.Range(4.2f, 6.2f);
            float sign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            Sequence wobble = DOTween.Sequence().SetUpdate(true).SetLink(slot.Whole);
            wobble.AppendInterval(pause);
            wobble.Append(slot.WholeRect.DOLocalRotate(new Vector3(0f, 0f, sign * tilt), 0.14f).SetEase(Ease.OutSine));
            wobble.Append(slot.WholeRect.DOLocalRotate(new Vector3(0f, 0f, -sign * tilt * 0.75f), 0.22f).SetEase(Ease.InOutSine));
            wobble.Append(slot.WholeRect.DOLocalRotate(new Vector3(0f, 0f, sign * tilt * 0.3f), 0.16f).SetEase(Ease.InOutSine));
            wobble.Append(slot.WholeRect.DOLocalRotate(Vector3.zero, 0.22f).SetEase(Ease.OutSine));
            wobble.OnComplete(() =>
            {
                if (!slot.Idling)
                    return;
                ScheduleEggIdle(slot, UnityEngine.Random.Range(3.6f, 5.6f));
            });
            slot.IdleTween = wobble;
        }

        void StopEggIdle(EggSlot slot)
        {
            if (slot == null)
                return;
            slot.Idling = false;
            slot.IdleTween?.Kill();
            slot.IdleTween = null;
            if (slot.WholeRect != null)
                slot.WholeRect.localRotation = Quaternion.identity;
        }

        void Reveal(EggSlot slot)
        {
            if (slot == null)
                return;
            StopEggIdle(slot);
            if (slot.Whole != null)
                slot.Whole.SetActive(false);
            if (slot.Opened != null)
                slot.Opened.SetActive(true);
            AwardColorEggCoverReward reward = RewardAt(slots.IndexOf(slot));
            if (slot.Count != null)
                slot.Count.gameObject.SetActive(reward.Count > 0);
            if (slot.Lucky != null)
                slot.Lucky.SetActive(reward.Lucky);
        }

        void RevealAll()
        {
            hammerTween?.Kill();
            otherRevealTween?.Kill();
            HideHammer();
            for (int i = 0; i < slots.Count; i++)
                Reveal(slots[i]);
            revealed = true;
            broken = true;
        }

        void PlayBrokenSound()
        {
            brokenSoundTween?.Kill();
            brokenSoundTween = DOVirtual.DelayedCall(BrokenSoundDelay, () =>
            {
                brokenSoundTween = null;
                AudioClip clip = AudioController.AudioClips != null
                    ? AudioController.AudioClips.colorEggBroken
                    : null;
                if (clip != null)
                    AudioController.PlaySound(clip);
            }).SetUpdate(true);
        }

        static Sprite LoadRewardSprite(AwardColorEggCoverReward reward)
        {
            if (reward.ItemId <= 0 || reward.Count <= 0)
                return null;
            if (reward.ItemId == 4 && GameData.Shop != null)
            {
                return LoadSprite("ShopIcon", GameData.Shop.GetCoinSpritePathByCnt(reward.Count));
            }

            return LoadSprite("LevelAward", "item_" + reward.ItemId);
        }

        void ClaimSelected()
        {
            if (finished || selected < 0 || !buttonsReady)
                return;
            buttonsReady = false;
            SetButtonRaycasts(claimButton, false);
            SetButtonRaycasts(pickAllButton, false);
            choiceState = 0;
            AwardColorEggCoverPending.MarkGranted(selected, false);
            PlayRewardDepart(false);
        }

        void HideActionButtons()
        {
            buttonsReady = false;
            lockFadeTween?.Kill();
            lockFadeTween = null;
            CacheButtonScales();
            SetLockNode(claimLock, false);
            SetLockNode(pickAllLock, false);
            SetButtonHidden(claimButton);
            SetButtonHidden(pickAllButton);
        }

        void PopInActionButtons()
        {
            if (buttonsReady || finished)
                return;
            lockFadeTween?.Kill();
            CacheButtonScales();
            Sequence pop = DOTween.Sequence().SetUpdate(true);
            JoinButtonPop(pop, claimButton, claimButtonScale);
            JoinButtonPop(pop, pickAllButton, pickAllButtonScale);
            pop.OnComplete(() =>
            {
                if (finished)
                    return;
                buttonsReady = true;
                SetButtonShown(claimButton, claimButtonScale);
                SetButtonShown(pickAllButton, pickAllButtonScale);
                if (GameData.BFromStartScene && pickAllButton != null)
                    GameBAData.ExcuteAdImpress(AdPlace);
            });
            lockFadeTween = pop;
        }

        void CacheButtonScales()
        {
            if (buttonScalesCached)
                return;
            if (claimButton != null)
                claimButtonScale = claimButton.transform.localScale;
            if (pickAllButton != null)
                pickAllButtonScale = pickAllButton.transform.localScale;
            buttonScalesCached = claimButton != null || pickAllButton != null;
        }

        void SetButtonHidden(Button button)
        {
            if (button == null)
                return;
            button.interactable = true;
            button.transform.localScale = Vector3.zero;
            SetButtonRaycasts(button, false);
        }

        void SetButtonShown(Button button, Vector3 shownScale)
        {
            if (button == null)
                return;
            button.transform.localScale = shownScale;
            if (button != pickAllButton || !waitingForAd)
                button.interactable = true;
            SetButtonRaycasts(button, true);
        }

        static void JoinButtonPop(Sequence pop, Button button, Vector3 shownScale)
        {
            if (button == null)
                return;
            SetButtonRaycasts(button, false);
            Transform target = button.transform;
            target.localScale = Vector3.zero;
            pop.Join(target.DOScale(shownScale * 1.12f, ButtonPopDuration).SetEase(Ease.OutCubic));
            pop.Insert(ButtonPopDuration, target.DOScale(shownScale, ButtonSettleDuration).SetEase(Ease.InOutSine));
        }

        static void SetButtonRaycasts(Button button, bool enabled)
        {
            if (button == null)
                return;
            Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null || graphics[i].gameObject.name == "Lock")
                    continue;
                graphics[i].raycastTarget = enabled;
            }
        }

        static void SetLockNode(GameObject lockNode, bool visible)
        {
            if (lockNode != null)
                lockNode.SetActive(visible);
        }

        void PickAll()
        {
            if (finished || waitingForAd || !buttonsReady)
                return;
            if (GameData.BFromStartScene)
                GameBAData.ExcuteAdClick(AdPlace);
            if (!AdsPlayStrategy.CheckRewardAdsPlay(AdPlace))
                return;

#if UNITY_EDITOR
            GrantAll();
#else
            waitingForAd = true;
            if (pickAllButton != null)
                pickAllButton.interactable = false;
            GameEvent.AddEventListener((int)EventNameDef.AwardColorEggCoverPickAll, GrantAll);
            GPSDKCallbackHandler handler = GPSDKMgr.Instance.GetCallbackHandler();
            if (handler != null)
                handler.OnHideAdsLoading += OnAdClosed;
            UIController.ShowPage<UIAdsLoading>();
            GPSDKMgr.Instance.PlayRewardedAd(AdPlace, RewardAdType);
#endif
        }

        void OnAdClosed()
        {
            GPSDKCallbackHandler handler = GPSDKMgr.Instance != null
                ? GPSDKMgr.Instance.GetCallbackHandler()
                : null;
            if (handler != null)
                handler.OnHideAdsLoading -= OnAdClosed;
            if (finished || !waitingForAd)
                return;
            waitingForAd = false;
            if (pickAllButton != null && buttonsReady)
                pickAllButton.interactable = true;
        }

        void GrantAll()
        {
            if (finished || rewardDepart != null)
                return;
            waitingForAd = false;
            buttonsReady = false;
            SetButtonRaycasts(claimButton, false);
            SetButtonRaycasts(pickAllButton, false);
            GameEvent.RemoveEventListener((int)EventNameDef.AwardColorEggCoverPickAll, GrantAll);
            if (selected < 0)
                selected = 0;
            choiceState = 1;
            RevealAll();
            AwardColorEggCoverPending.MarkGranted(-1, true);
            PlayRewardDepart(true);
        }

        void PlayRewardDepart(bool all)
        {
            rewardDepart?.Kill();
            ClearRewardClones();
            if (panelBehaviour == null)
            {
                Finish();
                return;
            }

            Sequence fly = DOTween.Sequence().SetUpdate(true).SetLink(panelBehaviour.gameObject);
            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!all && i != selected)
                    continue;
                Image clone = CloneRewardIcon(slots[i]);
                TextMeshProUGUI countClone = CloneRewardCount(slots[i]);
                if (clone == null && countClone == null)
                    continue;
                count++;
                if (clone != null)
                {
                    RectTransform rect = clone.rectTransform;
                    fly.Join(rect.DOAnchorPosY(rect.anchoredPosition.y + RewardDepartRise, RewardDepartDuration)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true));
                    fly.Join(clone.DOFade(0f, RewardDepartDuration).SetEase(Ease.InQuad).SetUpdate(true));
                }
                if (countClone != null)
                {
                    RectTransform countRect = countClone.rectTransform;
                    fly.Join(countRect.DOAnchorPosY(countRect.anchoredPosition.y + RewardDepartRise, RewardDepartDuration)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true));
                    fly.Join(countClone.DOFade(0f, RewardDepartDuration).SetEase(Ease.InQuad).SetUpdate(true));
                }
            }

            if (count == 0)
            {
                Finish();
                return;
            }

            fly.OnComplete(() =>
            {
                rewardDepart = null;
                Finish();
            });
            rewardDepart = fly;
        }

        Image CloneRewardIcon(EggSlot slot)
        {
            Image source = slot != null ? slot.Item : null;
            if (source == null || source.sprite == null || panelBehaviour == null)
                return null;

            GameObject copy = UnityEngine.Object.Instantiate(source.gameObject, panelBehaviour.transform, true);
            copy.name = "RewardDepart";
            Image image = copy.GetComponent<Image>();
            if (image == null)
            {
                UnityEngine.Object.Destroy(copy);
                return null;
            }

            image.raycastTarget = false;
            copy.transform.SetAsLastSibling();
            rewardClones.Add(copy);
            return image;
        }

        TextMeshProUGUI CloneRewardCount(EggSlot slot)
        {
            TextMeshProUGUI source = slot != null ? slot.Count : null;
            if (source == null || !source.gameObject.activeInHierarchy || panelBehaviour == null)
                return null;
            if (string.IsNullOrEmpty(source.text))
                return null;

            GameObject copy = UnityEngine.Object.Instantiate(source.gameObject, panelBehaviour.transform, true);
            copy.name = "RewardDepartCount";
            TextMeshProUGUI text = copy.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                UnityEngine.Object.Destroy(copy);
                return null;
            }

            text.raycastTarget = false;
            copy.SetActive(true);
            copy.transform.SetAsLastSibling();
            rewardClones.Add(copy);
            return text;
        }

        void ClearRewardClones()
        {
            for (int i = 0; i < rewardClones.Count; i++)
            {
                if (rewardClones[i] != null)
                    UnityEngine.Object.Destroy(rewardClones[i]);
            }
            rewardClones.Clear();
        }

        void Finish()
        {
            if (finished)
                return;
            finished = true;
            ReportEggSmash(
                2,
                AwardColorEggCoverPending.FormatRewardId(selected, choiceState == 1),
                choiceState);
            hammerTween?.Kill();
            brokenSoundTween?.Kill();
            brokenSoundTween = null;
            ClearHammerTrack();
            otherRevealTween?.Kill();
            lockFadeTween?.Kill();
            rewardDepart?.Kill();
            rewardDepart = null;
            UnhookAd();
            Action callback = onFinished;
            onFinished = null;
            CloseSelf(() => callback?.Invoke(), false);
        }

        void UnhookAd()
        {
            GameEvent.RemoveEventListener((int)EventNameDef.AwardColorEggCoverPickAll, GrantAll);
            GPSDKCallbackHandler handler = GPSDKMgr.Instance != null
                ? GPSDKMgr.Instance.GetCallbackHandler()
                : null;
            if (handler != null)
                handler.OnHideAdsLoading -= OnAdClosed;
        }

        void ReportEggSmash(int status, string rewardId, int choice)
        {
            GameBAData.ExcuteEggSmash(
                status,
                AwardColorEggCoverPending.RewardGroup,
                rewardId,
                popupType,
                choice);
        }

        static GameObject FindLock(Button button)
        {
            if (button == null)
                return null;
            Transform lockNode = button.transform.Find("Lock");
            return lockNode != null ? lockNode.gameObject : null;
        }

        static GameObject FindLucky(Transform egg)
        {
            if (egg == null)
                return null;
            GameObject fallback = null;
            for (int i = 0; i < egg.childCount; i++)
            {
                Transform child = egg.GetChild(i);
                if (child.name != "Lucky")
                    continue;
                Graphic graphic = child.GetComponent<Graphic>();
                if (graphic != null)
                    graphic.raycastTarget = false;
                if (child.GetComponent<Image>() != null)
                    return child.gameObject;
                if (fallback == null)
                    fallback = child.gameObject;
            }
            return fallback;
        }

        static TextMeshProUGUI FindText(Transform root, string path)
        {
            Transform found = root.Find(path);
            return found != null ? found.GetComponent<TextMeshProUGUI>() : null;
        }

        static Image FindImage(Transform root, string path)
        {
            Transform found = root.Find(path);
            return found != null ? found.GetComponent<Image>() : null;
        }

        static Button FindButton(Transform root, string path)
        {
            Transform found = root.Find(path);
            return found != null ? found.GetComponent<Button>() : null;
        }

    }
}
