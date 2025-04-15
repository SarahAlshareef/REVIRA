// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

public class CartManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform cartContent;
    public GameObject cartItemPrefab;
    public TextMeshProUGUI totalText;

    private string userId;
    private const string storeId = "storeID_123";
    private DatabaseReference dbRef;

    private Dictionary<string, float> itemTotals = new();
    private float currentTotal = 0f;

    void Start()
    {
        Debug.Log("CartManager started...");

        
        if (UserManager.Instance == null)
        {
            Debug.LogError("UserManager is NULL. Scene was loaded without login?");
            return;
        }

        
        Debug.Log("Cart Content: " + (cartContent != null));
        Debug.Log("Cart Item Prefab: " + (cartItemPrefab != null));
        Debug.Log("Total Text: " + (totalText != null));

       
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;

       
        LoadCartTotal();
        LoadCartItems();
    }
    public void LoadCartItems()
    {
        foreach (Transform child in cartContent)
        {
            Destroy(child.gameObject);
        }

        itemTotals.Clear();
        currentTotal = 0f;

        dbRef.Child($"REVIRA/Consumers/{userId}/cart/cartItems").GetValueAsync().ContinueWithOnMainThread(cartTask =>
        {
            if (!cartTask.IsCompleted || !cartTask.Result.Exists)
            {
                Debug.LogWarning("No cart items found.");
                UpdateTotalUI();
                return;
            }

            foreach (DataSnapshot itemSnapshot in cartTask.Result.Children)
            {
                string productId = itemSnapshot.Key;
                string selectedColor = itemSnapshot.Child("color").Value.ToString();
                string selectedSize = "";
                int quantity = 1;

                foreach (DataSnapshot sizeEntry in itemSnapshot.Child("sizes").Children)
                {
                    selectedSize = sizeEntry.Key;
                    quantity = int.Parse(sizeEntry.Value.ToString());
                    break;
                }

                float basePrice = float.Parse(itemSnapshot.Child("price").Value.ToString());
                string productName = itemSnapshot.Child("productName").Value.ToString();

                // Now get image, discount, and stock
                dbRef.Child($"REVIRA/stores/{storeId}/products/{productId}").GetValueAsync().ContinueWithOnMainThread(productTask =>
                {
                    if (!productTask.IsCompleted || !productTask.Result.Exists)
                    {
                        Debug.LogWarning($"Product {productId} not found in store.");
                        return;
                    }

                    DataSnapshot productData = productTask.Result;
                    string imageUrl = productData.Child("image").Value.ToString();

                    float discount = 0f;
                    if (productData.HasChild("discount") && productData.Child("discount").Child("exists").Value.ToString().ToLower() == "true")
                    {
                        discount = float.Parse(productData.Child("discount").Child("percentage").Value.ToString());
                    }

                    // Load stock structure
                    Dictionary<string, Dictionary<string, int>> stockData = new();
                    foreach (var colorEntry in productData.Child("colors").Children)
                    {
                        string colorName = colorEntry.Key;
                        Dictionary<string, int> sizeData = new();
                        foreach (var sizeEntry in colorEntry.Child("sizes").Children)
                        {
                            sizeData[sizeEntry.Key] = int.Parse(sizeEntry.Value.ToString());
                        }
                        stockData[colorName] = sizeData;
                    }

                    GameObject itemObj = Instantiate(cartItemPrefab, cartContent);

                    Debug.Log("Cart item instantiated: " + itemObj.name);
                    Debug.Log("parent is " + itemObj.transform.parent.name);
                    Debug.Log("active in hierarchy : " + itemObj.activeInHierarchy);
                    CartItemUI itemUI = itemObj.GetComponent<CartItemUI>();
                    itemUI.SetManager(this);

                    itemUI.Initialize(
                        userId,
                        productId,
                        productName,
                        basePrice,
                        discount,
                        selectedColor,
                        selectedSize,
                        quantity,
                        stockData,
                        imageUrl 
                        );

                    float finalPrice = discount > 0 ? basePrice - (basePrice * discount / 100f) : basePrice;
                    float itemTotal = finalPrice * quantity;

                    itemTotals[productId] = itemTotal;
                    UpdateTotalUI();
                });
            }
        });
    }
    private void LoadCartTotal()
    {
        dbRef.Child($"REVIRA/Consumers/{userId}/cart/cartTotal").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                float totalPrice = float.Parse(task.Result.Child("totalPrice").Value.ToString());
                totalText.text = totalPrice.ToString("F1") ;
            }
            else
            {
                totalText.text = " 0 ";
            }
        });
    }

    public void UpdateItemTotal(string productId, float newTotal)
    {
        itemTotals[productId] = newTotal;
        UpdateTotalUI();
    }

    private void UpdateTotalUI()
    {
        float total = 0f;
        foreach (var item in itemTotals)
        {
            total += item.Value;
        }

        currentTotal = total;
        totalText.text = $" {currentTotal:F1} ";
    }

    public void RestoreStock(string productId, string color, string size, int qty)
    {
        string path = $"REVIRA/stores/{storeId}/products/{productId}/colors/{color}/sizes/{size}";
        dbRef.Child(path).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                int currentStock = int.Parse(task.Result.Value.ToString());
                dbRef.Child(path).SetValueAsync(currentStock + qty);
            }
        });
    }
}









