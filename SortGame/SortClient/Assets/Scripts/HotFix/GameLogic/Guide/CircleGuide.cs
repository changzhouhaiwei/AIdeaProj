using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 圆形遮罩镂空
/// </summary>
public class CircleGuide : MonoBehaviour
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
    /// 镂空区域半径
    /// </summary>
    private float radius = 0f;

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
    /// 当前高亮区域半径
    /// </summary>
    private float currentRadius = 0f;

    /// <summary>
    /// 高亮区域缩放的动画时间
    /// </summary>
    private readonly float shrinkTime = 0.1f;

    private float offsetRadius = 0.0f;

    private void Awake()
    {
        var request = xasset.Asset.Load("Assets/CABRes/Shaders/Guide/Guide_Circle.shader", typeof(Shader));
        mMaskMaterial = new Material(request.asset as Shader);
    }

    public void Guide(Canvas canvas, RectTransform target, float offset)
    {
        this.canvas = canvas;
        mTargetRect = target;
        offsetRadius = offset;

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

        //计算最终高亮显示区域的半径
        radius = Vector2.Distance(corners[0], corners[2]) / 2 + offsetRadius / 2;

        //计算高亮显示区域的中心
        float x = corners[0].x + ((corners[3].x - corners[0].x) / 2);
        float y = corners[0].y + ((corners[1].y - corners[0].y) / 2);
        Vector3 center = new Vector3(x, y, 0);

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
            //将画布顶点距离高亮区域中心最近的距离昨晚当前高亮区域半径的初始值
            foreach (var corner in corners)
            {
                currentRadius = Mathf.Max(Vector3.Distance(GameUtility.WorldToCanvasPos(canvas, corner), corner), currentRadius);
            }
        }
        mMaskMaterial.SetFloat("_Slider", currentRadius);
    }

    /// <summary>
    /// 收缩速度
    /// </summary>
    private float shrinkVelocity = 0f;
    private void Update()
    {
        RefreshMask();

        //从当前半径到目标半径差值显示收缩动画
        float value = Mathf.SmoothDamp(currentRadius, radius, ref shrinkVelocity, shrinkTime);
        if (!Mathf.Approximately(value, currentRadius))
        {
            currentRadius = value;
            mMaskMaterial.SetFloat("_Slider", currentRadius);
        }
    }

    private void OnDisable()
    {
        mTargetRect = null;
    }
}
