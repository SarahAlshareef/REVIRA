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
    public TMP_Dropdown sizeDropdown, colorDropdown, quantityDropdown;
    public Button addToCartButton;
    public TextMeshProUGUI errorText;

    private ProductsManager productsManager;
    private UserManager userManager;
    private Coroutine cooldownCoroutine;
    private CartManager cartManager;

    private bool isAdding = false;
    private bool hasAdded = false;

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        productsManager = FindObjectOfType<ProductsManager>();
        userManager = FindObjectOfType<UserManager>();
        cartManager = FindObjectOfType<CartManager>();

        if (addToCartButton != null)
            addToCartButton.onClick.AddListener(AddToCart);

        if (colorDropdown != null)
            colorDropdown.onValueChanged.AddListener(delegate { ValidateSelection(); });

        if (sizeDropdown != null)
            sizeDropdown.onValueChanged.AddListener(delegate { ValidateSelection(); });

        if (quantityDropdown != null)
            quantityDropdown.onValueChanged.AddListener(delegate { ValidateSelection(); });

        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }

        RemoveExpiredCartItems();
    }

    public void AddToCart()
    {
        Debug.Log("[DEBUG] AddToCart triggered");
        if (isAdding || hasAdded)
        {
            ShowError("Product was already added. Please wait a few seconds.");
            return;
        }

        if (!ValidateSelection() || string.IsNullOrEmpty(userManager.UserId))
        {
            ShowError("Invalid selection or user not logged in.");
            return;
        }

        ProductData productData = productsManager.GetProductData();
        if (productData == null || string.IsNullOrEmpty(productsManager.productID))
        {
            ShowError("Product data is missing.");
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

        isAdding = true;

        ReduceStock(selectedColor, selectedSize, quantity, () =>
        {
            string cartItemPath = $"REVIRA/Consumers/{userID}/cart/cartItems/{productID}";
            string sizePath = $"{cartItemPath}/sizes/{selectedSize}";

            dbReference.Child(sizePath).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                int existingQuantity = task.IsCompleted && task.Result.Exists ? int.Parse(task.Result.Value.ToString()) : 0;
                int newQuantity = existingQuantity + quantity;

                Dictionary<string, object> cartItem = new()
                {
                    { "productID", productID },
                    { "productName", productName },
                    { "color", selectedColor },
                    { "price", productPrice },
                    { "timestamp", GetUnixTimestamp() },
                    { "expiresAt", expirationTime }
                };

                dbReference.Child(cartItemPath).UpdateChildrenAsync(cartItem).ContinueWithOnMainThread(updateTask =>
                {
                    if (updateTask.IsCompletedSuccessfully)
                    {
                        dbReference.Child(sizePath).SetValueAsync(newQuantity).ContinueWithOnMainThread(sizeUpdate =>
                        {
                            if (sizeUpdate.IsCompletedSuccessfully)
                            {
                                Debug.Log("[DEBUG] Product added successfully.");
                                UpdateCartSummary(userID);
                                cartManager?.LoadCartItems();
                                errorText.color = Color.green;
                                errorText.text = "Product added successfully.";
                                errorText.gameObject.SetActive(true);
                                hasAdded = true;
                                cooldownCoroutine = StartCoroutine(EnableButtonAfterDelay(5f));
                            }
                            else
                            {
                                ShowError("Failed to set quantity. Try again.");
                                Debug.LogError("Size update error: " + sizeUpdate.Exception);
                            }
                            isAdding = false;
                        });
                    }
                    else
                    {
                        ShowError("Could not add product. Try again.");
                        Debug.LogError("Cart item update error: " + updateTask.Exception);
                        isAdding = false;
                    }
                });
            });
        });
    }

    private void UpdateCartSummary(string userId)
    {
        string cartItemsPath = $"REVIRA/Consumers/{userId}/cart/cartItems";
        string cartTotalPath = $"REVIRA/Consumers/{userId}/cart/cartTotal";

        dbReference.Child(cartItemsPath).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted || !task.Result.Exists) return;

            float totalPrice = 0f;
            int totalItems = 0;

            foreach (var item in task.Result.Children)
            {
                if (!item.HasChild("price") || !item.HasChild("sizes")) continue;
                float price = float.Parse(item.Child("price").Value.ToString());

                foreach (var size in item.Child("sizes").Children)
                {
                    int qty = int.Parse(size.Value.ToString());
                    totalPrice += price * qty;
                    totalItems += qty;
                }
            }

            Dictionary<string, object> cartTotalData = new()
            {
                { "totalPrice", totalPrice },
                { "totalItems", totalItems }
            };

            dbReference.Child(cartTotalPath).SetValueAsync(cartTotalData);
        });
    }

    private void ReduceStock(string color, string size, int quantity, System.Action onSuccess)
    {
        string path = $"stores/storeID_123/products/{productsManager.productID}/colors/{color}/sizes/{size}";
        dbReference.Child("REVIRA").Child(path).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted || !task.Result.Exists) return;

            int currentStock = int.Parse(task.Result.Value.ToString());
            int updatedStock = Mathf.Max(currentStock - quantity, 0);

            dbReference.Child("REVIRA").Child(path).SetValueAsync(updatedStock).ContinueWithOnMainThread(stockUpdateTask =>
            {
                if (stockUpdateTask.IsCompleted)
                    onSuccess?.Invoke();
                else
                    isAdding = false;
            });
        });
    }

    private bool ValidateSelection()
    {
        if (colorDropdown.options[colorDropdown.value].text == "Select Color")
        {
            ShowError("Please select a color.");
            return false;
        }

        if (sizeDropdown.options[sizeDropdown.value].text == "Select Size")
        {
            ShowError("Please select a size.");
            return false;
        }

        if (quantityDropdown.options[quantityDropdown.value].text == "Select Quantity")
        {
            ShowError("Please select a quantity.");
            return false;
        }

        ClearMessage();
        return true;
    }

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.color = Color.red;
            errorText.text = message;
            errorText.gameObject.SetActive(true);
            CancelInvoke(nameof(ClearMessage));
            Invoke(nameof(ClearMessage), 5f);
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

    private IEnumerator EnableButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        addToCartButton.interactable = true;
        hasAdded = false;
    }

    private long GetUnixTimestamp()
    {
        return (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
    }

    private void RemoveExpiredCartItems()
    {
        string userID = userManager.UserId;
        DatabaseReference cartItemsRef = dbReference.Child($"REVIRA/Consumers/{userID}/cart/cartItems");

        cartItemsRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                long now = GetUnixTimestamp();
                List<string> expiredItems = new();

                foreach (var item in task.Result.Children)
                {
                    if (item.Child("expiresAt").Value != null &&
                        long.Parse(item.Child("expiresAt").Value.ToString()) < now)
                    {
                        expiredItems.Add(item.Key);
                        RestoreStock(item.Key, item);
                    }
                }

                foreach (string id in expiredItems)
                    cartItemsRef.Child(id).RemoveValueAsync();

                cartItemsRef.GetValueAsync().ContinueWithOnMainThread(checkTask =>
                {
                    if (!checkTask.Result.Exists || checkTask.Result.ChildrenCount == 0)
                        dbReference.Child($"REVIRA/Consumers/{userID}/cart").RemoveValueAsync();
                });
            }
        });
    }

    private void RestoreStock(string productID, DataSnapshot item)
    {
        foreach (var sizeEntry in item.Child("sizes").Children)
        {
            string size = sizeEntry.Key;
            int qty = int.Parse(sizeEntry.Value.ToString());
            string path = $"stores/storeID_123/products/{productID}/colors/{item.Child("color").Value}/sizes/{size}";

            dbReference.Child("REVIRA").Child(path).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    int currentStock = int.Parse(task.Result.Value.ToString());
                    dbReference.Child("REVIRA").Child(path).SetValueAsync(currentStock + qty);
                }
            });
        }
    }
}
