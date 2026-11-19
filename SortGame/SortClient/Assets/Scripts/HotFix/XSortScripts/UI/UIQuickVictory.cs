using System;
using FishFramework;
using Spine.Unity;
using XSortGameCore;
using UnityEngine;

namespace FishXSortPlay
{
    public class UIQuickVictory : UIPage
    {
        public const string PrefabPath = "Assets/CABRes/Prefabs/UIMain/Canvas/UIQuickVictory.prefab";

        [SerializeField]
        protected SkeletonGraphic spine;

        [SerializeField]
        string animationName = "QuickVictory";

        public static void Show()
        {
            ShowFromPrefab<UIQuickVictory>(PrefabPath);
        }

        protected static T ShowFromPrefab<T>(string prefabPath) where T : UIPage
        {
            T page = UIController.GetPage<T>();
            if (page == null)
                page = LoadPage<T>(prefabPath);
            if (page == null)
                return null;

            UIController.ShowPage<T>();
            return page;
        }

        static T LoadPage<T>(string prefabPath) where T : UIPage
        {
            Canvas canvas = UIController.MainCanvas;
            if (canvas == null)
            {
                Debug.LogError("[QuickVictory] UI Main Canvas is missing.");
                return null;
            }

            GameObject prefab = UILoadHelper.LoadAsset<GameObject>(prefabPath);
#if UNITY_EDITOR
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
#endif
            if (prefab == null)
            {
                Debug.LogError($"[QuickVictory] Prefab is missing: {prefabPath}");
                return null;
            }

            GameObject instance = Instantiate(prefab, canvas.transform, false);
            instance.name = prefab.name.Replace("(Clone)", string.Empty);
            instance.layer = canvas.gameObject.layer;

            T page = instance.GetComponent<T>();
            if (page == null)
            {
                Debug.LogError($"[QuickVictory] Prefab is missing {typeof(T).Name}: {prefabPath}", instance);
                Destroy(instance);
                return null;
            }

            UIController.RegisterPage(page);
            return page;
        }

        public static void HideAll()
        {
            UIController.HidePage<UIQuickVictory>();
            UIController.HidePage<UIBoxCoverQuickVictory>();
            UIController.HidePage<UIIQChallengeQuickVictory>();
        }

        public static bool IsAnyDisplayed()
        {
            return UIController.IsDisplayed<UIQuickVictory>()
                || UIController.IsDisplayed<UIBoxCoverQuickVictory>()
                || UIController.IsDisplayed<UIIQChallengeQuickVictory>();
        }

        public override void Init()
        {
            IgnoreUnityTimeScale();
        }

        void OnEnable()
        {
            IgnoreUnityTimeScale();
        }

        protected void IgnoreUnityTimeScale()
        {
            if (spine == null)
                return;

            spine.UnscaledTime = true;
            spine.timeScale = 1f;
        }

        public override void PlayHideAnimation()
        {
            ClearSpineAnim();
            UIController.OnPageClosed(this);
        }

        public override void PlayShowAnimation()
        {
            DockActor.HideSubmitImpactEffectsForSettlement();

            LevelDirector.WinState = 3;
            Play();
            GameBAData.ExcuteStageEvent(14);
        }

        void ClearSpineAnim()
        {
            if (spine == null || spine.AnimationState == null || spine.Skeleton == null)
                return;

            spine.AnimationState.ClearTrack(0);
            spine.Skeleton.SetToSetupPose();
            spine.Update(0);
        }

        protected virtual void Play()
        {
            if (spine == null)
                return;

            IgnoreUnityTimeScale();
            spine.SetActiveEx(true);
            string anim = string.IsNullOrEmpty(animationName) ? "QuickVictory" : animationName;
            if (spine.AnimationState != null)
                spine.AnimationState.SetAnimation(0, anim, false);
        }
    }
}
