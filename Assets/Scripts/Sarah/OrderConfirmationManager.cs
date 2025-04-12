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
        dbRef.Child("REVIRA/Consumers/" + userId + "/orders").GetValueAsync().ContinueWithOnMainThread(orderTask =>
        {
            if (orderTask.IsCompleted)
            {
                var snapshot = orderTask.Result;
                int nextOrderNumber = 1;
                if (snapshot.Exists)
                    nextOrderNumber = (int)snapshot.ChildrenCount + 1;

                string orderId = "Order" + nextOrderNumber;
                string orderPath = $"REVIRA/Consumers/{userId}/orders/{orderId}";

                float finalPrice = OrderSummaryManager.FinalTotal;
                float cartTotal = float.Parse(GameObject.Find("OrderSummaryManager").GetComponent<OrderSummaryManager>().subtotalText.text);
                float discountedTotal = PromotionalManager.DiscountedTotal > 0 ? PromotionalManager.DiscountedTotal : cartTotal;
                float deliveryPrice = DeliveryManager.DeliveryPrice;

                string orderDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm tt");
                long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

                var orderData = new Dictionary<string, object>
                {
                    {"orderId", orderId},
                    {"timestamp", timestamp},
                    {"orderDate", orderDate},
                    {"cartTotal", cartTotal},
                    {"discountedTotal", discountedTotal},
                    {"deliveryPrice", deliveryPrice},
                    {"finalPrice", finalPrice},
                    {"usedPromoCode", PromotionalManager.UsedPromoCode},
                    {"discountPercentage", PromotionalManager.DiscountPercentage},
                    {"paymentMethod", "Account Balance"},
                    {"orderStatus", "Pending"},
                    {"deliveryCompany", DeliveryManager.DeliveryCompany},
                    {"deliveryDuration", DeliveryManager.DeliveryDuration},
                    {"deliveryAddress", JsonUtility.ToJson(AddressBookManager.SelectedAddress)}
                };

                dbRef.Child("REVIRA/Consumers/" + userId + "/cart/cartItems").GetValueAsync().ContinueWithOnMainThread(cartTask =>
                {
                    if (cartTask.IsCompleted && cartTask.Result.Exists)
                    {
                        Dictionary<string, object> items = new();

                        foreach (var item in cartTask.Result.Children)
                        {
                            items[item.Key] = item.Value;
                        }

                        orderData["items"] = items;

                        dbRef.Child(orderPath).SetValueAsync(orderData).ContinueWithOnMainThread(setTask =>
                        {
                            if (setTask.IsCompleted)
                            {
                                float newBalance = UserManager.Instance.AccountBalance - finalPrice;
                                dbRef.Child("REVIRA/Consumers/" + userId + "/accountBalance").SetValueAsync(newBalance);
                                UserManager.Instance.UpdateAccountBalance(newBalance);

                                dbRef.Child("REVIRA/Consumers/" + userId + "/cart/cartItems").RemoveValueAsync();

                                confirmationPopup.SetActive(false);
                                successPopup.SetActive(true);
                            }
                            else
                            {
                                Debug.LogError("Failed to save order: " + setTask.Exception);
                            }
                        });
                    }
                    else
                    {
                        Debug.LogError("Failed to load cart items: " + cartTask.Exception);
                    }
                });
            }
            else
            {
                Debug.LogError("Failed to get existing orders: " + orderTask.Exception);
            }
        });
    }
}
