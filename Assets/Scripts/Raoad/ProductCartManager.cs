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
    private DatabaseReference dbReference;

    // UI elements for product selection
    public TMP_Dropdown sizeDropdown, colorDropdown, quantityDropdown;
    public Button addToCartButton;
    public TextMeshPro feedbackText; // Changed from errorText and type to TextMeshProUGUI

    // References to other manager scripts
    private ProductsManager productsManager;
    private UserManager userManager;
    private Coroutine cooldownCoroutine; // Keep the Coroutine reference
    private CartManager cartManager;

    // Flags to prevent multiple clicks and manage cooldown
    private bool isAdding = false;
    private bool hasAdded = false; // Keep the hasAdded flag

    void Start()
    {
        Debug.Log("[DEBUG] Start");

        // Get the root reference of the Firebase database
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        Debug.Log($"[DEBUG] Firebase DB Root Reference: {dbReference?.ToString()}");

        // Find references to other required manager scripts in the scene
        productsManager = FindObjectOfType<ProductsManager>();
        if (productsManager == null) Debug.LogError("[DEBUG] ProductsManager not found in scene!");
        else Debug.Log("[DEBUG] ProductsManager found.");

        // Use FindObjectOfType for UserManager and CartManager as in your provided code
        userManager = FindObjectOfType<UserManager>();
        if (userManager == null) Debug.LogError("[DEBUG] UserManager not found in scene!");
        else Debug.Log("[DEBUG] UserManager found.");

        cartManager = FindObjectOfType<CartManager>();
        if (cartManager == null) Debug.LogError("[DEBUG] CartManager not found in scene!");
        else Debug.Log("[DEBUG] CartManager found.");


        // Check if UI elements are assigned
        if (feedbackText == null) Debug.LogError("[DEBUG] FeedbackText (TextMeshProUGUI) not assigned!"); // Updated log
        if (addToCartButton == null) Debug.LogError("[DEBUG] AddToCartButton not assigned!");
        if (colorDropdown == null) Debug.LogError("[DEBUG] ColorDropdown not assigned!");
        if (sizeDropdown == null) Debug.LogError("[DEBUG] SizeDropdown not assigned!");
        if (quantityDropdown == null) Debug.LogError("[DEBUG] QuantityDropdown not assigned!");


        // Hook up button and dropdowns
        if (addToCartButton != null)
        {
            // --- IMPORTANT: Add RemoveAllListeners() to prevent double clicks ---
            //addToCartButton.onClick.RemoveAllListeners();
            //addToCartButton.onClick.AddListener(AddToCart);
            Debug.Log("[DEBUG] AddToCart listener added to button.");
        }


        if (colorDropdown != null)
        {
            colorDropdown.onValueChanged.AddListener(delegate { ValidateSelection(); });
            Debug.Log("[DEBUG] ColorDropdown listener added.");
        }

        if (sizeDropdown != null)
        {
            sizeDropdown.onValueChanged.AddListener(delegate { ValidateSelection(); });
            Debug.Log("[DEBUG] SizeDropdown listener added.");
        }

        if (quantityDropdown != null)
        {
            quantityDropdown.onValueChanged.AddListener(delegate { ValidateSelection(); });
            Debug.Log("[DEBUG] QuantityDropdown listener added.");
        }


        if (feedbackText != null) // Use feedbackText
        {
            feedbackText.text = "";
            feedbackText.gameObject.SetActive(false);
            Debug.Log("[DEBUG] FeedbackText initialized."); // Updated log
        }

        // Try remove expired items if user already logged in
        // Check if userManager is not null and UserId is available
        if (userManager != null && !string.IsNullOrEmpty(userManager.UserId))
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

        // Prevent multiple clicks during the adding process
        Debug.Log("[DEBUG] isAdding check. Value: " + isAdding);
        if (isAdding)
        {
            Debug.Log("[DEBUG] Already adding, skipping.");
            return;
        }

        // Prevent adding the same product again immediately after adding
        Debug.Log("[DEBUG] hasAdded check. Value: " + hasAdded);
        if (hasAdded)
        {
            ShowFeedback("Product was recently added. Please wait.", false); // Use ShowFeedback for success message
            Debug.Log("[DEBUG] Product recently added, skipping.");
            // Start the cooldown coroutine if not already running
            if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine); // Stop existing coroutine if any
            cooldownCoroutine = StartCoroutine(EnableButtonAfterDelay(5f)); // Start new cooldown
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


        // Check for UserManager and User ID
        Debug.Log("[DEBUG] Checking UserManager and User ID availability.");
        if (userManager == null || string.IsNullOrEmpty(userManager.UserId))
        {
            ShowFeedback("User not logged in or data not loaded.", true); // Use ShowFeedback for error
            Debug.LogError("[DEBUG] UserManager or UserId missing. UserManager == null: " + (userManager == null) + ", UserId empty: " + string.IsNullOrEmpty(userManager?.UserId));
            isAdding = false; // Reset flag on failure
            return;
        }
        string userID = userManager.UserId;
        Debug.Log($"[DEBUG] Retrieved User ID: {userID}");


        // Check for ProductsManager and Product Data
        Debug.Log("[DEBUG] Checking ProductsManager and product data.");
        if (productsManager == null)
        {
            ShowFeedback("Product manager not available.", true); // Use ShowFeedback for error
            Debug.LogError("[DEBUG] ProductsManager is null when AddToCart is called.");
            isAdding = false; // Reset flag on failure
            return;
        }
        ProductData productData = productsManager.GetProductData();
        string productID = productsManager.productID;
        if (productData == null || string.IsNullOrEmpty(productID))
        {
            ShowFeedback("Missing product data.", true); // Use ShowFeedback for error
            Debug.LogError($"[DEBUG] Missing product data. productData==null:{productData == null}, productID empty:{string.IsNullOrEmpty(productID)}");
            isAdding = false; // Reset flag on failure
            return;
        }
        Debug.Log($"[DEBUG] Product ID: {productID}, Name: {productData.name}, Price: {productData.price}");


        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        int quantity = int.Parse(quantityDropdown.options[quantityDropdown.value].text);
        long expirationTime = GetUnixTimestamp() + (24 * 60 * 60); // 24 hours in seconds
        Debug.Log($"[DEBUG] Selected: Color={selectedColor}, Size={selectedSize}, Quantity={quantity}, Expires={expirationTime}");


        // Local stock check
        Debug.Log("[DEBUG] Performing local stock check.");
        if (!productsManager.productColorsAndSizes.ContainsKey(selectedColor) ||
            !productsManager.productColorsAndSizes[selectedColor].ContainsKey(selectedSize) ||
            productsManager.productColorsAndSizes[selectedColor][selectedSize] < quantity)
        {
            ShowFeedback("Not enough stock or size/color not available.", true); // Use ShowFeedback for error
            if (!productsManager.productColorsAndSizes.ContainsKey(selectedColor)) Debug.LogError($"[DEBUG] Color '{selectedColor}' not found in productColorsAndSizes.");
            else if (!productsManager.productColorsAndSizes[selectedColor].ContainsKey(selectedSize)) Debug.LogError($"[DEBUG] Size '{selectedSize}' not found for color '{selectedColor}'.");
            else Debug.LogError($"[DEBUG] Not enough stock. Requested: {quantity}, Available: {productsManager.productColorsAndSizes[selectedColor][selectedSize]}");
            isAdding = false; // Reset flag on failure
            return;
        }
        Debug.Log($"[DEBUG] Local stock OK. Available: {productsManager.productColorsAndSizes[selectedColor][selectedSize]}. Requested: {quantity}.");


        // Throttle
        isAdding = true;
        if (addToCartButton != null) addToCartButton.interactable = false;
        Debug.Log("[DEBUG] isAdding set to true, button disabled.");


        // Step 1: decrement stock in Firebase
        string stockPath = $"REVIRA/stores/storeID_123/products/{productID}/colors/{selectedColor}/sizes/{selectedSize}";
        Debug.Log($"[DEBUG] Reading stock from Firebase path: {stockPath}");
        dbReference.Child("REVIRA").Child("stores").Child("storeID_123") // Use chained Child calls
                   .Child("products").Child(productID)
                   .Child("colors").Child(selectedColor)
                   .Child("sizes").Child(selectedSize)
                   .GetValueAsync().ContinueWithOnMainThread(stockReadTask =>
                   {
                       Debug.Log("[DEBUG] Stock READ task completed.");
                       if (stockReadTask.Exception != null)
                       {
                           Debug.LogError("[DEBUG] Stock READ failed: " + stockReadTask.Exception); // Log the exception
                           ShowFeedback("Could not read stock.", true); // Use ShowFeedback for error
                           isAdding = false; // Reset flag on failure
                           if (addToCartButton != null) addToCartButton.interactable = true; // Re-enable button
                           return;
                       }
                       if (!stockReadTask.Result.Exists)
                       {
                           Debug.LogError("[DEBUG] Stock data does not exist at path: " + stockPath);
                           ShowFeedback("Stock data not found.", true); // Use ShowFeedback for error
                           isAdding = false; // Reset flag on failure
                           if (addToCartButton != null) addToCartButton.interactable = true; // Re-enable button
                           return;
                       }


                       int currentStock = int.Parse(stockReadTask.Result.Value.ToString());
                       int updatedStock = Mathf.Max(currentStock - quantity, 0);
                       Debug.Log($"[DEBUG] Current Stock: {currentStock}, New Stock: {updatedStock}");

                       Debug.Log($"[DEBUG] Writing new stock ({updatedStock}) to Firebase path: {stockPath}");
                       dbReference.Child("REVIRA").Child("stores").Child("storeID_123") // Use chained Child calls
                                  .Child("products").Child(productID)
                                  .Child("colors").Child(selectedColor)
                                  .Child("sizes").Child(selectedSize)
                                  .SetValueAsync(updatedStock).ContinueWithOnMainThread(stockWriteTask =>
                                  {
                                      Debug.Log("[DEBUG] Stock WRITE task completed.");
                                      if (stockWriteTask.Exception != null)
                                      {
                                          Debug.LogError("[DEBUG] Stock WRITE failed: " + stockWriteTask.Exception); // Log the exception
                                          ShowFeedback("Could not update stock.", true); // Use ShowFeedback for error
                                          isAdding = false; // Reset flag on failure
                                          if (addToCartButton != null) addToCartButton.interactable = true; // Re-enable button
                                          return;
                                      }
                                      Debug.Log("[DEBUG] Stock updated successfully in Firebase.");


                                      // Step 2: read existing cart qty for this size
                                      string cartItemSizePath = $"REVIRA/Consumers/{userID}/cart/cartItems/{productID}/sizes/{selectedSize}";
                                      Debug.Log($"[DEBUG] Initiating Cart READ from Firebase path: {cartItemSizePath}");

                                      // Added log right before ContinueWithOnMainThread for cart read
                                      Debug.Log("[DEBUG] About to call ContinueWithOnMainThread for Cart READ task.");

                                      dbReference.Child("REVIRA").Child("Consumers").Child(userID) // Use chained Child calls
                                      .Child("cart").Child("cartItems").Child(productID)
                                      .Child("sizes").Child(selectedSize)
                                      .GetValueAsync().ContinueWithOnMainThread(cartReadTask =>
                                      {
                                          // Added try-catch block
                                          try
                                          {
                                              Debug.Log("[DEBUG] Cart READ task completed.");
                                              if (cartReadTask.Exception != null)
                                              {
                                                  Debug.LogError("[DEBUG] Cart READ failed: " + cartReadTask.Exception); // Log the exception
                                                  ShowFeedback("Could not read cart.", true); // Use ShowFeedback for error
                                                  isAdding = false; // Ensure flag is reset
                                                  if (addToCartButton != null) addToCartButton.interactable = true; // Ensure button is re-enabled
                                                  return;
                                              }

                                              int existingQuantity = cartReadTask.Result.Exists
                                       ? int.Parse(cartReadTask.Result.Value.ToString())
                                       : 0;
                                              int newQuantity = existingQuantity + quantity;
                                              Debug.Log($"[DEBUG] Existing cart quantity for size '{selectedSize}': {existingQuantity}, New total quantity: {newQuantity}");


                                              // Step 3: update cart entry using UpdateChildrenAsync for specific fields
                                              var updateMap = new Dictionary<string, object>()
                                   {
                            // Include product details (these might already exist, but good to ensure they are present)
                            { "productID",      productID },
                            // --- FIX: Use productData.name and productData.price ---
                            { "productName",    productData.name },
                            { "color",          selectedColor }, // Note: This will overwrite the existing color if the user adds the same product ID with a different color. Consider a different structure if multiple colors per product ID are needed in cartItems.
                            { "price",          productData.price }, // Note: This will overwrite the existing price. Consider storing original price or handling price changes.
                            // --- End FIX ---
                            { "timestamp",      GetUnixTimestamp() }, // Timestamp of the last update
                            { "expiresAt",      expirationTime }, // Expiration timestamp

                            // Update the quantity for the specific size using the correct path key
                            { $"sizes/{selectedSize}", newQuantity } // Correctly updates only the quantity for this size
                                   };
                                              Debug.Log($"[DEBUG] Preparing cart update data for product ID: {productID}");

                                              string cartItemPath = $"REVIRA/Consumers/{userID}/cart/cartItems/{productID}";
                                              Debug.Log($"[DEBUG] Writing cart entry to Firebase path: {cartItemPath}");
                                              dbReference.Child("REVIRA").Child("Consumers").Child(userID) // Use chained Child calls
                                              .Child("cart").Child("cartItems").Child(productID)
                                              .UpdateChildrenAsync(updateMap).ContinueWithOnMainThread(cartWriteTask =>
                                              {
                                                  Debug.Log("[DEBUG] Cart WRITE task completed.");
                                                  if (cartWriteTask.Exception != null)
                                                  {
                                                      Debug.LogError("[DEBUG] Cart WRITE failed: " + cartWriteTask.Exception); // Log the exception
                                                      ShowFeedback("Could not update cart.", true); // Use ShowFeedback for error
                                                                                                    // This is a critical failure - stock was reduced but cart wasn't updated.
                                                                                                    // You should implement robust error handling and potential rollback/compensation logic here.
                                                      isAdding = false; // Reset flag on failure
                                                      if (addToCartButton != null) addToCartButton.interactable = true; // Re-enable button
                                                      return;
                                                  }
                                                  Debug.Log("[DEBUG] Cart entry updated successfully in Firebase.");


                                                  ShowFeedback("Added to cart", false); // Use ShowFeedback for success

                                                  // Step 4: update cart summary
                                                  UpdateCartSummary(userID); // Call UpdateCartSummary

                                                  // Load cart items into UI after summary is updated (optional, can be done in UpdateCartSummary callback)
                                                  cartManager?.LoadCartItems(); // Use null conditional operator

                                                  hasAdded = true; // Set hasAdded to true on successful addition
                                                                   // Start the cooldown coroutine to re-enable the button and reset hasAdded
                                                  if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine); // Stop existing coroutine if any
                                                  cooldownCoroutine = StartCoroutine(EnableButtonAfterDelay(5f)); // Start new cooldown

                                                  isAdding = false; // Reset isAdding flag on success
                                                                    // Button will be re-enabled by the coroutine
                                              });
                                          }
                                          catch (Exception ex)
                                          {
                                              // Catch any unexpected exceptions within the callback
                                              Debug.LogError("[DEBUG] UNHANDLED EXCEPTION in Cart READ task callback: " + ex.Message + "\n" + ex.StackTrace);
                                              ShowFeedback("An unexpected error occurred during cart read.", true); // Use ShowFeedback for error
                                              isAdding = false; // Reset flag on failure
                                              if (addToCartButton != null) addToCartButton.interactable = true; // Re-enable button
                                          }
                                      });
                                  });
                   });
    }

    // --- Removed duplicate ShowSuccess method ---
    // private void ShowSuccess(string message)
    // {
    //     if (errorText != null) // Use errorText as per your code
    //     {
    //         errorText.color = Color.green;
    //         errorText.text = message;
    //         errorText.gameObject.SetActive(true);
    //         CancelInvoke(nameof(ClearMessage)); // Use nameof
    //         Invoke(nameof(ClearMessage), 3f); // Show success for 3 seconds
    //         Debug.Log($"[DEBUG] Showing Success: {message}");
    //     }
    // }
    // --- End Removed ---


    private bool ValidateSelection()
    {
        Debug.Log("[DEBUG] Validating selections.");
        // Add null checks for dropdowns before accessing options
        if (colorDropdown == null || sizeDropdown == null || quantityDropdown == null)
        {
            Debug.LogError("[DEBUG] Dropdown references are not assigned during validation!");
            ShowFeedback("UI elements not assigned.", true); // Use ShowFeedback for error
            if (addToCartButton != null) addToCartButton.interactable = false; // Disable button if UI is missing
            return false;
        }

        if (colorDropdown.options.Count == 0 || colorDropdown.options[colorDropdown.value].text == "Select Color")
        {
            ShowFeedback("Please select a color.", true); // Use ShowFeedback for error
            Debug.Log("[DEBUG] Validation failed: No color selected.");
            return false;
        }

        if (sizeDropdown.options.Count == 0 || sizeDropdown.options[sizeDropdown.value].text == "Select Size")
        {
            ShowFeedback("Please select a size.", true); // Use ShowFeedback for error
            Debug.Log("[DEBUG] Validation failed: No size selected.");
            return false;
        }

        if (quantityDropdown.options.Count == 0 || quantityDropdown.options[quantityDropdown.value].text == "Select Quantity")
        {
            ShowFeedback("Please select a quantity.", true); // Use ShowFeedback for error
            Debug.Log("[DEBUG] Validation failed: No quantity selected.");
            return false;
        }

        ClearMessage(); // Clear message on successful validation
        Debug.Log("[DEBUG] Validation successful.");
        return true;
    }

    private void UpdateCartSummary(string userId)
    {
        Debug.Log("[DEBUG] Updating cart summary for user: " + userId);
        dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").Child("cartItems").GetValueAsync().ContinueWithOnMainThread(task => // Use ContinueWithOnMainThread
        {
            Debug.Log("[DEBUG] Cart items READ for summary task completed.");
            if (task.Exception != null) // Check for exceptions
            {
                Debug.LogError("[DEBUG] Summary READ failed: " + task.Exception);
                // Decide how to handle summary update failure - maybe log and continue without updating summary node?
                return;
            }

            float totalPrice = 0f;
            int totalItems = 0;

            if (task.Result.Exists)
            {
                Debug.Log("[DEBUG] Cart items found for summary calculation.");
                foreach (var item in task.Result.Children)
                {
                    Debug.Log($"[DEBUG] Processing item for summary: {item.Key}");
                    float price = 0f;
                    if (item.Child("price").Value != null) // Check for null value
                    {
                        if (!float.TryParse(item.Child("price").Value.ToString(), out price))
                        {
                            Debug.LogWarning($"[DEBUG] Could not parse price for item '{item.Key}'. Value: {item.Child("price").Value}");
                        }
                    }
                    Debug.Log($"[DEBUG] Item '{item.Key}' price: {price}");

                    if (item.Child("sizes").Exists) // Check if sizes node exists
                    {
                        Debug.Log($"[DEBUG] Item '{item.Key}' has sizes node.");
                        foreach (var size in item.Child("sizes").Children)
                        {
                            int qty = 0;
                            if (size.Value != null) // Check for null value
                            {
                                if (!int.TryParse(size.Value.ToString(), out qty))
                                {
                                    Debug.LogWarning($"[DEBUG] Could not parse quantity for size '{size.Key}' in item '{item.Key}'. Value: {size.Value}");
                                }
                            }
                            Debug.Log($"[DEBUG] - Size: {size.Key}, Quantity: {qty}");
                            totalPrice += price * qty;
                            totalItems += qty;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[DEBUG] Item '{item.Key}' is missing 'sizes' node for summary calculation.");
                    }
                }
                Debug.Log($"[DEBUG] Calculated Total Price: {totalPrice}, Total Items: {totalItems}");
            }
            else
            {
                Debug.Log("[DEBUG] No cart items found for summary calculation. Totals are 0.");
            }


            Dictionary<string, object> cartTotalData = new Dictionary<string, object>
            {
                { "totalPrice", totalPrice },
                { "totalItems", totalItems }
            };

            Debug.Log($"[DEBUG] Writing cart total summary to Firebase.");
            dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").Child("cartTotal").SetValueAsync(cartTotalData).ContinueWithOnMainThread(totalWriteTask => // Use ContinueWithOnMainThread
            {
                Debug.Log("[DEBUG] Cart total summary WRITE task completed.");
                if (totalWriteTask.Exception != null) // Check for exceptions
                {
                    Debug.LogError("[DEBUG] Cart total summary WRITE failed: " + totalWriteTask.Exception);
                    // Decide how to handle total summary write failure
                }
                else
                {
                    Debug.Log("[DEBUG] Cart total summary updated successfully.");
                }
                // No Finish() here, as the cooldown coroutine handles re-enabling the button
            });
        });
    }

    private void ReduceStock(string color, string size, int quantity, System.Action onSuccess)
    {
        Debug.Log($"[DEBUG] Reducing stock for color: {color}, size: {size}, quantity: {quantity}");
        string path = $"REVIRA/stores/storeID_123/products/{productsManager.productID}/colors/{color}/sizes/{size}"; // Corrected path to include REVIRA
        Debug.Log($"[DEBUG] Reading stock for reduction from path: {path}");
        dbReference.Child(path).GetValueAsync().ContinueWithOnMainThread(task => // Use ContinueWithOnMainThread
        {
            Debug.Log("[DEBUG] Stock READ for reduction task completed.");
            if (task.Exception != null) // Check for exceptions
            {
                Debug.LogError("[DEBUG] Stock READ for reduction failed: " + task.Exception);
                ShowFeedback("Could not read stock for reduction.", true); // Use ShowFeedback for error
                isAdding = false; // Reset flag on failure
                if (addToCartButton != null) addToCartButton.interactable = true; // Re-enable button
                return;
            }

            if (task.IsCompleted && task.Result.Exists)
            {
                int currentStock = int.Parse(task.Result.Value.ToString());
                int updatedStock = Mathf.Max(currentStock - quantity, 0);
                Debug.Log($"[DEBUG] Current Stock for reduction: {currentStock}, Updated Stock: {updatedStock}");


                Debug.Log($"[DEBUG] Writing updated stock ({updatedStock}) to Firebase path: {path}");
                dbReference.Child(path).SetValueAsync(updatedStock).ContinueWithOnMainThread(stockUpdateTask => // Use ContinueWithOnMainThread
                {
                    Debug.Log("[DEBUG] Stock WRITE for reduction task completed.");
                    if (stockUpdateTask.IsCompletedSuccessfully) // Check for successful completion
                    {
                        Debug.Log("[DEBUG] Stock reduced successfully.");
                        onSuccess?.Invoke(); // Invoke success callback
                    }
                    else
                    {
                        Debug.LogError("Error updating stock: " + stockUpdateTask.Exception); // Log the exception
                        ShowFeedback("Failed to update stock.", true); // Use ShowFeedback for error
                        isAdding = false; // Reset flag on failure
                        if (addToCartButton != null) addToCartButton.interactable = true; // Re-enable button
                    }
                });
            }
            else
            {
                Debug.LogError("[DEBUG] Stock data not found at path for reduction: " + path);
                ShowFeedback("Stock data not found for reduction.", true); // Use ShowFeedback for error
                isAdding = false; // Reset flag on failure
                if (addToCartButton != null) addToCartButton.interactable = true; // Re-enable button
            }
        });
    }

    private void RemoveExpiredCartItems()
    {
        Debug.Log("[DEBUG] Checking for expired cart items.");
        // Use null conditional operator for userManager
        string userID = userManager?.UserId;

        if (string.IsNullOrEmpty(userID))
        {
            Debug.LogWarning("[DEBUG] User ID is null or empty. Cannot check for expired items.");
            return;
        }
        Debug.Log($"[DEBUG] User ID available for expired items check: {userID}");

        DatabaseReference cartItemsRef = dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").Child("cartItems");
        Debug.Log($"[DEBUG] Reading cart items for expiration check from path: {cartItemsRef.ToString()}");

        cartItemsRef.GetValueAsync().ContinueWithOnMainThread(task => // Use ContinueWithOnMainThread
        {
            Debug.Log("[DEBUG] Read cart items for expiration check task completed.");
            if (task.Exception != null) // Check for exceptions
            {
                Debug.LogError("[DEBUG] Error reading cart items for expiration check: " + task.Exception);
                return; // Stop if read failed
            }

            if (task.IsCompleted && task.Result.Exists)
            {
                Debug.Log("[DEBUG] Cart items found for expiration check.");
                long currentTimestamp = GetUnixTimestamp();
                List<string> expiredItems = new();
                Debug.Log($"[DEBUG] Current timestamp: {currentTimestamp}");


                foreach (var item in task.Result.Children)
                {
                    Debug.Log($"[DEBUG] Checking item '{item.Key}' for expiration.");
                    if (item.Child("expiresAt").Value != null)
                    {
                        if (long.TryParse(item.Child("expiresAt").Value.ToString(), out var exp) && exp < currentTimestamp)
                        {
                            Debug.Log($"[DEBUG] Item '{item.Key}' expires at: {exp}. Expired.");
                            string productID = item.Key;
                            expiredItems.Add(productID);
                            RestoreStock(productID, item); // Call RestoreStock for expired item
                        }
                        else
                        {
                            Debug.Log($"[DEBUG] Item '{item.Key}' expires at: {item.Child("expiresAt").Value}. Not expired or parse failed.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[DEBUG] Item '{item.Key}' is missing 'expiresAt' node.");
                    }
                }

                if (expiredItems.Count > 0)
                {
                    Debug.Log($"[DEBUG] Removing {expiredItems.Count} expired cart items.");
                    foreach (string id in expiredItems)
                    {
                        Debug.Log($"[DEBUG] Removing expired item with ID: {id}");
                        cartItemsRef.Child(id).RemoveValueAsync().ContinueWithOnMainThread(removeTask => // Use ContinueWithOnMainThread
                        {
                            if (removeTask.Exception != null)
                            {
                                Debug.LogError($"[DEBUG] Failed to remove expired item '{id}': " + removeTask.Exception);
                            }
                            else
                            {
                                Debug.Log($"[DEBUG] Expired item '{id}' removed successfully.");
                            }
                        });
                    }

                    // After removing items, check if the cart is now empty and remove the cart node if so
                    cartItemsRef.GetValueAsync().ContinueWithOnMainThread(checkTask => // Use ContinueWithOnMainThread
                    {
                        Debug.Log("[DEBUG] Cart items check after removal task completed.");
                        if (checkTask.IsCompleted && (!checkTask.Result.Exists || checkTask.Result.ChildrenCount == 0))
                        {
                            Debug.Log("[DEBUG] Cart is now empty after removing expired items. Removing cart node.");
                            dbReference.Child("REVIRA").Child("Consumers").Child(userID).Child("cart").RemoveValueAsync().ContinueWithOnMainThread(removeCartTask => // Use ContinueWithOnMainThread
                            {
                                if (removeCartTask.Exception != null)
                                {
                                    Debug.LogError("[DEBUG] Failed to remove empty cart node: " + removeCartTask.Exception);
                                }
                                else
                                {
                                    Debug.Log("[DEBUG] Empty cart node removed successfully.");
                                }
                            });
                        }
                        else
                        {
                            Debug.Log("[DEBUG] Cart is not empty after removing expired items.");
                        }
                        // Update the cart summary after removal and check
                        UpdateCartSummary(userID);
                        cartManager?.LoadCartItems(); // Load cart items into UI
                    });
                }
                else
                {
                    Debug.Log("[DEBUG] No expired cart items found.");
                }
            }
            else
            {
                Debug.Log("[DEBUG] No cart items found for expiration check.");
            }
        });
    }


    private void RestoreStock(string productID, DataSnapshot item)
    {
        Debug.Log($"[DEBUG] Restoring stock for expired item '{productID}'.");
        if (item.Child("sizes").Exists) // Check if sizes node exists
        {
            Debug.Log($"[DEBUG] Item '{productID}' has sizes node for stock restoration.");
            foreach (var sizeEntry in item.Child("sizes").Children)
            {
                string size = sizeEntry.Key;
                int quantity = 0;
                if (sizeEntry.Value != null) // Check for null value
                {
                    if (!int.TryParse(sizeEntry.Value.ToString(), out quantity))
                    {
                        Debug.LogWarning($"[DEBUG] Could not parse quantity for size '{size}' in item '{productID}' for stock restoration. Value: {sizeEntry.Value}");
                        continue; // Skip this size if quantity cannot be parsed
                    }
                }
                else
                {
                    Debug.LogWarning($"[DEBUG] Quantity value is null for size '{size}' in item '{productID}' for stock restoration.");
                    continue; // Skip if value is null
                }

                Debug.Log($"[DEBUG] - Restoring quantity {quantity} for size {size}.");
                string path = $"REVIRA/stores/storeID_123/products/{productID}/colors/{item.Child("color").Value}/sizes/{size}"; // Corrected path
                Debug.Log($"[DEBUG] Reading current stock for restoration from path: {path}");

                dbReference.Child(path).GetValueAsync().ContinueWithOnMainThread(task => // Use ContinueWithOnMainThread
                {
                    Debug.Log("[DEBUG] Stock READ for restoration task completed.");
                    if (task.Exception != null) // Check for exceptions
                    {
                        Debug.LogError("[DEBUG] Stock READ for restoration failed: " + task.Exception);
                        return;
                    }

                    if (task.IsCompleted && task.Result.Exists)
                    {
                        int currentStock = 0;
                        if (task.Result.Value != null) // Check for null value
                        {
                            if (!int.TryParse(task.Result.Value.ToString(), out currentStock))
                            {
                                Debug.LogWarning($"[DEBUG] Could not parse current stock for restoration at path '{path}'. Value: {task.Result.Value}");
                                // Proceed with 0 if parsing fails, or decide to skip
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[DEBUG] Current stock value is null for restoration at path: {path}");
                            // Proceed with 0 if value is null
                        }

                        int newStock = currentStock + quantity;
                        Debug.Log($"[DEBUG] Current Stock for restoration: {currentStock}, Restoring quantity: {quantity}, New Stock: {newStock}");

                        Debug.Log($"[DEBUG] Writing restored stock ({newStock}) to Firebase path: {path}");
                        dbReference.Child(path).SetValueAsync(newStock).ContinueWithOnMainThread(writeTask => // Use ContinueWithOnMainThread
                        {
                            Debug.Log("[DEBUG] Stock WRITE for restoration task completed.");
                            if (writeTask.Exception != null)
                            {
                                Debug.LogError("[DEBUG] Stock WRITE for restoration failed: " + writeTask.Exception);
                            }
                            else
                            {
                                Debug.Log("[DEBUG] Stock restored successfully.");
                            }
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"[DEBUG] Stock data not found at path for restoration: {path}");
                    }
                });
            }
        }
        else
        {
            Debug.LogWarning($"[DEBUG] Item '{productID}' is missing 'sizes' node for stock restoration.");
        }
    }


    // --- Consolidated feedback method ---
    private void ShowFeedback(string message, bool isError)
    {
        if (feedbackText == null) // Use feedbackText
        {
            Debug.LogError("[DEBUG] FeedbackText is null. Cannot show message: " + message);
            return;
        }
        feedbackText.color = isError ? Color.red : Color.green;
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
        CancelInvoke(nameof(ClearMessage)); // Use nameof
        Invoke(nameof(ClearMessage), isError ? 5f : 3f); // Show error for 5s, success for 3s
        Debug.Log($"[DEBUG] Showing Feedback ({(isError ? "Error" : "Success")}): {message}");
    }

    // --- Removed separate ShowError method ---
    // private void ShowError(string message)
    // {
    //     if (errorText != null)
    //     {
    //         errorText.color = Color.red;
    //         errorText.text = message;
    //         errorText.gameObject.SetActive(true);
    //         CancelInvoke(nameof(ClearMessage)); // Use nameof
    //         Invoke(nameof(ClearMessage), 5f); // Show error for 5 seconds
    //         Debug.Log($"[DEBUG] Showing Error: {message}");
    //     }
    //      else
    //     {
    //         Debug.LogError($"[DEBUG] ErrorText is null. Cannot show error message: {message}");
    //     }
    // }
    // --- End Removed ---


    private IEnumerator EnableButtonAfterDelay(float delay)
    {
        Debug.Log($"[DEBUG] Starting cooldown coroutine for {delay} seconds.");
        yield return new WaitForSeconds(delay);
        if (addToCartButton != null)
        {
            addToCartButton.interactable = true;
            Debug.Log("[DEBUG] AddToCart button re-enabled.");
        }
        hasAdded = false; // Reset hasAdded after cooldown
        Debug.Log("[DEBUG] hasAdded reset to false.");
    }


    private void ClearMessage()
    {
        if (feedbackText != null) // Use feedbackText
        {
            feedbackText.text = "";
            feedbackText.gameObject.SetActive(false);
            Debug.Log("[DEBUG] Message cleared.");
        }
    }

    private long GetUnixTimestamp()
    {
        long timestamp = (long)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
        // Debug.Log($"[DEBUG] Generated Unix Timestamp: {timestamp}"); // Log only if needed, can be noisy
        return timestamp;
    }
}
