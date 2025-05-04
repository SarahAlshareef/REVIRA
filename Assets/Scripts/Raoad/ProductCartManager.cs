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

    void Start()
    {
        db = FirebaseDatabase.DefaultInstance.RootReference;
        productsManager = FindObjectOfType<ProductsManager>();
        userManager = FindObjectOfType<UserManager>();
        cartManager = FindObjectOfType<CartManager>();

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

        if (isAdding)
        {
            Debug.Log("[DEBUG] Already adding, skipping");
            return;
        }

        if (!ValidateSelection()) return;

        string userId = userManager?.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            ShowError("User not logged in.");
            return;
        }

        var pd = productsManager?.GetProductData();
        string pid = productsManager?.productID;
        if (pd == null || string.IsNullOrEmpty(pid))
        {
            ShowError("Missing product data.");
            return;
        }

        string color = colorDropdown.options[colorDropdown.value].text;
        string size = sizeDropdown.options[sizeDropdown.value].text;
        int qty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        long expires = GetUnixTimestamp() + 86400;

        if (!productsManager.productColorsAndSizes.TryGetValue(color, out var sizesDict)
         || !sizesDict.TryGetValue(size, out var inStock)
         || inStock < qty)
        {
            ShowError("Not enough stock.");
            return;
        }

        // throttle further clicks
        isAdding = true;
        addToCartButton.interactable = false;

        // --- STEP 1: Reduce stock in Firebase ---
        Debug.Log($"[DEBUG] Reading stock for {pid}/{color}/{size}");
        db.Child("REVIRA").Child("stores").Child("storeID_123")
          .Child("products").Child(pid)
          .Child("colors").Child(color)
          .Child("sizes").Child(size)
          .GetValueAsync().ContinueWithOnMainThread(stockRead =>
          {
              if (stockRead.Exception != null || !stockRead.Result.Exists)
              {
                  Debug.LogError("[DEBUG] Stock READ failed: " + stockRead.Exception);
                  Fail("Could not read stock.");
                  return;
              }

              int currentStock = int.Parse(stockRead.Result.Value.ToString());
              int newStock = Mathf.Max(0, currentStock - qty);
              Debug.Log($"[DEBUG] Writing new stock {newStock}");
              db.Child("REVIRA").Child("stores").Child("storeID_123")
                .Child("products").Child(pid)
                .Child("colors").Child(color)
                .Child("sizes").Child(size)
                .SetValueAsync(newStock).ContinueWithOnMainThread(stockWrite =>
                {
                    if (stockWrite.Exception != null)
                    {
                        Debug.LogError("[DEBUG] Stock WRITE failed: " + stockWrite.Exception);
                        Fail("Could not update stock.");
                        return;
                    }

                    // --- STEP 2: Read existing cart qty for this size ---
                    Debug.Log($"[DEBUG] Reading cart qty for {pid}/{size}");
                    db.Child("REVIRA").Child("Consumers").Child(userId)
                    .Child("cart").Child("cartItems").Child(pid)
                    .Child("sizes").Child(size)
                    .GetValueAsync().ContinueWithOnMainThread(cartRead =>
                    {
                        if (cartRead.Exception != null)
                        {
                            Debug.LogError("[DEBUG] Cart READ failed: " + cartRead.Exception);
                            Fail("Could not read cart.");
                            return;
                        }

                        int existingQty = cartRead.Result.Exists
                          ? int.Parse(cartRead.Result.Value.ToString())
                          : 0;
                        int updatedQty = existingQty + qty;
                        Debug.Log($"[DEBUG] Updating cart size '{size}' to {updatedQty}");

                        // --- STEP 3: Write updated cart entry ---
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

                        db.Child("REVIRA").Child("Consumers").Child(userId)
                        .Child("cart").Child("cartItems").Child(pid)
                        .UpdateChildrenAsync(updateMap)
                        .ContinueWithOnMainThread(cartWrite =>
                        {
                            if (cartWrite.Exception != null)
                            {
                                Debug.LogError("[DEBUG] Cart WRITE failed: " + cartWrite.Exception);
                                Fail("Could not update cart.");
                                return;
                            }

                            // --- STEP 4: Success! Refresh UI & summary ---
                            Debug.Log("[DEBUG] Cart entry updated successfully");
                            ShowSuccess("Added to cart");
                            UpdateCartSummary(userId, () => cartManager?.LoadCartItems());

                            isAdding = false;
                            addToCartButton.interactable = true;
                        });
                    });
                });
          });
    }

    private void UpdateCartSummary(string userId, System.Action onDone)
    {
        Debug.Log("[DEBUG] Updating cart summary");
        db.Child("REVIRA").Child("Consumers").Child(userId)
          .Child("cart").Child("cartItems")
          .GetValueAsync().ContinueWithOnMainThread(summaryRead =>
          {
              if (summaryRead.Exception != null)
              {
                  Debug.LogError("[DEBUG] Summary READ failed: " + summaryRead.Exception);
                  onDone?.Invoke();
                  return;
              }

              float totalP = 0f;
              int totalI = 0;
              if (summaryRead.Result.Exists)
              {
                  foreach (var item in summaryRead.Result.Children)
                  {
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

              db.Child("REVIRA").Child("Consumers").Child(userId)
                .Child("cart").Child("cartTotal")
                .SetValueAsync(cartTotal)
                .ContinueWithOnMainThread(_ =>
                {
                    Debug.Log("[DEBUG] Cart summary updated");
                    onDone?.Invoke();
                });
          });
    }

    private void RemoveExpiredCartItems()
    {
        string uid = userManager.UserId;
        var refCart = db.Child("REVIRA").Child("Consumers").Child(uid)
                       .Child("cart").Child("cartItems");
        refCart.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null || !task.Result.Exists) return;
            long now = GetUnixTimestamp();
            var toRemove = new List<string>();
            foreach (var item in task.Result.Children)
            {
                if (item.Child("expiresAt").Value != null
                 && long.Parse(item.Child("expiresAt").Value.ToString()) < now)
                    toRemove.Add(item.Key);
            }
            foreach (var id in toRemove)
                refCart.Child(id).RemoveValueAsync();
        });
    }

    private bool ValidateSelection()
    {
        if (colorDropdown.options[colorDropdown.value].text == "Select Color"
         || sizeDropdown.options[sizeDropdown.value].text == "Select Size"
         || quantityDropdown.options[quantityDropdown.value].text == "Select Quantity")
        {
            ShowError("Please make all selections.");
            return false;
        }
        ClearMessage();
        return true;
    }

    private void Fail(string msg)
    {
        Debug.LogError("[DEBUG] FAIL: " + msg);
        ShowError(msg);
        isAdding = false;
        addToCartButton.interactable = true;
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
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }

    private long GetUnixTimestamp() =>
        (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
}
