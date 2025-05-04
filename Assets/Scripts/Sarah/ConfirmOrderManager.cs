using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;

public class ConfirmOrderManager : MonoBehaviour
{
    [Header("Popups & Buttons")]
    public GameObject confirmationPopup;
    public GameObject successPopup;
    public Button confirmButton;
    public Button cancelButton;
    public TextMeshProUGUI errorText;

    private DatabaseReference dbRef;
    private string userId;
    private bool orderSubmitted = false;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;

        confirmButton.onClick.AddListener(OnConfirmOrder);
        cancelButton.onClick.AddListener(() => confirmationPopup.SetActive(false));
    }

    void OnConfirmOrder()
    {
        if (orderSubmitted) return;

        // Validate address
        var address = AddressBookManager.SelectedAddress;
        if (address == null)
        {
            errorText.text = "Please select a delivery address before confirming the order.";
            return;
        }

        // Validate delivery method
        if (string.IsNullOrEmpty(DeliveryManager.DeliveryCompany))
        {
            errorText.text = "Please select a delivery method before confirming the order.";
            return;
        }

        // Get next ID and build data
        GetNextOrderId(orderId =>
        {
            BuildOrderData(orderId, orderData =>
            {
                if (orderData.ContainsKey("items") && ((Dictionary<string, object>)orderData["items"]).Count > 0)
                {
                    ConfirmOrder(orderId, orderData);
                }
                else
                {
                    Debug.LogError("[ConfirmOrderManager] No valid items to submit.");
                    errorText.text = "Cart items are invalid or missing.";
                }
            });
        });
    }

    void GetNextOrderId(Action<string> callback)
    {
        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("OrderHistory")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                int nextOrderNumber = 1;
                if (task.IsCompleted && task.Result.Exists)
                    nextOrderNumber = (int)task.Result.ChildrenCount + 1;

                string newId = "Order" + nextOrderNumber;
                Debug.Log($"[ConfirmOrderManager] Next Order ID: {newId}");
                callback?.Invoke(newId);
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

        var orderData = new Dictionary<string, object>
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
        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").Child("cartItems")
            .GetValueAsync().ContinueWithOnMainThread(cartTask =>
            {
                if (!cartTask.IsCompleted || !cartTask.Result.Exists)
                {
                    Debug.LogWarning("[ConfirmOrderManager] Cart is empty or failed to load.");
                    callback(orderData);
                    return;
                }

                var itemsDict = new Dictionary<string, object>();
                int total = (int)cartTask.Result.ChildrenCount;
                int processed = 0;

                foreach (var item in cartTask.Result.Children)
                {
                    string productId = item.Key;
                    if (!item.HasChild("price") || !item.HasChild("productName") || !item.HasChild("sizes") || !item.HasChild("color"))
                    {
                        Debug.LogWarning($"[ConfirmOrderManager] Skipping malformed item: {productId}");
                        processed++;
                        if (processed == total) Finalize();
                        continue;
                    }

                    float price = float.Parse(item.Child("price").Value.ToString());
                    string name = item.Child("productName").Value.ToString();
                    string color = item.Child("color").Value.ToString();

                    string sizeKey = null;
                    int quantity = 0;
                    foreach (var sz in item.Child("sizes").Children)
                    {
                        sizeKey = sz.Key;
                        quantity = int.Parse(sz.Value.ToString());
                        break;
                    }

                    dbRef.Child("REVIRA").Child("stores").Child("storeID_123").Child("products").Child(productId)
                        .GetValueAsync().ContinueWithOnMainThread(prodTask =>
                        {
                            float priceAfterDiscount = price;
                            float priceAfterPromo = price;

                            if (prodTask.IsCompleted && prodTask.Result.Exists)
                            {
                                var p = prodTask.Result;
                                bool hasDiscount = p.Child("discount").Child("exists").Value?.ToString() == "True";
                                if (hasDiscount)
                                {
                                    float d = float.Parse(p.Child("discount").Child("percentage").Value.ToString());
                                    priceAfterDiscount = price - (price * d / 100f);
                                }

                                if (PromotionalManager.ProductDiscounts.TryGetValue(productId, out var promoInfo))
                                    priceAfterPromo = promoInfo.finalPrice / quantity;
                                else
                                    priceAfterPromo = priceAfterDiscount;
                            }

                            float totalPrice = priceAfterPromo * quantity;
                            itemsDict[productId] = new Dictionary<string, object>
                        {
                        {"productID", productId},
                        {"productName", name},
                        {"originalPrice", price},
                        {"priceAfterDiscount", priceAfterDiscount},
                        {"priceAfterPromoDiscount", priceAfterPromo},
                        {"totalPrice", totalPrice},
                        {"color", color},
                        {"sizes", new Dictionary<string, object> { { sizeKey, quantity } } }
                        };

                            processed++;
                            if (processed == total) Finalize();
                        });
                }

                void Finalize()
                {
                    orderData["items"] = itemsDict;
                    callback(orderData);
                }
            });
    }

    void ConfirmOrder(string orderId, Dictionary<string, object> orderData)
    {
        string path = $"REVIRA/Consumers/{userId}/OrderHistory/{orderId}";
        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("OrderHistory").Child(orderId)
            .SetValueAsync(orderData).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    // Deduct balance
                    float newBal = UserManager.Instance.AccountBalance - OrderSummaryManager.FinalTotal;
                    dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("accountBalance").SetValueAsync(newBal);
                    UserManager.Instance.UpdateAccountBalance(newBal);

                    // Clear cart
                    dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").RemoveValueAsync();

                    confirmationPopup.SetActive(false);
                    successPopup.SetActive(true);
                    orderSubmitted = true;
                    Debug.Log($"[ConfirmOrderManager] Order {orderId} submitted successfully.");
                }
                else
                {
                    Debug.LogError("[ConfirmOrderManager] Failed to submit order: " + task.Exception);
                }
            });
    }
}
