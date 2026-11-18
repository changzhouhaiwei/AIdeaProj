using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using xasset;
using FishFramework;
using Utility = xasset.Utility;

internal class CheckUpdateScreen : MonoBehaviour
{
    public TextMeshProUGUI version;
    public GameObject progressBg;
    public Image progressBar;
    public TextMeshProUGUI msgText;

    private string newVersion;
    private Action mCompleted;

    public void Initialize(Action completed)
    {
        mCompleted = completed;
        SetVersion();
#if UNITY_EDITOR
        EnterGame();
#else
        {
            EnterGame();
        }
#endif
    }

    public void SetVisible(bool visible)
    {
        progressBg.SetActive(visible);
    }

    private void OnMessage(string message, float progress)
    {
        msgText.text = message;
        progressBar.fillAmount = progress;
    }

    private void SetVersion()
    {
        version.text = $"v_{PlayerPrefs.GetString(SettingModule.RES_VERSION)}";
    }

    private void StartUpdate()
    {
        var url = GameModule.Setting.GetServerVersionURL();
        Debug.Log("version_url--" + url);
        var request = UnityWebRequest.Get(url);
        request.SendWebRequest().completed += operation =>
        {
            if (!string.IsNullOrEmpty(request.error))
            {
                // tips:获取服务器版本失败：{request.error}
                MessageBox.Show(string.Format(I2LocalizeMgr.GetString("HotFangshenhe_1"), request.error), retry =>
                {
                    if (retry)
                    {
                        StartUpdate();
                    }
                    else
                    {
                        Quit();
                    }
                }, "重试");
            }
            else
            {
                var json = request.downloadHandler.text;
                Debug.Log(json);
                var versionData = JsonUtility.FromJson<ServerVersions>(json);
                Downloader.DownloadURL = $"{versionData.web_res_url}/";
                Debug.Log("Assets.downloadURL -------------- " + Downloader.DownloadURL);
                if (!CheckGameBigUpdate(versionData))
                {
                    var serverVersion = new System.Version(versionData.res_version);
                    var localVersion = new System.Version(PlayerPrefs.GetString(SettingModule.RES_VERSION));
                    if (serverVersion > localVersion)
                    {
                        newVersion = versionData.res_version;
                        StartCheck();
                    }
                    else
                    {
                        EnterGame();
                    }
                }
            }

            request.Dispose();
        };
    }

    private bool CheckGameBigUpdate(ServerVersions versionData)
    {
        if (versionData.big_version <= GameModule.Setting.GetGameSettings().appVersion) return false;

        GameBigUpdateDlg.OpenUI(() => { Application.OpenURL(versionData.big_version_url); });

        return true;
    }

    private void StartCheck()
    {
        StartCoroutine(Checking());
    }

    private IEnumerator Checking()
    {
        var url = $"{Downloader.DownloadURL}/{newVersion}/{Versions.Filename}";
        var checking = Versions.CheckUpdateAsync(url);
        yield return checking;
        if (checking.status == OperationStatus.Failed)
        {
            // tips:检查版本信息失败，请检测网络链接后重试
            yield return MessageBox.Show(I2LocalizeMgr.GetString("HotFangshenhe_2"), ok =>
            {
                if (ok)
                {
                    StartCheck(); // 重新启动更新检查
                }
                else
                {
                    Quit();
                }
            }, "重试");
            yield break;
        }

        Debug.Log("Checking");

        if (checking.downloadSize > 0)
        {
            StartCoroutine(CheckUpdateDownloading(checking.DownloadAsync(), GetDownloadSizeAsync));
            yield break;
        }

        GetDownloadSizeAsync();
    }

    private IEnumerator CheckUpdateDownloading(DownloadFiles download, Action completed)
    {
        while (download.status != OperationStatus.Success)
        {
            yield return null;
            if (!download.isDone || download.status == OperationStatus.Success)
                continue;
            var messageBox = MessageBox.Show(I2LocalizeMgr.GetString("HotFangshenhe_3")); // tips:请检查网络状态后重试
            yield return messageBox;
            if (messageBox.ok)
            {
                download.Retry();
            }
            else
            {
                Application.Quit();
                break;
            }
        }

        if (download.status == OperationStatus.Success) completed?.Invoke();
    }

    private void GetDownloadSizeAsync()
    {
        StartCoroutine(GetDownloadSize());
    }

    private IEnumerator GetDownloadSize()
    {
        var getDownloadSize = Versions.GetDownloadSizeAsync();
        yield return getDownloadSize;
        if (getDownloadSize.downloadSize > 0)
        {
            DownloadAsync(getDownloadSize.DownloadAsync(), OnComplete);
            yield break;
        }

        OnComplete();
    }

    private void DownloadAsync(DownloadFiles download, Action completed)
    {
        StartCoroutine(Downloading(download, completed));
    }

    private IEnumerator Downloading(DownloadFiles download, Action completed)
    {
        SetVisible(true);

        while (download.status != OperationStatus.Success)
        {
            var current = Download.TotalDownloadedBytes;
            var max = Download.TotalSize;
            var speed = Download.TotalBandwidth;
            if (current >= max)
            {
                OnMessage("", 1);
            }
            else
            {
                SetSpeedMessage(current, max, speed);
            }

            yield return null;
            if (!download.isDone || download.status == OperationStatus.Success)
                continue;
            var messageBox = MessageBox.Show(I2LocalizeMgr.GetString("HotFangshenhe_4")); // tips:下载失败！请检查网络状态后重试
            yield return messageBox;
            if (messageBox.ok)
            {
                download.Retry();
            }
            else
            {
                Application.Quit();
                break;
            }
        }

        SetVisible(false);
        if (download.status == OperationStatus.Success) completed?.Invoke();
    }

    private void SetSpeedMessage(long current, long max, long speed)
    {
        OnMessage($"{Utility.FormatBytes(current)}/{Utility.FormatBytes(max)}(Speed {Utility.FormatBytes(speed)}/s)", current * 1f / max);
    }

    private void OnComplete()
    {
        Debug.Log("OnComplete");
        if (!string.IsNullOrEmpty(newVersion))
        {
            PlayerPrefs.SetString(SettingModule.RES_VERSION, newVersion);
        }

        SetVersion();
        // tips:更新完成
        OnMessage(I2LocalizeMgr.GetString("HotFangshenhe_5"), 1);
        EnterGame();
    }

    private void EnterGame()
    {
        Debug.Log("EnterGame");
#if UNITY_EDITOR
        PermanentResources(mCompleted);
#else
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            StartCoroutine(PreloadResources());
        }
        else
        {
            PermanentResources(mCompleted);
        }
#endif
    }

    private IEnumerator PreloadResources()
    {
        var getDownloadSizeAsync = Versions.GetDownloadSizeAsyncWithManifests("Arts", "DLL", "Videos");
        yield return getDownloadSizeAsync;
        var downloadAsync = getDownloadSizeAsync.DownloadAsync();
        while (downloadAsync.status != OperationStatus.Success)
        {
            yield return null;
        }

        PermanentResources(mCompleted);
    }

    private void PermanentResources(Action completed)
    {
        Asset.LoadAsync("Assets/CABRes/Shaders/Gray.shader", typeof(Shader), delegate(Asset asset) { completed?.Invoke(); });
    }

    private void OnDestroy()
    {
        MessageBox.Dispose();
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}