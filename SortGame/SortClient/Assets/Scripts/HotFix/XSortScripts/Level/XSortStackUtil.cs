using System.Collections.Generic;
using UnityEngine;

namespace FishXSortPlay
{
    public static class XSortStackUtil
    {
        private const float StrongStackOverlapAreaRatio = 0.8f;

        /// <summary>
        /// 实际玩法与 Cost 共用的“上层棋正在遮挡下层棋”判定。
        /// 使用 Collider.bounds，和原 IsXSortUnconcealedNew 的碰撞语义一致，
        /// 同时排除已提交、失活或缺碰撞体的幽灵棋。
        /// </summary>
        public static bool IsCurrentUpperBlocker(XSortActor lower, XSortActor upper)
        {
            if (lower == null
                || upper == null
                || lower == upper
                || lower.IsSubmitted
                || upper.IsSubmitted
                || !lower.gameObject.activeInHierarchy
                || !upper.gameObject.activeInHierarchy)
                return false;
            if (lower.LayerID <= 0 || upper.LayerID >= lower.LayerID)
                return false;

            return AreXSortsOverlappingXY(lower, upper);
        }

        public static bool AreXSortsOverlappingXY(XSortActor a, XSortActor b)
        {
            if (a == null || b == null)
                return false;

            BoxCollider colliderA = a.BoxCollider != null
                ? a.BoxCollider
                : a.GetComponent<BoxCollider>();
            BoxCollider colliderB = b.BoxCollider != null
                ? b.BoxCollider
                : b.GetComponent<BoxCollider>();
            return AreCollidersOverlappingXY(colliderA, colliderB);
        }

        public static bool AreCollidersOverlappingXY(BoxCollider a, BoxCollider b)
        {
            if (a == null || b == null || !a.enabled || !b.enabled)
                return false;

            return BoundsOverlapXY(a.bounds, b.bounds);
        }

        public static bool IsStrongStackPair(XSortActor lower, XSortActor upper)
        {
            if (lower == null || upper == null || lower.IsSubmitted || upper.IsSubmitted)
            {
                return false;
            }

            if (upper.LayerID >= lower.LayerID)
            {
                return false;
            }

            if (!TryGetBounds(lower, out Bounds lowerBounds) || !TryGetBounds(upper, out Bounds upperBounds))
            {
                return false;
            }

            if (!BoundsOverlapXY(lowerBounds, upperBounds))
            {
                return false;
            }

            float overlapX = Mathf.Min(lowerBounds.max.x, upperBounds.max.x) - Mathf.Max(lowerBounds.min.x, upperBounds.min.x);
            float overlapY = Mathf.Min(lowerBounds.max.y, upperBounds.max.y) - Mathf.Max(lowerBounds.min.y, upperBounds.min.y);
            float XSortWidth = lowerBounds.size.x;
            float XSortHeight = lowerBounds.size.y;
            if (XSortWidth <= 0f || XSortHeight <= 0f)
            {
                return false;
            }

            // 按轴判定，不是面积：露出一条边时面积大约只有 81%，用面积阈值会漏判。
            return overlapX / XSortWidth > StrongStackOverlapAreaRatio
                && overlapY / XSortHeight > StrongStackOverlapAreaRatio;
        }

        public static bool HasStrongStackUpperBlock(XSortActor XSort, IReadOnlyList<XSortActor> actXSorts)
        {
            if (XSort == null || XSort.IsSubmitted || actXSorts == null)
            {
                return false;
            }

            for (int i = 0; i < actXSorts.Count; i++)
            {
                XSortActor upper = actXSorts[i];
                if (upper == null || upper == XSort || upper.IsSubmitted)
                {
                    continue;
                }

                if (IsStrongStackPair(XSort, upper))
                {
                    return true;
                }
            }

            return false;
        }

        public static void CollectOverlappingUpperXSorts(
            XSortActor XSort,
            IReadOnlyList<XSortActor> actXSorts,
            List<XSortActor> dest)
        {
            if (dest == null || XSort == null || XSort.IsSubmitted || actXSorts == null)
            {
                return;
            }

            if (!TryGetBounds(XSort, out Bounds lowerBounds))
            {
                return;
            }

            for (int i = 0; i < actXSorts.Count; i++)
            {
                XSortActor upper = actXSorts[i];
                if (upper == null || upper == XSort || upper.IsSubmitted)
                {
                    continue;
                }

                if (upper.LayerID >= XSort.LayerID)
                {
                    continue;
                }

                if (!TryGetBounds(upper, out Bounds upperBounds))
                {
                    continue;
                }

                if (BoundsOverlapXY(lowerBounds, upperBounds))
                {
                    dest.Add(upper);
                }
            }
        }

