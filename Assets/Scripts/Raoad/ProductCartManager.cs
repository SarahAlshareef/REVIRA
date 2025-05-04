using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;

public class ProductCartManager : MonoBehaviour
{
    // Reference to the Firebase Database root
    private DatabaseReference db;

    // UI elements for product selection
    public TMP_Dropdown sizeDropdown, colorDropdown, quantityDropdown;
    public Button addToCartButton;
    public TextMeshProUGUI errorText;

    // References to other manager scripts
    private ProductsManager productsManager;
    private UserManager userManager;
    private CartManager cartManager;

    // Flag to prevent multiple clicks while adding
    private bool isAdding = false;

    void Start()
    {
        Debug.Log("[DEBUG] Start");
        db = FirebaseDatabase.DefaultInstance.RootReference;
        Debug.Log("[DEBUG] Firebase DB Root: " + (db == null ? "null" : "ok"));

        productsManager = FindObjectOfType<ProductsManager>();
        userManager = FindObjectOfType<UserManager>();
        cartManager = FindObjectOfType<CartManager>();

        if (addToCartButton == null) Debug.LogError("[DEBUG] AddToCartButton missing!");
        if (colorDropdown == null) Debug.LogError("[DEBUG] ColorDropdown missing!");
        if (sizeDropdown == null) Debug.LogError("[DEBUG] SizeDropdown missing!");
        if (quantityDropdown == null) Debug.LogError("[DEBUG] QuantityDropdown missing!");
        if (errorText == null) Debug.LogError("[DEBUG] ErrorText missing!");

        addToCartButton.onClick.AddListener(AddToCart);
        colorDropdown.onValueChanged.AddListener(_ => ValidateSelection());
        sizeDropdown.onValueChanged.AddListener(_ => ValidateSelection());
        quantityDropdown.onValueChanged.AddListener(_ => ValidateSelection());

        ClearMessage();
        RemoveExpiredCartItems();
    }

    public void AddToCart()
    {
        Debug.Log("[DEBUG] AddToCart triggered");
        if (isAdding)
        {
            Debug.Log("[DEBUG] Already adding – skipping");
            return;
        }
        if (!ValidateSelection()) return;

        string userId = userManager?.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            Fail("User not logged in.");
            return;
        }
        Debug.Log("[DEBUG] UserId: " + userId);

        var pd = productsManager?.GetProductData();
        string pid = productsManager?.productID;
        if (pd == null || string.IsNullOrEmpty(pid))
        {
            Fail("Missing product data.");
            return;
        }
        Debug.Log($"[DEBUG] PID={pid} Name={pd.name} Price={pd.price}");

        string color = colorDropdown.options[colorDropdown.value].text;
        string size = sizeDropdown.options[sizeDropdown.value].text;
        int qty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        long expires = GetUnixTimestamp() + 86400;

        if (!productsManager.productColorsAndSizes.TryGetValue(color, out var sizesDict)
         || !sizesDict.TryGetValue(size, out var inStock)
         || inStock < qty)
        {
            Fail("Not enough stock.");
            return;
        }
        Debug.Log($"[DEBUG] Local stock OK: {inStock}");

        isAdding = true;
        addToCartButton.interactable = false;

