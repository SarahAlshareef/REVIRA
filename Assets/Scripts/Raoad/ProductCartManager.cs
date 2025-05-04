using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;

public class ProductCartManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown colorDropdown;
    public TMP_Dropdown sizeDropdown;
    public TMP_Dropdown quantityDropdown;
    public Button addToCartButton;
    public TextMeshProUGUI feedbackText; // For showing success or error messages

    private DatabaseReference dbRoot;
    private bool _isAdding = false;

    private ProductsManager productsManager;
    private UserManager userManager;

    void Start()
    {
        Debug.Log("[ProductCartManager] Start");

        dbRoot = FirebaseDatabase.DefaultInstance.RootReference;
        Debug.Log("[ProductCartManager] Firebase DB Root Reference initialized.");

        productsManager = FindObjectOfType<ProductsManager>();
        if (productsManager == null)
        {
            Debug.LogError("[ProductCartManager] ProductsManager not found in scene.");
        }

        userManager = UserManager.Instance;
        if (userManager == null)
        {
            Debug.LogError("[ProductCartManager] UserManager instance is null.");
        }

        addToCartButton.onClick.AddListener(AddToCart);
    }

    void AddToCart()
    {
        if (_isAdding) return;
        _isAdding = true;

        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        string selectedQuantityStr = quantityDropdown.options[quantityDropdown.value].text;

        Debug.Log($"[ProductCartManager] Selected Color: {selectedColor}, Size: {selectedSize}, Quantity: {selectedQuantityStr}");

        if (selectedColor == "Select Color" || selectedSize == "Select Size" || selectedQuantityStr == "Select Quantity" || selectedQuantityStr == "Out of Stock")
        {
            feedbackText.text = "Please select color, size, and quantity.";
            _isAdding = false;
            return;
        }

        int quantity = int.Parse(selectedQuantityStr);
        ProductData product = productsManager.GetProductData();

        if (product == null)
        {
            Debug.LogError("[ProductCartManager] No product data available.");
            _isAdding = false;
            return;
        }

        string productId = productsManager.productID;
        string storeId = productsManager.storeID;

        float finalPrice = product.price;
        if (product.discount.exists && product.discount.percentage > 0)
        {
            finalPrice = product.price - (product.price * (product.discount.percentage / 100));
        }

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long expiresAt = timestamp + 86400; // 24 hours

        Dictionary<string, object> productData = new Dictionary<string, object>
        {
            { "productID", productId },
            { "productName", product.name },
            { "color", selectedColor },
            { "price", finalPrice },
            { "timestamp", timestamp },
            { "expiresAt", expiresAt },
            { "sizes", new Dictionary<string, object> { { selectedSize, quantity } } }
        };

        string userId = userManager.UserId;
        string path = $"REVIRA/Consumers/{userId}/cart/cartItems/{productId}";

        Debug.Log($"[ProductCartManager] Writing to path: {path}");

        dbRoot.Child(path).SetValueAsync(productData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                feedbackText.text = "Added to cart successfully!";
                Debug.Log("[ProductCartManager] Product added to cart.");
            }
            else
            {
                Debug.LogError("[ProductCartManager] Failed to add product to cart: " + task.Exception);
                feedbackText.text = "Error adding product to cart.";
            }

            _isAdding = false;
        });
    }
}
