using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;

public class ProductCartManager : MonoBehaviour
{
    private DatabaseReference dbReference;

    public TMP_Dropdown sizeDropdown;
    public TMP_Dropdown colorDropdown;
    public TMP_Dropdown quantityDropdown;
    public Button addToCartButton;

    private string storeID = "storeID_123"; // Store ID
    private ProductsManager productsManager; // Reference to ProductsManager

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        // Find ProductsManager in the scene
        productsManager = FindObjectOfType<ProductsManager>();

        // Add listener to "Add to Cart" button
        addToCartButton.onClick.AddListener(AddToCart);
    }

    public void AddToCart()
    {
        // Ensure UserManager is initialized
        if (UserManager.Instance == null)
        {
            Debug.LogError("UserManager is not initialized!");
            return;
        }

        // Get user data from UserManager
        string userID = UserManager.Instance.UserId;
        string userName = UserManager.Instance.FirstName + " " + UserManager.Instance.LastName;

        // Ensure ProductsManager is initialized
        if (productsManager == null)
        {
            Debug.LogError("ProductsManager script is missing in the scene!");
            return;
        }

        // Get product data from ProductsManager
        ProductData productData = productsManager.GetProductData();
        if (productData == null || string.IsNullOrEmpty(productsManager.productID))
        {
            Debug.LogError("Product data is missing!");
            return;
        }

        string productID = productsManager.productID;
        string productName = productData.name;
        float productPrice = productData.price;

        // Get selected options
        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        string selectedQuantity = quantityDropdown.options[quantityDropdown.value].text;

        // Debugging selected values
        Debug.Log("Selected Color: " + selectedColor);
        Debug.Log("Selected Size: " + selectedSize);
        Debug.Log("Selected Quantity: " + selectedQuantity);

        // Ensure selections are valid
        if (selectedColor == "Select Color" || selectedSize == "Select Size" || selectedQuantity == "Select Quantity")
        {
            Debug.LogError("Size, Color, or Quantity is not selected properly!");
            return;
        }

        int quantity = int.Parse(selectedQuantity);
        string orderID = dbReference.Child("carts").Child(storeID).Child(userID).Push().Key; // Generate unique order ID
        long expirationTime = GetUnixTimestamp() + (24 * 60 * 60); // 24 hours in seconds

        // Create cart item dictionary
        Dictionary<string, object> cartItem = new Dictionary<string, object>
        {
            { "userID", userID },
            { "userName", userName },
            { "productID", productID },
            { "productName", productName },
            { "price", productPrice },
            { "size", selectedSize },
            { "color", selectedColor },
            { "quantity", quantity },
            { "timestamp", ServerValue.Timestamp },
            { "expiresAt", expirationTime } // Session expiration
        };

        // Store the order under carts/{storeID}/{userID}/{orderID}
        dbReference.Child("carts").Child(storeID).Child(userID).Child(orderID).SetValueAsync(cartItem)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Order added to Firebase successfully!");
                }
                else
                {
                    Debug.LogError("Error adding order to Firebase: " + task.Exception);
                }
            });
    }

    // Helper function to get current Unix timestamp
    private long GetUnixTimestamp()
    {
        return (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
    }
}