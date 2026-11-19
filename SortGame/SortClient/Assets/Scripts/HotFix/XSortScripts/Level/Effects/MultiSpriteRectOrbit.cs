using System.Collections.Generic;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// 圆角矩形轨道流光。固定顺时针；流光子物体放在预制体里（通常 2 个），不运行时创建。
    /// TrailRenderer 参数在子物体组件上直接调，本脚本不暴露。
    /// </summary>
    public class MultiSpriteRectOrbit : MonoBehaviour
    {
        private enum PieceKind { Straight, Arc }

        private struct PathPiece
        {
            public PieceKind Kind;
            public float Length;
            public Vector2 A, B;
            public Vector2 Control;
            public Vector2 ArcCenter;
            public float ArcRadius;
            public float StartRad;
            public float EndRad;

            public Vector2 Sample(float t)
            {
                t = Mathf.Clamp01(t);
                if (Kind == PieceKind.Straight)
                {
                    float invT = 1f - t;
                    return invT * invT * A + 2f * invT * t * Control + t * t * B;
                }
                float ang = Mathf.Lerp(StartRad, EndRad, t);
                return ArcCenter + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * ArcRadius;
            }
        }

        [SerializeField] float width = 0.9f;
        [SerializeField] float height = 0.9f;
        [SerializeField] float cornerRadius = 0.15f;
        [SerializeField] float moveSpeed = 0.45f;
        [SerializeField] float straightArc = 0.04f;

        readonly List<PathPiece> pieces = new(8);
        readonly List<OrbitElement> elements = new();

        float currentTime;
        float totalPathLength;
        bool isInitialized;
        int sortingOrder;

        class OrbitElement
        {
            public Transform Transform;
            public TrailRenderer Trail;
            public SpriteRenderer SpriteRenderer;
            public float PhaseOffset;
            public float Direction;
        }

        /// <summary>
        /// 由 XSortLongPressHintEffect 下发路径参数。
        /// </summary>
        public void ApplyPath(float w, float h, float corner, float speed, float trailArc, bool rebuildNow)
        {
            width = Mathf.Max(0.1f, w);
            height = Mathf.Max(0.1f, h);
            cornerRadius = Mathf.Clamp(corner, 0f, Mathf.Min(width, height) * 0.5f);
            moveSpeed = Mathf.Max(0.01f, speed);
            straightArc = Mathf.Clamp(trailArc, -Mathf.Min(width, height) * 0.4f, Mathf.Min(width, height) * 0.4f);

            if (rebuildNow && (isInitialized || isActiveAndEnabled))
                RebuildAndPlay();
        }

        public void SetSortingOrder(int order)
        {
            sortingOrder = order;
            for (int i = 0; i < elements.Count; i++)
            {
                var el = elements[i];
                if (el.SpriteRenderer != null)
                    el.SpriteRenderer.sortingOrder = order;
                if (el.Trail != null)
                {
                    el.Trail.sortingOrder = order;
                    el.Trail.sortingLayerID = 0;
                }
            }
        }

        public void RebuildAndPlay()
        {
            CachePrefabElements();
            RebuildPath();
            SetElementsStartAtBottomCenter();
            isInitialized = true;
            currentTime = 0f;
            ClearTrails();
            for (int i = 0; i < elements.Count; i++)
                UpdateElementPosition(elements[i]);
        }

        void OnEnable()
        {
            if (!isInitialized)
                RebuildAndPlay();
            else
                ClearTrails();
        }

        void OnDisable()
        {
            ClearTrails();
        }

        void Update()
        {
            if (!isInitialized || elements.Count == 0) return;

            currentTime += Time.deltaTime * moveSpeed;
            for (int i = 0; i < elements.Count; i++)
                UpdateElementPosition(elements[i]);
        }

        void CachePrefabElements()
        {
            elements.Clear();
            int count = transform.childCount;
            if (count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                var child = transform.GetChild(i);
                elements.Add(new OrbitElement
                {
                    Transform = child,
                    Trail = child.GetComponent<TrailRenderer>(),
                    SpriteRenderer = child.GetComponentInChildren<SpriteRenderer>(true),
                    PhaseOffset = 0f,
                    Direction = i % 2 == 0 ? 1f : -1f
                });
            }

            SetSortingOrder(sortingOrder);
        }

        void RebuildPath()
        {
            pieces.Clear();

            float hw = width * 0.5f;
            float hh = height * 0.5f;
            float r = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(width, height) * 0.5f);

            float arcLen = Mathf.PI * r * 0.5f;
            float lineArc = Mathf.Clamp(straightArc, -Mathf.Min(width, height) * 0.4f, Mathf.Min(width, height) * 0.4f);

            Vector2 trC = new Vector2(hw - r, hh - r);
            Vector2 brC = new Vector2(hw - r, -hh + r);
            Vector2 blC = new Vector2(-hw + r, -hh + r);
            Vector2 tlC = new Vector2(-hw + r, hh - r);

            void AddStraight(Vector2 a, Vector2 b, Vector2 outward)
            {
                Vector2 control = (a + b) * 0.5f + outward * (lineArc * 2f);
                pieces.Add(new PathPiece
                {
                    Kind = PieceKind.Straight,
                    Length = EstimateQuadraticLength(a, control, b),
                    A = a,
                    B = b,
                    Control = control
                });
            }

            void AddArc(Vector2 center, float startDeg, float endDeg, float len)
            {
                pieces.Add(new PathPiece
                {
                    Kind = PieceKind.Arc,
                    Length = len,
                    ArcCenter = center,
                    ArcRadius = r,
                    StartRad = startDeg * Mathf.Deg2Rad,
                    EndRad = endDeg * Mathf.Deg2Rad
                });
            }

            AddStraight(new Vector2(-hw + r, hh), new Vector2(hw - r, hh), Vector2.up);
            AddArc(trC, 90f, 0f, arcLen);
            AddStraight(new Vector2(hw, hh - r), new Vector2(hw, -hh + r), Vector2.right);
            AddArc(brC, 0f, -90f, arcLen);
            AddStraight(new Vector2(hw - r, -hh), new Vector2(-hw + r, -hh), Vector2.down);
            AddArc(blC, -90f, -180f, arcLen);
            AddStraight(new Vector2(-hw, -hh + r), new Vector2(-hw, hh - r), Vector2.left);
            AddArc(tlC, -180f, -270f, arcLen);

            totalPathLength = 0f;
            for (int i = 0; i < pieces.Count; i++)
                totalPathLength += pieces[i].Length;
        }

        void SetElementsStartAtBottomCenter()
        {
            if (totalPathLength <= 0f || pieces.Count < 5)
            {
                return;
            }

            // 路径按上边、右边、下边、左边顺时针构建；第 4 段为下边，取其正中作为共同起点。
            float bottomCenterDistance = 0f;
            for (int i = 0; i < 4; i++)
            {
                bottomCenterDistance += pieces[i].Length;
            }
            bottomCenterDistance += pieces[4].Length * 0.5f;

            float bottomCenterPhase = bottomCenterDistance / totalPathLength;
            for (int i = 0; i < elements.Count; i++)
            {
                elements[i].PhaseOffset = bottomCenterPhase;
            }
        }

        static float EstimateQuadraticLength(Vector2 a, Vector2 control, Vector2 b)
        {
            const int Segments = 8;
            float length = 0f;
            Vector2 previous = a;
            for (int i = 1; i <= Segments; i++)
            {
                float t = (float)i / Segments;
                float invT = 1f - t;
                Vector2 current = invT * invT * a + 2f * invT * t * control + t * t * b;
                length += Vector2.Distance(previous, current);
                previous = current;
            }

            return Mathf.Max(0.001f, length);
        }

        void UpdateElementPosition(OrbitElement element)
        {
            if (totalPathLength <= 0f || element.Transform == null) return;

            float phase = Mathf.Repeat(currentTime * element.Direction + element.PhaseOffset, 1f);
            float dist = phase * totalPathLength;
            Vector2 pos = GetPositionAtDistance(dist);
            element.Transform.localPosition = new Vector3(pos.x, pos.y, -0.05f);
        }

        Vector2 GetPositionAtDistance(float distance)
        {
            if (totalPathLength <= 0f) return Vector2.zero;
            distance = Mathf.Repeat(distance, totalPathLength);

            float acc = 0f;
            for (int i = 0; i < pieces.Count; i++)
            {
                float next = acc + pieces[i].Length;
                if (distance < next || i == pieces.Count - 1)
                {
                    float t = pieces[i].Length > 0f ? (distance - acc) / pieces[i].Length : 0f;
                    return pieces[i].Sample(t);
                }
                acc = next;
            }

            return pieces[pieces.Count - 1].Sample(1f);
        }

        void ClearTrails()
        {
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].Trail != null)
                    elements[i].Trail.Clear();
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            width = Mathf.Max(0.1f, width);
            height = Mathf.Max(0.1f, height);
            cornerRadius = Mathf.Max(0f, cornerRadius);
            moveSpeed = Mathf.Max(0.01f, moveSpeed);
            straightArc = Mathf.Clamp(straightArc, -Mathf.Min(width, height) * 0.4f, Mathf.Min(width, height) * 0.4f);

            if (!Application.isPlaying || !isInitialized) return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || !isInitialized) return;
                RebuildAndPlay();
            };
        }
#endif
    }
}
