using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.Networking;
using System.Collections;

public class CartItem : MonoBehaviour
{
    [Header("UI Elements")]
    public Image productImage;
    public TMP_Text productNameText;
    public TMP_Text priceText;
    public TMP_Text originalPriceText;
    public TMP_Dropdown colorDropdown;
    public TMP_Dropdown sizeDropdown;
    public TMP_Dropdown quantityDropdown;
    public Button removeButton;

    private string productID;
    private string storeID = "storeID_123";
    private string userID;

    private float basePrice;
    private float discountPercentage;
    private bool hasDiscount;

    private Dictionary<string, Dictionary<string, int>> allColorsAndSizes;
    private DatabaseReference dbReference;

    private string selectedColor;
    private string selectedSize;
    private int selectedQuantity;

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        userID = UserManager.Instance.UserId;

        colorDropdown.onValueChanged.AddListener(delegate { OnColorChanged(); });
        sizeDropdown.onValueChanged.AddListener(delegate { OnSizeChanged(); });
        quantityDropdown.onValueChanged.AddListener(delegate { OnQuantityChanged(); });
        removeButton.onClick.AddListener(OnRemoveItem);
    }

    public void SetUpItem(string id, string name, string imageUrl, string color, string size, int quantity, float price, bool discountExists, float discount, Dictionary<string, Dictionary<string, int>> colorsAndSizes)
    {
        productID = id;
        productNameText.text = name;
        basePrice = price;
        hasDiscount = discountExists;
        discountPercentage = discount;
        allColorsAndSizes = colorsAndSizes;

        selectedColor = color;
        selectedSize = size;
        selectedQuantity = quantity;

        StartCoroutine(LoadImage(imageUrl));
        UpdatePriceText();

        PopulateColorDropdown();
        PopulateSizeDropdown();
        PopulateQuantityDropdown();
    }

    private IEnumerator LoadImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            productImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogError("Failed to load product image: " + request.error);
        }
    }

    private void UpdatePriceText()
    {
        float finalPrice = hasDiscount ? basePrice - (basePrice * discountPercentage / 100f) : basePrice;

        if (hasDiscount)
        {
            originalPriceText.text = basePrice.ToString("F2");
            originalPriceText.gameObject.SetActive(true);
        }
        else
        {
            originalPriceText.gameObject.SetActive(false);
        }

        priceText.text = finalPrice.ToString("F2");
    }

    private void PopulateColorDropdown()
    {
        colorDropdown.ClearOptions();
        List<string> options = new List<string>(allColorsAndSizes.Keys);
        colorDropdown.AddOptions(options);
        colorDropdown.value = options.IndexOf(selectedColor);
        colorDropdown.RefreshShownValue();
    }

    private void PopulateSizeDropdown()
    {
        sizeDropdown.ClearOptions();
        List<string> sizes = new List<string>(allColorsAndSizes[selectedColor].Keys);
        sizeDropdown.AddOptions(sizes);
        sizeDropdown.value = sizes.IndexOf(selectedSize);
        sizeDropdown.RefreshShownValue();
    }

    private void PopulateQuantityDropdown()
    {
        quantityDropdown.ClearOptions();
        int stock = allColorsAndSizes[selectedColor][selectedSize];
        int max = Mathf.Min(stock, 10);
        List<string> quantities = new List<string>();
        for (int i = 1; i <= max; i++)
        {
            quantities.Add(i.ToString());
        }
        quantityDropdown.AddOptions(quantities);
        quantityDropdown.value = selectedQuantity - 1;
        quantityDropdown.RefreshShownValue();
    }

    private void OnColorChanged()
    {
        selectedColor = colorDropdown.options[colorDropdown.value].text;
        PopulateSizeDropdown();
        SaveToFirebase();
    }

    private void OnSizeChanged()
    {
        selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        PopulateQuantityDropdown();
        SaveToFirebase();
    }

    private void OnQuantityChanged()
    {
        selectedQuantity = quantityDropdown.value + 1;
        SaveToFirebase();
    }

    private void SaveToFirebase()
    {
        Dictionary<string, object> sizes = new Dictionary<string, object>
        {
            { selectedSize, selectedQuantity }
        };

        Dictionary<string, object> updateData = new Dictionary<string, object>
        {
            { "color", selectedColor },
            { "sizes", sizes },
            { "timestamp", CartUtilities.GetCurrentTimestamp() },
            { "expiresAt", CartUtilities.GetExpiryTimestamp() }
        };

        dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child(productID)
            .UpdateChildrenAsync(updateData);
    }

    private void OnRemoveItem()
    {
        string stockPath = $"REVIRA/stores/{storeID}/products/{productID}/colors/{selectedColor}/sizes/{selectedSize}";

        dbReference.Child(stockPath).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                int currentStock = int.Parse(task.Result.Value.ToString());
                int updatedStock = currentStock + selectedQuantity;

                dbReference.Child(stockPath).SetValueAsync(updatedStock);
                dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child(productID).RemoveValueAsync();

                Destroy(gameObject);
                CartUIManager.Instance.RefreshTotalUI();
                CartUIManager.Instance.CheckScrollVisibility();
            }
        });
    }
}
