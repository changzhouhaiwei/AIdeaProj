using Coffee.UIEffects;
using TMPro;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// Keeps UIEffectTweener's changing transition rate in sync with TMP Sprite Asset sub-meshes.
    /// TMP creates those sub-meshes at runtime. Updating the parent UIEffect material alone does not
    /// reliably update the visible sprite-digit material while it is being tweened.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIEffect))]
    [RequireComponent(typeof(UIEffectTweener))]
    public sealed class UIEffectTMPSpriteTweenerSync : MonoBehaviour
    {
        private UIEffect m_Effect;
        private UIEffectTweener m_Tweener;
        private TextMeshProUGUI m_Text;
        private float m_LastTransitionRate = float.NaN;

        private static readonly int TransitionRateId = Shader.PropertyToID("_TransitionRate");

        private void Awake()
        {
            m_Effect = GetComponent<UIEffect>();
            m_Tweener = GetComponent<UIEffectTweener>();
            m_Text = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            m_LastTransitionRate = float.NaN;
        }

        private void LateUpdate()
        {
            if (m_Effect == null || m_Tweener == null || !m_Tweener.isTweening)
            {
                return;
            }

            float transitionRate = m_Effect.transitionRate;
            if (Mathf.Approximately(m_LastTransitionRate, transitionRate))
            {
                return;
            }

            m_LastTransitionRate = transitionRate;
            SyncSpriteSubMeshMaterials(transitionRate);
        }

        private void SyncSpriteSubMeshMaterials(float transitionRate)
        {
            if (m_Text == null)
            {
                return;
            }

            var subMeshes = m_Text.GetComponentsInChildren<TMP_SubMeshUI>(true);
            foreach (var subMesh in subMeshes)
            {
                if (subMesh == null)
                {
                    continue;
                }

                var material = subMesh.canvasRenderer.GetMaterial();
                if (material != null)
                {
                    material.SetFloat(TransitionRateId, transitionRate);
                }
            }
        }
    }
}
