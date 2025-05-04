using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions; // Required for ContinueWithOnMainThread
using TMPro;
using System; // Required for DateTime

public class ProductCartManager : MonoBehaviour
{
    // Reference to the Firebase Database root
    private DatabaseReference db;

    // UI elements for product selection
    public TMP_Dropdown sizeDropdown, colorDropdown, quantityDropdown;
    public Button addToCartButton;
    public TextMeshProUGUI errorText; // Used for showing errors and success messages

    // References to other manager scripts
    private ProductsManager productsManager;
    private UserManager userManager; // Keep this reference
    private CartManager cartManager;

    // Flag to prevent multiple clicks while adding
    private bool isAdding = false;

    void Start()
    {
        Debug.Log("[DEBUG] Start");

        // Get the root reference of the Firebase database
        // Ensure Firebase is initialized elsewhere in your project before this runs
        db = FirebaseDatabase.DefaultInstance.RootReference;
        Debug.Log($"[DEBUG] Firebase DB Root Reference: {db?.ToString()}");

        // Find references to other required manager scripts in the scene
        productsManager = FindObjectOfType<ProductsManager>();
        // Get the UserManager Instance directly
        userManager = UserManager.Instance; // Use the Singleton Instance
        cartManager = FindObjectOfType<CartManager>();

        // Check if managers were found/assigned
        if (productsManager == null) Debug.LogError("[DEBUG] ProductsManager not found in scene!");
        if (userManager == null) Debug.LogError("[DEBUG] UserManager.Instance is null!"); // Specific log for UserManager
        if (cartManager == null) Debug.LogError("[DEBUG] CartManager not found in scene!");
        if (errorText == null) Debug.LogError("[DEBUG] ErrorText (TextMeshProUGUI) not assigned!");
        if (addToCartButton == null) Debug.LogError("[DEBUG] AddToCartButton not assigned!");
        if (colorDropdown == null) Debug.LogError("[DEBUG] ColorDropdown not assigned!");
        if (sizeDropdown == null) Debug.LogError("[DEBUG] SizeDropdown not assigned!");
        if (quantityDropdown == null) Debug.LogError("[DEBUG] QuantityDropdown not assigned!");


        // --- Removed programmatic listener addition ---
        // The Add to Cart button's OnClick event should be set up manually in the Unity Inspector.
        // If you need to add listeners programmatically, use AddListener here,
        // potentially after removing existing ones with RemoveAllListeners().
        // Example if you were to add it programmatically:
        // if (addToCartButton != null)
        // {
        //     addToCartButton.onClick.RemoveAllListeners(); // Optional: clear existing listeners
        //     addToCartButton.onClick.AddListener(AddToCart);
        //     Debug.Log("[DEBUG] AddToCart listener added to button.");
        // }
        // --- End Removed Code ---


        // Add listeners to dropdowns to validate selections
        if (colorDropdown != null) colorDropdown.onValueChanged.AddListener(_ => ValidateSelection());
        if (sizeDropdown != null) sizeDropdown.onValueChanged.AddListener(_ => ValidateSelection());
        if (quantityDropdown != null) quantityDropdown.onValueChanged.AddListener(_ => ValidateSelection());

        // Initially clear any error message
        ClearMessage();

        // Attempt to remove expired items on start (requires userManager to be initialized)
        // This might not work if userManager isn't ready immediately in Start
        // Consider calling this after user login is confirmed if it fails here
        if (userManager != null && !string.IsNullOrEmpty(userManager.UserId))
        {
            Debug.Log("[DEBUG] UserManager and UserId available in Start for RemoveExpiredCartitems.");
            RemoveExpiredCartItems();
        }
        else
        {
            Debug.LogWarning("[DEBUG] UserManager or UserId not available in Start for RemoveExpiredCartitems. Current UserId: " + (userManager?.UserId ?? "null UserManager or empty UserId"));
            // You might want to call RemoveExpiredCartItems after user login is confirmed
        }
    }

