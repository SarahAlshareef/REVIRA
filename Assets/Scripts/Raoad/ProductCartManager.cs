using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using TMPro;

public class ProductCartManager : MonoBehaviour
{
    private DatabaseReference dbReference;
    public TMP_Dropdown sizeDropdown, colorDropdown, quantityDropdown;
    public Button addToCartButton;
    public TextMeshProUGUI errorText;
    public TextMeshProUGUI successText;

    private ProductsManager productsManager;
    private UserManager userManager;

    private bool isAdding = false;
   
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
            

        RemoveExpiredCartItems();
    }

    public void ValidateSelection()
    {
        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        if (selectedColor == "Select Color")
        {
            ShowError("Please select a color.");
            return;
        }

        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        if (selectedSize == "Select Size")
        {
            ShowError("Please select a size.");
            return;
        }

        string selectedQuantity = quantityDropdown.options[quantityDropdown.value].text;
        if (selectedQuantity == "Select Quantity")
        {
            ShowError("Please select a quantity.");
            return;
        }
        errorText.text = ("");
        errorText.color = Color.red;  

    }
    public void AddToCart()
    {
        if (isAdding) return;

        ValidateSelection();
        if (!string.IsNullOrEmpty(errorText.text)) return;

        isAdding = true;

        if (string.IsNullOrEmpty(userManager.UserId))
        {
            ShowError("User not logged in.");
            isAdding = false;
            
            return;
        }

        ProductData productData = productsManager.GetProductData();
        if (productData == null || string.IsNullOrEmpty(productsManager.productID))
        {
            ShowError("Product data is missing.");
            isAdding = false;
            
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
            isAdding = false;
            
            return;
        }

        ReduceStock(selectedColor, selectedSize, quantity, () =>
        {
            dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child("cartItems").Child(productID).Child("sizes").Child(selectedSize).GetValueAsync().ContinueWith(task =>
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

                dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child("cartItems").Child(productID).UpdateChildrenAsync(cartItem).ContinueWith(updateTask =>
                {
                    isAdding = false;

                    if (updateTask.IsCompleted)
                    {
                        UpdateCartSummary(userID);
                        addToCartButton.interactable = true;
                        Debug.Log("Before calling show success");
                        ShowSuccess("Product added successfully!");
                       
                    }
                    else
                    {
                        Debug.LogError("failled to add");
                        ShowError("Failed to add product. Try again.");
                        
                    }
                });
            });
        });
    }
    private void UpdateCartSummary(string userId)
    {
        dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").Child("cartItems").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                float totalPrice = 0f;
                int totalItems = 0;

                foreach (var item in task.Result.Children)
                {
                    float price = float.Parse(item.Child("price").Value.ToString());
                    foreach (var size in item.Child("sizes").Children)
                    {
                        int qty = int.Parse(size.Value.ToString());
                        totalPrice += price * qty;
                        totalItems += qty;
                    }
                }

                Dictionary<string, object> cartTotalData = new Dictionary<string, object>
                {
                    { "totalPrice", totalPrice },
                    { "totalItems", totalItems }
                };

                dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").Child("cartTotal").SetValueAsync(cartTotalData);
            }
        });
    }
    private void ReduceStock(string color, string size, int quantity, System.Action onSuccess)
    {
        string path = $"stores/storeID_123/products/{productsManager.productID}/colors/{color}/sizes/{size}";
        dbReference.Child("REVIRA").Child(path).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                int currentStock = int.Parse(task.Result.Value.ToString());
                int updatedStock = Mathf.Max(currentStock - quantity, 0);

                dbReference.Child("REVIRA").Child(path).SetValueAsync(updatedStock).ContinueWith(stockUpdateTask =>
                {
                    if (stockUpdateTask.IsCompleted)
                    {
                        onSuccess?.Invoke();
                    }
                    else
                    {
                        Debug.LogError("Error updating stock: " + stockUpdateTask.Exception);
                        addToCartButton.interactable = true;
                        isAdding = false;
                    }
                });
            }
        });
    }
    private void RemoveExpiredCartItems()
    {
        string userID = userManager.UserId;
        dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child("cartItems").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                long currentTimestamp = GetUnixTimestamp();
                foreach (var item in task.Result.Children)
                {
                    if (item.Child("expiresAt").Value != null && long.Parse(item.Child("expiresAt").Value.ToString()) < currentTimestamp)
                    {
                        string productID = item.Key;
                        dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child("cartItems").Child(productID).RemoveValueAsync();
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
            string path = $"stores/storeID_123/products/{productID}/colors/{item.Child("color").Value}/sizes/{size}";

            dbReference.Child("REVIRA").Child(path).GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    int currentStock = int.Parse(task.Result.Value.ToString());
                    dbReference.Child("REVIRA").Child(path).SetValueAsync(currentStock + quantity);
                }
            });
        }
    }
    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.color = Color.red;
            errorText.text = message;
            errorText.gameObject.SetActive(true);
            CancelInvoke(nameof(ClearMessage));
            Invoke(nameof(ClearMessage), 3f);
        }
    }

    private void ShowSuccess(string message)
    {
        Debug.Log("ShowSuccess trigerd");
        if (successText != null)
        {
            successText.text = message;
            successText.gameObject.SetActive(true);

            CancelInvoke(nameof(ClearSuccess));
            Invoke(nameof(ClearSuccess), 3f);
        }
    }
    private void ClearSuccess()
    {
        if (successText != null)
        {
            successText.text = "";
            successText.gameObject.SetActive(false);
        }
    }

    private void ClearMessage()
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


