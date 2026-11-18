using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 把笔刷溶解参数做成可被动画的字段。Timeline 关键帧这些字段，渲染时再写入材质。
/// UI Image 的材质属性本身不能直接进 Timeline。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class BrushStrokeDissolveAnim : MonoBehaviour, IMaterialModifier
{
    static readonly int ProgressId = Shader.PropertyToID("_Progress");
    static readonly int ReverseId = Shader.PropertyToID("_Reverse");
    static readonly int DirectionId = Shader.PropertyToID("_Direction");
    static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    static readonly int StrokeCountId = Shader.PropertyToID("_StrokeCount");
    static readonly int StrokeDepthId = Shader.PropertyToID("_StrokeDepth");
    static readonly int SeedId = Shader.PropertyToID("_Seed");
    static readonly int SizeVariationId = Shader.PropertyToID("_SizeVariation");
    static readonly int TimingVariationId = Shader.PropertyToID("_TimingVariation");
    static readonly int BristleCountId = Shader.PropertyToID("_BristleCount");
    static readonly int BristleId = Shader.PropertyToID("_Bristle");

    [Range(0f, 1f)] public float progress;
    [Range(0f, 1f)] public float reverse;
    public Vector2 direction = new Vector2(0f, -1f);
    [Range(0.001f, 0.5f)] public float softness = 0.045f;
    [Range(1f, 12f)] public float strokeCount = 7f;
    [Range(0f, 0.6f)] public float strokeDepth = 0.28f;
    public float seed;
    [Range(0f, 1f)] public float sizeVariation = 0.75f;
    [Range(0f, 1f)] public float timingVariation = 0.85f;
    [Range(4f, 80f)] public float bristleCount = 18f;
    [Range(0f, 1f)] public float bristle = 0.8f;

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
        material.SetFloat(ProgressId, Mathf.Clamp01(progress));
        material.SetFloat(ReverseId, reverse);
        material.SetVector(DirectionId, new Vector4(direction.x, direction.y, 0f, 0f));
        material.SetFloat(SoftnessId, softness);
        material.SetFloat(StrokeCountId, strokeCount);
        material.SetFloat(StrokeDepthId, strokeDepth);
        material.SetFloat(SeedId, seed);
        material.SetFloat(SizeVariationId, sizeVariation);
        material.SetFloat(TimingVariationId, timingVariation);
        material.SetFloat(BristleCountId, bristleCount);
        material.SetFloat(BristleId, bristle);
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
