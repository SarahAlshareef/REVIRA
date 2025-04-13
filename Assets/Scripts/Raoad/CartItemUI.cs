using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

public class CartItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image productImage;
    public TMP_Text productNameText;

    [Header("Price Containers")]
    public GameObject originalPriceImage;
    public GameObject discountedPriceImage;
    public TMP_Text originalPriceText;
    public TMP_Text discountedPriceText;
    public Image strikeThroughImage; // NEW

    [Header("Dropdowns")]
    public TMP_Dropdown colorDropdown;
    public TMP_Dropdown sizeDropdown;
    public TMP_Dropdown quantityDropdown;

    [Header("Buttons")]
    public Button deleteButton;

    private string userId, productId, storeId = "storeID_123";
    private DatabaseReference dbRef;
    private float basePrice, discountPercentage;
    private Dictionary<string, Dictionary<string, int>> stockData;
    private CartManager cartManager;

    public void SetManager(CartManager manager)
    {
        cartManager = manager;
    }

    public void Initialize(
        string _userId,
        string _productId,
        string name,
        float price,
        float discount,
        string selectedColor,
        string selectedSize,
        int selectedQty,
        Dictionary<string, Dictionary<string, int>> _stockData,
        string imageUrl)
    {
        userId = _userId;
        productId = _productId;
        basePrice = price;
        discountPercentage = discount;
        stockData = _stockData;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        productNameText.text = name;
        DisplayPrice();
        StartCoroutine(LoadImage(imageUrl));

        PopulateColorDropdown(selectedColor);
        PopulateSizeDropdown(selectedColor, selectedSize);
        PopulateQuantityDropdown(selectedColor, selectedSize, selectedQty);

        colorDropdown.onValueChanged.AddListener(_ => OnColorChanged());
        sizeDropdown.onValueChanged.AddListener(_ => OnSizeChanged());
        quantityDropdown.onValueChanged.AddListener(_ => OnQuantityChanged());

        deleteButton.onClick.AddListener(DeleteItemFromCart);
    }

    private void DisplayPrice()
    {
        if (discountPercentage > 0)
        {
            float discounted = basePrice - (basePrice * discountPercentage / 100f);

            originalPriceText.text = $"{basePrice:F2}";
            discountedPriceText.text = $"{discounted:F2}";
            originalPriceText.fontStyle = FontStyles.Strikethrough;

            originalPriceImage.SetActive(true);
            discountedPriceImage.SetActive(true);
            if (strikeThroughImage != null)
                strikeThroughImage.enabled = true;
        }
        else
        {
            originalPriceText.text = $"{basePrice:F2}";
            originalPriceText.fontStyle = FontStyles.Normal;

            originalPriceImage.SetActive(true);
            discountedPriceImage.SetActive(false);
            if (strikeThroughImage != null)
                strikeThroughImage.enabled = false;
        }
    }

    void PopulateColorDropdown(string selected)
    {
        colorDropdown.ClearOptions();
        List<string> options = new(stockData.Keys);
        colorDropdown.AddOptions(options);
        int index = options.IndexOf(selected);
        colorDropdown.value = index >= 0 ? index : 0;
        colorDropdown.RefreshShownValue();
    }

    void PopulateSizeDropdown(string color, string selected)
    {
        sizeDropdown.ClearOptions();
        if (!stockData.ContainsKey(color)) return;
        List<string> sizes = new(stockData[color].Keys);
        sizeDropdown.AddOptions(sizes);
        int index = sizes.IndexOf(selected);
        sizeDropdown.value = index >= 0 ? index : 0;
        sizeDropdown.RefreshShownValue();
    }

    void PopulateQuantityDropdown(string color, string size, int selectedQty)
    {
        quantityDropdown.ClearOptions();
        if (!stockData.ContainsKey(color) || !stockData[color].ContainsKey(size)) return;
        int maxQty = Mathf.Min(5, stockData[color][size]);
        List<string> quantities = new();
        for (int i = 1; i <= maxQty; i++) quantities.Add(i.ToString());
        quantityDropdown.AddOptions(quantities);
        int index = quantities.IndexOf(selectedQty.ToString());
        quantityDropdown.value = index >= 0 ? index : 0;
        quantityDropdown.RefreshShownValue();
    }

    void OnColorChanged()
    {
        string color = GetSelectedColor();
        string size = stockData[color].Keys.Count > 0 ? new List<string>(stockData[color].Keys)[0] : "";
        PopulateSizeDropdown(color, size);
        OnFirebaseUpdate();
        UpdateCartManagerTotal();
    }

    void OnSizeChanged()
    {
        string color = GetSelectedColor();
        string size = GetSelectedSize();
        PopulateQuantityDropdown(color, size, 1);
        OnFirebaseUpdate();
        UpdateCartManagerTotal();
    }

    void OnQuantityChanged()
    {
        OnFirebaseUpdate();
        UpdateCartManagerTotal();
    }

    void UpdateCartManagerTotal()
    {
        int qty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        float finalPrice = discountPercentage > 0 ? basePrice - (basePrice * discountPercentage / 100f) : basePrice;
        float newTotal = finalPrice * qty;
        cartManager?.UpdateItemTotal(productId, newTotal);
    }

    void OnFirebaseUpdate()
    {
        string color = GetSelectedColor();
        string size = GetSelectedSize();
        int qty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Dictionary<string, object> updateData = new()
        {
            { "color", color },
            { "sizes/" + size, qty },
            { "price", basePrice },
            { "productID", productId },
            { "productName", productNameText.text },
            { "timestamp", timestamp },
            { "expiresAt", timestamp + 86400 }
        };

        string path = $"REVIRA/Consumers/{userId}/cart/cartItems/{productId}";
        dbRef.Child(path).UpdateChildrenAsync(updateData).ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted)
                Debug.LogError("Failed to update cart item: " + task.Exception);
        });
    }

    void DeleteItemFromCart()
    {
        string color = GetSelectedColor();
        string size = GetSelectedSize();
        int qty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);

        cartManager?.RestoreStock(productId, color, size, qty);

        string path = $"REVIRA/Consumers/{userId}/cart/cartItems/{productId}";
        dbRef.Child(path).RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
                Destroy(gameObject);
            else
                Debug.LogError("Failed to delete cart item: " + task.Exception);
        });
    }

    IEnumerator LoadImage(string url)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(request);
            productImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogError("Image load failed: " + request.error);
        }
    }

    string GetSelectedColor() => colorDropdown.options[colorDropdown.value].text;
    string GetSelectedSize() => sizeDropdown.options[sizeDropdown.value].text;
}
