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
    private FirebaseUser user;

    public TMP_Text productNameText;
    public TMP_Text productDescriptionText;
    public TMP_Text productPriceText;

    public TMP_Dropdown sizeDropdown;
    public TMP_Dropdown colorDropdown;
    public TMP_Dropdown quantityDropdown;

    public Button addToCartButton;



    // Start is called before the first frame update
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                auth = FirebaseAuth.DefaultInstance;
                user = auth.CurrentUser;

                if (user == null)
                {
                    Debug.LogError("User not logged in.");
                    return;
                }
            }
            else
            {
                Debug.LogError("Firebase setup error: " + task.Result);
            }
        });

        addToCartButton.onClick.AddListener(AddProductToCart);
    }

    void AddProductToCart()
    {
        if (user == null)
        {
            Debug.LogError("User not logged in.");
            return;
        }

        string productName = productNameText.text; // proudact name
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        int selectedQuantity = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        float productPrice = float.Parse(productPriceText.text.Trim('$'));

        DatabaseReference cartRef = dbReference.Child("cart").Child(user.UserId).Push();

        CartItem newItem = new CartItem
        {
            productName = productName,
            size = selectedSize,
            color = selectedColor,
            quantity = selectedQuantity,
            price = productPrice,
            userId = user.UserId
        };

        cartRef.SetRawJsonValueAsync(JsonUtility.ToJson(newItem)).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Product added to cart successfully!");
            }
            else
            {
                Debug.LogError("Failed to add product to cart: " + task.Exception);
            }
        });
    }


    [System.Serializable]
    public class CartItem
    {
        public string productName;
        public string size;
        public string color;
        public int quantity;
        public float price;
        public string userId;

    }
}   

