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
    private DatabaseReference db;
    public TMP_Dropdown sizeDropdown, colorDropdown, quantityDropdown;
    public Button addToCartButton;
    public TextMeshProUGUI errorText;

    private ProductsManager productsManager;
    private CartManager cartManager;

    private bool isAdding = false;

    void Start()
    {
        Debug.Log("[DEBUG] Start");

        db = FirebaseDatabase.DefaultInstance.RootReference;
        productsManager = FindObjectOfType<ProductsManager>();
        cartManager = FindObjectOfType<CartManager>();

        addToCartButton.onClick.RemoveAllListeners();
        addToCartButton.onClick.AddListener(AddToCart);

        colorDropdown?.onValueChanged.AddListener(_ => ValidateSelection());
        sizeDropdown?.onValueChanged.AddListener(_ => ValidateSelection());
        quantityDropdown?.onValueChanged.AddListener(_ => ValidateSelection());

        ClearMessage();

        if (!string.IsNullOrEmpty(UserManager.Instance.UserId))
            RemoveExpiredCartItems();
    }

    public void AddToCart()
    {
        Debug.Log("[DEBUG] AddToCart triggered");
        if (isAdding) return;

        if (!ValidateSelection()) return;

        string uid = UserManager.Instance.UserId;
        if (string.IsNullOrEmpty(uid))
        {
            ShowError("User not logged in.");
            Debug.LogError("[DEBUG] No UserId.");
            return;
        }

        var pd = productsManager.GetProductData();
        string pid = productsManager.productID;
        if (pd == null || string.IsNullOrEmpty(pid))
        {
            ShowError("Missing product data.");
            Debug.LogError("[DEBUG] No product data.");
            return;
        }

        string color = colorDropdown.options[colorDropdown.value].text;
        string size = sizeDropdown.options[sizeDropdown.value].text;
        int qty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        long exp = GetUnixTimestamp() + 86400;

        if (!productsManager.productColorsAndSizes.TryGetValue(color, out var sizesDict)
         || !sizesDict.TryGetValue(size, out var available)
         || available < qty)
        {
            ShowError("Not enough stock.");
            return;
        }

        isAdding = true;
        addToCartButton.interactable = false;

        string stockPath = $"REVIRA/stores/storeID_123/products/{pid}/colors/{color}/sizes/{size}";
        Debug.Log($"[DEBUG] Stock READ: {stockPath}");
        db.Child(stockPath).GetValueAsync().ContinueWithOnMainThread(stockRead =>
        {
            if (stockRead.Exception != null || !stockRead.Result.Exists)
            {
                Fail("Could not read stock.");
                return;
            }

            int current = int.Parse(stockRead.Result.Value.ToString());
            int updated = Mathf.Max(0, current - qty);

            Debug.Log($"[DEBUG] Stock WRITE: {stockPath} → {updated}");
            db.Child(stockPath).SetValueAsync(updated).ContinueWithOnMainThread(stockWrite =>
            {
                if (stockWrite.Exception != null)
                {
                    Fail("Could not update stock.");
                    return;
                }

                string cartSizePath = $"REVIRA/Consumers/{uid}/cart/cartItems/{pid}/sizes/{size}";
                Debug.Log($"[DEBUG] Cart READ: {cartSizePath}");
                db.Child(cartSizePath).GetValueAsync().ContinueWithOnMainThread(cartRead =>
                {
                    if (cartRead.Exception != null)
                    {
                        Fail("Could not read cart.");
                        return;
                    }

                    int existing = cartRead.Result.Exists
                                 ? int.Parse(cartRead.Result.Value.ToString())
                                 : 0;
                    int newQty = existing + qty;

                    var update = new Dictionary<string, object>()
                    {
                        {"productID",   pid},
                        {"productName", pd.name},
                        {"color",       color},
                        {"price",       pd.price},
                        {"timestamp",   GetUnixTimestamp()},
                        {"expiresAt",   exp},
                        {$"sizes/{size}", newQty}
                    };

                    string cartItemPath = $"REVIRA/Consumers/{uid}/cart/cartItems/{pid}";
                    Debug.Log($"[DEBUG] Cart WRITE: {cartItemPath}");
                    db.Child(cartItemPath).UpdateChildrenAsync(update)
                      .ContinueWithOnMainThread(cartWrite =>
                      {
                          if (cartWrite.Exception != null)
                          {
                              Fail("Could not update cart.");
                              return;
                          }

                          ShowSuccess("Added to cart");
                          UpdateCartSummary(uid, () =>
                          {
                              cartManager?.LoadCartItems();
                              isAdding = false;
                              addToCartButton.interactable = true;
                          });
                      });
                });
            });
        });
    }

    private void UpdateCartSummary(string uid, Action onDone)
    {
        Debug.Log("[DEBUG] Updating summary");
        string summaryPath = $"REVIRA/Consumers/{uid}/cart/cartItems";
        db.Child(summaryPath).GetValueAsync().ContinueWithOnMainThread(r =>
        {
            float totalP = 0f;
            int totalI = 0;
            if (r.Result.Exists)
            {
                foreach (var snap in r.Result.Children)
                {
                    float price = float.Parse(snap.Child("price").Value.ToString());
                    foreach (var sz in snap.Child("sizes").Children)
                    {
                        int q = int.Parse(sz.Value.ToString());
                        totalP += price * q;
                        totalI += q;
                    }
                }
            }

            var totals = new Dictionary<string, object>()
            {
                {"totalPrice", totalP},
                {"totalItems", totalI}
            };
            string totalPath = $"REVIRA/Consumers/{uid}/cart/cartTotal";
            db.Child(totalPath).SetValueAsync(totals)
              .ContinueWithOnMainThread(_ => onDone());
        });
    }

    private void RemoveExpiredCartItems()
    {
        Debug.Log("[DEBUG] Removing expired items");
        string uid = UserManager.Instance.UserId;
        string cartPath = $"REVIRA/Consumers/{uid}/cart/cartItems";
        db.Child(cartPath).GetValueAsync().ContinueWithOnMainThread(r =>
        {
            long now = GetUnixTimestamp();
            var toRemove = new List<string>();
            foreach (var snap in r.Result.Children)
            {
                if (snap.Child("expiresAt").Exists
                 && long.TryParse(snap.Child("expiresAt").Value.ToString(), out var e)
                 && e < now)
                {
                    toRemove.Add(snap.Key);
                }
            }
            foreach (var key in toRemove)
                db.Child($"{cartPath}/{key}").RemoveValueAsync();
        });
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

    private void Fail(string msg)
    {
        Debug.LogError("[DEBUG] FAIL: " + msg);
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
    }

    private void ShowSuccess(string m)
    {
        if (errorText == null) return;
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

    private long GetUnixTimestamp()
        => (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
}
