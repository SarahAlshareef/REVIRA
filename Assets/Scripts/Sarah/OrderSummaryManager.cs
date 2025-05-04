using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;

public class OrderSummaryManager : MonoBehaviour
{
    [Header("Scroll View Product Row")]
    public GameObject productPrefab;
    public Transform productListParent;

    [Header("Summary Fields")]
    public TextMeshProUGUI subtotalText;
    public TextMeshProUGUI discountText;
    public TextMeshProUGUI deliveryChargesText;
    public TextMeshProUGUI totalText;

    public Image subtotalSymbol;
    public Image discountSymbol;
    public Image deliverySymbol;
    public Image totalSymbol;

    [Header("Confirm Order")]
    public Button confirmButton;
    public GameObject confirmPopup;

    private DatabaseReference dbRef;
    private string userId;
    private float subtotal = 0f;
    private float promoDiscountAmount = 0f;
    private float delivery = 0f;
    private float total = 0f;

    private int productsToProcess = 0;
    private int productsProcessed = 0;
    private int productsDisplayed = 0;

    // Singleton instance for external access
    public static OrderSummaryManager Instance { get; private set; }
    // Final total static property
    public static float FinalTotal { get; private set; }
    // Expose subtotal for ConfirmOrderManager
    public float Subtotal => subtotal;

    void Start()
    {
        Instance = this;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;

        if (confirmButton != null && confirmPopup != null)
            confirmButton.onClick.AddListener(() => confirmPopup.SetActive(true));

        LoadOrderData();
    }

    void LoadOrderData()
    {
        Debug.Log($"[OrderSummaryManager] Loading cart for userId={userId}");
        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("cart")
            .GetValueAsync().ContinueWithOnMainThread(cartTask1 =>
            {
                if (cartTask1.IsFaulted || !cartTask1.Result.Exists)
                {
                    Debug.LogWarning("[OrderSummaryManager] No 'cart' node or failed to load cart.");
                    SetZeroSummary();
                    return;
                }

                var cartSnapshot = cartTask1.Result;
                if (!cartSnapshot.HasChild("cartItems") || cartSnapshot.Child("cartItems").ChildrenCount == 0)
                {
                    Debug.LogWarning("[OrderSummaryManager] 'cartItems' empty or missing.");
                    SetZeroSummary();
                    return;
                }

                // Clear previous list
                foreach (Transform child in productListParent)
                    Destroy(child.gameObject);

                subtotal = 0f;
                productsDisplayed = 0;
                productsProcessed = 0;
                var itemsSnapshot = cartSnapshot.Child("cartItems");
                productsToProcess = (int)itemsSnapshot.ChildrenCount;

                foreach (var item in itemsSnapshot.Children)
                {
                    string productId = item.Key;
                    string productName = item.Child("productName").Value?.ToString() ?? "";
                    float originalPrice = float.TryParse(item.Child("price").Value?.ToString(), out float priceVal) ? priceVal : 0f;
                    int quantity = 0;
                    foreach (var size in item.Child("sizes").Children)
                        quantity += int.TryParse(size.Value.ToString(), out int q) ? q : 0;

                    if (quantity <= 0)
                    {
                        Debug.LogWarning($"[OrderSummaryManager] Skipping product {productId} with 0 quantity.");
                        productsProcessed++;
                        CheckFinishedProcessing();
                        continue;
                    }

                    // Fetch product details
                    dbRef.Child("REVIRA").Child("stores").Child("storeID_123").Child("products").Child(productId)
                        .GetValueAsync().ContinueWithOnMainThread(productTask =>
                        {
                            float finalPrice = originalPrice;
                            if (productTask.IsCompleted && productTask.Result.Exists)
                            {
                                var prodSnap = productTask.Result;
                                bool hasDiscount = prodSnap.Child("discount").Child("exists").Value?.ToString() == "True";
                                if (hasDiscount)
                                {
                                    float.TryParse(prodSnap.Child("discount").Child("percentage").Value.ToString(), out float discountPct);
                                    finalPrice = originalPrice - (originalPrice * discountPct / 100f);
                                }
                            }

                            float itemTotal = finalPrice * quantity;
                            subtotal += itemTotal;
                            productsDisplayed++;

                            // Instantiate UI row
                            GameObject productGO = Instantiate(productPrefab, productListParent);
                            productGO.transform.Find("nameText").GetComponent<TextMeshProUGUI>().SetText(productName);
                            productGO.transform.Find("quantityText").GetComponent<TextMeshProUGUI>().SetText(quantity + "x");
                            productGO.transform.Find("priceText").GetComponent<TextMeshProUGUI>().SetText(itemTotal.ToString("F2"));

                            productsProcessed++;
                            CheckFinishedProcessing();
                        });
                }
            });
    }

    void CheckFinishedProcessing()
    {
        if (productsProcessed >= productsToProcess)
        {
            if (productsDisplayed == 0)
            {
                Debug.LogWarning("[OrderSummaryManager] No valid products displayed.");
                SetZeroSummary();
            }
            else
            {
                FetchPromoAndDelivery();
            }
        }
    }

    void SetZeroSummary()
    {
        subtotalText.text = "0.00";
        discountText.text = "0.00";
        deliveryChargesText.text = "0.00";
        totalText.text = "0.00";
        FinalTotal = 0f;
    }

    void FetchPromoAndDelivery()
    {
        float promoTotal = PromotionalManager.DiscountedTotal;
        promoDiscountAmount = !string.IsNullOrEmpty(PromotionalManager.UsedPromoCode) ? subtotal - promoTotal : 0f;
        delivery = DeliveryManager.DeliveryPrice;
        total = (promoTotal > 0 ? promoTotal : subtotal) + delivery;

        FinalTotal = total;
        UpdateSummaryUI(promoDiscountAmount);
    }

    public void RefreshSummaryWithDelivery(float newDeliveryPrice)
    {
        delivery = newDeliveryPrice;
        total = (PromotionalManager.DiscountedTotal > 0 ? PromotionalManager.DiscountedTotal : subtotal) + delivery;
        FinalTotal = total;
        UpdateSummaryUI(promoDiscountAmount);
    }

    void UpdateSummaryUI(float discountedAmount)
    {
        subtotalText.text = subtotal.ToString("F2");
        discountText.text = discountedAmount > 0 ? "-" + discountedAmount.ToString("F2") : "0.00";
        deliveryChargesText.text = delivery.ToString("F2");
        totalText.text = total.ToString("F2");
        subtotalSymbol.enabled = true;
        discountSymbol.enabled = true;
        deliverySymbol.enabled = true;
        totalSymbol.enabled = true;
    }
}
