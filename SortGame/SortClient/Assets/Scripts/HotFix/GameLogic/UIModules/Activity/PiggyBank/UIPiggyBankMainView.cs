using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIPiggyBankMain 预制体上的节点绑定（Inspector），供 UIPanel 读取。
/// </summary>
public sealed class UIPiggyBankMainView : MonoBehaviour
{
    [SerializeField] GameObject piggy;
    [SerializeField] TextMeshProUGUI numTex;
    [SerializeField] Button tapButton;
    [SerializeField] Transform coinFlyFrom;
    [SerializeField] SkeletonGraphic goldenPig;
    [SerializeField] ParticleSystem pigCoin;

    public GameObject Piggy => piggy;
    public TextMeshProUGUI NumTex => numTex;
    public Button TapButton => tapButton;
    public Transform CoinFlyFrom => coinFlyFrom;
    public SkeletonGraphic GoldenPig => goldenPig;
    public ParticleSystem PigCoin => pigCoin;
}
