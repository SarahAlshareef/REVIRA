using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;

public class ProductCartManager : MonoBehaviour
{

    private DatabaseReference dbReference;
    private FirebaseAuth auth;

    public Dropdown sizeDropdown;  // Dropdown for selecting size
    public Dropdown colorDropdown; // Dropdown for selecting color
    public Dropdown quantityDropdown; // Dropdown for selecting quantity
    public Button addToCartButton; // Button to add product to cart

    private string productID;
    private string productName;
    private float productPrice;

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        auth = FirebaseAuth.DefaultInstance;

        addToCartButton.onClick.AddListener(AddToCart);
    }

    public void SetProductDetails(string id, string name, float price)
    {
        productID = id;
        productName = name;
        productPrice = price;
    }

    public void AddToCart()
    {
        if (string.IsNullOrEmpty(productID))
        {
            Debug.LogError("Product ID is missing!");
            return;
        }

        string userID = auth.CurrentUser.UserId;
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        int selectedQuantity = int.Parse(quantityDropdown.options[quantityDropdown.value].text);

        Dictionary<string, object> cartItem = new Dictionary<string, object>
        {
            { "productID", productID },
            { "productName", productName },
            { "price", productPrice },
            { "size", selectedSize },
            { "color", selectedColor },
            { "quantity", selectedQuantity },
            { "timestamp", ServerValue.Timestamp }
        };

        dbReference.Child("users").Child(userID).Child("cart").Child(productID).SetValueAsync(cartItem)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Product added to cart successfully!");
                }
            });
    }

    // ========================== SESSION MANAGEMENT ==========================

    // Orders in cart are stored for 24 hours before being removed
    public void SetCartExpiration()
    {
        string userID = auth.CurrentUser.UserId;
        long expirationTime = GetUnixTimestamp() + (24 * 60 * 60); // 24 hours in seconds

        dbReference.Child("users").Child(userID).Child("cart").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (var item in task.Result.Children)
                {
                    dbReference.Child("users").Child(userID).Child("cart").Child(item.Key).Child("expiresAt").SetValueAsync(expirationTime);
                }

                Debug.Log("Cart session set for 24 hours.");
            }
        });
    }

    // When checkout starts, user has 1 hour to complete the order
    public void StartCheckout()
    {
        string userID = auth.CurrentUser.UserId;
        long expirationTime = GetUnixTimestamp() + (60 * 60); // 1 hour in seconds

        dbReference.Child("users").Child(userID).Child("cart").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (var item in task.Result.Children)
                {
                    dbReference.Child("users").Child(userID).Child("cart").Child(item.Key).Child("expiresAt").SetValueAsync(expirationTime);
                }

                Debug.Log("Checkout session started, you have 1 hour to complete the purchase.");
            }
        });
    }

    // Check if cart orders have expired and remove them
    public void CheckCartExpiration()
    {
        string userID = auth.CurrentUser.UserId;
        long currentTime = GetUnixTimestamp();

        dbReference.Child("users").Child(userID).Child("cart").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (var item in task.Result.Children)
                {
                    long expiresAt = long.Parse(item.Child("expiresAt").Value.ToString());
                    if (currentTime > expiresAt)
                    {
                        dbReference.Child("users").Child(userID).Child("cart").Child(item.Key).RemoveValueAsync();
                        Debug.Log("Expired cart item removed: " + item.Key);
                    }
                }
            }
        });
    }

    // Helper function to get current Unix timestamp
    private long GetUnixTimestamp()
    {
        return (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
    }
}