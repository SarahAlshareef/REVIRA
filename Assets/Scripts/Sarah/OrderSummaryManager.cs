using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

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
    private float delivery = 0f;
    private float promoDiscountAmount = 0f;
    private float total = 0f;

    public static float FinalTotal { get; private set; }

    private int productProcessedCount = 0;
    private int totalProductsExpected = 0;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;
        if (confirmButton != null && confirmPopup != null)
            confirmButton.onClick.AddListener(() => confirmPopup.SetActive(true));

        LoadOrderData();
    }

    void LoadOrderData()
    {
        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").Child("cartItems")
            .GetValueAsync().ContinueWithOnMainThread(cartTask =>
            {
                if (cartTask.IsCompleted && cartTask.Result.Exists)
                {
                    foreach (Transform child in productListParent)
                        Destroy(child.gameObject);

                    subtotal = 0f;
                    productProcessedCount = 0;
                    totalProductsExpected = (int)cartTask.Result.ChildrenCount;

                    foreach (var item in cartTask.Result.Children)
                    {
                        string productId = item.Key;
                        string productName = item.Child("productName").Value.ToString();
                        float basePrice = float.Parse(item.Child("price").Value.ToString());

                        int quantity = 0;
                        foreach (var size in item.Child("sizes").Children)
                            quantity += int.Parse(size.Value.ToString());

                        dbRef.Child("REVIRA/stores/storeID_123/products").Child(productId)
                            .GetValueAsync().ContinueWithOnMainThread(productTask =>
                            {
                                float finalPrice = basePrice;

                                if (productTask.IsCompleted && productTask.Result.Exists)
                                {
                                    var productSnapshot = productTask.Result;
                                    bool hasDiscount = productSnapshot.Child("discount").Child("exists").Value.ToString() == "True";
                                    float discountPercent = hasDiscount ? float.Parse(productSnapshot.Child("discount").Child("percentage").Value.ToString()) : 0f;

                                    if (hasDiscount)
                                        finalPrice *= (1 - discountPercent / 100f);
                                }

                                float itemTotal = finalPrice * quantity;
                                subtotal += itemTotal;

                                GameObject row = Instantiate(productPrefab, productListParent);
                                row.transform.Find("nameText")?.GetComponent<TextMeshProUGUI>().SetText(productName);
                                row.transform.Find("quantityText")?.GetComponent<TextMeshProUGUI>().SetText(quantity + "x");
                                row.transform.Find("priceText")?.GetComponent<TextMeshProUGUI>().SetText(itemTotal.ToString("F2"));

                                productProcessedCount++;
                                if (productProcessedCount == totalProductsExpected)
                                    CalculatePromoAndDelivery();
                            });
                    }
                }
            });
    }

    void CalculatePromoAndDelivery()
    {
        float promoPercent = PromotionalManager.DiscountPercentage;
        promoDiscountAmount = (promoPercent > 0) ? subtotal * promoPercent / 100f : 0f;

        delivery = DeliveryManager.DeliveryPrice;
        total = subtotal - promoDiscountAmount + delivery;
        FinalTotal = total;

        UpdateSummaryUI();
    }

    void UpdateSummaryUI()
    {
        subtotalText.text = subtotal.ToString("F2");
        discountText.text = promoDiscountAmount > 0 ? "-" + promoDiscountAmount.ToString("F2") : "-0.00";
        deliveryChargesText.text = delivery.ToString("F2");
        totalText.text = total.ToString("F2");

        subtotalSymbol.enabled = true;
        discountSymbol.enabled = true;
        deliverySymbol.enabled = true;
        totalSymbol.enabled = true;
    }
}
