using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using UnityEngine.SceneManagement;




public class CartManagerTwo : MonoBehaviour
{

    [Header("UI References")]
    public GameObject cartPanel;
    public Transform cartContent;
    public GameObject cartItemPrefab;
    public TextMeshProUGUI totalText;

    public GameObject promotionalCanvas;
    public Button checkoutButton;
    public Button cartToggleButton;
    public Button closeCartButton;

    private string userId;
    private const string storeId = "storeID_123";
    private DatabaseReference dbRef;

    private Dictionary<string, float> itemTotals = new();
    private float currentTotal = 0f;

    private float totalPrice = 0;
    private int totalItems = 0;

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

        if (cartToggleButton != null)
        {
            cartToggleButton.onClick.RemoveAllListeners();
            cartToggleButton.onClick.AddListener(() =>
            {
                if (cartPanel != null)
                {
                    cartPanel.SetActive(true);
                    FindObjectOfType<MenuManagerVR>().HandleUIOpened(cartPanel);
                }
            });
        }
        if (closeCartButton != null)
        {
            closeCartButton.onClick.RemoveAllListeners();
            closeCartButton.onClick.AddListener(() =>
            {
                if (cartPanel != null)
                {

                    cartPanel.SetActive(false);
                    FindObjectOfType<MenuManagerVR>().HandleUIClosed();
                }
            });
        }


    }
    public void OnEnable()
    {
        if (checkoutButton != null)
        {
            checkoutButton.onClick.RemoveAllListeners();
            checkoutButton.onClick.AddListener(() =>
            {
                if (promotionalCanvas != null)
                {
                    promotionalCanvas.SetActive(true);
                    FindAnyObjectByType<MenuManagerVR>().HandleUIOpened(promotionalCanvas);
                }
                else
                {
                    Debug.LogWarning("Promotional Canvas is not assigned!");

                }
            });
        }
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
                string selectedColor = itemSnapshot.Child("color")?.Value?.ToString() ?? "";
                string selectedSize = "";
                int quantity = 1;

                foreach (DataSnapshot sizeEntry in itemSnapshot.Child("sizes").Children)
                {
                    selectedSize = sizeEntry.Key;
                    int.TryParse(sizeEntry.Value?.ToString(), out quantity);
                    break;
                }

                float basePrice = float.TryParse(itemSnapshot.Child("price")?.Value?.ToString(), out float parsedPrice) ? parsedPrice : 0;
                string productName = itemSnapshot.Child("productName")?.Value?.ToString() ?? "Unnamed";

                dbRef.Child($"REVIRA/stores/{storeId}/products/{productId}").GetValueAsync().ContinueWithOnMainThread(productTask =>
                {
                    if (!productTask.IsCompleted || !productTask.Result.Exists)
                    {
                        Debug.LogWarning($"Product {productId} not found in store.");
                        return;
                    }

                    DataSnapshot productData = productTask.Result;
                    string imageUrl = productData.Child("image")?.Value?.ToString() ?? "";

                    float discount = 0f;
                    if (productData.HasChild("discount") &&
                        productData.Child("discount").Child("exists").Value?.ToString().ToLower() == "true")
                    {
                        float.TryParse(productData.Child("discount").Child("percentage").Value?.ToString(), out discount);
                    }

                    Dictionary<string, Dictionary<string, int>> stockData = new();
                    foreach (var colorEntry in productData.Child("colors").Children)
                    {
                        string colorName = colorEntry.Key;
                        Dictionary<string, int> sizeData = new();
                        foreach (var sizeEntry in colorEntry.Child("sizes").Children)
                        {
                            int.TryParse(sizeEntry.Value?.ToString(), out int stockQty);
                            sizeData[sizeEntry.Key] = stockQty;
                        }
                        stockData[colorName] = sizeData;
                    }

                    GameObject itemObj = Instantiate(cartItemPrefab, cartContent);
                    CartItemUI itemUI = itemObj.GetComponent<CartItemUI>();
                    //itemUI.SetManager(this);

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

                    float unitPrice = discount > 0 ? basePrice - (basePrice * discount / 100f) : basePrice;
                    float itemTotal = unitPrice * quantity;
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
                float.TryParse(task.Result.Child("totalPrice")?.Value?.ToString(), out totalPrice);
                int.TryParse(task.Result.Child("totalItems")?.Value?.ToString(), out totalItems);

                totalText.text = totalPrice.ToString("F1");
            }
            else
            {
                totalText.text = "0";
            }
        });
    }

    public void UpdateItemTotal(string productId, float changeInPrice, int quantityChange = 0)
    {
        if (!itemTotals.ContainsKey(productId))
            itemTotals[productId] = 0;

        itemTotals[productId] += changeInPrice;

        float newTotal = 0;
        foreach (var price in itemTotals.Values)
            newTotal += price;

        totalPrice = Mathf.Max(0, newTotal);
        totalItems = Mathf.Max(0, totalItems + quantityChange);

        Dictionary<string, object> cartTotalData = new()
        {
            { "totalPrice", totalPrice },
            { "totalItems", totalItems }
        };

        dbRef.Child($"REVIRA/Consumers/{userId}/cart/cartTotal").UpdateChildrenAsync(cartTotalData);

        UpdateTotalUI();

        if (totalPrice <= 0)
        {
            dbRef.Child($"REVIRA/Consumers/{userId}/cart").RemoveValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Cart deleted successfully because it became empty.");
                    itemTotals.Clear();
                    UpdateTotalUI();
                }
            });
        }
    }

    public void UpdateTotalUI()
    {
        float total = 0f;
        foreach (var pair in itemTotals)
            total += pair.Value;

        currentTotal = Mathf.Max(0, total);
        totalText.text = currentTotal > 0 ? currentTotal.ToString("F1") : "0";
    }

    public void RestoreStock(string productId, string color, string size, int qty)
    {
        string path = $"REVIRA/stores/{storeId}/products/{productId}/colors/{color}/sizes/{size}";
        dbRef.Child(path).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                int.TryParse(task.Result.Value?.ToString(), out int currentStock);
                dbRef.Child(path).SetValueAsync(currentStock + qty);
            }
        });
    }
    public void ToggleCartPanel()
    {
        bool isActive = cartPanel.activeSelf;
        cartPanel.SetActive(!isActive);
    }

    public void showCartUI()
    {
        if (cartPanel != null)
        {
            cartPanel.SetActive(true);
            FindObjectOfType<MenuManagerVR>().HandleUIOpened(cartPanel);
        }
    }

}