    // Main function to add the selected product to the user's cart
    public void AddToCart()
    {
        Debug.Log("[DEBUG] AddToCart triggered"); // This is the first log that should appear if the button works

        // Prevent multiple clicks
        Debug.Log("[DEBUG] isAdding check. Value: " + isAdding);
        if (isAdding)
        {
            Debug.Log("[DEBUG] Already adding, skipping");
            return;
        }

        // Validate dropdown selections
        Debug.Log("[DEBUG] Before ValidateSelection()");
        bool selectionValid = ValidateSelection();
        Debug.Log("[DEBUG] After ValidateSelection(). Result: " + selectionValid);

        if (!selectionValid)
        {
            Debug.Log("[DEBUG] Selection validation failed.");
            return;
        }

        // --- Enhanced Check for UserManager and User ID ---
        Debug.Log("[DEBUG] Checking UserManager and User ID availability.");
        if (UserManager.Instance == null) // Check the Singleton instance directly
        {
            ShowError("User manager not available.");
            Debug.LogError("[DEBUG] UserManager.Instance is null when AddToCart is called!");
            return; // Stop if UserManager is not found
        }

        string userId = UserManager.Instance.UserId; // Get the User ID from the Singleton instance
        Debug.Log($"[DEBUG] Retrieved User ID from UserManager.Instance: {userId}");

        if (string.IsNullOrEmpty(userId))
        {
            ShowError("User not logged in or data not loaded.");
            Debug.LogError("[DEBUG] User ID is null or empty after getting from UserManager.Instance. Cannot add to cart.");
            return; // Stop if user is not logged in or ID is not set
        }
        // --- End Enhanced Check ---


        Debug.Log($"[DEBUG] User ID: {userId}");


        // Get product data from ProductsManager
        var pd = productsManager?.GetProductData();
        string pid = productsManager?.productID;

        if (pd == null || string.IsNullOrEmpty(pid))
        {
            ShowError("Missing product data.");
            Debug.LogError($"[DEBUG] Missing product data. pd == null: {pd == null}, pid is null or empty: {string.IsNullOrEmpty(pid)}");
            return; // Stop if product data is missing
        }
        Debug.Log($"[DEBUG] Product ID: {pid}, Product Name: {pd.name}, Price: {pd.price}");


        // Get selected color, size, and quantity from dropdowns
        string color = colorDropdown.options[colorDropdown.value].text;
        string size = sizeDropdown.options[sizeDropdown.value].text;
        int qty = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        long expires = GetUnixTimestamp() + 86400; // Item expires in 24 hours

        Debug.Log($"[DEBUG] Selected: Color={color}, Size={size}, Quantity={qty}");


        // Check if enough stock is available locally (based on ProductsManager data)
        // This is a client-side check; a server-side check is recommended for critical applications
        // Corrected logic to avoid using 'sizesDict' and 'inStock' if TryGetValue fails
        int inStock = 0; // Initialize inStock to a default value
        Dictionary<string, int> sizesDict = null; // Initialize sizesDict to null

        // Check if productsManager is available and if the color exists
        if (productsManager == null)
        {
            Debug.LogError("[DEBUG] ProductsManager is null during stock check (color check).");
            ShowError("Product manager not available.");
            return;
        }

        if (!productsManager.productColorsAndSizes.TryGetValue(color, out sizesDict))
        {
            ShowError("Color not available for this product.");
            Debug.LogError($"[DEBUG] Color '{color}' not found in productColorsAndSizes.");
            return; // Stop if color is not found
        }

        // Now that we know sizesDict is assigned, check if the size exists and if stock is sufficient
        if (!sizesDict.TryGetValue(size, out inStock) || inStock < qty)
        {
            ShowError("Not enough stock or size not available.");
            if (!sizesDict.ContainsKey(size)) Debug.LogError($"[DEBUG] Size '{size}' not found for color '{color}'.");
            else Debug.LogError($"[DEBUG] Not enough stock. Requested: {qty}, Available: {inStock}");
            return; // Stop if size not found or not enough stock
        }

        Debug.Log($"[DEBUG] Local stock check passed. Available: {inStock}");


        // Throttle further clicks
        isAdding = true;
        if (addToCartButton != null) addToCartButton.interactable = false;
        Debug.Log("[DEBUG] isAdding set to true, button disabled.");


        // --- STEP 1: Reduce stock in Firebase ---
        // Construct the database path for the specific product size stock
        string stockPath = $"REVIRA/stores/storeID_123/products/{pid}/colors/{color}/sizes/{size}";
        Debug.Log($"[DEBUG] Reading stock from Firebase path: {stockPath}");

        db.Child("REVIRA").Child("stores").Child("storeID_123")
          .Child("products").Child(pid)
          .Child("colors").Child(color)
          .Child("sizes").Child(size)
          .GetValueAsync().ContinueWithOnMainThread(stockReadTask =>
          {
              Debug.Log("[DEBUG] Stock READ task completed.");

              // Check for errors during stock read
              if (stockReadTask.Exception != null)
              {
                  Debug.LogError("[DEBUG] Stock READ failed: " + stockReadTask.Exception);
                  Fail("Could not read stock.");
                  return;
              }

              // Check if the stock data exists
              if (!stockReadTask.Result.Exists)
              {
                  Debug.LogError("[DEBUG] Stock data does not exist at path: " + stockPath);
                  Fail("Stock data not found.");
                  return;
              }


              // Parse current stock and calculate new stock
              int currentStock = int.Parse(stockReadTask.Result.Value.ToString());
              int newStock = Mathf.Max(0, currentStock - qty); // Ensure stock doesn't go below zero
              Debug.Log($"[DEBUG] Current Stock: {currentStock}, New Stock: {newStock}");


              // Write the updated stock value back to Firebase
              Debug.Log($"[DEBUG] Writing new stock ({newStock}) to Firebase path: {stockPath}");
              db.Child("REVIRA").Child("stores").Child("storeID_123")
                .Child("products").Child(pid)
                .Child("colors").Child(color)
                .Child("sizes").Child(size)
                .SetValueAsync(newStock).ContinueWithOnMainThread(stockWriteTask =>
                {
                    Debug.Log("[DEBUG] Stock WRITE task completed.");

                    // Check for errors during stock write
                    if (stockWriteTask.Exception != null)
                    {
                        Debug.LogError("[DEBUG] Stock WRITE failed: " + stockWriteTask.Exception);
                        Fail("Could not update stock.");
                        // Consider rolling back the local stock change here if the write failed
                        return;
                    }

                    Debug.Log("[DEBUG] Stock updated successfully in Firebase.");


                    // --- STEP 2: Read existing cart quantity for this size ---
                    // Construct the database path for the existing item in the cart
                    string cartItemSizePath = $"REVIRA/Consumers/{userId}/cart/cartItems/{pid}/sizes/{size}";

                    // ADD THIS DEBUG LOG TO SEE THE CONSTRUCTED PATH AGAIN
                    Debug.Log($"[DEBUG] Constructed Cart Item Size Path for Read: {cartItemSizePath}");


                    Debug.Log($"[DEBUG] Reading existing cart quantity from Firebase path: {cartItemSizePath}");

                    db.Child("REVIRA").Child("Consumers").Child(userId)
                    .Child("cart").Child("cartItems").Child(pid)
                    .Child("sizes").Child(size)
                    .GetValueAsync().ContinueWithOnMainThread(cartReadTask =>
                    {
                        Debug.Log("[DEBUG] Cart READ task completed.");

                        // Check for errors during cart read
                        if (cartReadTask.Exception != null)
                        {
                            Debug.LogError("[DEBUG] Cart READ failed: " + cartReadTask.Exception);
                            Fail("Could not read cart.");
                            // Consider rolling back the stock change here as well
                            return;
                        }

                        // Determine the existing quantity of this size in the cart
                        int existingQty = cartReadTask.Result.Exists
                            ? int.Parse(cartReadTask.Result.Value.ToString())
                            : 0; // If data doesn't exist, existing quantity is 0

                        int updatedQty = existingQty + qty;
                        Debug.Log($"[DEBUG] Existing cart quantity for size '{size}': {existingQty}, New total quantity: {updatedQty}");


                        // --- STEP 3: Write updated cart entry ---
                        // Prepare the data to update/set in the cart
                        // Using UpdateChildrenAsync allows updating specific fields without overwriting the whole cart item
                        var updateMap = new Dictionary<string, object>()
                        {
                            // Include product details (these might already exist, but good to ensure they are present)
                            { "productID",      pid },
                            { "productName",    pd.name },
                            { "color",          color }, // Note: This will overwrite the existing color if the user adds the same product ID with a different color. Consider a different structure if multiple colors per product ID are needed in cartItems.
                            { "price",          pd.price }, // Note: This will overwrite the existing price. Consider storing original price or handling price changes.
                            { "timestamp",      GetUnixTimestamp() }, // Timestamp of the last update
                            { "expiresAt",      expires }, // Expiration timestamp

                            // Update the quantity for the specific size
                            { $"sizes/{size}", updatedQty } // Firebase uses / for nested paths in UpdateChildrenAsync keys
                        };
                        Debug.Log($"[DEBUG] Preparing cart update data for product ID: {pid}");


                        // Construct the database path for the cart item update
                        string cartItemPath = $"REVIRA/Consumers/{userId}/cart/cartItems/{pid}";

                        // ADD THIS DEBUG LOG TO SEE THE CONSTRUCTED PATH AGAIN
                        Debug.Log($"[DEBUG] Constructed Cart Item Path for Write: {cartItemPath}");


                        Debug.Log($"[DEBUG] Writing cart entry to Firebase path: {cartItemPath}");


                        db.Child("REVIRA").Child("Consumers").Child(userId)
                        .Child("cart").Child("cartItems").Child(pid)
                        .UpdateChildrenAsync(updateMap) // Use UpdateChildrenAsync to update specific fields
                        .ContinueWithOnMainThread(cartWriteTask =>
                        {
                            Debug.Log("[DEBUG] Cart WRITE task completed.");

                            // Check for errors during cart write
                            if (cartWriteTask.Exception != null)
                            {
                                Debug.LogError("[DEBUG] Cart WRITE failed: " + cartWriteTask.Exception);
                                Fail("Could not update cart.");
                                // This is a critical failure - stock was reduced but cart wasn't updated.
                                // You should implement robust error handling and potential rollback/compensation logic here.
                                return;
                            }

                            Debug.Log("[DEBUG] Cart entry updated successfully in Firebase.");


                            // --- STEP 4: Success! Refresh UI & summary ---
                            ShowSuccess("Added to cart");

                            // Update the overall cart summary (total items and total price)
                            // This is done after the cart item is successfully added/updated
                            UpdateCartSummary(userId, () =>
                            {
                                Debug.Log("[DEBUG] Cart summary update finished.");
                                // Optionally, load cart items into UI after summary is updated
                                cartManager?.LoadCartItems();
                            });


                            // Reset the adding flag and re-enable the button
                            isAdding = false;
                            if (addToCartButton != null) addToCartButton.interactable = true;
                            Debug.Log("[DEBUG] isAdding set to false, button enabled.");
                        });
                    });
                });
          });
    }