        // 1) Read & reduce stock on product node
        string stockPath = $"stores/storeID_123/products/{pid}/colors/{color}/sizes/{size}";
        Debug.Log("[DEBUG] Reading stock at " + stockPath);
        db.Child("REVIRA").Child(stockPath)
          .GetValueAsync().ContinueWithOnMainThread(stockRead =>
          {
              Debug.Log("[DEBUG] stockRead callback");
              if (stockRead.Exception != null || !stockRead.Result.Exists)
              {
                  Fail("Could not read stock.");
                  return;
              }

              int currentStock = int.Parse(stockRead.Result.Value.ToString());
              int newStock = Mathf.Max(0, currentStock - qty);
              Debug.Log($"[DEBUG] Setting newStock={newStock}");
              db.Child("REVIRA").Child(stockPath)
                .SetValueAsync(newStock).ContinueWithOnMainThread(stockWrite =>
                {
                    Debug.Log("[DEBUG] stockWrite callback");
                    if (stockWrite.Exception != null)
                    {
                        Fail("Could not update stock.");
                        return;
                    }

                    // 2) Read existing cart qty
                    string cartSizePath = $"Consumers/{userId}/cart/cartItems/{pid}/sizes/{size}";
                    Debug.Log("[DEBUG] Reading cart qty at " + cartSizePath);
                    db.Child("REVIRA").Child(cartSizePath)
                    .GetValueAsync().ContinueWithOnMainThread(cartRead =>
                    {
                        Debug.Log("[DEBUG] cartRead callback");
                        if (cartRead.Exception != null)
                        {
                            Fail("Could not read cart.");
                            return;
                        }

                        int existingQty = cartRead.Result.Exists
                          ? int.Parse(cartRead.Result.Value.ToString())
                          : 0;
                        int updatedQty = existingQty + qty;
                        Debug.Log($"[DEBUG] updatedQty={updatedQty}");

                        // 3) Update cart item
                        var updateMap = new Dictionary<string, object>()
                      {
                        { "productID",   pid },
                        { "productName", pd.name },
                        { "color",       color },
                        { "price",       pd.price },
                        { "timestamp",   GetUnixTimestamp() },
                        { "expiresAt",   expires },
                        { $"sizes/{size}", updatedQty }
                      };
                        string cartItemPath = $"Consumers/{userId}/cart/cartItems/{pid}";
                        Debug.Log("[DEBUG] Updating cart at " + cartItemPath);
                        db.Child("REVIRA").Child(cartItemPath)
                        .UpdateChildrenAsync(updateMap).ContinueWithOnMainThread(cartWrite =>
                        {
                            Debug.Log("[DEBUG] cartWrite callback");
                            if (cartWrite.Exception != null)
                            {
                                Fail("Could not update cart.");
                                return;
                            }

                            ShowSuccess("Added to cart");
                            UpdateCartSummary(userId, () =>
                            {
                                Debug.Log("[DEBUG] Summary updated, reloading UI");
                                cartManager?.LoadCartItems();
                            });

                            // finally re-enable
                            isAdding = false;
                            addToCartButton.interactable = true;
                            Debug.Log("[DEBUG] Done adding, button re-enabled");
                        });
                    });
                });
          });
    }

    private void UpdateCartSummary(string userId, Action onDone)
    {
        Debug.Log("[DEBUG] Updating cart summary");
        string itemsPath = $"Consumers/{userId}/cart/cartItems";
        db.Child("REVIRA").Child(itemsPath)
          .GetValueAsync().ContinueWithOnMainThread(summaryRead =>
          {
              Debug.Log("[DEBUG] summaryRead callback");
              if (summaryRead.Exception != null)
              {
                  Debug.LogError("[DEBUG] summaryRead error: " + summaryRead.Exception);
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

              var cartTotal = new Dictionary<string, object>
            {
                { "totalPrice", totalP },
                { "totalItems", totalI }
            };
              string totalPath = $"Consumers/{userId}/cart/cartTotal";
              Debug.Log("[DEBUG] Writing cartTotal");
              db.Child("REVIRA").Child(totalPath)
                .SetValueAsync(cartTotal).ContinueWithOnMainThread(_ =>
                {
                    Debug.Log("[DEBUG] cartTotal write complete");
                    onDone?.Invoke();
                });
          });
    }

    private void RemoveExpiredCartItems()
    {
        var uid = userManager?.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        string cartItems = $"Consumers/{uid}/cart/cartItems";
        Debug.Log("[DEBUG] Checking expired at " + cartItems);
        db.Child("REVIRA").Child(cartItems)
          .GetValueAsync().ContinueWithOnMainThread(expiredRead =>
          {
              Debug.Log("[DEBUG] expiredRead callback");
              if (expiredRead.Exception != null || !expiredRead.Result.Exists) return;

              long now = GetUnixTimestamp();
              var toRemove = new List<string>();

              foreach (var item in expiredRead.Result.Children)
              {
                  if (item.Child("expiresAt").Exists
                   && long.TryParse(item.Child("expiresAt").Value.ToString(), out var exp)
                   && exp < now)
                  {
                      toRemove.Add(item.Key);
                  }
              }

              foreach (var key in toRemove)
                  db.Child("REVIRA").Child(cartItems).Child(key).RemoveValueAsync();
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
        Debug.LogError("[DEBUG] Fail: " + msg);
        ShowError(msg);
        isAdding = false;
        addToCartButton.interactable = true;
    }

    private void ShowError(string m)
    {
        if (errorText == null) return;
        errorText.color = Color.red;
        errorText.text = m;
        errorText.gameObject.SetActive(true);
        CancelInvoke(nameof(ClearMessage));
        Invoke(nameof(ClearMessage), 5f);
        Debug.Log("[DEBUG] ShowError: " + m);
    }

    private void ShowSuccess(string m)
    {
        if (errorText == null) return;
        errorText.color = Color.green;
        errorText.text = m;
        errorText.gameObject.SetActive(true);
        CancelInvoke(nameof(ClearMessage));
        Invoke(nameof(ClearMessage), 3f);
        Debug.Log("[DEBUG] ShowSuccess: " + m);
    }

    private void ClearMessage()
    {
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }

    private long GetUnixTimestamp()
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
    }
}
