using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 把亮边溶解材质面板上的参数做成可关键帧的字段。
/// Timeline 改这些字段，渲染时再写入材质。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class BrightEdgeDissolveAnim : MonoBehaviour, IMaterialModifier
{
    static readonly int ProgressId = Shader.PropertyToID("_Progress");
    static readonly int DirectionId = Shader.PropertyToID("_Direction");
    static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    static readonly int NoiseAmountId = Shader.PropertyToID("_NoiseAmount");
    static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
    static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
    static readonly int EdgeIntensityId = Shader.PropertyToID("_EdgeIntensity");

    [Range(0f, 1f)] public float progress;
    public Vector2 direction = new Vector2(0f, -1f);
    [Range(0.001f, 0.5f)] public float softness = 0.001f;
    public Vector2 noiseScale = Vector2.one;
    [Range(0f, 1f)] public float noiseAmount;
    [ColorUsage(true, true)] public Color edgeColor = new Color(0f, 0.3308518f, 1f, 1f);
    [Range(0.001f, 0.4f)] public float edgeWidth = 0.2f;
    [Range(0f, 8f)] public float edgeIntensity = 2f;

    Material runtime;

    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (baseMaterial == null)
            return null;

        if (runtime == null || runtime.shader != baseMaterial.shader)
        {
            DestroyRuntime();
            runtime = new Material(baseMaterial)
            {
                name = baseMaterial.name + " (Timeline)",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        Write(runtime);
        return runtime;
    }

    void OnEnable()
    {
        SetDirty();
    }

    void OnDisable()
    {
        SetDirty();
    }

    void OnDestroy()
    {
        DestroyRuntime();
    }

    void OnValidate()
    {
        SetDirty();
    }

    void OnDidApplyAnimationProperties()
    {
        SetDirty();
    }

    void LateUpdate()
    {
        SetDirty();
    }

    void SetDirty()
    {
        var graphic = GetComponent<Graphic>();
        if (graphic != null)
            graphic.SetMaterialDirty();
    }

    void Write(Material material)
    {
        if (material.HasProperty(ProgressId))
            material.SetFloat(ProgressId, Mathf.Clamp01(progress));
        if (material.HasProperty(DirectionId))
            material.SetVector(DirectionId, new Vector4(direction.x, direction.y, 0f, 0f));
        if (material.HasProperty(SoftnessId))
            material.SetFloat(SoftnessId, softness);
        if (material.HasProperty(NoiseScaleId))
            material.SetVector(NoiseScaleId, new Vector4(noiseScale.x, noiseScale.y, 0f, 0f));
        if (material.HasProperty(NoiseAmountId))
            material.SetFloat(NoiseAmountId, noiseAmount);
        if (material.HasProperty(EdgeColorId))
            material.SetColor(EdgeColorId, edgeColor);
        if (material.HasProperty(EdgeWidthId))
            material.SetFloat(EdgeWidthId, edgeWidth);
        if (material.HasProperty(EdgeIntensityId))
            material.SetFloat(EdgeIntensityId, edgeIntensity);
    }

    void DestroyRuntime()
    {
        if (runtime == null)
            return;

        if (Application.isPlaying)
            Destroy(runtime);
        else
            DestroyImmediate(runtime);

        runtime = null;
    }
}
