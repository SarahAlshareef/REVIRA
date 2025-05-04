using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProductCartManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown colorDropdown;
    public TMP_Dropdown sizeDropdown;
    public TMP_Dropdown quantityDropdown;
    public Button addToCartButton;
    public TextMeshProUGUI feedbackText; // Renamed from errorText for clarity

    private DatabaseReference _dbRoot;
    private bool _isAdding = false;

    // References to other manager scripts (using private fields found in Start)
    private ProductsManager productsManager;
    private UserManager userManager;
    private CartManager cartManager;


    void Start()
    {
        Debug.Log("[DEBUG] Start");

        // Get the root reference of the Firebase database
        _dbRoot = FirebaseDatabase.DefaultInstance.RootReference;
        Debug.Log($"[DEBUG] Firebase DB Root Reference: {_dbRoot?.ToString()}");

        // Find references to other required manager scripts in the scene
        productsManager = FindObjectOfType<ProductsManager>();
        if (productsManager == null) Debug.LogError("[DEBUG] ProductsManager not found in scene!");
        else Debug.Log("[DEBUG] ProductsManager found.");

        // Get the UserManager Instance directly
        userManager = UserManager.Instance; // Use the Singleton Instance
        if (userManager == null) Debug.LogError("[DEBUG] UserManager.Instance is null!");
        else Debug.Log("[DEBUG] UserManager Instance found.");

        cartManager = FindObjectOfType<CartManager>();
        if (cartManager == null) Debug.LogError("[DEBUG] CartManager not found in scene!");
        else Debug.Log("[DEBUG] CartManager found.");


        // Check if UI elements are assigned
        if (feedbackText == null) Debug.LogError("[DEBUG] FeedbackText (TextMeshProUGUI) not assigned!");
        if (addToCartButton == null) Debug.LogError("[DEBUG] AddToCartButton not assigned!");
        if (colorDropdown == null) Debug.LogError("[DEBUG] ColorDropdown not assigned!");
        if (sizeDropdown == null) Debug.LogError("[DEBUG] SizeDropdown not assigned!");
        if (quantityDropdown == null) Debug.LogError("[DEBUG] QuantityDropdown not assigned!");


        // Hook up button and dropdowns
        if (addToCartButton != null)
        {
            // --- IMPORTANT: Add RemoveAllListeners() to prevent double clicks if also set in Inspector ---
            addToCartButton.onClick.RemoveAllListeners();
            addToCartButton.onClick.AddListener(AddToCart);
            Debug.Log("[DEBUG] AddToCart listener added to button.");
        }


        if (colorDropdown != null) colorDropdown.onValueChanged.AddListener(_ => ValidateSelection());
        if (sizeDropdown != null) sizeDropdown.onValueChanged.AddListener(_ => ValidateSelection());
        if (quantityDropdown != null) quantityDropdown.onValueChanged.AddListener(_ => ValidateSelection());


        ClearFeedback(); // Use ClearFeedback for the renamed TextMeshProUGUI

        // Try remove expired items if user already logged in
        if (userManager != null && !string.IsNullOrEmpty(userManager.UserId)) // Check if userManager is not null before accessing UserId
        {
            Debug.Log("[DEBUG] UserId available in Start for RemoveExpiredCartItems. Checking for expired items.");
            RemoveExpiredCartItems();
        }
        else
        {
            Debug.LogWarning("[DEBUG] UserManager or UserId not available in Start for RemoveExpiredCartItems. Current UserId: " + (userManager?.UserId ?? "null UserManager or empty UserId"));
            // Consider calling RemoveExpiredCartItems after user login is confirmed if it fails here
        }
    }

    public void AddToCart()
    {
        Debug.Log("[DEBUG] AddToCart triggered");

        // Prevent multiple clicks
        Debug.Log("[DEBUG] _isAdding check. Value: " + _isAdding);
        if (_isAdding)
        {
            Debug.Log("[DEBUG] Already adding, skipping");
            return;
        }

        // Validate dropdown selections
        Debug.Log("[DEBUG] Before ValidateSelection()");
        if (!ValidateSelection())
        {
            Debug.Log("[DEBUG] Selection validation failed.");
            return;
        }
        Debug.Log("[DEBUG] After ValidateSelection(). Result: true");


        // --- Check for UserManager and User ID ---
        Debug.Log("[DEBUG] Checking UserManager and User ID availability.");
        if (userManager == null || string.IsNullOrEmpty(userManager.UserId))
        {
            ShowFeedback("User not logged in or data not loaded.", true); // Use ShowFeedback
            Debug.LogError("[DEBUG] UserManager or UserId missing. UserManager == null: " + (userManager == null) + ", UserId empty: " + string.IsNullOrEmpty(userManager?.UserId));
            Finish(); // Ensure Finish is called on failure
            return;
        }
        string userId = userManager.UserId;
        Debug.Log($"[DEBUG] Retrieved User ID: {userId}");
        // --- End Check ---


        // --- Check for ProductsManager and Product Data ---
        Debug.Log("[DEBUG] Checking ProductsManager and product data.");
        if (productsManager == null)
        {
            ShowFeedback("Product manager not available.", true);
            Debug.LogError("[DEBUG] ProductsManager is null when AddToCart is called.");
            Finish();
            return;
        }
        var pd = productsManager.GetProductData();
        string pid = productsManager.productID;
        if (pd == null || string.IsNullOrEmpty(pid))
        {
            ShowFeedback("Missing product data.", true); // Use ShowFeedback
            Debug.LogError($"[DEBUG] Missing product data. pd==null:{pd == null}, pid empty:{string.IsNullOrEmpty(pid)}");
            Finish(); // Ensure Finish is called on failure
            return;
        }
        Debug.Log($"[DEBUG] Product ID: {pid}, Name: {pd.name}, Price: {pd.price}");
        // --- End Check ---


        string color = colorDropdown.options[colorDropdown.value].text;
        string size = sizeDropdown.options[sizeDropdown.value].text;
        int qty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        long expires = GetUnixTimestamp() + 86400; // Item expires in 24 hours
        Debug.Log($"[DEBUG] Selected: Color={color}, Size={size}, Quantity={qty}, Expires={expires}");


        // Local stock check
        Debug.Log("[DEBUG] Performing local stock check.");
        if (!productsManager.productColorsAndSizes.TryGetValue(color, out var sizesDict))
        {
            ShowFeedback("Color not available.", true); // Use ShowFeedback
            Debug.LogError($"[DEBUG] Color '{color}' not found in productColorsAndSizes.");
            Finish();
            return;
        }
        if (!sizesDict.TryGetValue(size, out var inStock) || inStock < qty)
        {
            ShowFeedback("Not enough stock.", true); // Use ShowFeedback
            if (!sizesDict.ContainsKey(size)) Debug.LogError($"[DEBUG] Size '{size}' not found for color '{color}'.");
            else Debug.LogError($"[DEBUG] Not enough stock. Requested: {qty}, Available: {inStock}");
            Finish();
            return;
        }
        Debug.Log($"[DEBUG] Local stock OK: {inStock} available.");


        // Throttle
        _isAdding = true;
        if (addToCartButton != null) addToCartButton.interactable = false;
        Debug.Log("[DEBUG] _isAdding set to true, button disabled.");


        // Step 1: decrement stock in Firebase
        string stockPath = $"REVIRA/stores/storeID_123/products/{pid}/colors/{color}/sizes/{size}";
        Debug.Log($"[DEBUG] Reading stock from Firebase path: {stockPath}");
        _dbRoot.Child("REVIRA").Child("stores").Child("storeID_123") // Use chained Child calls for clarity and safety
               .Child("products").Child(pid)
               .Child("colors").Child(color)
               .Child("sizes").Child(size)
               .GetValueAsync().ContinueWithOnMainThread(stockReadTask =>
               {
                   Debug.Log("[DEBUG] Stock READ task completed.");
                   if (stockReadTask.Exception != null)
                   {
                       Debug.LogError("[DEBUG] Stock READ failed: " + stockReadTask.Exception); // Log the exception
                       Fail("Could not read stock."); // Use Fail method
                       return;
                   }
                   if (!stockReadTask.Result.Exists)
                   {
                       Debug.LogError("[DEBUG] Stock data does not exist at path: " + stockPath);
                       Fail("Stock data not found.");
                       return;
                   }


                   int currentStock = int.Parse(stockReadTask.Result.Value.ToString());
                   int newStock = Mathf.Max(0, currentStock - qty);
                   Debug.Log($"[DEBUG] Current Stock: {currentStock}, New Stock: {newStock}");

                   Debug.Log($"[DEBUG] Writing new stock ({newStock}) to Firebase path: {stockPath}");
                   _dbRoot.Child("REVIRA").Child("stores").Child("storeID_123") // Use chained Child calls
                          .Child("products").Child(pid)
                          .Child("colors").Child(color)
                          .Child("sizes").Child(size)
                          .SetValueAsync(newStock).ContinueWithOnMainThread(stockWriteTask =>
                          {
                              Debug.Log("[DEBUG] Stock WRITE task completed.");
                              if (stockWriteTask.Exception != null)
                              {
                                  Debug.LogError("[DEBUG] Stock WRITE failed: " + stockWriteTask.Exception); // Log the exception
                                  Fail("Could not update stock."); // Use Fail method
                                  return;
                              }
                              Debug.Log("[DEBUG] Stock updated successfully in Firebase.");


                              // Step 2: read existing cart qty
                              string cartItemSizePath = $"REVIRA/Consumers/{userId}/cart/cartItems/{pid}/sizes/{size}";
                              Debug.Log($"[DEBUG] Reading existing cart quantity from Firebase path: {cartItemSizePath}");
                              _dbRoot.Child("REVIRA").Child("Consumers").Child(userId) // Use chained Child calls
                              .Child("cart").Child("cartItems").Child(pid)
                              .Child("sizes").Child(size)
                              .GetValueAsync().ContinueWithOnMainThread(cartReadTask =>
                              {
                                  Debug.Log("[DEBUG] Cart READ task completed.");
                                  if (cartReadTask.Exception != null)
                                  {
                                      Debug.LogError("[DEBUG] Cart READ failed: " + cartReadTask.Exception); // Log the exception
                                      Fail("Could not read cart."); // Use Fail method
                                      return;
                                  }

                                  int existingQty = cartReadTask.Result.Exists
                               ? int.Parse(cartReadTask.Result.Value.ToString())
                               : 0;
                                  int updatedQty = existingQty + qty;
                                  Debug.Log($"[DEBUG] Existing cart quantity for size '{size}': {existingQty}, New total quantity: {updatedQty}");


                                  // Step 3: update cart entry
                                  var updateMap = new Dictionary<string, object>()
                           {
                        { "productID", pid },
                        { "productName", pd.name },
                        { "color", color },
                        { "price", pd.price },
                        { "timestamp", GetUnixTimestamp() },
                        { "expiresAt", expires },
                        { $"sizes/{size}", updatedQty }
                           };
                                  Debug.Log($"[DEBUG] Preparing cart update data for product ID: {pid}");

                                  string cartItemPath = $"REVIRA/Consumers/{userId}/cart/cartItems/{pid}";
                                  Debug.Log($"[DEBUG] Writing cart entry to Firebase path: {cartItemPath}");
                                  _dbRoot.Child("REVIRA").Child("Consumers").Child(userId) // Use chained Child calls
                                  .Child("cart").Child("cartItems").Child(pid)
                                  .UpdateChildrenAsync(updateMap).ContinueWithOnMainThread(cartWriteTask =>
                                  {
                                      Debug.Log("[DEBUG] Cart WRITE task completed.");
                                      if (cartWriteTask.Exception != null)
                                      {
                                          Debug.LogError("[DEBUG] Cart WRITE failed: " + cartWriteTask.Exception); // Log the exception
                                          Fail("Could not update cart."); // Use Fail method
                                                                          // This is a critical failure - stock was reduced but cart wasn't updated.
                                                                          // You should implement robust error handling and potential rollback/compensation logic here.
                                          return;
                                      }
                                      Debug.Log("[DEBUG] Cart entry updated successfully in Firebase.");


                                      ShowSuccess("Added to cart"); // Use ShowSuccess

                                      // Step 4: update cart summary
                                      UpdateCartSummary(userId, () =>
                                      {
                                          Debug.Log("[DEBUG] Cart summary update finished.");
                                          cartManager?.LoadCartItems(); // Use null conditional operator
                                      });

                                      // reset throttle - Moved to the end of the success path
                                      // Finish(); // Finish is called at the end of the UpdateCartSummary task now
                                  });
                              });
                          });
               });
    }

    private void UpdateCartSummary(string userId, Action onDone)
    {
        Debug.Log("[DEBUG] Updating cart summary for user: " + userId);
        string summaryPath = $"REVIRA/Consumers/{userId}/cart/cartItems";
        Debug.Log($"[DEBUG] Reading cart items for summary from path: {summaryPath}");
        _dbRoot.Child("REVIRA").Child("Consumers").Child(userId) // Use chained Child calls
               .Child("cart").Child("cartItems")
               .GetValueAsync().ContinueWithOnMainThread(summaryReadTask =>
               {
                   Debug.Log("[DEBUG] Cart items READ for summary task completed.");
                   if (summaryReadTask.Exception != null)
                   {
                       Debug.LogError("[DEBUG] Summary READ failed: " + summaryReadTask.Exception); // Log the exception
                       ShowFeedback("Summary read failed", true); // Use ShowFeedback
                       onDone?.Invoke(); // Call the completion callback even on failure
                       Finish(); // Ensure Finish is called on failure
                       return;
                   }

                   int totalItems = 0;
                   float totalPrice = 0f;

                   if (summaryReadTask.Result.Exists)
                   {
                       Debug.Log("[DEBUG] Cart items found for summary calculation.");
                       foreach (var item in summaryReadTask.Result.Children)
                       {
                           Debug.Log($"[DEBUG] Processing item for summary: {item.Key}");
                           float price = 0f;
                           if (item.Child("price").Value != null) // Check for null value before ToString and Parse
                           {
                               if (!float.TryParse(item.Child("price").Value.ToString(), out price))
                               {
                                   Debug.LogWarning($"[DEBUG] Could not parse price for item '{item.Key}'. Value: {item.Child("price").Value}");
                               }
                           }
                           Debug.Log($"[DEBUG] Item '{item.Key}' price: {price}");


                           if (item.Child("sizes").Exists)
                           {
                               Debug.Log($"[DEBUG] Item '{item.Key}' has sizes node.");
                               foreach (var sizeSnap in item.Child("sizes").Children)
                               {
                                   int qty = 0;
                                   if (sizeSnap.Value != null) // Check for null value before ToString and Parse
                                   {
                                       if (!int.TryParse(sizeSnap.Value.ToString(), out qty))
                                       {
                                           Debug.LogWarning($"[DEBUG] Could not parse quantity for size '{sizeSnap.Key}' in item '{item.Key}'. Value: {sizeSnap.Value}");
                                       }
                                   }
                                   Debug.Log($"[DEBUG] - Size: {sizeSnap.Key}, Quantity: {qty}");
                                   totalItems += qty;
                                   totalPrice += price * qty;
                               }
                           }
                           else
                           {
                               Debug.LogWarning($"[DEBUG] Item '{item.Key}' is missing 'sizes' node.");
                           }
                       }
                       Debug.Log($"[DEBUG] Calculated Total Price: {totalPrice}, Total Items: {totalItems}");
                   }
                   else
                   {
                       Debug.Log("[DEBUG] No cart items found for summary calculation. Totals are 0.");
                   }

                   var cartTotalPath = $"REVIRA/Consumers/{userId}/cart/cartTotal";
                   var totals = new Dictionary<string, object>()
            {
                {"totalItems", totalItems},
                {"totalPrice", totalPrice}
            };
                   Debug.Log($"[DEBUG] Writing cart total summary to Firebase path: {cartTotalPath}");
                   _dbRoot.Child("REVIRA").Child("Consumers").Child(userId) // Use chained Child calls
                          .Child("cart").Child("cartTotal")
                          .SetValueAsync(totals).ContinueWithOnMainThread(cartTotalWriteTask => // Renamed task for clarity
                          {
                              Debug.Log("[DEBUG] Cart total summary WRITE task completed.");
                              if (cartTotalWriteTask.Exception != null) // Check for errors
                              {
                                  Debug.LogError("[DEBUG] Cart total summary WRITE failed: " + cartTotalWriteTask.Exception); // Log the exception
                                  ShowFeedback("Summary update failed", true); // Use ShowFeedback
                              }
                              else
                              {
                                  Debug.Log("[DEBUG] Cart total summary updated successfully.");
                              }

                              onDone?.Invoke(); // Call the completion callback
                              Finish(); // Call Finish after the entire process is done
                          });
               });
    }

    private void RemoveExpiredCartItems()
    {
        Debug.Log("[DEBUG] Checking for expired cart items.");
        string uid = userManager?.UserId; // Use null conditional operator

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("[DEBUG] User ID is null or empty. Cannot check for expired items.");
            return;
        }
        Debug.Log($"[DEBUG] User ID available for expired items check: {uid}");


        var refCart = _dbRoot.Child("REVIRA").Child("Consumers") // Use chained Child calls
                           .Child(uid)
                           .Child("cart").Child("cartItems");
        Debug.Log($"[DEBUG] Reading cart items for expiration check from path: {refCart.ToString()}");

        refCart.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            Debug.Log("[DEBUG] Read cart items for expiration check task completed.");
            if (task.Exception != null) // Check for errors
            {
                Debug.LogError("[DEBUG] Error reading cart items for expiration check: " + task.Exception); // Log the exception
                return; // Stop if read failed
            }
            if (!task.Result.Exists)
            {
                Debug.Log("[DEBUG] No cart items found to check for expiration.");
                return; // Stop if no items exist
            }

            long now = GetUnixTimestamp();
            var toRemove = new List<string>();
            Debug.Log($"[DEBUG] Current timestamp: {now}");

            foreach (var item in task.Result.Children)
            {
                Debug.Log($"[DEBUG] Checking item '{item.Key}' for expiration.");
                if (item.Child("expiresAt").Exists) // Check if expiresAt node exists
                {
                    if (long.TryParse(item.Child("expiresAt").Value?.ToString(), out var exp)) // Use null conditional and TryParse
                    {
                        Debug.Log($"[DEBUG] Item '{item.Key}' expires at: {exp}");
                        if (exp < now)
                        {
                            Debug.Log($"[DEBUG] Item '{item.Key}' has expired. Marking for removal.");
                            toRemove.Add(item.Key);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[DEBUG] Could not parse 'expiresAt' for item '{item.Key}'. Value: {item.Child("expiresAt").Value}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[DEBUG] Item '{item.Key}' is missing 'expiresAt' node.");
                }
            }

            if (toRemove.Count > 0)
            {
                Debug.Log($"[DEBUG] Removing {toRemove.Count} expired cart items.");
                foreach (var key in toRemove)
                {
                    Debug.Log($"[DEBUG] Removing item with key: {key}");
                    // Use chained Child calls for removal path
                    _dbRoot.Child("REVIRA").Child("Consumers").Child(uid)
                           .Child("cart").Child("cartItems").Child(key)
                           .RemoveValueAsync().ContinueWithOnMainThread(removeTask => // Add ContinueWithOnMainThread for removal task
                           {
                               if (removeTask.Exception != null)
                               {
                                   Debug.LogError($"[DEBUG] Failed to remove item '{key}': " + removeTask.Exception); // Log the exception
                               }
                               else
                               {
                                   Debug.Log($"[DEBUG] Item '{key}' removed successfully.");
                               }
                           });
                }
                // After removing items, update the cart summary
                UpdateCartSummary(uid, () => cartManager?.LoadCartItems()); // Use null conditional operator
            }
            else
            {
                Debug.Log("[DEBUG] No expired cart items found.");
            }
        });
    }

    private bool ValidateSelection()
    {
        Debug.Log("[DEBUG] Validating selections.");
        // Add null checks for dropdowns before accessing options
        if (colorDropdown == null || sizeDropdown == null || quantityDropdown == null)
        {
            Debug.LogError("[DEBUG] Dropdown references are not assigned during validation!");
            ShowFeedback("UI elements not assigned.", true);
            if (addToCartButton != null) addToCartButton.interactable = false; // Disable button if UI is missing
            return false;
        }


        if (colorDropdown.options.Count == 0
           || sizeDropdown.options.Count == 0
           || quantityDropdown.options.Count == 0
           || colorDropdown.options[colorDropdown.value].text == "Select Color"
           || sizeDropdown.options[sizeDropdown.value].text == "Select Size"
           || quantityDropdown.options[quantityDropdown.value].text == "Select Quantity")
        {
            ShowFeedback("Please make all selections.", true); // Use ShowFeedback
            Debug.Log("[DEBUG] Validation failed: Not all dropdowns selected.");
            if (addToCartButton != null) addToCartButton.interactable = false; // Disable button if validation fails
            return false;
        }
        Debug.Log("[DEBUG] Validation successful.");
        ClearFeedback(); // Use ClearFeedback
        if (!_isAdding && addToCartButton != null) addToCartButton.interactable = true; // Use _isAdding
        return true;
    }

    private void Fail(string msg)
    {
        Debug.LogError("[DEBUG] FAIL: " + msg); // Keep debug log
        ShowFeedback(msg, true); // Use ShowFeedback for user
        Finish(); // Use Finish method
    }

    private void ShowFeedback(string message, bool isError) // Renamed from ShowError
    {
        if (feedbackText == null) // Use feedbackText
        {
            Debug.LogError("[DEBUG] FeedbackText is null. Cannot show message: " + message);
            return;
        }
        feedbackText.color = isError ? Color.red : Color.green;
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
        CancelInvoke(nameof(ClearFeedback)); // Use ClearFeedback
        Invoke(nameof(ClearFeedback), 3f); // Schedule clearing after 3 seconds for success, 5 for error? Or just 3 for simplicity. Let's stick to 3.
        Debug.Log($"[DEBUG] Showing Feedback ({(isError ? "Error" : "Success")}): {message}");
    }

    private void ShowSuccess(string m) // Added ShowSuccess for clarity
    {
        ShowFeedback(m, false);
    }


    private void ClearFeedback() // Renamed from ClearMessage
    {
        if (feedbackText != null) // Use feedbackText
        {
            feedbackText.gameObject.SetActive(false);
            Debug.Log("[DEBUG] Feedback message cleared.");
        }
    }

    private long GetUnixTimestamp()
    {
        long timestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        // Debug.Log($"[DEBUG] Generated Unix Timestamp: {timestamp}"); // Log only if needed, can be noisy
        return timestamp;
    }

    private void Finish() // Centralized method to reset state
    {
        Debug.Log("[DEBUG] Finish called. Resetting state.");
        _isAdding = false;
        if (addToCartButton != null) addToCartButton.interactable = true;
    }
}
