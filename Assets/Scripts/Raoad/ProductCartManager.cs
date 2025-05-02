using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using TMPro;

public class ProductCartManager : MonoBehaviour
{
    private DatabaseReference dbReference;
    public TMP_Dropdown sizeDropdown, colorDropdown, quantityDropdown;
    public Button addToCartButton;
    public TextMeshProUGUI errorText;

    private ProductsManager productsManager;
    private UserManager userManager;
    private Coroutine cooldownCoroutine;
    private CartManager cartManager;

    private bool isAdding = false;
    private bool hasAdded = false;

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        productsManager = FindObjectOfType<ProductsManager>();
        userManager = FindObjectOfType<UserManager>();
        cartManager = FindObjectOfType<CartManager>();

        if (addToCartButton != null)
        {
            addToCartButton.onClick.AddListener(AddToCart);
            Debug.Log("[DEBUG] AddToCart button listener registered.");
        }

        if (colorDropdown != null)
            colorDropdown.onValueChanged.AddListener(delegate { Debug.Log("[DEBUG] Color dropdown changed."); });

        if (sizeDropdown != null)
            sizeDropdown.onValueChanged.AddListener(delegate { Debug.Log("[DEBUG] Size dropdown changed."); });

        if (quantityDropdown != null)
            quantityDropdown.onValueChanged.AddListener(delegate { Debug.Log("[DEBUG] Quantity dropdown changed."); });

        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }

        RemoveExpiredCartItems();
    }

    // Debug version: only confirms button is working with controller
    public void AddToCart()
    {
        Debug.Log("[DEBUG] AddToCart triggered");

        if (errorText != null)
        {
            errorText.color = Color.green;
            errorText.text = "AddToCart triggered successfully.";
            errorText.gameObject.SetActive(true);
        }

        StartCoroutine(ClearMessageAfterSeconds(3f));
    }

    private IEnumerator ClearMessageAfterSeconds(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }
    }

    // --- The rest of your original logic is preserved below ---

    public bool ValidateSelection()
    {
        string color = colorDropdown.options[colorDropdown.value].text;
        if (color == "Select Color")
        {
            errorText.text = "Please select a color.";
            errorText.gameObject.SetActive(true);
            return false;
        }

        string size = sizeDropdown.options[sizeDropdown.value].text;
        if (size == "Select Size")
        {
            errorText.text = "Please select a size.";
            errorText.gameObject.SetActive(true);
            return false;
        }

        string quantity = quantityDropdown.options[quantityDropdown.value].text;
        if (quantity == "Select Quantity")
        {
            errorText.text = "Please select a quantity.";
            errorText.gameObject.SetActive(true);
            return false;
        }

        errorText.text = "";
        errorText.gameObject.SetActive(false);
        return true;
    }

    private void RemoveExpiredCartItems()
    {
        string userID = userManager.UserId;
        DatabaseReference cartItemsRef = dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child("cartItems");

        cartItemsRef.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                long currentTimestamp = GetUnixTimestamp();
                List<string> expiredItems = new();

                foreach (var item in task.Result.Children)
                {
                    if (item.Child("expiresAt").Value != null &&
                        long.Parse(item.Child("expiresAt").Value.ToString()) < currentTimestamp)
                    {
                        string productID = item.Key;
                        expiredItems.Add(productID);
                        RestoreStock(productID, item);
                    }
                }

                if (expiredItems.Count > 0)
                {
                    foreach (string id in expiredItems)
                    {
                        cartItemsRef.Child(id).RemoveValueAsync();
                    }

                    cartItemsRef.GetValueAsync().ContinueWith(checkTask =>
                    {
                        if (checkTask.IsCompleted && (!checkTask.Result.Exists || checkTask.Result.ChildrenCount == 0))
                        {
                            dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").RemoveValueAsync();
                        }
                    });
                }
            }
        });
    }

    private void RestoreStock(string productID, DataSnapshot item)
    {
        foreach (var sizeEntry in item.Child("sizes").Children)
        {
            string size = sizeEntry.Key;
            int quantity = int.Parse(sizeEntry.Value.ToString());
            string path = $"stores/storeID_123/products/{productID}/colors/{item.Child("color").Value}/sizes/{size}";

            dbReference.Child("REVIRA").Child(path).GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    int currentStock = int.Parse(task.Result.Value.ToString());
                    dbReference.Child("REVIRA").Child(path).SetValueAsync(currentStock + quantity);
                }
            });
        }
    }

    private long GetUnixTimestamp()
    {
        return (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
    }
}
