using Spine.Unity;

namespace FishFramework
{
    public static class SkeletonAnimationExtensions
    {
        public static void SetSkin(this SkeletonAnimation skeletonAnimation, string skinName)
        {
            skeletonAnimation.Skeleton.SetSkin(skinName);
            skeletonAnimation.Skeleton.SetSlotsToSetupPose();
            skeletonAnimation.AnimationState.Apply(skeletonAnimation.Skeleton);
        }
    }
}