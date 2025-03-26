using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using TMPro;

public class ConfirmOrderManager : MonoBehaviour
{
    private DatabaseReference dbReference;
    private UserManager userManager;

    public TextMeshProUGUI errorText;

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        userManager = FindObjectOfType<UserManager>();
        if (errorText != null)
            errorText.text = "";
    }

    public void OnConfirmOrderButtonClicked()
    {
        string userId = userManager.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            ShowMessage("User not logged in.");
            return;
        }

        dbReference.Child("REVIRA").Child("Consumers").Child(userId).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                DataSnapshot userSnapshot = task.Result;
                DataSnapshot cartSnapshot = userSnapshot.Child("cart");

                if (!cartSnapshot.Exists)
                {
                    ShowMessage("Your cart is empty.");
                    return;
                }

                Dictionary<string, object> cartItems = new Dictionary<string, object>();
                double cartTotal = 0;

                foreach (DataSnapshot item in cartSnapshot.Children)
                {
                    double price = double.Parse(item.Child("price").Value.ToString());

                    foreach (DataSnapshot sizeEntry in item.Child("sizes").Children)
                    {
                        int quantity = int.Parse(sizeEntry.Value.ToString());
                        cartTotal += price * quantity;
                    }

                    cartItems[item.Key] = item.Value;
                }

                double discountedTotal = CheckoutManager.DiscountedTotal > 0 ? CheckoutManager.DiscountedTotal : cartTotal;
                double finalPrice = discountedTotal + CheckoutManager.DeliveryPrice;
                double balance = userManager.AccountBalance;

                if (finalPrice > balance)
                {
                    ShowMessage("Insufficient balance.");
                    return;
                }

                double updatedBalance = balance - finalPrice;
                userManager.UpdateAccountBalance((float)updatedBalance);
                dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("accountBalance").SetValueAsync(updatedBalance);

                DataSnapshot addressSnapshot = userSnapshot.Child("AddressBook");
                Dictionary<string, object> selectedAddress = new Dictionary<string, object>();

                if (addressSnapshot.Exists)
                {
                    foreach (DataSnapshot address in addressSnapshot.Children)
                    {
                        foreach (DataSnapshot field in address.Children)
                        {
                            selectedAddress[field.Key] = field.Value;
                        }
                        break;
                    }
                }

                string orderId = dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("OrderHistory").Push().Key;
                Dictionary<string, object> orderData = new Dictionary<string, object>
                {
                    { "orderId", orderId },
                    { "items", cartItems },
                    { "cartTotal", cartTotal },
                    { "discountedTotal", discountedTotal },
                    { "deliveryPrice", CheckoutManager.DeliveryPrice },
                    { "finalPrice", finalPrice },
                    { "usedPromoCode", CheckoutManager.UsedPromoCode },
                    { "discountPercentage", CheckoutManager.DiscountPercentage },
                    { "paymentMethod", "Account Balance" },
                    { "orderStatus", "Pending" },
                    { "deliveryCompany", CheckoutManager.DeliveryCompany },
                    { "deliveryDuration", CheckoutManager.DeliveryDuration },
                    { "deliveryAddress", selectedAddress },
                    { "timestamp", GetUnixTimestamp() },
                    { "orderDate", System.DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt") }
                };

                dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("OrderHistory").Child(orderId).SetValueAsync(orderData);
                dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").RemoveValueAsync();

                ShowMessage("Order confirmed successfully!", true);
            }
            else
            {
                ShowMessage("Failed to load user/cart data.");
            }
        });
    }

    private void ShowMessage(string message, bool success = false)
    {
        if (errorText != null)
        {
            errorText.text = message;
            if (success)
            {
                CancelInvoke("ClearMessage");
                Invoke("ClearMessage", 3f);
            }
        }

        Debug.Log(message);
    }

    private void ClearMessage()
    {
        if (errorText != null)
            errorText.text = "";
    }

    private long GetUnixTimestamp()
    {
        return (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
    }
}
