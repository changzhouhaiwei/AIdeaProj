using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using FishFramework;

public class LoginScene : MonoBehaviour
{
    [SerializeField] private GameObject CheckUpdateNode;

    /// <summary>
    /// 1.StartScene->LoginScene
    /// 2.入口 GameApp（框架初始化、SDK、加载埋点均在 GameApp.CorStartLogic）
    /// </summary>
    private void Start()
    {
        EditorUiPointer.Apply();
        GameApp.Entrance();
    }
}

/// <summary>
/// Game 视图被缩放过时，新输入模块的鼠标坐标对不上按钮，欢迎窗能看见但点不中。
/// 编辑器里改回旧的 UI 点击。打包后仍走场景上原来的输入模块。
/// </summary>
static class EditorUiPointer
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply();
    }

    public static void Apply()
    {
        var systems = Object.FindObjectsOfType<EventSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            var eventSystem = systems[i];
            if (eventSystem == null || !eventSystem.gameObject.activeInHierarchy)
            {
                continue;
            }

            var modules = eventSystem.GetComponents<BaseInputModule>();
            for (int m = 0; m < modules.Length; m++)
            {
                if (modules[m] is StandaloneInputModule)
                {
                    continue;
                }

                modules[m].enabled = false;
            }

            var legacy = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacy == null)
            {
                legacy = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }

            legacy.enabled = true;
        }
    }
#else
    public static void Apply()
    {
    }
#endif
}
