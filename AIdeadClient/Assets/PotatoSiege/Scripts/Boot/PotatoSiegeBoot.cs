using UnityEngine;

namespace PotatoSiege
{
    /// <summary>
    /// 场景入口：挂到空物体上，Play 后自动搭好整局游戏。
    /// </summary>
    public class PotatoSiegeBoot : MonoBehaviour
    {
        void Awake()
        {
            // Camera
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 11f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.16f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // Light (URP 2D optional — keep simple)
            if (FindObjectOfType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            // Arena floor
            var floor = new GameObject("ArenaFloor");
            floor.transform.SetParent(transform, false);
            var floorSr = floor.AddComponent<SpriteRenderer>();
            floorSr.sprite = SpriteBank.Get("floor");
            if (floorSr.sprite == SpriteBank.White || floorSr.sprite.name.Contains("Circle"))
                floorSr.sprite = SpriteBank.White;
            floorSr.color = new Color(0.22f, 0.28f, 0.24f, 1f);
            floorSr.sortingOrder = -20;
            floor.transform.localScale = new Vector3(PSConst.ArenaHalf * 2.2f, PSConst.ArenaHalf * 2.2f, 1f);

            // Border
            CreateBorder(transform);

            // Session + UI
            var sessionGo = new GameObject("GameSession");
            sessionGo.transform.SetParent(transform, false);
            var session = sessionGo.AddComponent<GameSession>();
            session.SetWorldRoot(transform);

            var uiGo = new GameObject("UIRoot");
            uiGo.transform.SetParent(transform, false);
            var ui = uiGo.AddComponent<PotatoUI>();
            ui.Build();
            session.BindUi(ui);
        }

        static void CreateBorder(Transform parent)
        {
            float h = PSConst.ArenaHalf + 0.4f;
            float t = 0.35f;
            MakeWall(parent, new Vector3(0, h, 0), new Vector3(h * 2 + t, t, 1));
            MakeWall(parent, new Vector3(0, -h, 0), new Vector3(h * 2 + t, t, 1));
            MakeWall(parent, new Vector3(h, 0, 0), new Vector3(t, h * 2 + t, 1));
            MakeWall(parent, new Vector3(-h, 0, 0), new Vector3(t, h * 2 + t, 1));
        }

        static void MakeWall(Transform parent, Vector3 pos, Vector3 scale)
        {
            var go = new GameObject("Wall");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteBank.White;
            sr.color = new Color(0.35f, 0.4f, 0.38f);
            sr.sortingOrder = -10;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }
    }
}
