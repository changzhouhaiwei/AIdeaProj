using UnityEngine;

namespace Art.Role
{
    /// <summary>Follows player within open farm map bounds (no enclosed arena).</summary>
    public class BadgeCameraFollow : MonoBehaviour
    {
        public Transform target;
        public float mapHalfX = 9.2f;
        public float mapHalfY = 5.1f;
        public float smooth = 12f;

        Camera _cam;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
        }

        void LateUpdate()
        {
            if (target == null) return;
            if (_cam == null) _cam = GetComponent<Camera>();

            float halfH = _cam != null && _cam.orthographic ? _cam.orthographicSize : 5f;
            float halfW = halfH * (_cam != null ? _cam.aspect : 16f / 9f);

            float limX = Mathf.Max(0f, mapHalfX - halfW);
            float limY = Mathf.Max(0f, mapHalfY - halfH);

            Vector3 want = target.position;
            want.z = transform.position.z;
            want.x = Mathf.Clamp(want.x, -limX, limX);
            want.y = Mathf.Clamp(want.y, -limY, limY);

            float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, want, t);
        }
    }
}
