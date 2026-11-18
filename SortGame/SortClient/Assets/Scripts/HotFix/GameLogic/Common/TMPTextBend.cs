using TMPro;
using UnityEngine;

/// <summary>
/// 挂在 TMP 文字上（如 titleTex），按曲线把文字顶点弯成弧形。
/// Bend Amount 为正时中间向上拱，为负时中间向下凹。
/// 弯曲写在 OnPreRenderText：Animator 把缩放打到 0 时，TMP 会在 LateUpdate 之后重生成网格。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class TMPTextBend : MonoBehaviour
{
    [Header("弯曲")]
    [Tooltip("中间相对两端的偏移（像素）。正值向上拱，负值向下凹。")]
    [SerializeField] float bendAmount = 24f;
    [Tooltip("字母是否沿弧线旋转")]
    [SerializeField] bool rotateCharacters = true;
    [Tooltip("0=左端，0.5=中间，1=右端。默认中间为 1、两端为 0。")]
    [SerializeField] AnimationCurve curve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f));

    TMP_Text _tmp;
    float _lastBend;
    bool _lastRotate;

    void OnEnable()
    {
        CacheTmp();
        EnsureCurve();
        Subscribe();
        Refresh();
    }

    void OnDisable()
    {
        if (_tmp != null)
            _tmp.OnPreRenderText -= ApplyBend;
    }

    void LateUpdate()
    {
        if (!Mathf.Approximately(_lastBend, bendAmount) || _lastRotate != rotateCharacters)
            Refresh();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureCurve();
        if (isActiveAndEnabled)
            Refresh();
    }
#endif

    void CacheTmp()
    {
        if (_tmp == null)
            _tmp = GetComponent<TMP_Text>();
    }

    void Subscribe()
    {
        if (_tmp == null)
            return;
        _tmp.OnPreRenderText -= ApplyBend;
        _tmp.OnPreRenderText += ApplyBend;
    }

    void EnsureCurve()
    {
        if (curve != null && curve.length >= 2)
            return;
        curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f));
    }

    void Refresh()
    {
        CacheTmp();
        if (_tmp == null)
            return;

        Subscribe();
        _lastBend = bendAmount;
        _lastRotate = rotateCharacters;
        _tmp.ForceMeshUpdate(true);
    }

    void ApplyBend(TMP_TextInfo textInfo)
    {
        if (textInfo == null || textInfo.characterCount == 0)
            return;

        EnsureCurve();

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        int visible = 0;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var ch = textInfo.characterInfo[i];
            if (!ch.isVisible)
                continue;
            visible++;
            minX = Mathf.Min(minX, ch.bottomLeft.x, ch.topLeft.x);
            maxX = Mathf.Max(maxX, ch.bottomRight.x, ch.topRight.x);
        }

        if (visible == 0 || maxX - minX < 0.01f)
            return;

        float width = maxX - minX;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var ch = textInfo.characterInfo[i];
            if (!ch.isVisible)
                continue;

            int matIndex = ch.materialReferenceIndex;
            int vertIndex = ch.vertexIndex;
            var verts = textInfo.meshInfo[matIndex].vertices;
            if (verts == null || vertIndex + 3 >= verts.Length)
                continue;

            Vector3 bl = verts[vertIndex];
            Vector3 tl = verts[vertIndex + 1];
            Vector3 tr = verts[vertIndex + 2];
            Vector3 br = verts[vertIndex + 3];
            Vector3 mid = (bl + tr) * 0.5f;

            float t = Mathf.InverseLerp(minX, maxX, mid.x);
            float y = curve.Evaluate(t) * bendAmount;
            Vector3 offset = new Vector3(0f, y, 0f);

            if (rotateCharacters)
            {
                float tNext = Mathf.Clamp01(t + 0.02f);
                float yNext = curve.Evaluate(tNext) * bendAmount;
                float angle = Mathf.Atan2(yNext - y, width * 0.02f) * Mathf.Rad2Deg;
                Quaternion rot = Quaternion.Euler(0f, 0f, angle);
                verts[vertIndex] = rot * (bl - mid) + mid + offset;
                verts[vertIndex + 1] = rot * (tl - mid) + mid + offset;
                verts[vertIndex + 2] = rot * (tr - mid) + mid + offset;
                verts[vertIndex + 3] = rot * (br - mid) + mid + offset;
            }
            else
            {
                verts[vertIndex] = bl + offset;
                verts[vertIndex + 1] = tl + offset;
                verts[vertIndex + 2] = tr + offset;
                verts[vertIndex + 3] = br + offset;
            }
        }
    }
}
