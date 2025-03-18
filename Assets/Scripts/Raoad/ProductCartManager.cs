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
            addToCartButton.onClick.AddListener(AddToCart);

        if (colorDropdown != null)
            colorDropdown.onValueChanged.AddListener(delegate { ValidateSelection(); });

        if (sizeDropdown != null)
            sizeDropdown.onValueChanged.AddListener(delegate { ValidateSelection(); });

        if (quantityDropdown != null)
            quantityDropdown.onValueChanged.AddListener(delegate { ValidateSelection(); });

        if (errorText != null)
            errorText.text = "";

        RemoveExpiredCartItems();
    }

    public void ValidateSelection()
    {
        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        string selectedQuantity = quantityDropdown.options[quantityDropdown.value].text;

        if (selectedColor == "Select Color")
        {
            ShowError("Please select a color.");
            return;
        }

        if (selectedSize == "Select Size")
        {
            ShowError("Please select a size.");
            return;
        }

        if (selectedQuantity == "Select Quantity")
        {
            ShowError("Please select a quantity.");
            return;
        }

        ShowError(""); // Clear error if everything is selected correctly
    }

    public void AddToCart()
    {
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
        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        int quantity = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        long expirationTime = GetUnixTimestamp() + (24 * 60 * 60);

        if (!productsManager.productColorsAndSizes.ContainsKey(selectedColor) ||
            !productsManager.productColorsAndSizes[selectedColor].ContainsKey(selectedSize) ||
            productsManager.productColorsAndSizes[selectedColor][selectedSize] < quantity)
        {
            ShowError("This product is out of stock.");
            return;
        }

        // ÎÕã ÇáßãíÉ ãÈÇÔÑÉ ÞÈá ÇáÅÖÇÝÉ
        ReduceStock(selectedColor, selectedSize, quantity, () =>
        {
            dbReference.Child("users").Child(userID).Child("cart").Child(productID).Child("sizes").Child(selectedSize).GetValueAsync().ContinueWith(task =>
            {
                int existingQuantity = task.IsCompleted && task.Result.Exists ? int.Parse(task.Result.Value.ToString()) : 0;
                int newQuantity = existingQuantity + quantity;

                Dictionary<string, object> cartItem = new Dictionary<string, object>
                {
                    { "productID", productID },
                    { "productName", productName },
                    { "color", selectedColor },
                    { "sizes/" + selectedSize, newQuantity },
                    { "price", productPrice },
                    { "timestamp", GetUnixTimestamp() },
                    { "expiresAt", expirationTime }
                };

                dbReference.Child("users").Child(userID).Child("cart").Child(productID).UpdateChildrenAsync(cartItem).ContinueWith(updateTask =>
                {
                    if (updateTask.IsCompleted)
                    {
                        ShowError("Product added to cart successfully!", true);
                    }
                    else
                    {
                        Debug.LogError("Error adding order to Firebase: " + updateTask.Exception);
                    }
                });
            });
        });
    }

    private void ReduceStock(string color, string size, int quantity, System.Action onSuccess)
    {
        string path = $"stores/{storeID}/products/{productsManager.productID}/colors/{color}/sizes/{size}";
        dbReference.Child(path).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                int currentStock = int.Parse(task.Result.Value.ToString());
                int updatedStock = Mathf.Max(currentStock - quantity, 0);

                dbReference.Child(path).SetValueAsync(updatedStock).ContinueWith(stockUpdateTask =>
                {
                    if (stockUpdateTask.IsCompleted)
                    {
                        Debug.Log("Stock updated successfully.");
                        onSuccess?.Invoke();
                    }
                    else
                    {
                        Debug.LogError("Error updating stock: " + stockUpdateTask.Exception);
                    }
                });
            }
        });
    }

    private void RemoveExpiredCartItems()
    {
        string userID = userManager.UserId;
        dbReference.Child("users").Child(userID).Child("cart").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                long currentTimestamp = GetUnixTimestamp();
                foreach (var item in task.Result.Children)
                {
                    if (item.Child("expiresAt").Value != null && long.Parse(item.Child("expiresAt").Value.ToString()) < currentTimestamp)
                    {
                        string productID = item.Key;
                        dbReference.Child("users").Child(userID).Child("cart").Child(productID).RemoveValueAsync();
                        RestoreStock(productID, item);
                    }
                }
            }
        });
    }

    private void RestoreStock(string productID, DataSnapshot item)
    {
        foreach (var sizeEntry in item.Child("sizes").Children)
        {
            string size = sizeEntry.Key;
            int quantity = int.Parse(sizeEntry.Value.ToString());
            string path = $"stores/{storeID}/products/{productID}/colors/{item.Child("color").Value.ToString()}/sizes/{size}";
            dbReference.Child(path).GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    int currentStock = int.Parse(task.Result.Value.ToString());
                    dbReference.Child(path).SetValueAsync(currentStock + quantity);
                }
            });
        }
    }

    private void ShowError(string message, bool success = false)
    {
        if (errorText != null)
        {
            errorText.text = message;
            
        }
    }

    private long GetUnixTimestamp()
    {
        return (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
    }
}
