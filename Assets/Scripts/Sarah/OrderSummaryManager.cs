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
    private float promoDiscountAmount = 0f;
    private float delivery = 0f;
    private float total = 0f;

    private int productsToProcess = 0;
    private int productsProcessed = 0;

    public static float FinalTotal { get; private set; }
    public static OrderSummaryManager Instance { get; private set; }

    
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
        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").Child("cartItems")
            .GetValueAsync().ContinueWithOnMainThread(cartTask =>
            {
                if (cartTask.IsCompleted && cartTask.Result.Exists)
                {
                    foreach (Transform child in productListParent)
                        Destroy(child.gameObject);

                    subtotal = 0f;
                    productsToProcess = (int)cartTask.Result.ChildrenCount;
                    productsProcessed = 0;

                    foreach (var item in cartTask.Result.Children)
                    {
                        string productId = item.Key;
                        string productName = item.Child("productName").Value.ToString();
                        float originalPrice = float.Parse(item.Child("price").Value.ToString());

                        int quantity = 0;
                        foreach (var size in item.Child("sizes").Children)
                            quantity += int.Parse(size.Value.ToString());

                        dbRef.Child("REVIRA/stores/storeID_123/products").Child(productId).GetValueAsync().ContinueWithOnMainThread(productTask =>
                        {
                            float finalPrice = originalPrice;

                            if (productTask.IsCompleted && productTask.Result.Exists)
                            {
                                var productSnapshot = productTask.Result;
                                bool hasDiscount = productSnapshot.Child("discount").Child("exists").Value.ToString() == "True";
                                if (hasDiscount)
                                {
                                    float discountPercentage = float.Parse(productSnapshot.Child("discount").Child("percentage").Value.ToString());
                                    finalPrice = originalPrice - (originalPrice * discountPercentage / 100f);
                                }
                            }

                            float itemTotal = finalPrice * quantity;
                            subtotal += itemTotal;

                            GameObject productGO = Instantiate(productPrefab, productListParent);
                            productGO.transform.Find("nameText")?.GetComponent<TextMeshProUGUI>().SetText(productName);
                            productGO.transform.Find("quantityText")?.GetComponent<TextMeshProUGUI>().SetText(quantity + "x");
                            productGO.transform.Find("priceText")?.GetComponent<TextMeshProUGUI>().SetText(itemTotal.ToString("F2"));

                            productsProcessed++;
                            if (productsProcessed == productsToProcess)
                                FetchPromoAndDelivery();
                        });
                    }
                }
                else
                {
                    subtotalText.text = "0.00";
                    discountText.text = "0.00";
                    deliveryChargesText.text = "0.00";
                    totalText.text = "0.00";
                }
            });
    }

    void FetchPromoAndDelivery()
    {
        float promoTotal = PromotionalManager.DiscountedTotal;
        promoDiscountAmount = PromotionalManager.UsedPromoCode != "" ? subtotal - promoTotal : 0f;

        delivery = DeliveryManager.DeliveryPrice;
        total = (promoTotal > 0 ? promoTotal : subtotal) + delivery;

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
