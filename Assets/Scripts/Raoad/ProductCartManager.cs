using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using TMPro;

public class ProductCartManager : MonoBehaviour
{
    private DatabaseReference dbReference;
    public TMP_Dropdown sizeDropdown;
    public TMP_Dropdown colorDropdown;
    public TMP_Dropdown quantityDropdown;
    public Button addToCartButton;
    public TextMeshProUGUI errorText;

    private string storeID = "storeID_123";
    private ProductsManager productsManager;
    private UserManager userManager;

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        productsManager = FindObjectOfType<ProductsManager>();
        userManager = FindObjectOfType<UserManager>();

        if (addToCartButton != null)
        {
            addToCartButton.onClick.AddListener(AddToCart);
        }

        // Hide error message on start
        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }

        // Add dropdown listeners to validate selection
        if (colorDropdown != null)
            colorDropdown.onValueChanged.AddListener(delegate { ValidateSelection(); });

        if (sizeDropdown != null)
            sizeDropdown.onValueChanged.AddListener(delegate { ValidateSelection(); });

        if (quantityDropdown != null)
            quantityDropdown.onValueChanged.AddListener(delegate { ValidateSelection(); });
    }

    public void ValidateSelection()
    {
        string selectedColor = (colorDropdown.value > 0) ? colorDropdown.options[colorDropdown.value].text : null;
        string selectedSize = (sizeDropdown.value > 0) ? sizeDropdown.options[sizeDropdown.value].text : null;
        string selectedQuantity = (quantityDropdown.value > 0) ? quantityDropdown.options[quantityDropdown.value].text : null;

        if (selectedColor == null)
        {
            ShowError("Please select a color.");
            return;
        }

        if (selectedSize == null)
        {
            ShowError("Please select a size.");
            return;
        }

        if (selectedQuantity == null)
        {
            ShowError("Please select a quantity.");
            return;
        }

        HideError();
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

        string selectedColor = (colorDropdown.value > 0) ? colorDropdown.options[colorDropdown.value].text : null;
        string selectedSize = (sizeDropdown.value > 0) ? sizeDropdown.options[sizeDropdown.value].text : null;
        string selectedQuantity = (quantityDropdown.value > 0) ? quantityDropdown.options[quantityDropdown.value].text : null;

        Debug.Log("Attempting to add product to cart...");
        Debug.Log("User ID: " + userManager.UserId);
        Debug.Log("Product ID: " + productsManager.productID);
        Debug.Log("Selected Color: " + selectedColor);
        Debug.Log("Selected Size: " + selectedSize);
        Debug.Log("Selected Quantity: " + selectedQuantity);

        if (selectedColor == null)
        {
            ShowError("Please select a color.");
            return;
        }

        if (selectedSize == null)
        {
            ShowError("Please select a size.");
            return;
        }

        if (selectedQuantity == null)
        {
            ShowError("Please select a quantity.");
            return;
        }

        int quantity = int.Parse(selectedQuantity);

        // Check stock availability before adding to cart
        if (!productsManager.productColorsAndSizes.ContainsKey(selectedColor) ||
            !productsManager.productColorsAndSizes[selectedColor].ContainsKey(selectedSize) ||
            productsManager.productColorsAndSizes[selectedColor][selectedSize] < quantity)
        {
            ShowError("This product is out of stock.");
            return;
        }

        long expirationTime = GetUnixTimestamp() + (24 * 60 * 60);

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

        dbReference.Child("users").Child(userID).Child("cart").Child(productID).SetValueAsync(cartItem)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    ShowError("Product added to cart successfully!", success: true);
                    Debug.Log("Order added to Firebase successfully!");
                }
                else
                {
                    Debug.LogError("Error adding order to Firebase: " + task.Exception);
                }
            });
    }

    void ShowError(string message, bool success = false)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = success ? Color.green : Color.red;
            errorText.gameObject.SetActive(true);
        }
    }

    void HideError()
    {
        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }
    }

    private long GetUnixTimestamp()
    {
        return (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
    }
}