        public static bool HasUpperOverlap(XSortActor XSort, IReadOnlyList<XSortActor> actXSorts)
        {
            if (XSort == null || XSort.IsSubmitted || actXSorts == null)
            {
                return false;
            }

            if (!TryGetBounds(XSort, out Bounds lowerBounds))
            {
                return false;
            }

            for (int i = 0; i < actXSorts.Count; i++)
            {
                XSortActor upper = actXSorts[i];
                if (upper == null || upper == XSort || upper.IsSubmitted)
                {
                    continue;
                }

                if (upper.LayerID >= XSort.LayerID)
                {
                    continue;
                }

                if (!TryGetBounds(upper, out Bounds upperBounds))
                {
                    continue;
                }

                if (BoundsOverlapXY(lowerBounds, upperBounds))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsPointCoveredByUpperXSorts(XSortActor XSort, Vector3 worldPoint, IReadOnlyList<XSortActor> actXSorts)
        {
            if (XSort == null || XSort.IsSubmitted || actXSorts == null)
            {
                return false;
            }

            if (!TryGetBounds(XSort, out Bounds lowerBounds))
            {
                return false;
            }

            for (int i = 0; i < actXSorts.Count; i++)
            {
                XSortActor upper = actXSorts[i];
                if (upper == null || upper == XSort || upper.IsSubmitted)
                {
                    continue;
                }

                if (upper.LayerID >= XSort.LayerID)
                {
                    continue;
                }

                if (!TryGetBounds(upper, out Bounds upperBounds))
                {
                    continue;
                }

                if (!BoundsOverlapXY(lowerBounds, upperBounds))
                {
                    continue;
                }

                if (IsPointInsideBoundsXY(worldPoint, upperBounds))
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetHighestPriorityExposedCornerIndex(XSortActor XSort, Vector3[] cornerWorldPoints, IReadOnlyList<XSortActor> actXSorts)
        {
            if (cornerWorldPoints == null || cornerWorldPoints.Length == 0)
            {
                return -1;
            }

            for (int i = 0; i < cornerWorldPoints.Length; i++)
            {
                if (!IsPointCoveredByUpperXSorts(XSort, cornerWorldPoints[i], actXSorts))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool TryGetBounds(XSortActor XSort, out Bounds bounds)
        {
            bounds = default;
            if (XSort == null)
            {
                return false;
            }

            BoxCollider collider = XSort.BoxCollider;
            if (collider == null)
            {
                collider = XSort.GetComponent<BoxCollider>();
            }

            if (collider == null)
            {
                return false;
            }

            Transform t = XSort.transform;
            Vector3 parentLossy = t.parent != null ? t.parent.lossyScale : Vector3.one;
            float sx = Mathf.Abs(parentLossy.x);
            float sy = Mathf.Abs(parentLossy.y);
            float sz = Mathf.Abs(parentLossy.z);
            if (sx < 0.01f) sx = 1f;
            if (sy < 0.01f) sy = 1f;
            if (sz < 0.01f) sz = 1f;

            Vector3 scaledCenter = new Vector3(collider.center.x * sx, collider.center.y * sy, collider.center.z * sz);
            Vector3 worldCenter = t.position + t.rotation * scaledCenter;
            Vector3 size = new Vector3(collider.size.x * sx, collider.size.y * sy, collider.size.z * sz);
            Vector3 right = t.rotation * new Vector3(size.x * 0.5f, 0f, 0f);
            Vector3 up = t.rotation * new Vector3(0f, size.y * 0.5f, 0f);
            Vector3 forward = t.rotation * new Vector3(0f, 0f, size.z * 0.5f);
            Vector3 extents = new Vector3(
                Mathf.Abs(right.x) + Mathf.Abs(up.x) + Mathf.Abs(forward.x),
                Mathf.Abs(right.y) + Mathf.Abs(up.y) + Mathf.Abs(forward.y),
                Mathf.Abs(right.z) + Mathf.Abs(up.z) + Mathf.Abs(forward.z));
            bounds = new Bounds(worldCenter, extents * 2f);
            return true;
        }

        private static bool BoundsOverlapXY(Bounds a, Bounds b)
        {
            return a.min.x <= b.max.x && a.max.x >= b.min.x
                && a.min.y <= b.max.y && a.max.y >= b.min.y;
        }

        private static bool IsPointInsideBoundsXY(Vector3 point, Bounds bounds)
        {
            return point.x >= bounds.min.x && point.x <= bounds.max.x
                && point.y >= bounds.min.y && point.y <= bounds.max.y;
        }

        static readonly List<Vector2> SpriteShape = new(64);

        public static bool SpriteCoversPoint(SpriteRenderer renderer, Vector3 worldPoint)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                return false;
            Sprite sprite = renderer.sprite;
            if (sprite == null)
                return false;

            Vector3 lossy = renderer.transform.lossyScale;
            if (Mathf.Abs(lossy.x) < 0.0001f || Mathf.Abs(lossy.y) < 0.0001f)
                return false;

            Vector3 local = renderer.transform.InverseTransformPoint(worldPoint);
            if (renderer.flipX)
                local.x = -local.x;
            if (renderer.flipY)
                local.y = -local.y;

            int shapeCount = sprite.GetPhysicsShapeCount();
            if (shapeCount <= 0)
            {
                Bounds world = renderer.bounds;
                return worldPoint.x >= world.min.x && worldPoint.x <= world.max.x
                    && worldPoint.y >= world.min.y && worldPoint.y <= world.max.y;
            }

            for (int i = 0; i < shapeCount; i++)
            {
                SpriteShape.Clear();
                sprite.GetPhysicsShape(i, SpriteShape);
                if (PointInPolygon(local.x, local.y, SpriteShape))
                    return true;
            }

            return false;
        }

        static bool PointInPolygon(float x, float y, List<Vector2> polygon)
        {
            bool inside = false;
            int count = polygon.Count;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > y) == (b.y > y))
                    continue;
                float cross = (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x;
                if (x < cross)
                    inside = !inside;
            }

            return inside;
        }
    }
}
