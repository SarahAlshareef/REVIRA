using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;

public class ProductCartManager : MonoBehaviour
{
    private DatabaseReference db;
    public TMP_Dropdown sizeDropdown, colorDropdown, quantityDropdown;
    public Button addToCartButton;
    public TextMeshProUGUI errorText;

    private ProductsManager productsManager;
    private UserManager userManager;
    private CartManager cartManager;

    private bool isAdding = false;
    private bool hasAdded = false;

    void Start()
    {
        db = FirebaseDatabase.DefaultInstance.RootReference;
        productsManager = FindObjectOfType<ProductsManager>();
        userManager = FindObjectOfType<UserManager>();
        cartManager = FindObjectOfType<CartManager>();

        if (addToCartButton != null)
            addToCartButton.onClick.AddListener(AddToCart);

        colorDropdown?.onValueChanged.AddListener(_ => ValidateSelection());
        sizeDropdown?.onValueChanged.AddListener(_ => ValidateSelection());
        quantityDropdown?.onValueChanged.AddListener(_ => ValidateSelection());

        ClearMessage();
        RemoveExpiredCartItems();
    }

    public void AddToCart()
    {
        Debug.Log("[DEBUG] AddToCart triggered");

        // guard busy/recent click
        if (isAdding || hasAdded)
        {
            ShowError("Please wait before adding again.");
            return;
        }

        // guard valid selection
        if (!ValidateSelection())
            return;

        // guard login
        string userId = userManager?.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            ShowError("User not logged in.");
            return;
        }

        // guard product data
        var pd = productsManager?.GetProductData();
        string pid = productsManager?.productID;
        if (pd == null || string.IsNullOrEmpty(pid))
        {
            ShowError("Missing product data.");
            return;
        }

        // capture UI inputs
        string color = colorDropdown.options[colorDropdown.value].text;
        string size = sizeDropdown.options[sizeDropdown.value].text;
        int qty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        long expires = GetUnixTimestamp() + 86400;

        // guard stock
        if (!productsManager.productColorsAndSizes.TryGetValue(color, out var sizesDict)
         || !sizesDict.TryGetValue(size, out var available)
         || available < qty)
        {
            ShowError("Not enough stock.");
            return;
        }

        isAdding = true;

