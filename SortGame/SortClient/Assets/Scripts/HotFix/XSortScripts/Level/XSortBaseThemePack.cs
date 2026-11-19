using Spine.Unity;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// 一套主题的资源引用。文件放到 Themes/{XSortbaseId}.asset，按 id 加载，互不打进同一包。
    /// </summary>
    [CreateAssetMenu(menuName = "Content/XSort Base Theme Pack", fileName = "XSortBaseThemePack")]
    public class XSortBaseThemePack : ScriptableObject
    {
        public int XSortbaseId;
        public string theme;

        [Header("棋子")]
        public Sprite XSortBase;
        public Sprite unknown;
        public AudioClip collectSound;
        public GameObject trail;
        public Material brokenMat;
        public GameObject brokenEffect;
        public AudioClip brokenSound;

        [Header("局内五连 / 备战栏")]
        public GameObject combo5Effect;
        public AudioClip combo5Sound;
        public Sprite column;
        [Tooltip("可乐箱 / 裂变棋激活且本字段有图时才替换备战栏；空则主题关仍用 column，无主题才用 XSort_fangzhidi_0")]
        public Sprite columnBoxCover;
        public Sprite slot;

        [Header("结算")]
        public GameObject winEffect;
        public SkeletonDataAsset winTitle;
        public SkeletonDataAsset winGift;
        public GameObject winCelebrateEffect;
        [Header("失败")]
        public GameObject failEffect;
        public SkeletonDataAsset failClock;
        public AudioClip failSound;
        [Header("通用")]
        public Sprite mainBg;
        public Sprite levelBg;
        public Sprite button;
        [Tooltip("主题按钮出现溶解材质；为空则 themeStyle 无溶解动效")]
        public Material buttonAppearMaterial;
        public GameObject buttonClickVfx;
        public AudioClip buttonClickSound;
    }
}
