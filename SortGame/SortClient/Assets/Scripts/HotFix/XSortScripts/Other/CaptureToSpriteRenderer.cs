using System;
using System.Collections;
using UnityEngine;

public class CaptureToSprite : MonoBehaviour
{
    public Camera captureCam;       // 用于截图的相机
    public SpriteRenderer targetRenderer; // 用于显示截图的 SpriteRenderer
    public Camera mainCam;          // 用于显示 Sprite 的相机（一般就是主相机）
    Action mAction;

    void Start()
    {
        // StartCoroutine(CaptureAndDisplay());
    }

    void OnEnable()
    {
        // StartCoroutine(CaptureAndDisplay());
    }

    void OnDisable() //内存释放
    {
        if (targetRenderer.sprite != null)
        {
            Destroy(targetRenderer.sprite.texture);
            Destroy(targetRenderer.sprite);
            targetRenderer.sprite = null;
        }
    }

    public void CaptureNow(Action act)
    {
        mAction = act;
        StartCoroutine(CaptureAndDisplay());
    }

    IEnumerator CaptureAndDisplay()
    {
        yield return new WaitForEndOfFrame();

        // 1️⃣ 创建 RenderTexture
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        captureCam.targetTexture = rt;

        // 2️⃣ 创建 Texture2D
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        captureCam.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        // 3️⃣ 生成 Sprite
        Sprite spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        targetRenderer.sprite = spr;

        // 4️⃣ 铺满屏幕
        FitSpriteToScreen(targetRenderer, mainCam);

        // 5️⃣ 清理
        captureCam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        mAction?.Invoke();
    }

    void FitSpriteToScreen(SpriteRenderer sr, Camera cam)
    {
        if (sr.sprite == null) return;

        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        float spriteWidth = sr.sprite.bounds.size.x;
        float spriteHeight = sr.sprite.bounds.size.y;

        // 计算缩放比例
        Vector3 scale = sr.transform.localScale;
        scale.x = screenWidth / spriteWidth;
        scale.y = screenHeight / spriteHeight;
        sr.transform.localScale = scale;

        // 保证居中显示
        sr.transform.position = cam.transform.position + cam.transform.forward * 1f;
    }
}
