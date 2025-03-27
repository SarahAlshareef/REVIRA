using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;

public class CartUIManager : MonoBehaviour
{
    public static CartUIManager Instance;

    public GameObject scrollViewContent;
    public GameObject cartItemPanel;
    public TextMeshProUGUI totalText;
    public GameObject scrollView;

    private DatabaseReference dbReference;
    private string userId;
    private float totalPrice = 0f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;
        LoadCartItems();
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

        dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                scrollView.SetActive(true);

                foreach (DataSnapshot productSnapshot in task.Result.Children)
                {
                    string productId = productSnapshot.Key;
                    string productName = productSnapshot.Child("productName").Value.ToString();
                    string color = productSnapshot.Child("color").Value.ToString();
                    float price = float.Parse(productSnapshot.Child("price").Value.ToString());
                    string imageUrl = "";

                    string selectedSize = "";
                    int quantity = 0;

                    foreach (var sizeEntry in productSnapshot.Child("sizes").Children)
                    {
                        selectedSize = sizeEntry.Key;
                        quantity = int.Parse(sizeEntry.Value.ToString());
                        break;
                    }

                    dbReference.Child("REVIRA").Child("stores").Child("storeID_123").Child("products").Child(productId).GetValueAsync().ContinueWithOnMainThread(productTask =>
                    {
                        if (productTask.IsCompleted && productTask.Result.Exists)
                        {
                            imageUrl = productTask.Result.Child("image").Value.ToString();

                            float discount = 0f;
                            bool hasDiscount = false;

                            if (productTask.Result.Child("discount").Child("exists").Value.ToString() == "True")
                            {
                                hasDiscount = true;
                                discount = float.Parse(productTask.Result.Child("discount").Child("percentage").Value.ToString());
                            }

                            Dictionary<string, Dictionary<string, int>> allColorsAndSizes = new Dictionary<string, Dictionary<string, int>>();
                            var colorsSnapshot = productTask.Result.Child("colors");

                            foreach (var colorEntry in colorsSnapshot.Children)
                            {
                                string colorName = colorEntry.Key;
                                Dictionary<string, int> sizesDict = new Dictionary<string, int>();

                                foreach (var sizeEntry in colorEntry.Child("sizes").Children)
                                {
                                    string size = sizeEntry.Key;
                                    int stock = int.Parse(sizeEntry.Value.ToString());
                                    sizesDict[size] = stock;
                                }

                                allColorsAndSizes[colorName] = sizesDict;
                            }

                            float finalPrice = hasDiscount ? price - (price * discount / 100f) : price;
                            totalPrice += finalPrice * quantity;

                            GameObject newItem = Instantiate(cartItemPanel, scrollViewContent.transform);
                            newItem.SetActive(true);

                            CartItem itemScript = newItem.GetComponent<CartItem>();
                            itemScript.SetUpItem(productId, productName, imageUrl, color, selectedSize, quantity, price, hasDiscount, discount, allColorsAndSizes);

                            RefreshTotalUI();
                            CheckScrollVisibility();
                        }
                    });
                }
            }
            else
            {
                scrollView.SetActive(false);
            }
        });
    }

    public void RefreshTotalUI()
    {
        totalText.text = "Total: " + totalPrice.ToString("F2") + " SAR";
    }

    public void CheckScrollVisibility()
    {
        scrollView.SetActive(scrollViewContent.transform.childCount > 0);
    }

    public void ResetTotal()
    {
        totalPrice = 0f;
    }
}
