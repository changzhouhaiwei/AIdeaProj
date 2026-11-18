using TMPro;
using UnityEngine;

public class PuppyAdventureHud : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI progressLb;

    void OnEnable()
    {
        Refresh();
        PuppyAdventureDemoState.Changed += Refresh;
    }

    void OnDisable()
    {
        PuppyAdventureDemoState.Changed -= Refresh;
    }

    public void Refresh()
    {
        if (progressLb != null)
            progressLb.text = string.Empty;
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
    }
}
