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

    public TMP_Dropdown sizeDropdown;  // Dropdown for selecting size
    public TMP_Dropdown colorDropdown; // Dropdown for selecting color
    public TMP_Dropdown quantityDropdown; // Dropdown for selecting quantity
    public Button addToCartButton; // Button to add product to cart

    private string productID;
    private string productName;
    private float productPrice;
    private string storeID = "storeID_123"; // Store ID

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        auth = FirebaseAuth.DefaultInstance;

        addToCartButton.onClick.AddListener(AddToCart);
    }

    public void SetProductDetails(string id, string name, float price)
    {
        productID = id;
        productName = name;
        productPrice = price;
    }

    public void AddToCart()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("User not logged in!");
            return;
        }

        string userID = auth.CurrentUser.UserId;
        if (string.IsNullOrEmpty(productID))
        {
            Debug.LogError("Product ID is missing!");
            return;
        }

        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        int selectedQuantity = int.Parse(quantityDropdown.options[quantityDropdown.value].text);

        Dictionary<string, object> cartItem = new Dictionary<string, object>
        {
            { "productID", productID },
            { "productName", productName },
            { "price", productPrice },
            { "size", selectedSize },
            { "color", selectedColor },
            { "quantity", selectedQuantity },
            { "storeID", storeID },
            { "timestamp", ServerValue.Timestamp }
        };

        dbReference.Child("users").Child(userID).Child("cart").Child(productID).SetValueAsync(cartItem)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Product added to cart successfully!");
                }
                else
                {
                    Debug.LogError("Error adding product to cart: " + task.Exception);
                }
            });
    }
}