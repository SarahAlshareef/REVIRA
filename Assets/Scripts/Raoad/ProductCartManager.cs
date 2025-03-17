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
    public TextMeshProUGUI errorText; // ·≈ŸÂ«— «·√Œÿ«¡ ›Ì «·Ê«ÃÂ…

    private string storeID = "storeID_123"; // Store ID
    private ProductsManager productsManager; // Reference to ProductsManager
    private UserManager userManager; // Reference to UserManager

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        // «·⁄ÀÊ— ⁄·Ï «·”ﬂ—» «  «·√Œ—Ï ›Ì «·„‘Âœ
        productsManager = FindObjectOfType<ProductsManager>();
        userManager = FindObjectOfType<UserManager>();

        if (addToCartButton != null)
        {
            addToCartButton.onClick.AddListener(AddToCart);
        }

        if (errorText != null)
        {
            errorText.text = ""; // ≈Œ›«¡ √Ì —”«·… Œÿ√ ⁄‰œ «·»œ«Ì…
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

        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        string selectedQuantity = quantityDropdown.options[quantityDropdown.value].text;

        // «· Õﬁﬁ «· œ—ÌÃÌ „‰ «·ﬁÌ„ »«· — Ì» «·’ÕÌÕ
        if (selectedColor == "Select Color" && selectedSize == "Select Size" && selectedQuantity == "Select Quantity")
        {
            ShowError("«·—Ã«¡ «Œ Ì«— „Ê«’›«  «·„‰ Ã.");
            return;
        }

        if (selectedColor != "Select Color" && selectedSize == "Select Size")
        {
            ShowError("«·—Ã«¡ «Œ Ì«— «·ÕÃ„ Ê«·ﬂ„Ì….");
            return;
        }

        if (selectedColor != "Select Color" && selectedSize != "Select Size" && selectedQuantity == "Select Quantity")
        {
            ShowError("«·—Ã«¡ «Œ Ì«— «·ﬂ„Ì….");
            return;
        }

        // ≈–« Ê’· ≈·Ï Â‰«° ›Â–« Ì⁄‰Ì √‰ ﬂ· «·ﬁÌ„  „ «Œ Ì«—Â«
        int quantity = int.Parse(selectedQuantity);
        string userID = userManager.UserId;
        string productID = productsManager.productID;
        string productName = productData.name;
        float productPrice = productData.price;
        string orderID = dbReference.Child("carts").Push().Key;
        long expirationTime = GetUnixTimestamp() + (24 * 60 * 60);

        //  ÃÂÌ“ «·»Ì«‰«  ·Õ›ŸÂ« ›Ì Firebase
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

        // Õ›Ÿ «·»Ì«‰«  ›Ì Firebase
        dbReference.Child("carts").Child(storeID).Child(userID).Child(orderID).SetValueAsync(cartItem)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    ShowError(" „  ≈÷«›… «·„‰ Ã ≈·Ï «·”·… »‰Ã«Õ!", success: true);
                    Debug.Log("Order added to Firebase successfully!");
                }
                else
                {
                    Debug.LogError("Error adding order to Firebase: " + task.Exception);
                }
            });
    }

    // œ«·… ·≈ŸÂ«— «·√Œÿ«¡ ⁄·Ï «·‘«‘…
    void ShowError(string message, bool success = false)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = success ? Color.green : Color.red; // «·√Œ÷— ··‰Ã«Õ° «·√Õ„— ··√Œÿ«¡
        }
    }

    // œ«·… ··Õ’Ê· ⁄·Ï «· ÊﬁÌ  «·Õ«·Ì
    private long GetUnixTimestamp()
    {
        return (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
    }
}