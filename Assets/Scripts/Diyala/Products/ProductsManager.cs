using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.UI;
using TMPro;

public class ProductsManager : MonoBehaviour
{

    private DatabaseReference dbReference;
    private ProductData product;

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

        // Fetch product data from Firebase
        dbReference.Child("stores").Child(storeID).Child("products").Child(productID)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists)
                    {
                        Debug.Log("Firebase Data Loaded: " + snapshot.GetRawJsonValue());

                        product = new ProductData();

                        // Retrieve basic product details
                        product.name = snapshot.Child("name").Value.ToString();
                        product.price = float.Parse(snapshot.Child("price").Value.ToString());
                        product.color = snapshot.Child("color").Value.ToString();
                        product.discount = new DiscountData
                        {
                            exists = bool.Parse(snapshot.Child("discount").Child("exists").Value.ToString()),
                            percentage = float.Parse(snapshot.Child("discount").Child("percentage").Value.ToString())
                        };

                        // Handle product sizes if available
                        if (snapshot.HasChild("sizes"))
                        {
                            if (snapshot.Child("sizes").Value is string)
                            {
                                // Product has a single size (e.g., "One Size" or "Standard")
                                product.singleSize = snapshot.Child("sizes").Value.ToString();
                                product.sizes = null; // No multiple sizes available
                                Debug.Log("Single Size Found: " + product.singleSize);
                            }
                            else
                            {
                                // Product has multiple sizes
                                product.sizes = new Dictionary<string, int>();
                                foreach (var size in snapshot.Child("sizes").Children)
                                {
                                    product.sizes.Add(size.Key, int.Parse(size.Value.ToString()));
                                }
                                Debug.Log("Multiple Sizes Found: " + string.Join(", ", product.sizes.Keys));
                            }
                        }
                        else
                        {
                            product.sizes = null;
                            product.singleSize = null;
                            Debug.LogWarning("No sizes found in Firebase for this product!");
                        }

                        // Update UI elements with retrieved data
                        if (productNameText != null)
                            productNameText.text = product.name;

                        if (productPriceText != null)
                            productPriceText.text = $"{product.price:F2} SAR";

                        if (discountText != null)
                            discountText.text = product.discount.exists
                                ? $"Discount: {product.discount.percentage}%"
                                : "No Discount";

                        // Update color dropdown
                        if (colorDropdown != null)
                        {
                            colorDropdown.ClearOptions();
                            colorDropdown.AddOptions(new List<string> { product.color });
                            Debug.Log("Updated Color: " + product.color);
                        }

                        // Update size dropdown
                        sizeDropdown.ClearOptions();
                        if (product.sizes != null && product.sizes.Count > 0)
                        {
                            List<string> sizes = new List<string>(product.sizes.Keys);
                            sizeDropdown.AddOptions(sizes);
                            Debug.Log("Sizes Loaded: " + string.Join(", ", sizes));
                        }
                        else if (!string.IsNullOrEmpty(product.singleSize))
                        {
                            sizeDropdown.AddOptions(new List<string> { product.singleSize });
                            Debug.Log("Single Size Set: " + product.singleSize);
                        }
                        else
                        {
                            Debug.LogWarning("No sizes found for this product!");
                        }

                        // Refresh the dropdown to reflect changes
                        sizeDropdown.RefreshShownValue();
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
        if (colorDropdown == null || sizeDropdown == null) return;

        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        UpdateSizeDropdown(selectedColor);
    }

    void UpdateSizeDropdown(string color)
    {
        if (sizeDropdown == null) return;

        sizeDropdown.ClearOptions();

        if (product != null && product.sizes != null && product.sizes.Count > 0)
        {
            List<string> sizes = new List<string>(product.sizes.Keys);
            sizeDropdown.AddOptions(sizes);
            Debug.Log("Sizes Updated: " + string.Join(", ", sizes));
        }
        else if (product != null && !string.IsNullOrEmpty(product.singleSize))
        {
            sizeDropdown.AddOptions(new List<string> { product.singleSize });
            Debug.Log("Single Size Set in Dropdown: " + product.singleSize);
        }
        else
        {
            Debug.LogWarning("No sizes available for this product!");
        }

        sizeDropdown.RefreshShownValue();
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
    public string singleSize;
    public int quantity;
}

[System.Serializable]
public class DiscountData
{
    public bool exists;
    public float percentage;
}