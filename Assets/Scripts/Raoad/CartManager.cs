
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.Networking;

public class CartManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject scrollViewContent;
    public GameObject cartItemPanel;
    public TextMeshProUGUI totalText;
    public GameObject scrollView;

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

    private float totalPrice = 0f;

    void Start()
    {
        Debug.Log("start method is called");

        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        userID = UserManager.Instance.UserId;

        Debug.Log("Current User ID: " + userID);

        if (removeButton != null)
            removeButton.onClick.AddListener(OnRemoveItem);

        if (colorDropdown != null) colorDropdown.onValueChanged.AddListener(delegate { OnColorChanged(); });
        if (sizeDropdown != null) sizeDropdown.onValueChanged.AddListener(delegate { OnSizeChanged(); });
        if (quantityDropdown != null) quantityDropdown.onValueChanged.AddListener(delegate { OnQuantityChanged(); });

        LoadCartItems();
    }
     void Update()
    {
        Debug.Log("Update is running...");
    }

    public void LoadCartItems()
    {
        foreach (Transform child in scrollViewContent.transform)
        {
            if (child != cartItemPanel.transform)
                Destroy(child.gameObject);
        }

        cartItemPanel.SetActive(false);
        totalPrice = 0f;

        dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child("cartItems").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                Debug.Log("Cart data loaded successfully. Total items: " + task.Result.ChildrenCount);
                Debug.Log("Cart found ! item count: " + task.Result.ChildrenCount);
                scrollView.SetActive(true);

                foreach (DataSnapshot productSnapshot in task.Result.Children)
                {
                    string productID = productSnapshot.Key;
                    Debug.Log("Processing cart item: " + productID);

                    string selectedColor = productSnapshot.Child("color").Value.ToString();
                    float basePrice = float.Parse(productSnapshot.Child("price").Value.ToString());

                    string selectedSize = "";
                    int selectedQuantity = 1;
                    foreach (var sizeEntry in productSnapshot.Child("sizes").Children)
                    {
                        selectedSize = sizeEntry.Key;
                        selectedQuantity = int.Parse(sizeEntry.Value.ToString());
                        break;
                    }

                    dbReference.Child("REVIRA/stores").Child("storeID_123").Child("products").Child(productID).GetValueAsync().ContinueWith(productTask =>
                    {
                        if (productTask.IsCompleted && productTask.Result.Exists)
                        {
                            Debug.Log("Loaded product details from store for: " + productID);

                            string imageUrl = productTask.Result.Child("image").Value.ToString();
                            string productName = productTask.Result.Child("name").Value.ToString();

                            bool hasDiscount = productTask.Result.Child("discount").Child("exists").Value.ToString() == "true";
                            float discountPercentage = hasDiscount ? float.Parse(productTask.Result.Child("discount").Child("percentage").Value.ToString()) : 0f;

                            Dictionary<string, Dictionary<string, int>> allColorsAndSizes = new Dictionary<string, Dictionary<string, int>>();
                            foreach (var colorNode in productTask.Result.Child("colors").Children)
                            {
                                Dictionary<string, int> sizes = new Dictionary<string, int>();
                                foreach (var sizeNode in colorNode.Child("sizes").Children)
                                {
                                    sizes[sizeNode.Key] = int.Parse(sizeNode.Value.ToString());
                                }
                                allColorsAndSizes[colorNode.Key] = sizes;
                            }

                            float finalPrice = hasDiscount ? basePrice - (basePrice * discountPercentage / 100f) : basePrice;
                            totalPrice += finalPrice * selectedQuantity;

                            GameObject newItem = Instantiate(cartItemPanel, scrollViewContent.transform);
                            newItem.SetActive(true);

                            CartManager item = newItem.GetComponent<CartManager>();
                            item.Initialize(productID, productName, imageUrl, selectedColor, selectedSize, selectedQuantity, basePrice, hasDiscount, discountPercentage, allColorsAndSizes);
                        }
                        else
                        {
                            Debug.LogWarning("Product not found in store: " + productID);
                        }

                        UpdateTotalText();
                        CheckScrollVisibility();
                    });
                }
            }
            else
            {
                Debug.LogWarning("Cart is empty or failed to load.");
                scrollView.SetActive(false);
            }
        });
    }
    public void Initialize(
    string id,
    string name,
    string imageUrl,
    string color,
    string size,
    int quantity,
    float price,
    bool discountExists,
    float discount,
    Dictionary<string,
     Dictionary<string, 
    int>> colorsAndSizes)
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
    private IEnumerator<UnityWebRequestAsyncOperation> LoadImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            productImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }

    private void UpdatePriceText()
    {
        float finalPrice = hasDiscount ? basePrice - (basePrice * discountPercentage / 100f) : basePrice;
        priceText.text = finalPrice.ToString("F2");

        if (hasDiscount)
        {
            originalPriceText.text = basePrice.ToString("F2");
            originalPriceText.gameObject.SetActive(true);
        }
        else
        {
            originalPriceText.gameObject.SetActive(false);
        }
    }

    private void PopulateColorDropdown()
    {
        colorDropdown.ClearOptions();
        List<string> colors = new List<string>(allColorsAndSizes.Keys);
        colorDropdown.AddOptions(colors);
        colorDropdown.value = colors.IndexOf(selectedColor);
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
            quantities.Add(i.ToString());

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
            { "timestamp", GetCurrentTimestamp() },
            { "expiresAt", GetExpiryTimestamp() }
        };

        dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child("cartItems").Child(productID).UpdateChildrenAsync(updateData);
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
                dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child("cartItems").Child(productID).RemoveValueAsync();

                Destroy(gameObject);
                RefreshTotalUI();
                CheckScrollVisibility();
            }
        });
    }

    private long GetCurrentTimestamp()
    {
        return System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private long GetExpiryTimestamp()
    {
        return System.DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeMilliseconds();
    }

    public void UpdateTotalText()
    {
        totalText.text = "Total: " + totalPrice.ToString("F2") + " SAR";
    }

    public void RefreshTotalUI()
    {
        UpdateTotalText();
    }

    public void CheckScrollVisibility()
    {
        scrollView.SetActive(scrollViewContent.transform.childCount > 1);
    }
}


