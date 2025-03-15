using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;
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
    private UserManager userManager; // Reference to UserManager

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        // Find the required managers
        productsManager = FindObjectOfType<ProductsManager>();
        userManager = FindObjectOfType<UserManager>();

        if (addToCartButton != null)
        {
            addToCartButton.onClick.AddListener(AddToCart);
        }
    }

    public void AddToCart()
    {
        if (userManager == null)
        {
            Debug.LogError("UserManager is missing in the scene!");
            return;
        }

        if (productsManager == null)
        {
            Debug.LogError("ProductsManager is missing in the scene!");
            return;
        }

        if (string.IsNullOrEmpty(userManager.UserId))
        {
            Debug.LogError("User is not logged in!");
            return;
        }

        ProductData productData = productsManager.GetProductData();
        if (productData == null || string.IsNullOrEmpty(productsManager.productID))
        {
            Debug.LogError("Product data is missing!");
            return;
        }

        string userID = userManager.UserId;
        string productID = productsManager.productID;
        string productName = productData.name;
        float productPrice = productData.price;

        // Get selected values from dropdowns
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
        string orderID = dbReference.Child("carts").Push().Key; // Generate unique order ID
        long expirationTime = GetUnixTimestamp() + (24 * 60 * 60); // 24 hours in seconds

        // Prepare the cart data to be stored in Firebase
        Dictionary<string, object> cartItem = new Dictionary<string, object>
        {
            { "productID", productID },
            { "productName", productName },
            { "price", productPrice },
            { "size", selectedSize },
            { "color", selectedColor },
            { "quantity", quantity },
            { "storeID", storeID },
            { "timestamp", GetUnixTimestamp() },
            { "expiresAt", expirationTime }
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