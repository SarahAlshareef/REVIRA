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

    public TMP_Dropdown sizeDropdown;
    public TMP_Dropdown colorDropdown;
    public TMP_Dropdown quantityDropdown;
    public Button addToCartButton;

    private string storeID = "storeID_123";
    private ProductsManager productsManager;

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        auth = FirebaseAuth.DefaultInstance;

        productsManager = FindObjectOfType<ProductsManager>();

        addToCartButton.onClick.AddListener(AddToCart);
    }

    public void AddToCart()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("User not logged in");
            return;
        }

        if (productsManager == null)
        {
            Debug.LogError("ProductsManager script is missing in the scene");
            return;
        }

        string userID = auth.CurrentUser.UserId;
        string productID = productsManager.productID;
        string productName = productsManager.productName.text;
        float productPrice = float.Parse(productsManager.productPrice.text);

        if (string.IsNullOrEmpty(productID) || string.IsNullOrEmpty(productName))
        {
            Debug.LogError("Product ID or Name is missing");
            return;
        }

        string selectedSize = sizeDropdown.options[sizeDropdown.value].text.Trim();
        string selectedColor = colorDropdown.options[colorDropdown.value].text.Trim();
        string selectedQuantity = quantityDropdown.options[quantityDropdown.value].text.Trim();

        Debug.Log("Selected Color: " + selectedColor);
        Debug.Log("Selected Size: " + selectedSize);
        Debug.Log("Selected Quantity: " + selectedQuantity);

        if (selectedColor == "Select Color" || selectedSize == "Select Size" || selectedQuantity == "Select Quantity")
        {
            Debug.LogError("Size, Color, or Quantity is not selected properly");
            return;
        }

        int quantity = int.Parse(selectedQuantity);
        string orderID = dbReference.Child("orders").Push().Key;

        Dictionary<string, object> cartItem = new Dictionary<string, object>
        {
            { "productID", productID },
            { "productName", productName },
            { "price", productPrice },
            { "size", selectedSize },
            { "color", selectedColor },
            { "quantity", quantity },
            { "storeID", storeID },
            { "timestamp", ServerValue.Timestamp }
        };

        dbReference.Child("orders").Child(userID).Child(orderID).SetValueAsync(cartItem)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Order added to Firebase successfully");
                    SetOrderExpiration(userID, orderID);
                }
                else
                {
                    Debug.LogError("Error adding order to Firebase: " + task.Exception);
                }
            });
    }

    private void SetOrderExpiration(string userID, string orderID)
    {
        long expirationTime = GetUnixTimestamp() + (24 * 60 * 60);

        dbReference.Child("orders").Child(userID).Child(orderID).Child("expiresAt").SetValueAsync(expirationTime)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Order expiration set for 24 hours");
                }
                else
                {
                    Debug.LogError("Error setting order expiration: " + task.Exception);
                }
            });
    }

    public void CheckOrderExpiration()
    {
        string userID = auth.CurrentUser.UserId;
        long currentTime = GetUnixTimestamp();

        dbReference.Child("orders").Child(userID).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (var item in task.Result.Children)
                {
                    long expiresAt = long.Parse(item.Child("expiresAt").Value.ToString());
                    if (currentTime > expiresAt)
                    {
                        dbReference.Child("orders").Child(userID).Child(item.Key).RemoveValueAsync();
                        Debug.Log("Expired order removed: " + item.Key);
                    }
                }
            }
        });
    }

    private long GetUnixTimestamp()
    {
        return (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
    }
}