        // 1) Reduce stock
        db.Child("REVIRA")
          .Child("stores").Child("storeID_123")
          .Child("products").Child(pid)
          .Child("colors").Child(color)
          .Child("sizes").Child(size)
          .GetValueAsync().ContinueWithOnMainThread(stockTask =>
          {
              if (stockTask.Exception != null || !stockTask.Result.Exists)
              {
                  Debug.LogError("[DEBUG] Stock read failed: " + stockTask.Exception);
                  FailAdd("Could not read stock.");
                  return;
              }

              int currentStock = int.Parse(stockTask.Result.Value.ToString());
              int newStock = Mathf.Max(0, currentStock - qty);

              db.Child("REVIRA")
                .Child("stores").Child("storeID_123")
                .Child("products").Child(pid)
                .Child("colors").Child(color)
                .Child("sizes").Child(size)
                .SetValueAsync(newStock).ContinueWithOnMainThread(setTask =>
                {
                    if (setTask.Exception != null)
                    {
                        Debug.LogError("[DEBUG] Stock write failed: " + setTask.Exception);
                        FailAdd("Could not update stock.");
                        return;
                    }

                    // 2) Read existing cart size
                    db.Child("REVIRA")
                    .Child("Consumers").Child(userId)
                    .Child("cart").Child("cartItems")
                    .Child(pid)
                    .Child("sizes").Child(size)
                    .GetValueAsync().ContinueWithOnMainThread(getTask =>
                    {
                        if (getTask.Exception != null)
                        {
                            Debug.LogError("[DEBUG] Cart read failed: " + getTask.Exception);
                            FailAdd("Could not read cart.");
                            return;
                        }

                        int existingQty = getTask.Result.Exists
                          ? int.Parse(getTask.Result.Value.ToString())
                          : 0;
                        int updatedQty = existingQty + qty;

                        // 3) Update cart entry
                        var updateMap = new Dictionary<string, object>()
                      {
                        { "productID",     pid },
                        { "productName",   pd.name },
                        { "color",         color },
                        { "price",         pd.price },
                        { "timestamp",     GetUnixTimestamp() },
                        { "expiresAt",     expires },
                        { $"sizes/{size}", updatedQty }
                      };

                        db.Child("REVIRA")
                        .Child("Consumers").Child(userId)
                        .Child("cart").Child("cartItems")
                        .Child(pid)
                        .UpdateChildrenAsync(updateMap)
                        .ContinueWithOnMainThread(updateTask =>
                        {
                            if (updateTask.Exception != null)
                            {
                                Debug.LogError("[DEBUG] Cart write failed: " + updateTask.Exception);
                                FailAdd("Could not update cart.");
                                return;
                            }

                            // 4) Update summary & UI
                            hasAdded = true;
                            ShowSuccess("Product added successfully.");
                            UpdateCartSummary(userId, () => cartManager?.LoadCartItems());
                            StartCoroutine(ResetAddLock(5f));
                            isAdding = false;
                        });
                    });
                });
          });
    }

    private void UpdateCartSummary(string userId, System.Action onComplete)
    {
        db.Child("REVIRA")
          .Child("Consumers").Child(userId)
          .Child("cart").Child("cartItems")
          .GetValueAsync().ContinueWithOnMainThread(task =>
          {
              float totalP = 0f;
              int totalI = 0;
              if (task.Result.Exists)
              {
                  foreach (var item in task.Result.Children)
                  {
                      if (!item.HasChild("price") || !item.HasChild("sizes")) continue;
                      float pr = float.Parse(item.Child("price").Value.ToString());
                      foreach (var sz in item.Child("sizes").Children)
                      {
                          int q = int.Parse(sz.Value.ToString());
                          totalP += pr * q;
                          totalI += q;
                      }
                  }
              }
              var cartTotal = new Dictionary<string, object>()
            {
                { "totalPrice", totalP },
                { "totalItems", totalI }
            };
              db.Child("REVIRA")
                .Child("Consumers").Child(userId)
                .Child("cart").Child("cartTotal")
                .SetValueAsync(cartTotal)
                .ContinueWithOnMainThread(_ => onComplete?.Invoke());
          });
    }

    private void RemoveExpiredCartItems()
    {
        string uid = userManager.UserId;
        var refCart = db.Child("REVIRA")
                       .Child("Consumers").Child(uid)
                       .Child("cart").Child("cartItems");

        refCart.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.Result.Exists) return;
            long now = GetUnixTimestamp();
            var toRemove = new List<string>();

            foreach (var item in task.Result.Children)
            {
                if (item.Child("expiresAt").Value != null &&
                    long.Parse(item.Child("expiresAt").Value.ToString()) < now)
                {
                    toRemove.Add(item.Key);
                    RestoreStock(item.Key, item);
                }
            }
            foreach (var id in toRemove)
                refCart.Child(id).RemoveValueAsync();
        });
    }

    private void RestoreStock(string productID, DataSnapshot item)
    {
        string color = item.Child("color").Value.ToString();
        foreach (var sz in item.Child("sizes").Children)
        {
            string s = sz.Key;
            int q = int.Parse(sz.Value.ToString());
            db.Child("REVIRA")
              .Child("stores").Child("storeID_123")
              .Child("products").Child(productID)
              .Child("colors").Child(color)
              .Child("sizes").Child(s)
              .GetValueAsync().ContinueWithOnMainThread(task =>
              {
                  if (task.Result.Exists)
                  {
                      int cur = int.Parse(task.Result.Value.ToString());
                      db.Child("REVIRA")
                        .Child("stores").Child("storeID_123")
                        .Child("products").Child(productID)
                        .Child("colors").Child(color)
                        .Child("sizes").Child(s)
                        .SetValueAsync(cur + q);
                  }
              });
        }
    }

    private bool ValidateSelection()
    {
        if (colorDropdown.options[colorDropdown.value].text == "Select Color" ||
            sizeDropdown.options[sizeDropdown.value].text == "Select Size" ||
            quantityDropdown.options[quantityDropdown.value].text == "Select Quantity")
        {
            ShowError("Please make all selections.");
            return false;
        }
        ClearMessage();
        return true;
    }

    private void FailAdd(string msg)
    {
        isAdding = false;
        hasAdded = false;
        ShowError(msg);
    }

    private IEnumerator ResetAddLock(float delay)
    {
        yield return new WaitForSeconds(delay);
        hasAdded = false;
    }

    private void ShowError(string m)
    {
        errorText.color = Color.red;
        errorText.text = m;
        errorText.gameObject.SetActive(true);
        CancelInvoke(nameof(ClearMessage));
        Invoke(nameof(ClearMessage), 5f);
    }

    private void ShowSuccess(string m)
    {
        errorText.color = Color.green;
        errorText.text = m;
        errorText.gameObject.SetActive(true);
        CancelInvoke(nameof(ClearMessage));
        Invoke(nameof(ClearMessage), 3f);
    }

    private void ClearMessage()
    {
        errorText.text = "";
        errorText.gameObject.SetActive(false);
    }

    private long GetUnixTimestamp() =>
        (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
}