    // Updates the total number of items and total price in the cart summary
    private void UpdateCartSummary(string userId, System.Action onDone)
    {
        Debug.Log("[DEBUG] Updating cart summary for user: " + userId);

        // Read all items currently in the cart
        db.Child("REVIRA").Child("Consumers").Child(userId)
          .Child("cart").Child("cartItems")
          .GetValueAsync().ContinueWithOnMainThread(summaryReadTask =>
          {
              Debug.Log("[DEBUG] Cart items READ for summary task completed.");

              // Check for errors during summary read
              if (summaryReadTask.Exception != null)
              {
                  Debug.LogError("[DEBUG] Summary READ failed: " + summaryReadTask.Exception);
                  onDone?.Invoke(); // Call the completion callback even on failure
                  return;
              }

              float totalP = 0f; // Total price
              int totalI = 0;  // Total number of items

              // If cart items exist, iterate through them to calculate totals
              if (summaryReadTask.Result.Exists)
              {
                  Debug.Log("[DEBUG] Cart items found for summary calculation.");
                  foreach (var itemSnapshot in summaryReadTask.Result.Children)
                  {
                      // Get the price of the product
                      float price = 0f;
                      if (itemSnapshot.Child("price").Value != null)
                      {
                          float.TryParse(itemSnapshot.Child("price").Value.ToString(), out price);
                      }
                      Debug.Log($"[DEBUG] Processing item: {itemSnapshot.Key}, Price: {price}");

                      // Iterate through the sizes of the product to sum quantities
                      if (itemSnapshot.Child("sizes").Exists)
                      {
                          foreach (var sizeSnapshot in itemSnapshot.Child("sizes").Children)
                          {
                              int quantity = 0;
                              if (sizeSnapshot.Value != null)
                              {
                                  int.TryParse(sizeSnapshot.Value.ToString(), out quantity);
                              }
                              Debug.Log($"[DEBUG] - Size: {sizeSnapshot.Key}, Quantity: {quantity}");
                              totalP += price * quantity; // Add to total price
                              totalI += quantity;      // Add to total item count
                          }
                      }
                      else
                      {
                          Debug.LogWarning($"[DEBUG] Item {itemSnapshot.Key} has no 'sizes' node.");
                      }
                  }
                  Debug.Log($"[DEBUG] Calculated Total Price: {totalP}, Total Items: {totalI}");
              }
              else
              {
                  Debug.Log("[DEBUG] No cart items found for summary calculation. Totals are 0.");
              }


              // Prepare the data for the cart total summary node
              var cartTotal = new Dictionary<string, object>()
              {
                  { "totalPrice", totalP },
                  { "totalItems", totalI }
              };

              // Write the calculated totals to the cartTotal node
              Debug.Log($"[DEBUG] Writing cart total summary to Firebase.");
              db.Child("REVIRA").Child("Consumers").Child(userId)
                .Child("cart").Child("cartTotal")
                .SetValueAsync(cartTotal) // SetValueAsync is appropriate here as it's a single node
                .ContinueWithOnMainThread(cartTotalWriteTask =>
                {
                    Debug.Log("[DEBUG] Cart total summary WRITE task completed.");

                    // Check for errors during cart total write
                    if (cartTotalWriteTask.Exception != null)
                    {
                        Debug.LogError("[DEBUG] Cart total summary WRITE failed: " + cartTotalWriteTask.Exception);
                    }

                    Debug.Log("[DEBUG] Cart summary updated.");
                    onDone?.Invoke(); // Call the completion callback
                });
          });
    }

