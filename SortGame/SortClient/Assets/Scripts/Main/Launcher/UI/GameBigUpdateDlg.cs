using System;
using UnityEngine;
using UnityEngine.UI;
using xasset;
using Object = UnityEngine.Object;

public static class GameBigUpdateDlg
{
    private static Asset mAsset = null;
    private static GameObject gameObject = null;

    public static void OpenUI(Action completed = null)
    {
        if (mAsset == null)
        {
            mAsset = Asset.Load("Assets/CABRes/Prefabs/HotFangshenhe/GameBigUpdateDlg.prefab", typeof(GameObject));
            gameObject = Object.Instantiate(mAsset.asset as GameObject);
        }
        var updateButton = gameObject.FindChildComponent<Button>("nineBg/updateBtn");
        updateButton.onClick.AddListener(delegate ()
        {
            completed?.Invoke();
            // Dispose();
        });
    }
    
    private static void Dispose()
    {
        Object.Destroy(gameObject);
        gameObject = null;
        mAsset.ReleaseRef();
        mAsset = null;
    }
}
