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

    [Header("Optional Visuals")]
    public GameObject lineImage;
    public GameObject redRiyalImage;

    [Header("Fallback Image")]
    public Sprite placeholderImage;

    private string userId, productId, storeId = "storeID_123";
    private float basePrice, discountPercentage;
    private int quantity;

    private DatabaseReference dbRef;
    private CartManager cartManager;
    private Dictionary<string, Dictionary<string, int>> stockData;

    private float lastKnownItemTotal = 0f;
    private int previousQty = 0;
    private bool initialized = false;

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
        StartCoroutine(LoadImageWithRetry(imageUrl));

        PopulateColorDropdown(selectedColor);
        PopulateSizeDropdown(selectedColor, selectedSize);
        PopulateQuantityDropdown(selectedColor, selectedSize, selectedQty);

        quantity = selectedQty;
        previousQty = selectedQty;

        float unitPrice = discountPercentage > 0
            ? basePrice - (basePrice * discountPercentage / 100f)
            : basePrice;

        lastKnownItemTotal = unitPrice * selectedQty;

        colorDropdown.onValueChanged.AddListener(_ => OnColorChanged());
        sizeDropdown.onValueChanged.AddListener(_ => OnSizeChanged());
        quantityDropdown.onValueChanged.AddListener(_ => OnQuantityChanged());
        removeButton.onClick.AddListener(DeleteItemFromCart);

        DisplayPrice();

        if (cartManager != null)
            cartManager.UpdateItemTotal(productId, lastKnownItemTotal, quantity);

        initialized = true;
    }

    private void DisplayPrice()
    {
        int currentQty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        float currentUnitPrice = basePrice;
        float finalPrice = currentUnitPrice * currentQty;

        if (discountPercentage > 0)
        {
            currentUnitPrice = basePrice - (basePrice * discountPercentage / 100f);
            finalPrice = currentUnitPrice * currentQty;

            originalPriceText.text = (basePrice * currentQty).ToString("F1");
            discountedPriceText.text = finalPrice.ToString("F1");

            originalPriceText.gameObject.SetActive(true);
            discountedPriceText.gameObject.SetActive(true);
            lineImage?.SetActive(true);
            redRiyalImage?.SetActive(true);
        }
        else
        {
            originalPriceText.text = finalPrice.ToString("F1");

            originalPriceText.gameObject.SetActive(true);
            discountedPriceText.gameObject.SetActive(false);
            lineImage?.SetActive(false);
            redRiyalImage?.SetActive(false);
        }
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

        int maxQty = Mathf.Min(stockData[color][size], 5);
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
        if (!stockData.ContainsKey(color) || stockData[color].Count == 0) return;

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
        if (!initialized) return;

        int newQuantity = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        if (newQuantity == previousQty) return;

        int quantityDifference = newQuantity - previousQty;

        float unitPrice = discountPercentage > 0
            ? basePrice - (basePrice * discountPercentage / 100f)
            : basePrice;

        float newItemTotal = unitPrice * newQuantity;
        float priceDifference = newItemTotal - lastKnownItemTotal;

        previousQty = newQuantity;
        lastKnownItemTotal = newItemTotal;

        cartManager?.UpdateItemTotal(productId, priceDifference, quantityDifference);
        UpdateFirebaseSizeAndTotal();
        DisplayPrice();
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
            .UpdateChildrenAsync(updateData);

        DisplayPrice();
    }

    private void UpdateFirebaseSizeAndTotal()
    {
        string color = GetSelectedColor();
        string size = GetSelectedSize();
        int qty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Dictionary<string, object> updateData = new()
        {
            { "sizes/" + size, qty },
            { "timestamp", timestamp },
            { "expiresAt", timestamp + 86400 }
        };

        dbRef.Child($"REVIRA/Consumers/{userId}/cart/cartItems/{productId}")
            .UpdateChildrenAsync(updateData);
    }

    private void DeleteItemFromCart()
    {
        string color = GetSelectedColor();
        string size = GetSelectedSize();
        int qty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);

        DatabaseReference cartRef = dbRef.Child($"REVIRA/Consumers/{userId}/cart");

        cartRef.Child("cartItems").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.Result.Exists)
            {
                Debug.LogWarning("Failed to retrieve cart items.");
                return;
            }

            int itemCount = 0;
            foreach (var _ in task.Result.Children) itemCount++;

            if (itemCount <= 1)
            {
                cartRef.RemoveValueAsync().ContinueWithOnMainThread(removeTask =>
                {
                    if (removeTask.IsCompleted)
                    {
                        cartManager?.RestoreStock(productId, color, size, qty);
                        cartManager?.UpdateItemTotal(productId, -lastKnownItemTotal, -qty);
                        Destroy(gameObject);
                    }
                });
            }
            else
            {
                cartRef.Child("cartItems").Child(productId).RemoveValueAsync().ContinueWithOnMainThread(removeTask =>
                {
                    if (removeTask.IsCompleted)
                    {
                        cartManager?.RestoreStock(productId, color, size, qty);
                        cartManager?.UpdateItemTotal(productId, -lastKnownItemTotal, -qty);
                        Destroy(gameObject);
                    }
                });
            }
        });
    }

    private IEnumerator LoadImageWithRetry(string url, int retries = 2, float delay = 0.5f)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[CartItemUI] Image URL is empty.");
            productImage.sprite = placeholderImage;
            yield break;
        }

        for (int attempt = 0; attempt <= retries; attempt++)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    productImage.sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    yield break;
                }
                else
                {
                    Debug.LogWarning($"[CartItemUI] Attempt {attempt + 1} failed to load image: {url} - {request.error}");

                    if (attempt == retries)
                    {
                        Debug.LogError("[CartItemUI] Failed to load image. Showing placeholder.");
                        productImage.sprite = placeholderImage;
                    }
                    else
                    {
                        yield return new WaitForSeconds(delay * (attempt + 1));
                    }
                }
            }
        }
    }

    private string GetSelectedColor() => colorDropdown.options[colorDropdown.value].text;
    private string GetSelectedSize() => sizeDropdown.options[sizeDropdown.value].text;
}
