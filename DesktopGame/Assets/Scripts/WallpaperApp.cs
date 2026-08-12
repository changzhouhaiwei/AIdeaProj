using System;
using UnityEngine;

/// <summary>
/// Train-window live wallpaper. Port of AIdeaDesktop (pygame) logic.
/// Assign sprites on the component, or leave null to Resources.Load from Wallpaper/.
/// </summary>
public class WallpaperApp : MonoBehaviour
{
    [Header("Mode")]
    [Tooltip("挂到桌面 WorkerW（仅 Standalone 生效；Editor 始终预览）")]
    [SerializeField] bool desktopMode;

    [Header("Sprites (optional — null then Resources/Wallpaper/*)")]
    [SerializeField] Sprite foreground;
    [SerializeField] Sprite background;
    [SerializeField] TextAsset configAsset;
    [SerializeField] TextAsset charLayoutAsset;
    [SerializeField] Sprite[] characterSprites;

    [Header("Runtime (overridden by config if present)")]
    [SerializeField] float speed = 48f;
    [SerializeField] int targetFps = 30;
    [Tooltip("远景缩放：1=贴齐屏高；小于1缩小（看得更多），大于1放大")]
    [SerializeField] [Range(0.2f, 3f)] float backgroundScale = 0.75f;

    Camera _cam;
    Transform _root;
    SpriteRenderer _fg;
    SpriteRenderer[] _bgTiles;
    CharacterCycle _chars;
    float _offset;
    float _bgWorldWidth;
    bool _paused;
    float _reassertTimer;
    IntPtr _hwnd;
    IntPtr _worker;
    int _screenW;
    int _screenH;
    bool _useDesktop;

