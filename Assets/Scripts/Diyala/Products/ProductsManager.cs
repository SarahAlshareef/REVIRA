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

    // Product and store identifiers
    public string productID;
    public string storeID;

    // Popup window
    public GameObject productPopup;

    // Product Data
    public TMP_Text productName;
    public TMP_Text productPrice;
    public TMP_Text productDescription;

    // Drop-down lists
    public TMP_Dropdown colorDropdown;
    public TMP_Dropdown sizeDropdown;
    public TMP_Dropdown quantityDropdown;

    // Discount Data
    public TMP_Text productDiscount;
    public TMP_Text discountedPrice;
    public GameObject discountTag;

    // Active buttons
    public Button closePopup;
    public Button openPopup;

    private Dictionary<string, Dictionary<string, int>> productColorsAndSizes; // Stores available colors and sizes

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

        if (productPopup != null)
        {
            productPopup.SetActive(false);
        }
        if (closePopup != null)
        {
            closePopup.onClick.AddListener(CloseProductPopup);
        }
        if (openPopup != null)
        {
            openPopup.onClick.AddListener(OpenProductPopup);
        }
        // Update quantity drop-down when size changes
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
                        product = new ProductData();

                        // Retrieve product details from Firebase
                        product.name = snapshot.Child("name").Value.ToString();
                        product.price = float.Parse(snapshot.Child("price").Value.ToString());
                        product.color = snapshot.Child("color").Value.ToString();
                        product.description = snapshot.Child("description").Value.ToString();
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
                            }
                            else
                            {
                                // Product has multiple sizes
                                product.sizes = new Dictionary<string, int>();
                                foreach (var size in snapshot.Child("sizes").Children)
                                {
                                    product.sizes.Add(size.Key, int.Parse(size.Value.ToString()));
                                }
                            }
                        }
                        else
                        {
                            product.sizes = null;
                            product.singleSize = null;
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


                        // Check if the product has a discount
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
                            // No discount, show the original price without strikethrough
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

                        // Color drop-down
                        if (colorDropdown != null)
                        {
                            colorDropdown.ClearOptions();
                            colorDropdown.AddOptions(new List<string> { product.color });
                        }

                        // Size drop-down
                        sizeDropdown.ClearOptions();
                        if (product.sizes != null && product.sizes.Count > 0)
                        {
                            List<string> sizes = new List<string>(product.sizes.Keys);
                            sizeDropdown.AddOptions(sizes);
                        }
                        else if (!string.IsNullOrEmpty(product.singleSize))
                        {
                            sizeDropdown.AddOptions(new List<string> { product.singleSize });
                        }
                        // Refresh the dropdown to reflect changes
                        sizeDropdown.RefreshShownValue();

                        // Reset quantity to default
                        SetDefaultQuantityDropdown();
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

    public void UpdateQuantityDropdown()
    {
        if (quantityDropdown == null || sizeDropdown == null || product.sizes == null)
            return;

        quantityDropdown.ClearOptions();

        // Get selected size
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        if (!product.sizes.ContainsKey(selectedSize))
        {
            SetDefaultQuantityDropdown();
            return;
        }

        // Get available stock
        int availableStock = product.sizes[selectedSize];

        // Set max selectable quantity (min of 5 or available stock)
        int maxSelectable = Mathf.Min(5, availableStock);

        // Populate dropdown with values from 1 to maxSelectable
        List<string> quantities = new List<string> { "Select Quantity" };
        for (int i = 1; i <= maxSelectable; i++)
        {
            quantities.Add(i.ToString());
        }

        quantityDropdown.AddOptions(quantities);
        quantityDropdown.value = 0;
        quantityDropdown.RefreshShownValue();
    }

    // Set default state for quantity dropdown
    private void SetDefaultQuantityDropdown()
    {
        if (quantityDropdown != null)
        {
            quantityDropdown.ClearOptions();
            quantityDropdown.AddOptions(new List<string> { "Select size first" });
            quantityDropdown.value = 0;
            quantityDropdown.RefreshShownValue();
        }
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
        }
        else if (product != null && !string.IsNullOrEmpty(product.singleSize))
        {
            sizeDropdown.AddOptions(new List<string> { product.singleSize });
        }

        sizeDropdown.RefreshShownValue();
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
    public string description;
}

[System.Serializable]
public class DiscountData
{
    public bool exists;
    public float percentage;
}