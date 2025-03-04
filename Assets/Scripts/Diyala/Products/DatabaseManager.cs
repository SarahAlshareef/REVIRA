using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.UI;
using TMPro;

public class ProductUIManager : MonoBehaviour
{
    private DatabaseReference dbReference;

    // Product and store identifiers (set manually for each product)
    public string productID;
    public string storeID;

    // UI elements for the product
    public TMP_Text productNameText;
    public TMP_Dropdown colorDropdown;
    public TMP_Dropdown sizeDropdown;
    public TMP_Text productPriceText;
    public TMP_Text discountText;
    public GameObject productPopup; // Popup window for product details

    private Dictionary<string, Dictionary<string, int>> productColorsAndSizes; // Stores available colors and sizes

    public void Start()
    {
        Debug.Log("ProductUIManager script is running!"); // Debug to check if script is executing
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        LoadProductData();
    }

    public void LoadProductData()
    {
        Debug.Log("LoadProductData() is called! Product ID: " + productID + " Store ID: " + storeID);

        dbReference.Child("stores").Child(storeID).Child("products").Child(productID)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists)
                    {
                        string jsonData = snapshot.GetRawJsonValue();
                        Debug.Log("Product Data Loaded: " + jsonData);

                        ProductData product = JsonUtility.FromJson<ProductData>(jsonData);

                        if (product == null)
                        {
                            Debug.LogError("Failed to parse product data!");
                            return;
                        }

                        // Update UI elements
                        if (productNameText != null)
                        {
                            productNameText.text = product.name;
                            Debug.Log("Updated Product Name: " + product.name);
                        }
                        if (productPriceText != null)
                        {
                            productPriceText.text = $"{product.price:F2}";
                            Debug.Log("Updated Product Price: " + product.price);
                        }
                        if (discountText != null)
                        {
                            discountText.text = product.discount.exists
                                ? $"Discount: {product.discount.percentage}%"
                                : "No Discount";
                            Debug.Log("Updated Discount: " + discountText.text);
                        }

                        // Populate color dropdown (assuming one color per product)
                        if (colorDropdown != null)
                        {
                            colorDropdown.ClearOptions();
                            List<string> colors = new List<string> { product.color };
                            colorDropdown.AddOptions(colors);
                            Debug.Log("Updated Color: " + product.color);
                        }

                        // Populate size dropdown based on sizeType
                        sizeDropdown.ClearOptions();
                        if (product.sizes != null && product.sizes.Count > 0)
                        {
                            // If the product has multiple sizes
                            List<string> sizes = new List<string>(product.sizes.Keys);
                            sizeDropdown.AddOptions(sizes);
                            Debug.Log("Updated Sizes: " + string.Join(", ", sizes));
                        }
                        else if (product.sizeType == "Single")
                        {
                            // If the product has only one size (like "One Size" or "Standard")
                            sizeDropdown.AddOptions(new List<string> { "One Size" });
                            Debug.Log("Updated Size: One Size");
                        }
                        else
                        {
                            Debug.LogWarning("No sizes found for this product!");
                        }
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

    void UpdateColorDropdown()
    {
        if (productColorsAndSizes == null) return;

        colorDropdown.ClearOptions();
        List<string> colors = new List<string>(productColorsAndSizes.Keys);
        colorDropdown.AddOptions(colors);

        // Update sizes based on the first available color
        if (colors.Count > 0)
        {
            UpdateSizeDropdown(colors[0]);
        }

        // Add event listener to update sizes when color is changed
        colorDropdown.onValueChanged.AddListener(delegate { OnColorChanged(); });
    }

    void OnColorChanged()
    {
        if (colorDropdown != null || sizeDropdown == null) return;

        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        UpdateSizeDropdown(selectedColor);
    }

    void UpdateSizeDropdown(string color)
    {
        if (productColorsAndSizes == null) return;

        sizeDropdown.ClearOptions();
        if (productColorsAndSizes.ContainsKey(color))
        {
            List<string> sizes = new List<string>(productColorsAndSizes[color].Keys);
            sizeDropdown.AddOptions(sizes);
        }
    }
}

// Class for storing product data
[System.Serializable]
public class ProductData
{
    public string name;
    public float price;
    public string color;
    public string sizeType;
    public DiscountData discount;
    public Dictionary<string, int> sizes;
    public int quantity;
}

[System.Serializable]
public class DiscountData
{
    public bool exists;
    public float percentage;
}
