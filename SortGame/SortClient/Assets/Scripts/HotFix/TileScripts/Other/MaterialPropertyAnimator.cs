using UnityEngine;
using UnityEngine.UI;

namespace FishTilePlay
{
    /// <summary>
    /// 把材质参数公开成可序列化字段，供 Animation / Animator K 帧。
    /// 挂到带 Graphic（UI Image 等）或 Renderer 的物体上即可。
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class MaterialPropertyAnimator : MonoBehaviour
    {
        public enum TargetMode
        {
            Auto,
            Graphic,
            Renderer,
        }

        [Header("Target")]
        [SerializeField] TargetMode targetMode = TargetMode.Auto;
        [Tooltip("为空则用本物体上的 Graphic / Renderer")]
        [SerializeField] Graphic targetGraphic;
        [SerializeField] Renderer targetRenderer;
        [Tooltip("建议拖入你的特效材质（如 IceBG_Mask）。Image 的 Material 为空时 Unity 会返回 Default UI Material，不填这里容易实例化错材质")]
        [SerializeField] Material sourceMaterial;
        [Tooltip("UI 建议开：实例化材质，避免改到共享 Material 资源")]
        [SerializeField] bool instantiateMaterial = true;
        [Tooltip("3D Renderer 建议开：用 PropertyBlock，不打断合批、不改共享材质")]
        [SerializeField] bool usePropertyBlock = true;

        [Header("Float A")]
        [SerializeField] bool useFloatA = true;
        [SerializeField] string floatAName = "_Progress";
        [SerializeField] float floatA;

        [Header("Float B")]
        [SerializeField] bool useFloatB;
        [SerializeField] string floatBName = "_Intensity";
        [SerializeField] float floatB;

        [Header("Float C")]
        [SerializeField] bool useFloatC;
        [SerializeField] string floatCName = "_Softness";
        [SerializeField] float floatC;

        [Header("Color A")]
        [SerializeField] bool useColorA;
        [SerializeField] string colorAName = "_Color";
        [SerializeField] Color colorA = Color.white;

        [Header("Color B")]
        [SerializeField] bool useColorB;
        [SerializeField] string colorBName = "_TintColor";
        [SerializeField] Color colorB = Color.white;

        [Header("Vector A")]
        [SerializeField] bool useVectorA;
        [SerializeField] string vectorAName = "_Center";
        [SerializeField] Vector4 vectorA = new Vector4(0.5f, 0.5f, 0f, 0f);

        Material materialInstance;
        MaterialPropertyBlock propertyBlock;
        int floatAId;
        int floatBId;
        int floatCId;
        int colorAId;
        int colorBId;
        int vectorAId;
        bool idsDirty = true;

        public float FloatA
        {
            get => floatA;
            set
            {
                floatA = value;
                Apply();
            }
        }

        public float FloatB
        {
            get => floatB;
            set
            {
                floatB = value;
                Apply();
            }
        }

        public float FloatC
        {
            get => floatC;
            set
            {
                floatC = value;
                Apply();
            }
        }

        public Color ColorA
        {
            get => colorA;
            set
            {
                colorA = value;
                Apply();
            }
        }

        public Color ColorB
        {
            get => colorB;
            set
            {
                colorB = value;
                Apply();
            }
        }

        public Vector4 VectorA
        {
            get => vectorA;
            set
            {
                vectorA = value;
                Apply();
            }
        }

        void OnEnable()
        {
            idsDirty = true;
            Apply();
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif
            // 播放模式下不强制还原共享材质；实例材质随物体销毁即可。
        }

        void OnDestroy()
        {
            DestroyMaterialInstance();
        }

        void OnValidate()
        {
            idsDirty = true;
            if (isActiveAndEnabled)
            {
                Apply();
            }
        }

        /// <summary>Animation 写入序列化字段后会调用。</summary>
        void OnDidApplyAnimationProperties()
        {
            Apply();
        }

        void LateUpdate()
        {
            // 兜底：部分版本/预览路径不一定触发 OnDidApplyAnimationProperties
            Apply();
        }

        public void Apply()
        {
            RefreshPropertyIds();

            if (TryApplyToGraphic())
            {
                return;
            }

            TryApplyToRenderer();
        }

        void RefreshPropertyIds()
        {
            if (!idsDirty)
            {
                return;
            }

            floatAId = Shader.PropertyToID(floatAName);
            floatBId = Shader.PropertyToID(floatBName);
            floatCId = Shader.PropertyToID(floatCName);
            colorAId = Shader.PropertyToID(colorAName);
            colorBId = Shader.PropertyToID(colorBName);
            vectorAId = Shader.PropertyToID(vectorAName);
            idsDirty = false;
        }

        bool TryApplyToGraphic()
        {
            if (targetMode == TargetMode.Renderer)
            {
                return false;
            }

            var graphic = targetGraphic != null ? targetGraphic : GetComponent<Graphic>();
            if (graphic == null)
            {
                return targetMode == TargetMode.Graphic;
            }

            var mat = EnsureGraphicMaterial(graphic);
            if (mat == null)
            {
                return true;
            }

            WriteToMaterial(mat);
            graphic.SetMaterialDirty();
            return true;
        }

        bool TryApplyToRenderer()
        {
            if (targetMode == TargetMode.Graphic)
            {
                return false;
            }

            var renderer = targetRenderer != null ? targetRenderer : GetComponent<Renderer>();
            if (renderer == null)
            {
                return false;
            }

            if (usePropertyBlock)
            {
                propertyBlock ??= new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock);
                WriteToPropertyBlock(propertyBlock);
                renderer.SetPropertyBlock(propertyBlock);
                return true;
            }

            var mat = EnsureRendererMaterial(renderer);
            if (mat == null)
            {
                return true;
            }

            WriteToMaterial(mat);
            return true;
        }

