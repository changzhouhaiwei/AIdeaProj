using cfg.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PuppyAdventureUiUtil
{
    public static T Find<T>(Transform root, string path) where T : Component
    {
        if (root == null || string.IsNullOrEmpty(path))
            return null;
        Transform child = root.Find(path);
        return child != null ? child.GetComponent<T>() : null;
    }

    public static void BindIf(FishFramework.UIView _, Button button, System.Action<Button> bind)
    {
        if (button != null)
            bind(button);
    }

    public static Color Brown => new Color(0.35f, 0.2f, 0.08f, 1f);

    public static int GetPlayerFrameId()
    {
        if (!GPUserDataManager.HasInit() || GPUserDataManager.GPUserData == null)
            return GPUserData.DefaultAvatarFrameId;
        int frameId = GPUserDataManager.GPUserData.userframe;
        return frameId > 0 ? frameId : GPUserData.DefaultAvatarFrameId;
    }

    public static int PickRandomFrameId()
    {
        var list = ConfigHelper.Tables?.TbAvatarFrameCfg?.DataList;
        if (list == null || list.Count == 0)
            return GPUserData.DefaultAvatarFrameId;
        return list[Random.Range(0, list.Count)].Id;
    }

    public static string GetFramePic(int frameId)
    {
        AvatarFrameCfg cfg = ConfigHelper.Tables?.TbAvatarFrameCfg?.GetOrDefault(frameId);
        return cfg != null ? cfg.Pic : null;
    }

    /// <summary>
    /// 打开 UserFrameNode、换 frame 图，并关掉 honor（Puppy 不展示连胜标记）。
    /// </summary>
    public static void ApplyAvatarFrame(Transform avatar, Sprite frameSprite)
    {
        if (avatar == null)
            return;

        Transform frameRoot = avatar.Find("UserFrameNode");
        if (frameRoot == null)
            return;

        frameRoot.gameObject.SetActive(true);

        Transform honor = AvatarFrameUiHelper.FindHonorNode(frameRoot)
            ?? AvatarFrameUiHelper.FindHonorNode(avatar);
        if (honor != null)
            honor.gameObject.SetActive(false);

        Transform wins = avatar.Find("TotalWinCount");
        if (wins != null)
            wins.gameObject.SetActive(false);

        Transform frameTran = frameRoot.Find("frame");
        if (frameTran == null)
            return;

        Image frameImage = frameTran.GetComponent<Image>();
        if (frameImage == null)
            return;

        if (frameSprite != null)
            frameImage.sprite = frameSprite;
        frameTran.gameObject.SetActive(true);
    }
}
