using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;

public class ConfirmOrderManager : MonoBehaviour
{
    public GameObject successPopup;

    private DatabaseReference dbRef;
    private string userId;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;
    }

    public void ConfirmOrder()
    {
        StartCoroutine(ProcessOrder());
    }

    IEnumerator ProcessOrder()
    {
        string ordersPath = $"REVIRA/Consumers/{userId}/orders";

        // Step 1: Get last order ID count
        var orderCountTask = dbRef.Child(ordersPath).GetValueAsync();
        yield return new WaitUntil(() => orderCountTask.IsCompleted);

        int orderIndex = 1;
        if (orderCountTask.Result.Exists)
            orderIndex = (int)orderCountTask.Result.ChildrenCount + 1;

        string orderId = "Order" + orderIndex;
        long timestamp = CartUtilities.GetCurrentTimestamp();
        string dateTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm tt");

        // Step 2: Build Order Data
        Dictionary<string, object> orderData = new()
        {
            {"orderId", orderId},
            {"timestamp", timestamp},
            {"orderDate", dateTime},
            {"cartTotal", OrderSummaryManager.Instance != null ? OrderSummaryManager.Instance.subtotalText.text : "0.00"},
            {"discountedTotal", PromotionalManager.DiscountedTotal},
            {"deliveryPrice", DeliveryManager.DeliveryPrice},
            {"finalPrice", OrderSummaryManager.FinalTotal},
            {"usedPromoCode", PromotionalManager.UsedPromoCode},
            {"discountPercentage", PromotionalManager.DiscountPercentage},
            {"paymentMethod", "Account Balance"},
            {"orderStatus", "Pending"},
            {"deliveryCompany", DeliveryManager.DeliveryCompany},
            {"deliveryDuration", DeliveryManager.DeliveryDuration},
        };

        // Step 3: Add delivery address
        Address address = AddressBookManager.SelectedAddress;
        Dictionary<string, object> addressDict = new()
        {
            {"addressName", address.addressName},
            {"country", address.country},
            {"city", address.city},
            {"district", address.district},
            {"street", address.street},
            {"building", address.building},
            {"phoneNumber", address.phoneNumber},
        };
        orderData.Add("deliveryAddress", addressDict);

        // Step 4: Fetch Cart Items
        var cartTask = dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").Child("cartItems").GetValueAsync();
        yield return new WaitUntil(() => cartTask.IsCompleted);

        Dictionary<string, object> itemList = new();
        foreach (var item in cartTask.Result.Children)
        {
            string productId = item.Key;
            string productName = item.Child("productName").Value.ToString();
            float price = float.Parse(item.Child("price").Value.ToString());
            string color = item.Child("color").Value.ToString();

            Dictionary<string, object> sizesDict = new();
            foreach (var size in item.Child("sizes").Children)
                sizesDict[size.Key] = int.Parse(size.Value.ToString());

            Dictionary<string, object> productDetails = new()
            {
                {"productName", productName},
                {"price", price},
                {"color", color},
                {"sizes", sizesDict},
            };
            itemList[productId] = productDetails;
        }
        orderData.Add("items", itemList);

        // Step 5: Store Order in Firebase
        dbRef.Child(ordersPath).Child(orderId).SetValueAsync(orderData);

        // Step 6: Deduct Balance
        float newBalance = UserManager.Instance.AccountBalance - OrderSummaryManager.FinalTotal;
        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("accountBalance").SetValueAsync(newBalance);
        UserManager.Instance.UpdateAccountBalance(newBalance);

        // Step 7: Clear cart
        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").RemoveValueAsync();

        // Step 8: Show popup
        successPopup.SetActive(true);
    }
}
