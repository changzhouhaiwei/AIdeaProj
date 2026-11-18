using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TileGameCore;

namespace FishTilePlay
{
    public class PUReward : Reward
    {
        [SerializeField] PUModel[] powerUpsData;

        public override void Init()
        {
            foreach (PUModel powerUpData in powerUpsData)
            {
                powerUpData.Init();

                if (powerUpData.IconImage != null)
                {
                    PUActor powerUpBehavior = PUDirector.GetPowerUpBehavior(powerUpData.PowerUpType);
                    if (powerUpBehavior != null)
                    {
                        powerUpData.IconImage.sprite = powerUpBehavior.Settings.Icon;
                    }
                }

                if (powerUpData.AmountText != null)
                {
                    powerUpData.AmountText.text = string.Format(string.IsNullOrEmpty(powerUpData.TextFormating) ? powerUpData.Amount.ToString() : string.Format(powerUpData.TextFormating, powerUpData.Amount));
                }
            }
        }

        public override void ApplyReward()
        {
            foreach (PUModel powerUpData in powerUpsData)
            {
                PUDirector.AddPowerUp(powerUpData.PowerUpType, powerUpData.Amount);

                TextMeshProUGUI floatingText = powerUpData.PurchaseFloatingText;
                if (floatingText != null)
                {
                    floatingText.gameObject.SetActive(true);

                    floatingText.text = string.Format("+{0}", powerUpData.Amount);

                    RectTransform textRectTransform = floatingText.rectTransform;
                    textRectTransform.anchoredPosition = powerUpData.FloatingTextPosition;

                    floatingText.color = floatingText.color.SetAlpha(1.0f);

                    textRectTransform.DOAnchoredPosition(textRectTransform.anchoredPosition + new Vector2(0, 100), 1.0f).SetEasing(DG.Tweening.Ease.InSine);
                    floatingText.DOFade(0.0f, 1.0f).SetEasing(DG.Tweening.Ease.InQuint).OnComplete(() =>
                    {
                        textRectTransform.anchoredPosition = powerUpData.FloatingTextPosition;
                        floatingText.gameObject.SetActive(false);
                    });
                }
            }
        }


        [System.Serializable]
        public class PUModel
        {
            [SerializeField] PUType powerUpType;
            public PUType PowerUpType => powerUpType;

            [SerializeField] int amount;
            public int Amount => amount;

            [Space]
            [SerializeField] Image iconImage;
            public Image IconImage => iconImage;

            [SerializeField] TextMeshProUGUI amountText;
            public TextMeshProUGUI AmountText => amountText;

            [SerializeField] TextMeshProUGUI purchaseFloatingText;
            public TextMeshProUGUI PurchaseFloatingText => purchaseFloatingText;

            [SerializeField] string textFormating = "x{0}";
            public string TextFormating => textFormating;

            private Vector2 floatingTextPosition;
            public Vector2 FloatingTextPosition => floatingTextPosition;

            public void Init()
            {
                if(purchaseFloatingText != null)
                {
                    floatingTextPosition = purchaseFloatingText.rectTransform.anchoredPosition;
                }
            }
        }
    }
}
