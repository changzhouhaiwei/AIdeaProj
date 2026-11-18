using Spine.Unity;

namespace FishFramework
{
    public static class SkeletonGraphicExtensions
    {
        public static void SetSkin(this SkeletonGraphic skeletonGraphic, string skinName)
        {
            skeletonGraphic.Skeleton.SetSkin(skinName);
            skeletonGraphic.Skeleton.SetSlotsToSetupPose();
            skeletonGraphic.AnimationState.Apply(skeletonGraphic.Skeleton);
        }
    }
}