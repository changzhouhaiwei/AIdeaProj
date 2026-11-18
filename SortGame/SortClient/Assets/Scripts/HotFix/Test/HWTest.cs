using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;



namespace FishFramework
{
    public class HWTest : MonoBehaviour
    {
        // [SerializeField] SkeletonGraphic skeletonAnim;

        // // SkeletonAnimation skeletonAnim;

        // [SerializeField] Sprite sprite;

        // [SerializeField] Texture2D mainTexture;

        // private string slotName = "daoju";

        // public static HWTest Instance;
        // void Awake()
        // {
        //     Instance = this;

        //     StartCoroutine(Start1212());
        //     // TestSpineChange();
        // }

        // // void TestSpineChange()
        // // {
        // //     if (skeletonAnim == null || skeletonAnim.Skeleton == null || sprite == null)
        // //     {
        // //         return;
        // //     }

        // //     var slotName = "daoju"; // 确保与Skeleton中的槽位名一致
        // //     var skeleton = skeletonAnim.Skeleton;
        // //     var slot = skeleton.FindSlot(slotName);
        // //     if (slot == null)
        // //     {
        // //         return;
        // //     }

        // //     // 使用 SkeletonGraphic 的材质从 Sprite 生成 RegionAttachment
        // //     Material mat = null;
        // //     if (skeletonAnim.material != null)
        // //     {
        // //         // 基于相同 shader 创建全新材质，拷贝属性后仅替换贴图，避免影响共用材质
        // //         mat = new Material(skeletonAnim.material.shader);
        // //         mat.CopyPropertiesFromMaterial(skeletonAnim.material);
        // //         mat.mainTexture = mainTexture;
        // //     }


        // //     if (mat == null)
        // //     {
        // //         var sda = skeletonAnim.SkeletonDataAsset;
        // //         if (sda != null && sda.atlasAssets != null && sda.atlasAssets.Length > 0)
        // //         {
        // //             var atlas = sda.atlasAssets[0];
        // //             var matsEnum = atlas != null ? atlas.Materials : null;
        // //             if (matsEnum != null)
        // //             {
        // //                 foreach (var m in matsEnum)
        // //                 {
        // //                     mat = new Material(m.shader);
        // //                     mat.CopyPropertiesFromMaterial(m);
        // //                     mat.mainTexture = mainTexture;
        // //                     break;
        // //                 }
        // //             }
        // //         }
        // //     }

        // //     if (mat == null)
        // //     {
        // //         return;
        // //     }

        // //     var regionAttachment = sprite.ToRegionAttachment(mat);

        // //     // 直接赋给槽位，立即可见
        // //     slot.Attachment = regionAttachment;
        // //     skeletonAnim.Update(0);
        // // }


        // private SkeletonGraphic anim;
        // public Texture2D t;
        // public Texture2D t2;

        // // Use this for initialization
        // IEnumerator Start1212()
        // {
        //     anim = skeletonAnim;//GetComponent<SkeletonAnimation>();

        //     yield return new WaitForSeconds(0.2f);

        //     Material m = CreateMeshAttachmentByTexture(anim.Skeleton.FindSlot(slotName), t2);
        //     //换下一张图片，如果是动态加载的图片，需要消除之前的DestroyImmediate(m.mainTexture,false);
        //     m.mainTexture = t;
        // }

        // /// <summary>
        // /// Creates the region attachment by texture.
        // /// </summary>
        // /// <returns>The region attachment by texture.</returns>
        // /// <param name="slot">Slot.</param>
        // /// <param name="texture">Texture.</param>
        // public Material CreateRegionAttachmentByTexture(Spine.Slot slot, Texture2D texture)
        // {
        //     if (slot == null) return null;

        //     Spine.RegionAttachment oldAtt = slot.Attachment as Spine.RegionAttachment;
        //     if (oldAtt == null || texture == null) return null;

