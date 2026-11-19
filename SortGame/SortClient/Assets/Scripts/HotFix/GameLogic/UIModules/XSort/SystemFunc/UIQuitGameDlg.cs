

using UnityEngine;
using UnityEngine.UI;

public class UIQuitGameDlg : UIQuitGameDlgBase
{

    protected override void OnClicked(Button button)
    {
        if (button == yesBtn)
        {
            Application.Quit();
        }
        else if (button == noBtn || button == closeBtn)
        {
            CloseSelf();
        }
    }

    protected override bool OnEscButtonPressed()
    {
        CloseSelf();
        return true;
    }

}