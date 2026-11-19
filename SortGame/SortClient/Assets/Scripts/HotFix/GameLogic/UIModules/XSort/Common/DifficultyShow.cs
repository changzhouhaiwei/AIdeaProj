
using TMPro;
using XSortGamePlay;
using XSortGameCore;
using UnityEngine;
using UnityEngine.UI;

class DifficultyShow : MonoBehaviour
{
    [SerializeField]
    Image frameImage;

    [SerializeField]
    TextMeshProUGUI hardText;

    public void RefreshUI(int levelIndex)
    {
        Color hard = new Color(242, 78, 78, 255) / 255f;
        Color superHard = new Color(173, 29, 221, 255) / 255f;

        if (GameData.BFromStartScene)
        {
            gameObject.SetActive(false);

            var config = LevelDynamicConfigProvider.GetLevelConfig(levelIndex + 1);
            if (config != null && config.Difficulty > 0)
            {
                gameObject.SetActive(true);
                if (config.Difficulty == 1)
                {
                    frameImage.color = hard;
                    LocalizedTMPRefresher.SetText(hardText, () =>
                        I2LocalizeMgr.GetString("DifficultyName" + config.Difficulty));
                }
                else if (config.Difficulty == 2)
                {
                    frameImage.color = superHard;
                    LocalizedTMPRefresher.SetText(hardText, () =>
                        I2LocalizeMgr.GetString("DifficultyName" + config.Difficulty));
                }
            }
        }
        else
        {
            gameObject.SetActive(false);
        }   

    }
    

}