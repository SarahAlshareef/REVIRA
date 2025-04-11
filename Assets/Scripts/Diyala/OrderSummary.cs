// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// Firebase
using Firebase.Auth;
using Firebase.Database;
// C#
using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;


public class OrderSummary : MonoBehaviour
{

    public GameObject productItemPrefab;     
    public Transform contentPanel;    

    public TextMeshProUGUI subtotalText, discountText, deliveryText, totalText; //true

    public static float FinalTotal; //true

    private DatabaseReference dbReference;
    private string userId;

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;

        LoadCartFromFirebase();
    }

    void LoadCartFromFirebase()
    {
        dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").Child("cartItems")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || !task.Result.Exists)
                {
                    Debug.LogWarning("No cart data found");
                    return;
                }

                float subtotal = 0;
                float productDiscount = 0;

                foreach (var productSnap in task.Result.Children)
                {
                    string productName = productSnap.Child("productName").Value.ToString();
                    float unitPrice = float.Parse(productSnap.Child("price").Value.ToString());

                    int quantity = 0;
                    var sizesSnap = productSnap.Child("sizes");

                    foreach (var size in sizesSnap.Children)
                        quantity += int.Parse(size.Value.ToString());

                    float discountPercent = 0;
                    if (productSnap.Child("discount").Exists && productSnap.Child("discount").Child("percentage").Exists)
                        discountPercent = float.Parse(productSnap.Child("discount").Child("percentage").Value.ToString());

                    float itemTotal = unitPrice * quantity;
                    float itemDiscount = (unitPrice * discountPercent / 100f) * quantity;

                    subtotal += itemTotal;
                    productDiscount += itemDiscount;

                    // Instantiate UI item
                    GameObject item = Instantiate(productItemPrefab, contentPanel);
                    item.transform.Find("Text products name").GetComponent<TextMeshProUGUI>().text = productName;
                    item.transform.Find("Text quantity").GetComponent<TextMeshProUGUI>().text = quantity.ToString();
                    item.transform.Find("Text price").GetComponent<TextMeshProUGUI>().text = itemTotal.ToString("F2");
                }

                // Get delivery and promo discount
                float promoDiscount = PromotionalManager.DiscountedTotal;
                float delivery = DeliveryManager.DeliveryPrice;
                float totalDiscount = productDiscount + promoDiscount;
                float finalTotal = subtotal - totalDiscount + delivery;

                FinalTotal = finalTotal; //true

                subtotalText.text = subtotal.ToString("F2");
                discountText.text = totalDiscount.ToString("F2");
                deliveryText.text = delivery.ToString("F2");
                totalText.text = finalTotal.ToString("F2");
                
            });
    }
}