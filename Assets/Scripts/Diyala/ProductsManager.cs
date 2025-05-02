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
    private DatabaseReference dbReference;
    private ProductData product;
    [HideInInspector] public string productID, storeID;

    [Header("Game Objects")]
    public GameObject productPopup;
    public GameObject discountTag;

    [Header("Product Data")]
    public TMP_Text productName;
    public TMP_Text productPrice;
    public TMP_Text discountedPrice;
    public TMP_Text productDescription;
    public Image productImage;
    public TMP_Dropdown colorDropdown, sizeDropdown, quantityDropdown;

    [Header("Buttons")]
    public Button closePopup;

    public Dictionary<string, Dictionary<string, int>> productColorsAndSizes;


    // Getter & Setter
    public ProductData GetProductData() { return product; }
    public void SetProductData(ProductData newProduct) { product = newProduct; }

    public void Start()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == Firebase.DependencyStatus.Available)
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                LoadProductData();
            }
        });

        productPopup?.SetActive(false);
        closePopup?.onClick.AddListener(CloseProductPopup);

        colorDropdown?.onValueChanged.AddListener((index) => UpdateSizeDropdown());
        sizeDropdown?.onValueChanged.AddListener((index) => UpdateQuantityDropdown());
    }

    public void OnPreviewSpecificationClick()
    {
        ProductIdentifie idenrifier = GetComponent<ProductIdentifie>();
        storeID = idenrifier.StoreID;
        productID = idenrifier.ProductID;

        LoadProductData();

        GameObject cam = GameObject.Find("CenterEyeAnchor");
        if (cam != null && productPopup != null)
        {
            Transform vrCamera = cam.transform;
            Vector3 frontOffset = vrCamera.forward * 1.4f;
            productPopup.transform.position = vrCamera.position + frontOffset;
            productPopup.transform.rotation = Quaternion.LookRotation(productPopup.transform.position - vrCamera.position);
        }
        productPopup?.SetActive(true);      
    }

    public void LoadProductData()
    {

        dbReference.Child("REVIRA").Child("stores").Child(storeID).Child("products").Child(productID)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists)
                    {
                        product = new ProductData()
                        {
                            name = snapshot.Child("name").Value.ToString(),
                            price = float.Parse(snapshot.Child("price").Value.ToString()),
                            description = snapshot.Child("description").Value.ToString(),
                            image = snapshot.Child("image").Value.ToString(),

                            discount = new DiscountData()
                            {
                                exists = bool.Parse(snapshot.Child("discount").Child("exists").Value.ToString()),
                                percentage = float.Parse(snapshot.Child("discount").Child("percentage").Value.ToString())
                            }
                        };

                        if (productPrice != null)
                        {
                            if (product.discount.exists && product.discount.percentage > 0)
                            {
                                float newPrice = product.price - (product.price * (product.discount.percentage / 100));

                                productPrice.fontStyle = FontStyles.Strikethrough;
                                productPrice?.SetText($"{product.price:F2}");
                                discountedPrice?.SetText($"{newPrice:F2}");
                                discountedPrice?.gameObject.SetActive(true);
                                discountTag?.SetActive(true);
                            }
                            else
                            {
                                productPrice.fontStyle = FontStyles.Normal;
                                productPrice?.SetText($"{product.price:F2}");
                                discountedPrice?.gameObject.SetActive(false);
                                discountTag?.SetActive(false);
                            }
                        }

                        productColorsAndSizes = new Dictionary<string, Dictionary<string, int>>();

                        if (snapshot.HasChild("colors"))
                        {
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

                        productName?.SetText(product.name);
                        productDescription?.SetText(product.description);

                        StartCoroutine(LoadImageFromURL(product.image));
                        UpdateColorDropdown();
                        UpdateSizeDropdown();
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

    public void UpdateColorDropdown()
    {
        if (productColorsAndSizes == null) return;

        List<string> colors = new List<string> { "Select Color" };
        colors.AddRange(productColorsAndSizes.Keys);

        colorDropdown.ClearOptions();
        colorDropdown.AddOptions(colors);
        colorDropdown.SetValueWithoutNotify(0);
        colorDropdown.RefreshShownValue();
    }

    public void UpdateSizeDropdown()
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
        sizeDropdown.SetValueWithoutNotify(0);
        sizeDropdown.RefreshShownValue();
    }

    public void UpdateQuantityDropdown()
    {
        if (quantityDropdown == null || sizeDropdown == null || colorDropdown == null)
            return;

        List<string> quantities = new List<string> { "Select Quantity" };

        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;

        if (selectedColor != "Select Color" && selectedSize != "Select Size" && productColorsAndSizes.ContainsKey(selectedColor)
            && productColorsAndSizes[selectedColor].ContainsKey(selectedSize))
        {

            int availableStock = productColorsAndSizes[selectedColor][selectedSize];
            int maxSelectable = Mathf.Min(5, availableStock);

            if (maxSelectable == 0)
            {
                quantities.Add("Out of Stock");
            }
            else
            {
                for (int i = 1; i <= maxSelectable; i++)
                {
                    quantities.Add(i.ToString());
                }
            }
        }

        quantityDropdown.ClearOptions();
        quantityDropdown.AddOptions(quantities);
        quantityDropdown.SetValueWithoutNotify(0);
        quantityDropdown.RefreshShownValue();
    }

    public void CloseProductPopup()
    {
        productPopup?.SetActive(false);

        productName?.SetText("");
        productPrice?.SetText("");
        discountedPrice?.SetText("");
        productDescription?.SetText("");

        if (productImage != null)
        {
            productImage.sprite = null;
        }

        colorDropdown?.ClearOptions();
        sizeDropdown?.ClearOptions();
        quantityDropdown?.ClearOptions();

        discountedPrice?.gameObject.SetActive(false);
        discountTag?.SetActive(false);

        product = null;
    }
}
    public class ProductData
{
    public float price;
    public string name, color, image, description;
    public int quantity;
    public DiscountData discount;
    public Dictionary<string, int> sizes;
}

public class DiscountData
{
    public bool exists;
    public float percentage;
}