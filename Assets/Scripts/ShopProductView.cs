using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    public enum ShopResourceType
    {
        Feathers = 0,
        Hints = 1
    }

    public sealed class ShopProductView : MonoBehaviour
    {
        public ShopResourceType resourceType;
        [Min(1)] public int amount = 5;
        [Min(0)] public int price = 100;
        public string titleOverride;
        public Text nameText;
        public Text detailText;
        public Text priceText;
        public Image icon;

        public void Buy(CryptogramGame game)
        {
            var owner = game != null ? game : CryptogramGame.Current;
            owner?.BuyShopProduct(resourceType, amount, price);
        }

        public void ApplyPresentation()
        {
            if (nameText != null)
                nameText.text = string.IsNullOrWhiteSpace(titleOverride)
                    ? resourceType == ShopResourceType.Feathers ? "Пачка перьев" : "Подсказки"
                    : titleOverride;

            if (detailText != null)
                detailText.text = amount + (resourceType == ShopResourceType.Feathers ? " перьев" : " подсказок");

            if (priceText != null)
                priceText.text = price.ToString();
        }

        private void Awake()
        {
            ApplyPresentation();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            amount = Mathf.Max(1, amount);
            price = Mathf.Max(0, price);
            ApplyPresentation();
        }
#endif
    }
}