    // Removes cart items that have expired based on the 'expiresAt' timestamp
    private void RemoveExpiredCartItems()
    {
        Debug.Log("[DEBUG] Checking for expired cart items.");
        string uid = userManager?.UserId;

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("[DEBUG] User ID is null or empty. Cannot check for expired items.");
            return;
        }

        // Reference to the user's cart items
        var refCart = db.Child("REVIRA").Child("Consumers").Child(uid)
                         .Child("cart").Child("cartItems");

        // Read all cart items
        refCart.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            Debug.Log("[DEBUG] Read cart items for expiration check task completed.");

            // Check for errors or if no cart items exist
            if (task.Exception != null)
            {
                Debug.LogError("[DEBUG] Error reading cart items for expiration check: " + task.Exception);
                return;
            }
            if (!task.Result.Exists)
            {
                Debug.Log("[DEBUG] No cart items found to check for expiration.");
                return;
            }


            long now = GetUnixTimestamp(); // Get current Unix timestamp
            var toRemove = new List<string>(); // List to store keys of expired items

            Debug.Log($"[DEBUG] Current timestamp: {now}");

            // Iterate through cart items to find expired ones
            foreach (var itemSnapshot in task.Result.Children)
            {
                // Check if 'expiresAt' exists and is a valid timestamp
                if (itemSnapshot.Child("expiresAt").Value != null)
                {
                    long expiresAt = 0;
                    if (long.TryParse(itemSnapshot.Child("expiresAt").Value.ToString(), out expiresAt))
                    {
                        Debug.Log($"[DEBUG] Item '{itemSnapshot.Key}' expires at: {expiresAt}");
                        // If item has expired, add its key to the removal list
                        if (expiresAt < now)
                        {
                            Debug.Log($"[DEBUG] Item '{itemSnapshot.Key}' has expired. Marking for removal.");
                            toRemove.Add(itemSnapshot.Key);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[DEBUG] Could not parse 'expiresAt' for item '{itemSnapshot.Key}'. Value: {itemSnapshot.Child("expiresAt").Value}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[DEBUG] Item '{itemSnapshot.Key}' is missing 'expiresAt' node.");
                }
            }

            // Remove expired items from Firebase
            if (toRemove.Count > 0)
            {
                Debug.Log($"[DEBUG] Removing {toRemove.Count} expired cart items.");
                foreach (var id in toRemove)
                {
                    Debug.Log($"[DEBUG] Removing item with ID: {id}");
                    refCart.Child(id).RemoveValueAsync().ContinueWithOnMainThread(removeTask =>
                    {
                        if (removeTask.Exception != null)
                        {
                            Debug.LogError($"[DEBUG] Failed to remove item '{id}': " + removeTask.Exception);
                        }
                        else
                        {
                            Debug.Log($"[DEBUG] Item '{id}' removed successfully.");
                        }
                    });
                }
                // After removing items, update the cart summary
                UpdateCartSummary(uid, () => cartManager?.LoadCartItems());
            }
            else
            {
                Debug.Log("[DEBUG] No expired cart items found.");
            }
        });
    }


    // Validates that all dropdowns have a selected value other than the default "Select..." option
    private bool ValidateSelection()
    {
        Debug.Log("[DEBUG] Validating selections.");
        if (colorDropdown == null || sizeDropdown == null || quantityDropdown == null)
        {
            Debug.LogError("[DEBUG] Dropdown references are not assigned!");
            // Cannot validate if dropdowns are not assigned, assume invalid
            ShowError("UI elements not assigned.");
            return false;
        }


        if (colorDropdown.options.Count == 0 || colorDropdown.options[colorDropdown.value].text == "Select Color"
           || sizeDropdown.options.Count == 0 || sizeDropdown.options[sizeDropdown.value].text == "Select Size"
           || quantityDropdown.options.Count == 0 || quantityDropdown.options[quantityDropdown.value].text == "Select Quantity")
        {
            ShowError("Please make all selections.");
            Debug.Log("[DEBUG] Validation failed: Not all dropdowns selected.");
            // Disable button if validation fails
            if (addToCartButton != null) addToCartButton.interactable = false;
            return false;
        }

        // If validation passes, clear message and enable button (if not already adding)
        ClearMessage();
        if (!isAdding && addToCartButton != null) addToCartButton.interactable = true;
        Debug.Log("[DEBUG] Validation successful.");
        return true;
    }

    // Helper function to handle failures during the add-to-cart process
    private void Fail(string msg)
    {
        Debug.LogError("[DEBUG] FAIL: " + msg);
        ShowError(msg);
        // Ensure adding flag is reset and button is re-enabled on failure
        isAdding = false;
        if (addToCartButton != null) addToCartButton.interactable = true;
    }

    // Displays an error message to the user
    private void ShowError(string m)
    {
        if (errorText == null) return;
        errorText.color = Color.red;
        errorText.text = m;
        errorText.gameObject.SetActive(true);
        // Cancel any pending ClearMessage invokes
        CancelInvoke(nameof(ClearMessage));
        // Schedule clearing the message after 5 seconds
        Invoke(nameof(ClearMessage), 5f);
        Debug.Log($"[DEBUG] Showing Error: {m}");
    }

    // Displays a success message to the user
    private void ShowSuccess(string m)
    {
        if (errorText == null) return;
        errorText.color = Color.green;
        errorText.text = m;
        errorText.gameObject.SetActive(true);
        // Cancel any pending ClearMessage invokes
        CancelInvoke(nameof(ClearMessage));
        // Schedule clearing the message after 3 seconds
        Invoke(nameof(ClearMessage), 3f);
        Debug.Log($"[DEBUG] Showing Success: {m}");
    }

    // Hides the message text
    private void ClearMessage()
    {
        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
            Debug.Log("[DEBUG] Message cleared.");
        }
    }

    // Gets the current Unix timestamp (seconds since epoch)
    private long GetUnixTimestamp()
    {
        long timestamp = (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
        // Debug.Log($"[DEBUG] Generated Unix Timestamp: {timestamp}"); // Log only if needed, can be noisy
        return timestamp;
    }
}