        //     Spine.RegionAttachment att = new Spine.RegionAttachment(oldAtt.Name);
        //     att.RendererObject = CreateRegion(texture);
        //     att.Width = oldAtt.Width;
        //     att.Height = oldAtt.Height;
        //     // att.Offset = oldAtt.Offset;
        //     // att.Offset = oldAtt.Offset;
        //     att.Path = oldAtt.Path;

        //     att.X = oldAtt.X;
        //     att.Y = oldAtt.Y;
        //     att.Rotation = oldAtt.Rotation;
        //     att.ScaleX = oldAtt.ScaleX;
        //     att.ScaleY = oldAtt.ScaleY;

        //     att.SetUVs(0f, 1f, 1f, 0f, false);

        //     Material mat = new Material(Shader.Find("Sprites/Default"));
        //     mat.mainTexture = texture;
        //     (att.RendererObject as Spine.AtlasRegion).page.rendererObject = mat;

        //     slot.Attachment = att;
        //     return mat;
        // }

        // /// <summary>
        // /// Creates the mesh attachment by texture.
        // /// </summary>
        // /// <returns>The mesh attachment by texture.</returns>
        // /// <param name="slot">Slot.</param>
        // /// <param name="texture">Texture.</param>
        // public Material CreateMeshAttachmentByTexture(Spine.Slot slot, Texture2D texture)
        // {
        //     if (slot == null) return null;
        //     var oldAtt = slot.Attachment;//as Spine.MeshAttachment;
        //     if (oldAtt == null || texture == null) return null;

        //     Spine.MeshAttachment att = new Attachment(oldAtt.Name);
        //     att.RendererObject = CreateRegion(texture);
        //     att.Path = oldAtt.Path;

        //     att.Bones = oldAtt.Bones;
        //     att.Edges = oldAtt.Edges;
        //     att.Triangles = oldAtt.Triangles;
        //     att.Vertices = oldAtt.Vertices;
        //     att.WorldVerticesLength = oldAtt.WorldVerticesLength;
        //     att.HullLength = oldAtt.HullLength;
        //     att.RegionRotate = false;

        //     att.RegionU = 0f;
        //     att.RegionV = 1f;
        //     att.RegionU2 = 1f;
        //     att.RegionV2 = 0f;
        //     att.RegionUVs = oldAtt.RegionUVs;

        //     att.UpdateUVs();

        //     Material mat = new Material(Shader.Find("Sprites/Default"));
        //     mat.mainTexture = texture;
        //     (att.RendererObject as Spine.AtlasRegion).page.rendererObject = mat;

        //     slot.Attachment = att;
        //     return mat;
        // }



        // private Spine.AtlasRegion CreateRegion(Texture2D texture)
        // {

        //     Spine.AtlasRegion region = new Spine.AtlasRegion();
        //     region.width = texture.width;
        //     region.height = texture.height;
        //     region.originalWidth = texture.width;
        //     region.originalHeight = texture.height;
        //     region.rotate = false;
        //     region.page = new Spine.AtlasPage();
        //     region.page.name = texture.name;
        //     region.page.width = texture.width;
        //     region.page.height = texture.height;
        //     region.page.uWrap = Spine.TextureWrap.ClampToEdge;
        //     region.page.vWrap = Spine.TextureWrap.ClampToEdge;

        //     return region;
        // }


        // public static void TestHttp()
        // {
        //     Instance.StartCoroutine(Instance.Req());
        // }

        // IEnumerator Req()
        // {
        //     var preTime = Time.time;
        //     UnityWebRequest req = UnityWebRequest.Get("https://www.google.com/generate_204");
        //     req.timeout = 3; // 3秒超时

        //     yield return req.SendWebRequest();

        //     float dTime = Time.time - preTime;
        //     Debug.Log("delta time is " + dTime);
        //     if (req.responseCode == 0)
        //     {

        //     }

        //     yield return null;
        // }
    }
}