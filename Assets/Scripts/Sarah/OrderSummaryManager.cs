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
    private float discount = 0f;
    private float delivery = 0f;
    private float total = 0f;

    public float TotalAmount => total;
    public static float LastConfirmedTotal { get; private set; }

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;

        confirmButton.onClick.AddListener(() =>
        {
            LastConfirmedTotal = total;
            confirmPopup.SetActive(true);
        });

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

                    foreach (var item in cartTask.Result.Children)
                    {
                        string productName = item.Child("productName").Value.ToString();
                        float price = float.Parse(item.Child("price").Value.ToString());
                        int quantity = 0;

                        foreach (var size in item.Child("sizes").Children)
                            quantity += int.Parse(size.Value.ToString());

                        float itemTotal = price * quantity;
                        subtotal += itemTotal;

                        GameObject productGO = Instantiate(productPrefab, productListParent);
                        productGO.transform.Find("nameText").GetComponent<TextMeshProUGUI>().text = productName;
                        productGO.transform.Find("quantityText").GetComponent<TextMeshProUGUI>().text = quantity + "x";
                        productGO.transform.Find("priceText").GetComponent<TextMeshProUGUI>().text = price.ToString("F2");
                    }

                    FetchPromoAndDelivery();
                }
            });
    }

    void FetchPromoAndDelivery()
    {
        discount = PromotionalManager.DiscountPercentage;
        float discountedAmount = subtotal * (discount / 100f);

        delivery = DeliveryManager.DeliveryPrice;
        total = (subtotal - discountedAmount) + delivery;

        UpdateSummaryUI(discountedAmount);
    }

    void UpdateSummaryUI(float discountedAmount)
    {
        subtotalText.text = subtotal.ToString("F2");
        discountText.text = "-" + discountedAmount.ToString("F2");
        deliveryChargesText.text = delivery.ToString("F2");
        totalText.text = total.ToString("F2");

        subtotalSymbol.enabled = true;
        discountSymbol.enabled = true;
        deliverySymbol.enabled = true;
        totalSymbol.enabled = true;
    }
}
