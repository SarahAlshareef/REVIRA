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
    public TextMeshProUGUI errorText;

    private DatabaseReference dbRef;
    private string userId;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;

        Debug.Log("[ConfirmOrderManager] Initialized. UserID: " + userId);

        confirmButton.onClick.AddListener(OnConfirmOrder);
        cancelButton.onClick.AddListener(() => confirmationPopup.SetActive(false));
    }

    void OnConfirmOrder()
    {
        var address = AddressBookManager.SelectedAddress;

        Debug.Log("[ConfirmOrderManager] OnConfirmOrder triggered.");

        if (address == null)
        {
            Debug.LogError("[ConfirmOrderManager] No address selected.");
            if (errorText != null)
                errorText.text = "Please select a delivery address before confirming the order.";
            return;
        }

        Debug.Log("[ConfirmOrderManager] Address selected: " + address.addressName);

        if (string.IsNullOrEmpty(DeliveryManager.DeliveryCompany))
        {
            Debug.LogError("[ConfirmOrderManager] No delivery method selected.");
            if (errorText != null)
                errorText.text = "Please select a delivery method before confirming the order.";
            return;
        }

        Debug.Log("[ConfirmOrderManager] Delivery company: " + DeliveryManager.DeliveryCompany);
        Debug.Log("[ConfirmOrderManager] Delivery price: " + DeliveryManager.DeliveryPrice);
        Debug.Log("[ConfirmOrderManager] Final total: " + OrderSummaryManager.FinalTotal);

        GetNextOrderId(orderId =>
        {
            Debug.Log("[ConfirmOrderManager] Generated order ID: " + orderId);

            BuildOrderData(orderId, orderData =>
            {
                Debug.Log("[ConfirmOrderManager] Order data built. Submitting...");
                ConfirmOrder(orderId, orderData);
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
            else if (task.IsFaulted)
            {
                Debug.LogError("[ConfirmOrderManager] Error fetching order history: " + task.Exception);
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
                Debug.LogError("[ConfirmOrderManager] Failed to load cart items or cart is empty.");
                return;
            }

            Debug.Log("[ConfirmOrderManager] Cart items loaded.");

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
                            priceAfterPromo = promoInfo.finalPrice / quantity;
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
                        Debug.Log("[ConfirmOrderManager] All cart items processed. Finalizing order data.");
                        orderData["items"] = items;
                        callback?.Invoke(orderData);
                    }
                });
            }
        });
    }

    void ConfirmOrder(string orderId, Dictionary<string, object> orderData)
    {
        string orderPath = $"REVIRA/Consumers/{userId}/OrderHistory/{orderId}";

        dbRef.Child(orderPath).SetValueAsync(orderData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("[ConfirmOrderManager] Order successfully saved to Firebase.");

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
                Debug.LogError("[ConfirmOrderManager] Failed to save order: " + task.Exception);
            }
        });
    }
}
