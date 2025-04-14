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
    public TMP_Text discountedPriceText;
    public TMP_Text originalPriceText;

    public TMP_Dropdown colorDropdown;
    public TMP_Dropdown sizeDropdown;
    public TMP_Dropdown quantityDropdown;
    public Button removeButton;

    private string userId, productId, storeId = "storeID_123";
    private float basePrice, discountPercentage;
    private DatabaseReference dbRef;
    private CartManager cartManager;
    private Dictionary<string, Dictionary<string, int>> stockData;

    public void SetManager(CartManager manager)
    {
        cartManager = manager;
    }

    public void Initialize(
     string userId,
     string productId,
     string productName,
     float basePrice,
     float discount,
     string selectedColor,
     string selectedSize,
     int quantity,
     Dictionary<string, Dictionary<string, int>> stockData,
     string imageUrl)
    {
        this.userId = userId;
        this.productId = productId;
        this.basePrice = basePrice;
        this.discountPercentage = discount;
        this.stockData = stockData;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        productNameText.text = productName;
        DisplayPrice();
        StartCoroutine(LoadImage(imageUrl));

        PopulateColorDropdown(selectedColor);
        PopulateSizeDropdown(selectedColor, selectedSize);
        PopulateQuantityDropdown(selectedColor, selectedSize, quantity);

        colorDropdown.onValueChanged.AddListener(_ => OnColorChanged());
        sizeDropdown.onValueChanged.AddListener(_ => OnSizeChanged());
        quantityDropdown.onValueChanged.AddListener(_ => OnQuantityChanged());
        removeButton.onClick.AddListener(DeleteItemFromCart);
    }
    private void DisplayPrice()
    {
        float finalPrice = basePrice;
        if (discountPercentage > 0)
        {
            finalPrice = basePrice - (basePrice * discountPercentage / 100f);
            originalPriceText.text = basePrice.ToString("F2") + " SAR";
            originalPriceText.gameObject.SetActive(true);
        }
        else
        {
            originalPriceText.gameObject.SetActive(false);
        }

        discountedPriceText.text = finalPrice.ToString("F2") + " SAR";
    }

    private void PopulateColorDropdown(string selected)
    {
        colorDropdown.ClearOptions();
        var options = new List<string>(stockData.Keys);
        colorDropdown.AddOptions(options);

        int index = options.IndexOf(selected);
        colorDropdown.value = index >= 0 ? index : 0;
        colorDropdown.RefreshShownValue();
    }

    private void PopulateSizeDropdown(string color, string selected)
    {
        sizeDropdown.ClearOptions();
        if (!stockData.ContainsKey(color)) return;

        var sizes = new List<string>(stockData[color].Keys);
        sizeDropdown.AddOptions(sizes);

        int index = sizes.IndexOf(selected);
        sizeDropdown.value = index >= 0 ? index : 0;
        sizeDropdown.RefreshShownValue();
    }

    private void PopulateQuantityDropdown(string color, string size, int selectedQty)
    {
        quantityDropdown.ClearOptions();
        if (!stockData.ContainsKey(color) || !stockData[color].ContainsKey(size)) return;

        int maxQty = Mathf.Min(stockData[color][size], 10);
        List<string> quantities = new();
        for (int i = 1; i <= maxQty; i++)
            quantities.Add(i.ToString());

        quantityDropdown.AddOptions(quantities);
        int index = quantities.IndexOf(selectedQty.ToString());
        quantityDropdown.value = index >= 0 ? index : 0;
        quantityDropdown.RefreshShownValue();
    }

    private void OnColorChanged()
    {
        string color = GetSelectedColor();
        string size = new List<string>(stockData[color].Keys)[0];

        PopulateSizeDropdown(color, size);
        PopulateQuantityDropdown(color, size, 1);

        SaveChangesToFirebase();
    }

    private void OnSizeChanged()
    {
        string color = GetSelectedColor();
        string size = GetSelectedSize();

        PopulateQuantityDropdown(color, size, 1);
        SaveChangesToFirebase();
    }

    private void OnQuantityChanged()
    {
        SaveChangesToFirebase();
    }

    private void SaveChangesToFirebase()
    {
        string color = GetSelectedColor();
        string size = GetSelectedSize();
        int qty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Dictionary<string, object> updateData = new()
        {
            { "color", color },
            { "sizes/" + size, qty },
            { "timestamp", timestamp },
            { "expiresAt", timestamp + 86400 }
        };

        dbRef.Child($"REVIRA/Consumers/{userId}/cart/cartItems/{productId}")
            .UpdateChildrenAsync(updateData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    float finalPrice = discountPercentage > 0 ? basePrice - (basePrice * discountPercentage / 100f) : basePrice;
                    cartManager?.UpdateItemTotal(productId, finalPrice * qty);
                }
            });
    }

    private void DeleteItemFromCart()
    {
        string color = GetSelectedColor();
        string size = GetSelectedSize();
        int qty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);

        dbRef.Child($"REVIRA/Consumers/{userId}/cart/cartItems/{productId}")
            .RemoveValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    cartManager?.RestoreStock(productId, color, size, qty);
                    Destroy(gameObject);
                }
            });
    }

    private IEnumerator LoadImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
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

    private string GetSelectedColor() => colorDropdown.options[colorDropdown.value].text;
    private string GetSelectedSize() => sizeDropdown.options[sizeDropdown.value].text;
}



