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

        var address = AddressBookManager.SelectedAddress;
        if (address == null)
        {
            errorText.text = "Please select a delivery address before confirming the order.";
            return;
        }

        if (string.IsNullOrEmpty(DeliveryManager.DeliveryCompany))
        {
            errorText.text = "Please select a delivery method before confirming the order.";
            return;
        }

        GetNextOrderId(orderId =>
        {
            BuildOrderData(orderId, orderData =>
            {
                if (orderData.ContainsKey("items") && ((Dictionary<string, object>)orderData["items"]).Count > 0)
                {
                    SubmitOrder(orderId, orderData);
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
        dbRef.Child($"REVIRA/Consumers/{userId}/OrderHistory").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            int nextOrderNumber = 1;
            if (task.IsCompleted && task.Result.Exists)
                nextOrderNumber = (int)task.Result.ChildrenCount + 1;

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
                callback.Invoke(orderData);
                return;
            }

            Dictionary<string, object> items = new();
            int total = (int)cartTask.Result.ChildrenCount;
            int processed = 0;

            foreach (var item in cartTask.Result.Children)
            {
                string productId = item.Key;
                if (!item.HasChild("price") || !item.HasChild("productName") || !item.HasChild("sizes") || !item.HasChild("color"))
                {
                    Debug.LogWarning("[ConfirmOrderManager] Skipping malformed cart item: " + productId);
                    processed++;
                    if (processed == total) Finalize();
                    continue;
                }

                float price = float.Parse(item.Child("price").Value.ToString());
                string name = item.Child("productName").Value.ToString();
                string color = item.Child("color").Value.ToString();

                string size = "";
                int quantity = 1;
                foreach (var sizeEntry in item.Child("sizes").Children)
                {
                    size = sizeEntry.Key;
                    quantity = int.Parse(sizeEntry.Value.ToString());
                    break;
                }

                dbRef.Child($"REVIRA/stores/storeID_123/products/{productId}").GetValueAsync().ContinueWithOnMainThread(productTask =>
                {
                    float priceAfterDiscount = price;
                    float priceAfterPromo = price;

                    if (productTask.IsCompleted && productTask.Result.Exists)
                    {
                        var p = productTask.Result;
                        if (p.HasChild("discount") && p.Child("discount").Child("exists").Value.ToString() == "True")
                        {
                            float d = float.Parse(p.Child("discount").Child("percentage").Value.ToString());
                            priceAfterDiscount = price - (price * d / 100f);
                        }

                        if (PromotionalManager.ProductDiscounts.TryGetValue(productId, out DiscountInfo promo))
                            priceAfterPromo = promo.finalPrice / quantity;
                        else
                            priceAfterPromo = priceAfterDiscount;
                    }

                    float totalPrice = priceAfterPromo * quantity;

                    items[productId] = new Dictionary<string, object>
                    {
                        {"productID", productId},
                        {"productName", name},
                        {"originalPrice", price},
                        {"priceAfterDiscount", priceAfterDiscount},
                        {"priceAfterPromoDiscount", priceAfterPromo},
                        {"totalPrice", totalPrice},
                        {"color", color},
                        {"sizes", new Dictionary<string, object> { { size, quantity } } }
                    };

                    processed++;
                    if (processed == total) Finalize();
                });
            }

            void Finalize()
            {
                orderData["items"] = items;
                callback?.Invoke(orderData);
            }
        });
    }

    void SubmitOrder(string orderId, Dictionary<string, object> orderData)
    {
        string orderPath = $"REVIRA/Consumers/{userId}/OrderHistory/{orderId}";

        dbRef.Child(orderPath).SetValueAsync(orderData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                float newBalance = UserManager.Instance.AccountBalance - OrderSummaryManager.FinalTotal;
                dbRef.Child($"REVIRA/Consumers/{userId}/accountBalance").SetValueAsync(newBalance);
                UserManager.Instance.UpdateAccountBalance(newBalance);

                dbRef.Child($"REVIRA/Consumers/{userId}/cart").RemoveValueAsync();

                confirmationPopup.SetActive(false);
                successPopup.SetActive(true);
                orderSubmitted = true;
            }
            else
            {
                Debug.LogError("[ConfirmOrderManager] Failed to submit order: " + task.Exception);
            }
        });
    }
}
