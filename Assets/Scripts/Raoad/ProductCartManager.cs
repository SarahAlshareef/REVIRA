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

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        productsManager = FindObjectOfType<ProductsManager>();
        userManager = FindObjectOfType<UserManager>();
        cartManager = FindObjectOfType<CartManager>();

        if (addToCartButton != null)
        {
            addToCartButton.onClick.AddListener(AddToCart);
        }

        colorDropdown?.onValueChanged.AddListener(_ => ValidateSelection());
        sizeDropdown?.onValueChanged.AddListener(_ => ValidateSelection());
        quantityDropdown?.onValueChanged.AddListener(_ => ValidateSelection());

        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }

        RemoveExpiredCartItems();
    }

    public void AddToCart()
    {
        if (isAdding || !addToCartButton.interactable) return;
        if (!ValidateSelection()) return;

        string userID = userManager?.UserId;
        if (string.IsNullOrEmpty(userID))
        {
            ShowError("User not logged in.");
            return;
        }

        ProductData productData = productsManager?.GetProductData();
        string productID = productsManager?.productID;

        if (productData == null || string.IsNullOrEmpty(productID))
        {
            ShowError("Missing product data.");
            return;
        }

        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        int quantity = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        long expirationTime = GetUnixTimestamp() + 86400;

        if (!productsManager.productColorsAndSizes.ContainsKey(selectedColor) ||
            !productsManager.productColorsAndSizes[selectedColor].ContainsKey(selectedSize) ||
            productsManager.productColorsAndSizes[selectedColor][selectedSize] < quantity)
        {
            ShowError("Not enough stock.");
            return;
        }

        isAdding = true;
        addToCartButton.interactable = false;

        ReduceStock(selectedColor, selectedSize, quantity, () =>
        {
            var cartItem = new Dictionary<string, object>
            {
                { "productID", productID },
                { "productName", productData.name },
                { "color", selectedColor },
                { "price", productData.price },
                { "timestamp", GetUnixTimestamp() },
                { "expiresAt", expirationTime },
                { "sizes", new Dictionary<string, object> { { selectedSize, quantity } } }
            };

            dbReference.Child("REVIRA/Consumers").Child(userID).Child("cart/cartItems").Child(productID)
                .SetValueAsync(cartItem).ContinueWithOnMainThread(task =>
                {
                    isAdding = false;
                    if (task.IsCompletedSuccessfully)
                    {
                        ShowSuccess("Product added to cart.");
                        UpdateCartSummary(userID, () => cartManager?.LoadCartItems());
                        StartCoroutine(EnableButtonAfterDelay(1.5f));
                    }
                    else
                    {
                        Debug.LogError("Firebase set failed: " + task.Exception);
                        ShowError("Failed to add to cart.");
                        addToCartButton.interactable = true;
                    }
                });
        });
    }

    private void UpdateCartSummary(string userId, System.Action onComplete)
    {
        dbReference.Child("REVIRA/Consumers").Child(userId).Child("cart/cartItems").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
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

                var cartTotalData = new Dictionary<string, object>
                {
                    { "totalPrice", totalPrice },
                    { "totalItems", totalItems }
                };

                dbReference.Child("REVIRA/Consumers").Child(userId).Child("cart/cartTotal")
                    .SetValueAsync(cartTotalData).ContinueWithOnMainThread(_ => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        });
    }

    private void ReduceStock(string color, string size, int quantity, System.Action onSuccess)
    {
        string path = $"stores/storeID_123/products/{productsManager.productID}/colors/{color}/sizes/{size}";
        dbReference.Child("REVIRA").Child(path).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                int stock = int.Parse(task.Result.Value.ToString());
                int updated = Mathf.Max(0, stock - quantity);

                dbReference.Child("REVIRA").Child(path).SetValueAsync(updated).ContinueWithOnMainThread(_ =>
                {
                    onSuccess?.Invoke();
                });
            }
        });
    }

    private void RemoveExpiredCartItems()
    {
        string userID = userManager.UserId;
        var cartRef = dbReference.Child("REVIRA/Consumers").Child(userID).Child("cart/cartItems");

        cartRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                long now = GetUnixTimestamp();
                List<string> expired = new();

                foreach (var item in task.Result.Children)
                {
                    if (item.Child("expiresAt").Value != null &&
                        long.Parse(item.Child("expiresAt").Value.ToString()) < now)
                    {
                        expired.Add(item.Key);
                        RestoreStock(item.Key, item);
                    }
                }

                foreach (string id in expired)
                    cartRef.Child(id).RemoveValueAsync();
            }
        });
    }

    private void RestoreStock(string productID, DataSnapshot item)
    {
        string color = item.Child("color").Value.ToString();

        foreach (var size in item.Child("sizes").Children)
        {
            string sizeKey = size.Key;
            int qty = int.Parse(size.Value.ToString());
            string path = $"stores/storeID_123/products/{productID}/colors/{color}/sizes/{sizeKey}";

            dbReference.Child("REVIRA").Child(path).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    int current = int.Parse(task.Result.Value.ToString());
                    dbReference.Child("REVIRA").Child(path).SetValueAsync(current + qty);
                }
            });
        }
    }

    private bool ValidateSelection()
    {
        if (colorDropdown.options[colorDropdown.value].text == "Select Color" ||
            sizeDropdown.options[sizeDropdown.value].text == "Select Size" ||
            quantityDropdown.options[quantityDropdown.value].text == "Select Quantity")
        {
            ShowError("Please make all selections.");
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
            Invoke(nameof(ClearMessage), 4f);
        }
    }

    private void ShowSuccess(string message)
    {
        if (errorText != null)
        {
            errorText.color = Color.green;
            errorText.text = message;
            errorText.gameObject.SetActive(true);
            CancelInvoke(nameof(ClearMessage));
            Invoke(nameof(ClearMessage), 3f);
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
    }

    private long GetUnixTimestamp()
    {
        return (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
    }
}
