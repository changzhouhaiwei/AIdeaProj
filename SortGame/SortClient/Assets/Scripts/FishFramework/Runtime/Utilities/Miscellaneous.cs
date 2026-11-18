using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FishFramework
{
    public static class Miscellaneous
    {
        // Cooodernadas Horizontal e Vetical, 0:y, 1:x
        public static int[,] coord4 = new int[,]
        {
            {  0,  1 },
            { -1,  0 },
            {  0, -1 },
            {  1,  0 }
        };

        // Cooodernadas Horizontal e Vetical + Diagonais, 0:y, 1:x
        public static int[,] coord8 = new int[,]
        {
            {  0,  1 },
            { -1,  1 },
            { -1,  0 },
            { -1, -1 },
            {  0, -1 },
            {  1, -1 },
            {  1,  0 },
            {  1,  1 } 
        };

        // 用定义的值填充一维数组
        public static void Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        }

        // 用定义的值填充二维数组
        public static void Populate<T>(this T[,] arr, T value)
        {
            for (int j = 0; j < arr.GetLength(0); ++j)
            {
                for (int i = 0; i < arr.GetLength(1); ++i)
                {
                    arr[j, i] = value;
                }
            }
        }

        // 用定义的值填充数组的一个区域
        public static void PopulateArea<T>(
            this T[,] arr, T value, int x, int y, int xLength, int yLength
        )
        {
            for (int j = y; j < y + yLength; ++j)
            {
                for (int i = x; i < x + xLength; ++i)
                {
                    arr[j, i] = value;
                }
            }
        }

        // 如果是奇数，则返回下一对
        public static int ToEven(int n)
        {
            return n % 2 == 0 ? n : n + 1;
        }

        // 如果是偶数，则返回下一个奇数
        public static int ToOdd(int n)
        {
            return n % 2 == 0 ? n + 1 : n;
        }

        // StartCoroutine (InvokeRealtimeCoroutine (DoSomething, seconds));
        public static IEnumerator InvokeRealtimeCoroutine(Action action, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action?.Invoke();
        }

        public static IEnumerator InvokeCoroutine(Action action, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }
        
        public static Vector3 WorldPositionToCanvas(
            Vector3 position, RectTransform canvas, Camera camera
        )
        {
            Vector3 viewport = camera.WorldToViewportPoint(position);
            return new Vector3(
                viewport.x * canvas.sizeDelta.x,
                viewport.y * canvas.sizeDelta.y
            );
        }

        public static void SetCameraOrthographicSizeByWidth(
            Camera camera, float width
        )
        {
            camera.orthographicSize = (width / camera.aspect) / 2;
        }

        public static Vector3 ClampPositionToCameraLimits(
            Camera camera, Vector3 position, float wOffset = 0, float hOffset = 0
        )
        {
            float halfCameraWidth = HalfWidthCamera(camera);
            return new Vector3(Mathf.Clamp(
                    position.x,
                    -(halfCameraWidth - wOffset),
                    halfCameraWidth - wOffset),
                Mathf.Clamp(
                    position.y,
                    -(camera.orthographicSize - hOffset),
                    camera.orthographicSize - hOffset
                )
            );
        }

        public static bool CheckScreenBoundaries(
            Camera camera, Vector3 position,
            float wOffset = 0, float hOffset = 0
        )
        {
            float halfhalfCameraWidth = HalfWidthCamera(camera);
            return !(position.x < -(halfhalfCameraWidth + wOffset)
                     || position.x > halfhalfCameraWidth + wOffset
                     || position.y < -(camera.orthographicSize + hOffset)
                     || position.y > camera.orthographicSize + hOffset);
        }

        public static float HalfWidthCamera(Camera camera)
        {
            return camera.orthographicSize * camera.aspect;
        }

        public static float CameraViewportWidth(Camera camera, float w)
        {
            return Mathf.Lerp(-HalfWidthCamera(camera), HalfWidthCamera(camera), w);
        }

        public static float CameraViewportHeight(Camera camera, float h)
        {
            return Mathf.Lerp(-camera.orthographicSize, camera.orthographicSize, h);
        }

        // 创建一个带有颜色的纹理
        public static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public static Color ColorHEX(string hex)
        {
            if (hex.Length != 6) return Color.black;

            int r = int.Parse(hex.Substring(0, 2),
                System.Globalization.NumberStyles.HexNumber);
            int g = int.Parse(hex.Substring(2, 2),
                System.Globalization.NumberStyles.HexNumber);
            int b = int.Parse(hex.Substring(4, 2),
                System.Globalization.NumberStyles.HexNumber);

            return new Color(r / 255f, g / 255f, b / 255f);
        }

        public static float Map(
            float value, float fromMin,
            float fromMax, float toMin, float toMax
        )
        {
            return Mathf.Lerp(
                toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value)
            );
        }

        public static List<T> RandomizeOrder<T>(List<T> list)
        {
            if (list == null) return null;

            T[] array = list.ToArray();
            for (int i = 0; i < list.Count; ++i)
            {
                int rand = Random.Range(0, list.Count);
                T temp = array[rand];
                array[rand] = array[i];
                array[i] = temp;
            }

            return new List<T>(array);
        }

        public static T[,] ShuffleMatrix<T>(T[,] arr)
        {
            int m = arr.GetLength(0);
            int n = arr.GetLength(1);

            for (int i = m * n - 1; i > 0; --i)
            {
                int j = Random.Range(0, i + 1);

                T temp = arr[i / n, i % n];
                arr[i / n, i % n] = arr[j / n, j % n];
                arr[j / n, j % n] = temp;
            }

            return arr;
        }

        // 如果x是负的，对m + x取余
        public static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        public static T[] FindComponentsInChildrenWithTag<T>(
            this GameObject parent, string tag, bool forceActive = false
        ) where T : Component
        {
            if (parent == null)
            {
                throw new System.ArgumentNullException();
            }

            if (string.IsNullOrEmpty(tag) == true)
            {
                throw new System.ArgumentNullException();
            }

            List<T> list = new List<T>(
                parent.GetComponentsInChildren<T>(forceActive)
            );

            if (list.Count == 0)
            {
                return null;
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].CompareTag(tag) == false)
                {
                    list.RemoveAt(i);
                }
            }

            return list.ToArray();
        }

        public static T FindComponentInChildWithTag<T>(
            this GameObject parent, string tag
        ) where T : Component
        {
            Transform t = parent.transform;
            foreach (Transform tr in t)
            {
                if (tr.tag == tag)
                {
                    return tr.GetComponent<T>();
                }
            }

            return null;
        }

        public static float GetCurrentStateDuration(this Animator animator, int layerIndex = 0)
        {
            animator.Update(0);

            AnimatorClipInfo[] animatorClipInfos = animator.GetCurrentAnimatorClipInfo(layerIndex);
            float speedMultiplier = animator.GetCurrentAnimatorStateInfo(0).speed;

            if (animatorClipInfos.Length == 0 || speedMultiplier == 0)
                return 0;

            float duration = animatorClipInfos[0].clip.length;

            return duration / Mathf.Abs(speedMultiplier);
        }

        public static void ChangeAllParticleSystemsSortingLayer(Transform parentTransform, string sortingLayerName)
        {
            // 获取节点下所有的粒子系统组件
            ParticleSystem[] particleSystems = parentTransform.GetComponentsInChildren<ParticleSystem>(true);

            // 遍历所有粒子系统
            foreach (ParticleSystem ps in particleSystems)
            {
                // 获取粒子系统的Renderer组件
                ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();

                // 确保找到了Renderer组件
                if (psRenderer != null)
                {
                    // 设置新的排序层级名称
                    psRenderer.sortingLayerName = sortingLayerName;
                }
            }
        }

        public static T FindInParents<T>(GameObject go) where T : Component
        {
            if (go == null) return null;
            var comp = go.GetComponent<T>();

            if (comp != null)
                return comp;

            Transform t = go.transform.parent;
            while (t != null && comp == null)
            {
                comp = t.gameObject.GetComponent<T>();
                t = t.parent;
            }

            return comp;
        }
    }
}