    void Awake()
    {
        var args = Environment.GetCommandLineArgs();
        foreach (var a in args)
        {
            if (string.Equals(a, "--desktop", StringComparison.OrdinalIgnoreCase))
                desktopMode = true;
            if (string.Equals(a, "--preview", StringComparison.OrdinalIgnoreCase))
                desktopMode = false;
        }

        _useDesktop = desktopMode && !Application.isEditor;

        var cfg = WallpaperConfig.Load(configAsset);
        speed = cfg.speed > 0f ? cfg.speed : speed;
        targetFps = cfg.fps > 0 ? cfg.fps : targetFps;
        if (cfg.backgroundScale > 0f)
            backgroundScale = cfg.backgroundScale;
        Application.targetFrameRate = Mathf.Clamp(targetFps, 15, 60);
        QualitySettings.vSyncCount = 0;

        if (foreground == null)
            foreground = Resources.Load<Sprite>("Wallpaper/foreground");
        if (background == null)
            background = Resources.Load<Sprite>("Wallpaper/background");

        if (foreground == null || background == null)
        {
            Debug.LogError("[WallpaperApp] 缺少 foreground/background Sprite。请放到 Resources/Wallpaper/ 或挂到组件上。");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        if (!enabled) return;

        if (_useDesktop)
        {
            var size = DesktopWinHost.PrimaryScreenSize();
            _screenW = size.x;
            _screenH = size.y;
            Screen.SetResolution(_screenW, _screenH, FullScreenMode.FullScreenWindow);
        }
        else
        {
            _screenW = 1280;
            _screenH = 720;
            Screen.SetResolution(_screenW, _screenH, FullScreenMode.Windowed);
        }

        SetupCamera();
        SetupLayers();

        if (_useDesktop)
            StartCoroutine(EmbedNextFrame());
    }

    System.Collections.IEnumerator EmbedNextFrame()
    {
        yield return null;
        yield return null;
        try
        {
            _hwnd = DesktopWinHost.FindUnityHwnd();
            _worker = DesktopWinHost.EmbedAsWallpaper(_hwnd);
            Debug.Log($"[WallpaperApp] desktop host=0x{_hwnd.ToInt64():X} worker=0x{_worker.ToInt64():X}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[WallpaperApp] WorkerW 嵌入失败: {e}");
        }
    }

    void SetupCamera()
    {
        _cam = Camera.main;
        if (_cam == null)
        {
            var go = new GameObject("Main Camera");
            _cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
        }

        _cam.orthographic = true;
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = Color.black;
        _cam.orthographicSize = _screenH / 2f;
        _cam.transform.position = new Vector3(_screenW / 2f, _screenH / 2f, -10f);
        _cam.nearClipPlane = 0.1f;
        _cam.farClipPlane = 100f;
    }

    void SetupLayers()
    {
        _root = new GameObject("WallpaperRoot").transform;

        // Background: fit height × backgroundScale, tiled horizontally.
        float bgTexW = background.rect.width;
        float bgTexH = background.rect.height;
        float bgScale = (_screenH / bgTexH) * Mathf.Max(0.05f, backgroundScale);
        _bgWorldWidth = bgTexW * bgScale;

        int tileCount = Mathf.Max(2, Mathf.CeilToInt(_screenW / _bgWorldWidth) + 2);
        _bgTiles = new SpriteRenderer[tileCount];
        for (int i = 0; i < tileCount; i++)
        {
            var go = new GameObject($"BG_{i}");
            go.transform.SetParent(_root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = background;
            sr.sortingOrder = 0;
            go.transform.localScale = new Vector3(bgScale, bgScale, 1f);
            // Center pivot assumed.
            go.transform.position = new Vector3(
                i * _bgWorldWidth + _bgWorldWidth * 0.5f,
                _screenH * 0.5f,
                0f);
            _bgTiles[i] = sr;
        }

        // Foreground cover-fit.
        float fgTexW = foreground.rect.width;
        float fgTexH = foreground.rect.height;
        float fgScale = Mathf.Max(_screenW / fgTexW, _screenH / fgTexH);
        var fgGo = new GameObject("Foreground");
        fgGo.transform.SetParent(_root, false);
        _fg = fgGo.AddComponent<SpriteRenderer>();
        _fg.sprite = foreground;
        _fg.sortingOrder = 10;
        fgGo.transform.localScale = new Vector3(fgScale, fgScale, 1f);
        fgGo.transform.position = new Vector3(_screenW * 0.5f, _screenH * 0.5f, 0f);

        int fgNativeW = Mathf.RoundToInt(fgTexW);
        int fgNativeH = Mathf.RoundToInt(fgTexH);
        _chars = new CharacterCycle(
            _root, _screenW, _screenH, fgNativeW, fgNativeH,
            charLayoutAsset, characterSprites);
        if (_chars.Enabled)
            Debug.Log("[WallpaperApp] Character cycle ready");
        else
            Debug.LogWarning("[WallpaperApp] No standing characters loaded");
    }

    void Update()
    {
        float dt = Time.deltaTime;
        HandleInput();

        if (_useDesktop && _hwnd != IntPtr.Zero && _worker != IntPtr.Zero)
        {
            _reassertTimer += dt;
            if (_reassertTimer >= 5f)
            {
                _reassertTimer = 0f;
                DesktopWinHost.KeepBehindIcons(_hwnd, _worker);
            }
            if (DesktopWinHost.HotkeyQuitPressed())
            {
                Application.Quit();
                return;
            }
        }

        if (!_paused && _bgWorldWidth > 0f)
            _offset = (_offset + speed * dt) % _bgWorldWidth;

        // Shift tiles so one continuous strip scrolls left.
        float x0 = -_offset;
        for (int i = 0; i < _bgTiles.Length; i++)
        {
            float x = x0 + i * _bgWorldWidth + _bgWorldWidth * 0.5f;
            // Keep tiles covering [0, screenW] by wrapping.
            while (x < -_bgWorldWidth * 0.5f)
                x += _bgTiles.Length * _bgWorldWidth;
            while (x > _screenW + _bgWorldWidth * 0.5f)
                x -= _bgTiles.Length * _bgWorldWidth;
            var p = _bgTiles[i].transform.position;
            p.x = x;
            _bgTiles[i].transform.position = p;
        }

        _chars?.Update(dt, _paused);
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
        {
            if (!_useDesktop)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
            _paused = !_paused;
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            speed = Mathf.Max(0f, speed - 8f);
        if (Input.GetKeyDown(KeyCode.RightArrow))
            speed = Mathf.Min(400f, speed + 8f);
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            targetFps = Mathf.Min(60, targetFps + 5);
            Application.targetFrameRate = targetFps;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            targetFps = Mathf.Max(15, targetFps - 5);
            Application.targetFrameRate = targetFps;
        }
        // [ / ] 实时微调远景缩放（需重启 Play 才重建图层时无效；运行中直接改 tile scale）
        if (Input.GetKeyDown(KeyCode.LeftBracket))
            AdjustBackgroundScale(-0.05f);
        if (Input.GetKeyDown(KeyCode.RightBracket))
            AdjustBackgroundScale(0.05f);
    }

    void AdjustBackgroundScale(float delta)
    {
        backgroundScale = Mathf.Clamp(backgroundScale + delta, 0.2f, 3f);
        if (_bgTiles == null || background == null) return;

        float bgTexW = background.rect.width;
        float bgTexH = background.rect.height;
        float bgScale = (_screenH / bgTexH) * backgroundScale;
        _bgWorldWidth = bgTexW * bgScale;
        for (int i = 0; i < _bgTiles.Length; i++)
        {
            _bgTiles[i].transform.localScale = new Vector3(bgScale, bgScale, 1f);
        }
        Debug.Log($"[WallpaperApp] backgroundScale={backgroundScale:F2}");
    }
}
