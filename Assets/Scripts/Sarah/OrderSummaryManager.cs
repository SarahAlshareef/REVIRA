using UnityEngine;
using Firebase.Database;
using TMPro;
using UnityEngine.UI;

public class OrderSummaryManager : MonoBehaviour
{
    [Header("Scroll View Section")]
    public Transform contentParent;                // Scroll View content
    public GameObject productRowPrefab;            // Prefab with nameText, quantityText, priceText + symbol

    [Header("Final Summary Section (Below Scroll View)")]
    public TextMeshProUGUI subtotalText;
    public TextMeshProUGUI discountText;
    public TextMeshProUGUI deliveryText;
    public TextMeshProUGUI totalText;

    [Header("Confirmation")]
    public Button confirmButton;
    public GameObject confirmationPopup;

    private DatabaseReference dbRef;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        confirmationPopup.SetActive(false);

        confirmButton.onClick.AddListener(() =>
        {
            confirmationPopup.SetActive(true);
        });

        LoadCartAndSummary();
    }

    void LoadCartAndSummary()
    {
        string userId = UserManager.Instance.UserId;

        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                float subtotal = 0f;

                foreach (var product in task.Result.Children)
                {
                    string productName = product.Child("productName").Value.ToString();
                    float price = float.Parse(product.Child("price").Value.ToString());

                    int totalQty = 0;
                    foreach (var size in product.Child("sizes").Children)
                    {
                        totalQty += int.Parse(size.Value.ToString());
                    }

                    subtotal += price * totalQty;

                    // Create product row
                    GameObject row = Instantiate(productRowPrefab, contentParent);

                    foreach (TextMeshProUGUI text in row.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        if (text.name == "nameText")
                            text.text = productName;

                        else if (text.name == "quantityText")
                            text.text = totalQty + "x";

                        else if (text.name == "priceText")
                            text.text = price.ToString("F2");
                    }
                }

                // Calculate summary values
                float discount = PromotionalManager.DiscountedTotal > 0 ? subtotal - PromotionalManager.DiscountedTotal : 0f;
                float delivery = DeliveryManager.DeliveryPrice;
                float total = subtotal - discount + delivery;

                // Update static summary section (separate from scroll view)
                subtotalText.text = subtotal.ToString("F2");
                discountText.text = "-" + discount.ToString("F2");
                deliveryText.text = delivery.ToString("F2");
                totalText.text = total.ToString("F2");
            }
        });
    }
}
