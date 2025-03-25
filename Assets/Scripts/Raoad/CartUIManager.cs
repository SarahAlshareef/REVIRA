// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// Firebase
using Firebase.Database;
using Firebase.Extensions;

// TMP
using TMPro;

// C#
using System;
using System.Collections;
using System.Collections.Generic;

public class CartUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject scrollViewContent;
    public GameObject cartItemPanelTemplate; // Only one template kept in the scene
    public GameObject scrollViewParent; // The ScrollView itself to hide when empty
    public TextMeshProUGUI totalText;

    private DatabaseReference dbReference;
    private string userID;

    private float totalAmount = 0f;

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        userID = FindObjectOfType<UserManager>().UserId;

        if (!string.IsNullOrEmpty(userID))
        {
            FetchCartData();
        }
    }

    public void FetchCartData()
    {
        dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                ClearScrollView();
                totalAmount = 0f;
                scrollViewParent.SetActive(true);

                foreach (DataSnapshot product in task.Result.Children)
                {
                    GameObject panel = Instantiate(cartItemPanelTemplate, scrollViewContent.transform);
                    panel.SetActive(true);

                    string productID = product.Key;
                    string productName = product.Child("productName").Value.ToString();
                    string color = product.Child("color").Value.ToString();
                    float price = float.Parse(product.Child("price").Value.ToString());
                    long timestamp = long.Parse(product.Child("timestamp").Value.ToString());
                    long expiresAt = long.Parse(product.Child("expiresAt").Value.ToString());

                    string size = "";
                    int quantity = 0;
                    foreach (var sizeEntry in product.Child("sizes").Children)
                    {
                        size = sizeEntry.Key;
                        quantity = int.Parse(sizeEntry.Value.ToString());
                    }

                    float subtotal = price * quantity;
                    totalAmount += subtotal;

                    panel.transform.Find("Product name").GetComponent<TextMeshProUGUI>().text = productName;
                    panel.transform.Find("Text (price)").GetComponent<TextMeshProUGUI>().text = price.ToString("F2");
                    panel.transform.Find("Dropdown (Color)").GetComponent<TMP_Dropdown>().value = 0;
                    panel.transform.Find("Dropdown (Size)").GetComponent<TMP_Dropdown>().value = 0;
                    panel.transform.Find("Dropdown (Quantity)").GetComponent<TMP_Dropdown>().value = quantity - 1;

                    TMP_Dropdown colorDropdown = panel.transform.Find("Dropdown (Color)").GetComponent<TMP_Dropdown>();
                    TMP_Dropdown sizeDropdown = panel.transform.Find("Dropdown (Size)").GetComponent<TMP_Dropdown>();
                    TMP_Dropdown quantityDropdown = panel.transform.Find("Dropdown (Quantity)").GetComponent<TMP_Dropdown>();

                    colorDropdown.onValueChanged.AddListener((val) => UpdateCartField(productID, "color", colorDropdown.options[val].text));
                    sizeDropdown.onValueChanged.AddListener((val) => UpdateCartSize(productID, size, sizeDropdown.options[val].text, quantity));
                    quantityDropdown.onValueChanged.AddListener((val) => UpdateCartQuantity(productID, size, val + 1));

                    Button removeBtn = panel.transform.Find("Button (Remove)").GetComponent<Button>();
                    removeBtn.onClick.AddListener(() => RemoveCartItem(productID, size, quantity));
                }
                totalText.text = "Total : " + totalAmount.ToString("F2");
            }
            else
            {
                scrollViewParent.SetActive(false);
            }
        });
    }

    void UpdateCartField(string productID, string field, string value)
    {
        dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child(productID).Child(field).SetValueAsync(value);
    }

    void UpdateCartSize(string productID, string oldSize, string newSize, int quantity)
    {
        var sizeRef = dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child(productID).Child("sizes");
        sizeRef.Child(oldSize).RemoveValueAsync();
        sizeRef.Child(newSize).SetValueAsync(quantity);
    }

    void UpdateCartQuantity(string productID, string size, int newQuantity)
    {
        dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child(productID).Child("sizes").Child(size).SetValueAsync(newQuantity);
        FetchCartData(); // Recalculate total
    }

    void RemoveCartItem(string productID, string size, int quantity)
    {
        dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child(productID).RemoveValueAsync();

        // Restore stock
        string storeID = "storeID_123"; // You can change it if needed
        dbReference.Child("REVIRA").Child("stores").Child(storeID).Child("products").Child(productID)
            .Child("colors").Child("" /* you can track color */).Child("sizes").Child(size).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    int stock = int.Parse(task.Result.Value.ToString());
                    int updatedStock = stock + quantity;
                    dbReference.Child("REVIRA").Child("stores").Child(storeID).Child("products").Child(productID)
                        .Child("colors").Child("" /* same here */).Child("sizes").Child(size).SetValueAsync(updatedStock);
                }
            });

        FetchCartData();
    }

    void ClearScrollView()
    {
        foreach (Transform child in scrollViewContent.transform)
        {
            if (child != cartItemPanelTemplate.transform)
                Destroy(child.gameObject);
        }
    }
}
