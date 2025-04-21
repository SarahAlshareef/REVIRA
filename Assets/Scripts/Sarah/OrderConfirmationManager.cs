using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;

public class ConfirmOrderManager : MonoBehaviour
{
    public GameObject confirmationPopup;
    public GameObject successPopup;
    public Button confirmButton;
    public Button cancelButton;

    private DatabaseReference dbRef;
    private string userId;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;

        confirmButton.onClick.AddListener(OnConfirmOrder);
        cancelButton.onClick.AddListener(() => confirmationPopup.SetActive(false));
    }

    void OnConfirmOrder()
    {
        GetNextOrderId(orderId =>
        {
            BuildOrderData(orderId, orderData =>
            {
                SubmitOrder(orderId, orderData);
            });
        });
    }

    void GetNextOrderId(Action<string> callback)
    {
        dbRef.Child($"REVIRA/Consumers/{userId}/OrderHistory").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            int nextOrderNumber = 1;
            if (task.IsCompleted && task.Result.Exists)
            {
                nextOrderNumber = (int)task.Result.ChildrenCount + 1;
            }
            callback?.Invoke("Order" + nextOrderNumber);
        });
    }

    void BuildOrderData(string orderId, Action<Dictionary<string, object>> callback)
    {
        float finalPrice = OrderSummaryManager.FinalTotal;
        float cartTotal = OrderSummaryManager.Instance.Subtotal;
        float deliveryPrice = DeliveryManager.DeliveryPrice;
        string orderDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm tt");
        long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

        var address = AddressBookManager.SelectedAddress;

        Dictionary<string, object> orderData = new()
        {
            {"orderId", orderId},
            {"timestamp", timestamp},
            {"orderDate", orderDate},
            {"cartTotal", cartTotal},
            {"discountedTotal", PromotionalManager.DiscountedTotal},
            {"deliveryPrice", deliveryPrice},
            {"finalPrice", finalPrice},
            {"usedPromoCode", PromotionalManager.UsedPromoCode},
            {"discountPercentage", PromotionalManager.DiscountPercentage},
            {"paymentMethod", "Account Balance"},
            {"orderStatus", "Pending"},
            {"deliveryCompany", DeliveryManager.DeliveryCompany},
            {"deliveryDuration", DeliveryManager.DeliveryDuration},
            {"addressName", address.addressName},
            {"country", address.country},
            {"city", address.city},
            {"district", address.district},
            {"street", address.street},
            {"building", address.building},
            {"phoneNumber", address.phoneNumber}
        };

        LoadCartItems(orderData, callback);
    }

    void LoadCartItems(Dictionary<string, object> orderData, Action<Dictionary<string, object>> callback)
    {
        dbRef.Child($"REVIRA/Consumers/{userId}/cart/cartItems").GetValueAsync().ContinueWithOnMainThread(cartTask =>
        {
            if (!cartTask.IsCompleted || !cartTask.Result.Exists)
            {
                Debug.LogError("Failed to load cart items.");
                return;
            }

            Dictionary<string, object> items = new();
            int totalProducts = (int)cartTask.Result.ChildrenCount;
            int processed = 0;

            foreach (var item in cartTask.Result.Children)
            {
                string productId = item.Key;
                var productDict = (Dictionary<string, object>)item.Value;
                float originalPrice = Convert.ToSingle(productDict["price"]);
                string selectedColor = productDict["color"].ToString();

                string selectedSize = "";
                int quantity = 1;
                foreach (var size in (Dictionary<string, object>)productDict["sizes"])
                {
                    selectedSize = size.Key;
                    quantity = Convert.ToInt32(size.Value);
                    break;
                }

                dbRef.Child($"REVIRA/stores/storeID_123/products/{productId}").GetValueAsync().ContinueWithOnMainThread(productTask =>
                {
                    float priceAfterDiscount = originalPrice;
                    float priceAfterPromo = originalPrice;

                    if (productTask.IsCompleted && productTask.Result.Exists)
                    {
                        var productSnapshot = productTask.Result;

                        bool hasDiscount = Convert.ToBoolean(productSnapshot.Child("discount").Child("exists").Value);
                        float discountPercent = Convert.ToSingle(productSnapshot.Child("discount").Child("percentage").Value);

                        if (hasDiscount)
                            priceAfterDiscount = originalPrice * (1f - discountPercent / 100f);

                        if (PromotionalManager.ProductDiscounts.TryGetValue(productId, out DiscountInfo promoInfo))
                        {
                            priceAfterPromo = promoInfo.finalPrice / quantity; // unit promo price
                        }
                        else
                        {
                            priceAfterPromo = priceAfterDiscount;
                        }
                    }

                    float totalPrice = priceAfterPromo * quantity;

                    Dictionary<string, object> itemData = new()
                    {
                        {"productID", productId},
                        {"productName", productDict["productName"]},
                        {"originalPrice", originalPrice},
                        {"priceAfterDiscount", priceAfterDiscount},
                        {"priceAfterPromoDiscount", priceAfterPromo},
                        {"totalPrice", totalPrice},
                        {"color", selectedColor},
                        {"sizes", new Dictionary<string, object> { { selectedSize, quantity } } }
                    };

                    items[productId] = itemData;
                    processed++;

                    if (processed == totalProducts)
                    {
                        orderData["items"] = items;
                        callback?.Invoke(orderData);
                    }
                });
            }
        });
    }

    void SubmitOrder(string orderId, Dictionary<string, object> orderData)
    {
        string orderPath = $"REVIRA/Consumers/{userId}/OrderHistory/{orderId}";

        dbRef.Child(orderPath).SetValueAsync(orderData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                float newBalance = UserManager.Instance.AccountBalance - OrderSummaryManager.FinalTotal;
                dbRef.Child($"REVIRA/Consumers/{userId}/accountBalance").SetValueAsync(newBalance);
                UserManager.Instance.UpdateAccountBalance(newBalance);

                dbRef.Child($"REVIRA/Consumers/{userId}/cart/cartItems").RemoveValueAsync();
                dbRef.Child($"REVIRA/Consumers/{userId}/cart/cartTotal").RemoveValueAsync();

                confirmationPopup.SetActive(false);
                successPopup.SetActive(true);
            }
            else
            {
                Debug.LogError("Failed to save order: " + task.Exception);
            }
        });
    }
}