        Material EnsureGraphicMaterial(Graphic graphic)
        {
            var source = ResolveGraphicSourceMaterial(graphic);
            if (source == null)
            {
                return null;
            }

            if (!instantiateMaterial)
            {
                if (sourceMaterial != null && graphic.material != sourceMaterial)
                {
                    graphic.material = sourceMaterial;
                }

                return graphic.material;
            }

            if (materialInstance == null
                || materialInstance.shader != source.shader
                || IsDefaultUiMaterial(graphic, materialInstance))
            {
                DestroyMaterialInstance();
                materialInstance = Instantiate(source);
                materialInstance.name = source.name + " (Anim)";
                graphic.material = materialInstance;
            }
            else if (graphic.material != materialInstance)
            {
                graphic.material = materialInstance;
            }

            return materialInstance;
        }

        Material ResolveGraphicSourceMaterial(Graphic graphic)
        {
            if (sourceMaterial != null)
            {
                return sourceMaterial;
            }

            var current = graphic.material;
            if (IsDefaultUiMaterial(graphic, current))
            {
                return null;
            }

            // 已经是本脚本生成的实例时，继续用它（避免再 Instantiate）
            if (materialInstance != null && current == materialInstance)
            {
                return materialInstance;
            }

            return current;
        }

        static bool IsDefaultUiMaterial(Graphic graphic, Material material)
        {
            if (material == null)
            {
                return true;
            }

            if (graphic != null && material == graphic.defaultMaterial)
            {
                return true;
            }

            // Instantiate 后名字常变成 "Default UI Material (Anim)" / "(Instance)"
            return material.name.StartsWith("Default UI Material", System.StringComparison.Ordinal);
        }

        void DestroyMaterialInstance()
        {
            if (materialInstance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(materialInstance);
            }
            else
            {
                DestroyImmediate(materialInstance);
            }

            materialInstance = null;
        }

        Material EnsureRendererMaterial(Renderer renderer)
        {
            var source = sourceMaterial != null ? sourceMaterial : renderer.sharedMaterial;
            if (source == null)
            {
                return null;
            }

            if (!instantiateMaterial)
            {
                if (sourceMaterial != null)
                {
                    renderer.sharedMaterial = sourceMaterial;
                }

                return renderer.material;
            }

            if (materialInstance == null || materialInstance.shader != source.shader)
            {
                DestroyMaterialInstance();
                materialInstance = Instantiate(source);
                materialInstance.name = source.name + " (Anim)";
                renderer.sharedMaterial = materialInstance;
            }

            return materialInstance;
        }

        void WriteToMaterial(Material mat)
        {
            if (useFloatA && !string.IsNullOrEmpty(floatAName))
            {
                mat.SetFloat(floatAId, floatA);
            }

            if (useFloatB && !string.IsNullOrEmpty(floatBName))
            {
                mat.SetFloat(floatBId, floatB);
            }

            if (useFloatC && !string.IsNullOrEmpty(floatCName))
            {
                mat.SetFloat(floatCId, floatC);
            }

            if (useColorA && !string.IsNullOrEmpty(colorAName))
            {
                mat.SetColor(colorAId, colorA);
            }

            if (useColorB && !string.IsNullOrEmpty(colorBName))
            {
                mat.SetColor(colorBId, colorB);
            }

            if (useVectorA && !string.IsNullOrEmpty(vectorAName))
            {
                mat.SetVector(vectorAId, vectorA);
            }
        }

        void WriteToPropertyBlock(MaterialPropertyBlock block)
        {
            if (useFloatA && !string.IsNullOrEmpty(floatAName))
            {
                block.SetFloat(floatAId, floatA);
            }

            if (useFloatB && !string.IsNullOrEmpty(floatBName))
            {
                block.SetFloat(floatBId, floatB);
            }

            if (useFloatC && !string.IsNullOrEmpty(floatCName))
            {
                block.SetFloat(floatCId, floatC);
            }

            if (useColorA && !string.IsNullOrEmpty(colorAName))
            {
                block.SetColor(colorAId, colorA);
            }

            if (useColorB && !string.IsNullOrEmpty(colorBName))
            {
                block.SetColor(colorBId, colorB);
            }

            if (useVectorA && !string.IsNullOrEmpty(vectorAName))
            {
                block.SetVector(vectorAId, vectorA);
            }
        }
    }
}
