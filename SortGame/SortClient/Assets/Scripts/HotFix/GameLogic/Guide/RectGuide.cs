using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 矩形遮罩镂空
/// </summary>
public class RectGuide : MonoBehaviour
{
    /// <summary>
    /// 高亮显示目标
    /// </summary>
    private RectTransform mTargetRect;

    /// <summary>
    /// Canvas
    /// </summary>
    private Canvas canvas;

    /// <summary>
    /// 区域范围缓存
    /// </summary>
    private readonly Vector3[] corners = new Vector3[4];

    /// <summary>
    /// 镂空区域中心
    /// </summary>
    private Vector4 center;

    /// <summary>
    /// 最终的偏移x
    /// </summary>
    private float targetOffsetX = 0f;

    /// <summary>
    /// 最终的偏移y
    /// </summary>
    private float targetOffsetY = 0f;

    /// <summary>
    /// 遮罩材质
    /// </summary>
    private Material mMaskMaterial;
    public Material MaskMaterial
    {
        get
        {
            return mMaskMaterial;
        }
    }

    /// <summary>
    /// 当前的偏移x
    /// </summary>
    private float currentOffsetX = 0f;

    /// <summary>
    /// 当前的偏移y
    /// </summary>
    private float currentOffsetY = 0f;

    /// <summary>
    /// 高亮区域缩放的动画时间
    /// </summary>
    private readonly float shrinkTime = 0.1f;

    private float offsetX = 0.0f;
    private float offsetY = 0.0f;

    private void Awake()
    {
        var request = xasset.Asset.Load("Assets/CABRes/Shaders/Guide/Guide_Rect.shader", typeof(Shader));
        mMaskMaterial = new Material(request.asset as Shader);
    }

    public void Guide(Canvas canvas, RectTransform target, float offsetX, float offsetY)
    {
        this.canvas = canvas;
        mTargetRect = target;
        this.offsetX = offsetX;
        this.offsetY = offsetY;

        RefreshMask();
        PlayGuideAnimation();
    }

    private void RefreshMask()
    {
        if (mTargetRect == null) return;

        //获取高亮区域的四个顶点的世界坐标
        mTargetRect.GetWorldCorners(corners);

        //转换成屏幕坐标
        for (int i = 0; i < corners.Length; i++)
        {
            corners[i] = GameUtility.WorldToCanvasPos(canvas, corners[i]);
        }

        //计算高亮显示区域在画布中的范围
        targetOffsetX = Vector2.Distance(corners[0], corners[3]) / 2f + offsetX / 2;
        targetOffsetY = Vector2.Distance(corners[0], corners[1]) / 2f + offsetY / 2;

        //计算高亮显示区域的中心
        float x = corners[0].x + ((corners[3].x - corners[0].x) / 2);
        float y = corners[0].y + ((corners[1].y - corners[0].y) / 2);
        Vector2 center = new Vector3(x, y, 0);

        //设置遮罩材质中的中心变量
        Vector4 centerMat = new Vector4(center.x, center.y, 0, 0);
        mMaskMaterial.SetVector("_Center", centerMat);
    }

    public void PlayGuideAnimation()
    {
        //计算当前高亮显示区域的半径
        RectTransform canRectTransform = canvas.transform as RectTransform;
        if (canRectTransform != null)
        {
            //获取画布区域的四个顶点
            canRectTransform.GetWorldCorners(corners);

            //计算偏移初始值
            for (int i = 0; i < corners.Length; i++)
            {
                if (i % 2 == 0)
                {
                    currentOffsetX = Mathf.Max(Vector3.Distance(GameUtility.WorldToCanvasPos(canvas, corners[i]), center), currentOffsetX);
                }
                else
                {
                    currentOffsetY = Mathf.Max(Vector3.Distance(GameUtility.WorldToCanvasPos(canvas, corners[i]), center), currentOffsetY);
                }
            }
        }
        //设置遮罩材质中当前偏移的变量
        mMaskMaterial.SetFloat("_SliderX", currentOffsetX);
        mMaskMaterial.SetFloat("_SliderY", currentOffsetY);
    }

    /// <summary>
    /// 收缩速度
    /// </summary>
    private float shrinkVelocityX = 0f;
    private float shrinkVelocityY = 0f;
    private void Update()
    {
        RefreshMask();

        //从当前偏移量到目标偏移量差值显示收缩动画
        float valueX = Mathf.SmoothDamp(currentOffsetX, targetOffsetX, ref shrinkVelocityX, shrinkTime);
        float valueY = Mathf.SmoothDamp(currentOffsetY, targetOffsetY, ref shrinkVelocityY, shrinkTime);
        if (!Mathf.Approximately(valueX, currentOffsetX))
        {
            currentOffsetX = valueX;
            mMaskMaterial.SetFloat("_SliderX", currentOffsetX);
        }
        if (!Mathf.Approximately(valueY, currentOffsetY))
        {
            currentOffsetY = valueY;
            mMaskMaterial.SetFloat("_SliderY", currentOffsetY);
        }
    }

    private void OnDisable()
    {
        mTargetRect = null;
    }
}
