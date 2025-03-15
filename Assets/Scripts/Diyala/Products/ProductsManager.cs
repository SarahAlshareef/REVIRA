// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
// Firebase
using Firebase.Database; 
using Firebase.Extensions;
// C#
using System.Collections;
using System.Collections.Generic;


public class ProductsManager : MonoBehaviour
{

    // Firebase
    private DatabaseReference dbReference;
    private ProductData product;
    public string productID, storeID;

    // UI Elements
    public GameObject productPopup, discountTag;
    public TMP_Text productName, productPrice, discountedPrice, productDescription;
    public Image productImage;
    public TMP_Dropdown colorDropdown, sizeDropdown, quantityDropdown;
    public Button closePopup, openPopup;

    // Product Data
    private Dictionary<string, Dictionary<string, int>> productColorsAndSizes; 

    // Getter & Setter
    public ProductData GetProductData()
    {
        return product;
    }
    public void SetProductData(ProductData newProduct)
    {
        product = newProduct;
    }

    public void Start()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == Firebase.DependencyStatus.Available)
            {
                Debug.Log("Firebase is ready to use.");
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                LoadProductData();
            }
        });

        // Close the pop-up by default
        if (productPopup != null)
        {
            productPopup.SetActive(false);
        }
        // Open the pop-up on click
        if (openPopup != null)
        {
            openPopup.onClick.AddListener(OpenProductPopup);
        }
        // Close the pop-up on click
        if (closePopup != null)
        {
            closePopup.onClick.AddListener(CloseProductPopup);
        }
        // Only show sizes after selecting a color
        if (colorDropdown != null)
        {
            colorDropdown.onValueChanged.AddListener((index) => UpdateSizeDropdown());
        }
        // Only show quantities after selecting a size
        if (sizeDropdown != null)
        {
            sizeDropdown.onValueChanged.AddListener((index) => UpdateQuantityDropdown());
        }
    }

    public void LoadProductData()
    {
        // Fetch product data from Firebase
        dbReference.Child("stores").Child(storeID).Child("products").Child(productID)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists)
                    {      
                        // fetch data from Firebase and save them in ProductData
                        product = new ProductData()
                        { 
                        name = snapshot.Child("name").Value.ToString(),
                        price = float.Parse(snapshot.Child("price").Value.ToString()),
                        description = snapshot.Child("description").Value.ToString(),
                        image = snapshot.Child("image").Value.ToString(),

                            // fetch data from Firebase and save them in DiscountData
                            discount = new DiscountData
                            {
                            exists = bool.Parse(snapshot.Child("discount").Child("exists").Value.ToString()),
                            percentage = float.Parse(snapshot.Child("discount").Child("percentage").Value.ToString())
                            }
                        };

                        productColorsAndSizes = new Dictionary<string, Dictionary<string, int>>();

                        // Load colors and sizes
                        if (snapshot.HasChild("colors"))
                        {
                            product.sizes = new Dictionary<string, int>();
                            foreach (var colorNode in snapshot.Child("colors").Children)
                            {
                                string colorName = colorNode.Key;
                                Dictionary<string, int> sizes = new Dictionary<string, int>();

                                foreach (var sizeNode in colorNode.Child("sizes").Children)
                                {
                                    sizes.Add(sizeNode.Key, int.Parse(sizeNode.Value.ToString()));
                                }
                                productColorsAndSizes[colorName] = sizes;
                            }
                        }

                        // Product name
                        if (productName != null)
                            productName.text = product.name;

                        // Product price
                        if (productPrice != null)
                            productPrice.text = $"{product.price:F2}";

                        // Product description
                        if (productDescription != null)
                            productDescription.text = product.description;

                        // Product image
                        StartCoroutine(LoadImageFromURL(product.image));

                        // Check if the product has a discount
                        UpdatePriceAndDiscount();

                        // Color drop-down
                        if (colorDropdown != null)
                        {
                            colorDropdown.ClearOptions();
                            List<string> colorOption = new List<string> { "Select Color", product.color };
                            colorDropdown.AddOptions(colorOption);
                            colorDropdown.value = 0;
                            colorDropdown.RefreshShownValue();
                        }

                        // Size drop-down
                        sizeDropdown.ClearOptions();
                        List<string> sizeOptions = new List<string> { "Select Size" };

                        if (product.sizes != null && product.sizes.Count > 0)
                        {
                            sizeOptions.AddRange(product.sizes.Keys);
                        }
                        else if (!string.IsNullOrEmpty(product.singleSize))
                        {
                            sizeOptions.Add(product.singleSize);
                        }
                        sizeDropdown.AddOptions(sizeOptions);
                        sizeDropdown.value = 0;
                        sizeDropdown.RefreshShownValue();

                        UpdateColorDropdown();  
                    }
                    else
                    {
                        Debug.LogWarning("Product not found in Firebase!");
                    }
                }
                else
                {
                    Debug.LogError("Error loading product data: " + task.Exception);
                }
            });
    }
    IEnumerator LoadImageFromURL(string url)
    {
        if (productImage == null)
        {
            Debug.LogError("Product Image is not assigned in the Inspector.");
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                productImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                Debug.LogError("Failed to load image: " + request.error);
            }
        }
    }

    void UpdateColorDropdown()
    {
        if (productColorsAndSizes == null) return;

        List<string> colors = new List<string> { "Select Color" };
        colors.AddRange(productColorsAndSizes.Keys);

        colorDropdown.ClearOptions();
        colorDropdown.AddOptions(colors);
        colorDropdown.value = 0;
        colorDropdown.RefreshShownValue();
    }

    void UpdateSizeDropdown()
    {
        if (sizeDropdown == null) return;

        List<string> sizes = new List<string> { "Select Size" };

        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        if (selectedColor != null && productColorsAndSizes.ContainsKey(selectedColor))
        {
            sizes.AddRange(productColorsAndSizes[selectedColor].Keys);
        }

        sizeDropdown.ClearOptions();
        sizeDropdown.AddOptions(sizes);
        sizeDropdown.value = sizes.Count == 2 ? 1 : 0;
        sizeDropdown.RefreshShownValue();
    }

    public void UpdateQuantityDropdown()
    {
        if (quantityDropdown == null || sizeDropdown == null || colorDropdown == null)
            return;

        List<string> quantities = new List<string> { "Select Quantity" };

        // Get selected color and size
        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        string selectedSize = sizeDropdown.options[ sizeDropdown.value ].text;

        if (selectedColor != "Select Color" && selectedSize != "Select Size" && productColorsAndSizes.ContainsKey(selectedColor) 
            && productColorsAndSizes[selectedColor].ContainsKey(selectedSize))
        {
            // Get available stock
            int availableStock = productColorsAndSizes[selectedColor][selectedSize];

            // Set max selectable quantity (min of 5 or available stock)
            int maxSelectable = Mathf.Min(5, availableStock);

            for (int i = 1; i <= maxSelectable; i++)
            {
                quantities.Add(i.ToString());
            }
        }
        quantityDropdown.ClearOptions();
        quantityDropdown.AddOptions(quantities);
        quantityDropdown.value = 0;
        quantityDropdown.RefreshShownValue();
    }

    void UpdatePriceAndDiscount()
    {
        if (product.discount.exists && product.discount.percentage > 0)
        {
            // Calculate new discounted price
            float newPrice = product.price - (product.price * (product.discount.percentage / 100));

            // Display the original price with a strikethrough
            if (productPrice != null)
            {
                productPrice.text = $"{product.price:F2}";
                productPrice.fontStyle = FontStyles.Strikethrough;
            }

            // Display the discounted price
            if (discountedPrice != null)
            {
                discountedPrice.text = $"{newPrice:F2}";
                discountedPrice.gameObject.SetActive(true);
            }

            // Show the discount tag
            if (discountTag != null)
                discountTag.SetActive(true);
        }
        else
        {
            // No discount, show the original price 
            if (productPrice != null)
            {
                productPrice.text = $"{product.price:F2}";
                productPrice.fontStyle = FontStyles.Normal;
            }

            if (discountedPrice != null)
                discountedPrice.gameObject.SetActive(false);

            if (discountTag != null)
                discountTag.SetActive(false);
        }
    }
    public void OpenProductPopup()
    {
        if (productPopup != null)
            productPopup.SetActive(true);
    }
    public void CloseProductPopup()
    {
        if (productPopup != null)
            productPopup.SetActive(false);
    }
}

// Allows us to use the class's data in unity inspector
[System.Serializable]
public class ProductData
{
    public float price;
    public string name, color, image, sizeType, singleSize, description;
    public int quantity;
    public DiscountData discount;
    public Dictionary<string, int> sizes;
}

// Allows us to use the class's data in unity inspector
[System.Serializable]
public class DiscountData
{
    public bool exists;
    public float percentage;
}