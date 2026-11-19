using System.Collections;
using Spine;
using Spine.Unity;
using XSortGameCore;
using UnityEngine;

namespace FishXSortPlay
{
    public class UIBoxCoverQuickVictory : UIQuickVictory
    {
        public new const string PrefabPath = "Assets/CABRes/Prefabs/BoxCover/UIBoxCoverQuickVictory.prefab";

        const string OutAnimName = "BoxDog_Out";
        const string IdleAnimName = "BoxDog_Idle";
        const string AnimatorStateName = "UIBoxCoverQuickVictory_Anim";
        const float RevealDelay = 15f / 60f;

        protected Coroutine revealRoutine;
        protected Animator animator;

        public static new void Show()
        {
            UIBoxCoverQuickVictory existing = UIController.GetPage<UIBoxCoverQuickVictory>();
            bool alreadyDisplayed = existing != null && existing.IsPageDisplayed;
            UIBoxCoverQuickVictory page = ShowFromPrefab<UIBoxCoverQuickVictory>(PrefabPath);
            if (alreadyDisplayed && page != null)
                page.PlayShowAnimation();
        }

        protected override void Play()
        {
            BindBoxDog();
            StopReveal();
            IgnoreUnityTimeScale();
            if (spine != null)
                spine.gameObject.SetActive(false);

            RestartAnimator();
            RestartScreenEffects();
            revealRoutine = StartCoroutine(RevealBoxDogAfterDelay());
        }

        public override void PlayHideAnimation()
        {
            StopReveal();
            if (spine != null)
                spine.gameObject.SetActive(false);
            base.PlayHideAnimation();
        }

        protected void RestartAnimator()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.enabled = true;
            animator.Rebind();
            animator.Play(AnimatorStateName, 0, 0f);
            animator.Update(0f);
        }

        protected void BindBoxDog()
        {
            if (spine != null)
                return;

            Transform found = transform.Find("BoxDog");
            if (found == null)
                found = transform.Find("SkeletonGraphic (BoxDog)");
            if (found != null)
                spine = found.GetComponent<SkeletonGraphic>();
            if (spine == null)
                spine = GetComponentInChildren<SkeletonGraphic>(true);
        }

        IEnumerator RevealBoxDogAfterDelay()
        {
            yield return new WaitForSecondsRealtime(RevealDelay);
            revealRoutine = null;
            PlayReverseOutThenIdle();
        }

        void PlayReverseOutThenIdle()
        {
            BindBoxDog();
            if (spine == null)
                return;

            spine.gameObject.SetActive(true);
            IgnoreUnityTimeScale();
            if (!spine.IsValid)
                spine.Initialize(false);
            if (!spine.IsValid || spine.AnimationState == null)
                return;

            Spine.AnimationState state = spine.AnimationState;
            SkeletonData data = spine.Skeleton != null ? spine.Skeleton.Data : null;
            if (data == null)
                return;

            Spine.Animation outAnim = data.FindAnimation(OutAnimName);
            Spine.Animation idle = data.FindAnimation(IdleAnimName);
            if (outAnim == null)
            {
                if (idle != null)
                    state.SetAnimation(0, idle, true);
                spine.Update(0f);
                return;
            }

            TrackEntry entry = state.SetAnimation(0, outAnim, false);
            if (entry == null)
                return;

            entry.MixDuration = 0f;
            entry.MixTime = 0f;
            entry.Delay = 0f;
            entry.Reverse = true;
            entry.TimeScale = 1f;
            entry.TrackTime = 0f;

            if (idle != null)
            {
                TrackEntry idleEntry = state.AddAnimation(0, idle, true, 0f);
                if (idleEntry != null)
                {
                    idleEntry.MixDuration = 0f;
                    idleEntry.Reverse = false;
                    idleEntry.TimeScale = 1f;
                }
            }

            spine.Update(0f);
        }

        protected void RestartScreenEffects()
        {
            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null)
                    continue;

                ParticleSystem.MainModule main = ps.main;
                main.useUnscaledTime = true;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }

        protected void StopReveal()
        {
            if (revealRoutine == null)
                return;

            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }
    }
}
