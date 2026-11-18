
using UnityEngine;
using Spine.Unity.AttachmentTools;
using System.Collections;

// namespace Spine.Unity.Examples
// {
    //动态更换spine的部位帮助类
    public class SpineGraphicChange : MonoBehaviour
    {

    //     #region Inspector
    //     [SpineSkin]
    //     public string baseSkinName = "base";
    //     public Material sourceMaterial; // This will be used as the basis for shader and material property settings.

    //     [Header("Visor")]
    //     public Sprite visorSprite;
    //     [SpineSlot] public string visorSlot;
    //     [SpineAttachment(slotField: "visorSlot", skinField: "baseSkinName")] public string visorKey = "goggles";

    //     [Header("Runtime Repack Required!!")]
    //     public bool repack = true;

    //     [Header("Do not assign")]
    //     public Texture2D runtimeAtlas;
    //     public Material runtimeMaterial;
    //     #endregion

    //     Skin customSkin;

    //     void OnValidate()
    //     {
    //         if (sourceMaterial == null)
    //         {
    //             var skeletonGraphic = GetComponent<SkeletonGraphic>();
    //             if (skeletonGraphic != null)
    //                 sourceMaterial = skeletonGraphic.SkeletonDataAsset.atlasAssets[0].PrimaryMaterial;
    //         }
    //     }

    //     IEnumerator Start()
    //     {
    //         yield return new WaitForSeconds(1f); // Delay for 1 second. For testing.
    //         // Apply();
    //     }

    //     public void ExcuteApply(Sprite sp)
    //     {
    //         //代码协程动态获取sprite
    //         visorSprite = sp;
    //         Apply();
    //     }

    //     [ContextMenu("Apply")]
    //     void Apply()
    //     {
    //         var skeletonGraphic = GetComponent<SkeletonGraphic>();
    //         var skeleton = skeletonGraphic.Skeleton;

    //         // STEP 0: PREPARE SKINS
    //         // Let's prepare a new skin to be our custom skin with equips/customizations. We get a clone so our original skins are unaffected.
    //         customSkin = customSkin ?? new Skin("custom skin"); // This requires that all customizations are done with skin placeholders defined in Spine.
    //                                                             //customSkin = customSkin ?? skeleton.UnshareSkin(true, false, skeletonAnimation.AnimationState); // use this if you are not customizing on the default skin and don't plan to remove
    //                                                             // Next let's get the skin that contains our source attachments. These are the attachments that
    //         var baseSkin = skeleton.Data.FindSkin(baseSkinName);

    //         // Let's do this for the visor.
    //         int visorSlotIndex = skeleton.FindSlotIndex(visorSlot); // You can access GetAttachment and SetAttachment via string, but caching the slotIndex is faster.
    //         Attachment baseAttachment = baseSkin.GetAttachment(visorSlotIndex, visorKey); // STEP 1.1
    //         Attachment newAttachment = baseAttachment.GetRemappedClone(visorSprite, sourceMaterial); // STEP 1.2 - 1.3
    //                                                                                                  // Note: Each call to `GetRemappedClone()` with parameter `premultiplyAlpha` set to `true` creates
    //                                                                                                  // a cached Texture copy which can be cleared by calling AtlasUtilities.ClearCache() as done below.
    //         customSkin.SetAttachment(visorSlotIndex, visorKey, newAttachment); // STEP 1.4


    //         if (repack)
    //         {
    //             var repackedSkin = new Skin("repacked skin");
    //             repackedSkin.AddAttachments(skeleton.Data.DefaultSkin);
    //             repackedSkin.AddAttachments(customSkin);
    //             // Note: materials and textures returned by GetRepackedSkin() behave like 'new Texture2D()' and need to be destroyed
    //             if (runtimeMaterial)
    //                 Destroy(runtimeMaterial);
    //             if (runtimeAtlas)
    //                 Destroy(runtimeAtlas);
    //             repackedSkin = repackedSkin.GetRepackedSkin("repacked skin", sourceMaterial, out runtimeMaterial, out runtimeAtlas);
    //             skeleton.SetSkin(repackedSkin);
    //         }
    //         else
    //         {
    //             skeleton.SetSkin(customSkin);
    //         }

    //         //skeleton.SetSlotsToSetupPose();
    //         skeleton.SetToSetupPose();
    //         skeletonGraphic.Update(0);
    //         skeletonGraphic.OverrideTexture = runtimeAtlas;

    //         // AtlasUtilities.ClearCache();
    //         // Resources.UnloadUnusedAssets();
    //     }

    // }
}