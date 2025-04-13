// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System;

public class CartManager : MonoBehaviour
{
    public Transform cartContent;
    public GameObject cartItemPrefab;
    public TextMeshProUGUI totalText;

    private string userId;
    private DatabaseReference dbRef;
    private float currentTotal = 0f;
    private Dictionary<string, float> itemTotals = new();
    private const string storeId = "storeID_123";

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;
        LoadCartItems();
    }

    void LoadCartItems()
    {
        dbRef.Child($"REVIRA/Consumers/{userId}/cart/cartItems").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                currentTotal = 0f;
                itemTotals.Clear();

                foreach (Transform child in cartContent) Destroy(child.gameObject);

                foreach (DataSnapshot item in snapshot.Children)
                {
                    string productId = item.Key;

                    long expiresAt = item.HasChild("expiresAt") ? Convert.ToInt64(item.Child("expiresAt").Value) : 0;
                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    string color = item.Child("color").Value.ToString();
                    string size = "";
                    int qty = 1;

                    foreach (DataSnapshot sizeEntry in item.Child("sizes").Children)
                    {
                        size = sizeEntry.Key;
                        qty = int.Parse(sizeEntry.Value.ToString());
                        break;
                    }

                    // Temporarily disabled expiration logic for testing
                    /*
                    if (expiresAt > 0 && now >= expiresAt)
                    {
                        string stockPath = $"REVIRA/Stores/{storeId}/products/{productId}/stock/{color}/{size}";
                        dbRef.Child(stockPath).GetValueAsync().ContinueWithOnMainThread(stockTask =>
                        {
                            if (stockTask.IsCompleted)
                            {
                                int currentStock = stockTask.Result.Exists ? Convert.ToInt32(stockTask.Result.Value) : 0;
                                int updatedStock = currentStock + qty;
                                dbRef.Child(stockPath).SetValueAsync(updatedStock).ContinueWithOnMainThread(setTask =>
                                {
                                    if (setTask.IsCompleted)
                                        Debug.Log($"Restored {qty} units to stock for {productId} [{color}-{size}]");
                                    else
                                        Debug.LogError("Failed to restore stock: " + setTask.Exception);
                                });
                            }
                        });

                        dbRef.Child($"REVIRA/Consumers/{userId}/cart/cartItems/{productId}")
                            .RemoveValueAsync()
                            .ContinueWithOnMainThread(removeTask =>
                            {
                                if (removeTask.IsCompleted)
                                    Debug.Log($"Removed expired cart item: {productId}");
                                else
                                    Debug.LogError("Failed to remove expired item: " + removeTask.Exception);
                            });

                        continue;
                    }
                    */

                    string productName = item.Child("productName").Value.ToString();
                    float price = float.Parse(item.Child("price").Value.ToString());

                    dbRef.Child($"REVIRA/Stores/{storeId}/products/{productId}").GetValueAsync().ContinueWithOnMainThread(productTask =>
                    {
                        if (productTask.IsCompleted)
                        {
                            DataSnapshot productSnapshot = productTask.Result;
                            string imageUrl = productSnapshot.Child("imageUrl").Value.ToString();
                            float discount = productSnapshot.HasChild("discount") ?
                                             float.Parse(productSnapshot.Child("discount").Value.ToString()) : 0f;

                            Dictionary<string, Dictionary<string, int>> stockData = new();
                            foreach (DataSnapshot colorEntry in productSnapshot.Child("stock").Children)
                            {
                                string col = colorEntry.Key;
                                Dictionary<string, int> sizes = new();
                                foreach (DataSnapshot sizeEntry in colorEntry.Children)
                                {
                                    sizes[sizeEntry.Key] = int.Parse(sizeEntry.Value.ToString());
                                }
                                stockData[col] = sizes;
                            }

                            GameObject cartItemObj = Instantiate(cartItemPrefab, cartContent);
                            CartItemUI cartItem = cartItemObj.GetComponent<CartItemUI>();
                            cartItem.Initialize(userId, productId, productName, price, discount, color, size, qty, stockData, imageUrl);
                            cartItem.SetManager(this);

                            float finalPrice = discount > 0 ? price - (price * discount / 100f) : price;
                            float itemTotal = finalPrice * qty;
                            itemTotals[productId] = itemTotal;
                            UpdateTotalUI();
                        }
                    });
                }
            }
        });
    }

    public void UpdateItemTotal(string productId, float newTotal)
    {
        itemTotals[productId] = newTotal;
        UpdateTotalUI();
    }

    public void UpdateTotalUI()
    {
        float total = 0f;
        foreach (var pair in itemTotals) total += pair.Value;
        currentTotal = total;
        totalText.text = $"Total: {currentTotal:F2}";
    }

    public void AdjustTotal(float priceDelta)
    {
        currentTotal += priceDelta;
        UpdateTotalUI();
    }

    public void RestoreStock(string productId, string color, string size, int qty)
    {
        string stockPath = $"REVIRA/Stores/{storeId}/products/{productId}/stock/{color}/{size}";
        dbRef.Child(stockPath).GetValueAsync().ContinueWithOnMainThread(stockTask =>
        {
            if (stockTask.IsCompleted)
            {
                int currentStock = stockTask.Result.Exists ? Convert.ToInt32(stockTask.Result.Value) : 0;
                int updatedStock = currentStock + qty;
                dbRef.Child(stockPath).SetValueAsync(updatedStock).ContinueWithOnMainThread(setTask =>
                {
                    if (setTask.IsCompleted)
                        Debug.Log($"Restored {qty} units to stock for {productId} [{color}-{size}] after manual delete");
                    else
                        Debug.LogError("Failed to restore stock on delete: " + setTask.Exception);
                });
            }
        });
    }
}
