using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Database;
using Firebase.Extensions;
using System;

public class PromotionalCodeManager : MonoBehaviour
{
    public TMP_InputField promoCodeInput;
    public Button applyButton;
    public Button nextButton;
    public Button backToStoreButton;
    public Button exitButton;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI CoinText;

    private DatabaseReference dbRef;
    private string storeID = "storeID_123";
    private bool isApplied = false;

    private string pendingMessage = "";
    private bool hasNewMessage = false;
    private bool isSuccessMessage = false;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        CoinText.text = UserManager.Instance.AccountBalance.ToString("F2");

        applyButton.onClick.AddListener(ValidatePromoCode);
        nextButton.onClick.AddListener(() => SceneManager.LoadScene("Address"));
        backToStoreButton.onClick.AddListener(() => SceneManager.LoadScene("Store"));
        exitButton.onClick.AddListener(() => SceneManager.LoadScene("Store"));
    }

    void Update()
    {
        if (hasNewMessage)
        {
            messageText.text = pendingMessage;
            messageText.color = isSuccessMessage ? Color.green : Color.red;
            hasNewMessage = false;
        }
    }

    void ValidatePromoCode()
    {
        string enteredCode = promoCodeInput.text.ToUpper().Trim();
        Debug.Log("Entered promo code: " + enteredCode);

        if (string.IsNullOrEmpty(enteredCode))
        {
            ShowMessage("Please enter a promo code.", false);
            return;
        }

        dbRef.Child("REVIRA").Child("stores").Child(storeID).Child("PromotionalCodes")
        .GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                var snapshot = task.Result;

                if (snapshot.HasChild(enteredCode))
                {
                    var codeData = snapshot.Child(enteredCode);
                    bool isActive = Convert.ToBoolean(codeData.Child("isActive").Value);
                    string appliesTo = codeData.Child("appliesTo").Value.ToString();
                    float discount = float.Parse(codeData.Child("discountPercentage").Value.ToString());

                    DateTime startDate = DateTime.Parse(codeData.Child("startDate").Value.ToString());
                    DateTime endDate = DateTime.Parse(codeData.Child("endDate").Value.ToString());
                    DateTime now = DateTime.Now;

                    if (isActive && now >= startDate && now <= endDate)
                    {
                        CheckCart(enteredCode, appliesTo, discount);
                    }
                    else
                    {
                        ShowMessage("This promotional code is expired or inactive.", false);
                    }
                }
                else
                {
                    ShowMessage("This code is not existed.", false);
                }
            }
            else
            {
                Debug.LogError("Failed to fetch promo codes: " + task.Exception);
                ShowMessage("Failed to validate the code. Try again.", false);
            }
        });
    }

    void CheckCart(string enteredCode, string appliesTo, float promoPercentage)
    {
        string userId = UserManager.Instance.UserId;

        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").Child("cartItems")
        .GetValueAsync().ContinueWithOnMainThread(cartTask =>
        {
            if (cartTask.IsCompleted && cartTask.Result.Exists)
            {
                float discountedTotal = 0f;
                bool hasValidProduct = false;
                int checkedCount = 0;
                int totalItems = (int)cartTask.Result.ChildrenCount;

                PromotionalManager.ProductDiscounts.Clear();

                foreach (var item in cartTask.Result.Children)
                {
                    string productId = item.Key;
                    float originalPrice = float.Parse(item.Child("price").Value.ToString());
                    int quantity = 0;
                    foreach (var size in item.Child("sizes").Children)
                        quantity += int.Parse(size.Value.ToString());

                    dbRef.Child("REVIRA").Child("stores").Child(storeID).Child("products").Child(productId)
                    .GetValueAsync().ContinueWithOnMainThread(productTask =>
                    {
                        if (productTask.IsCompleted && productTask.Result.Exists)
                        {
                            var productData = productTask.Result;
                            float productDiscountPercent = 0f;
                            float discountedPrice = originalPrice;

                            // Apply product-level discount
                            if (productData.Child("discount").Child("exists").Value.ToString() == "True")
                            {
                                productDiscountPercent = float.Parse(productData.Child("discount").Child("percentage").Value.ToString());
                                discountedPrice = originalPrice - (originalPrice * productDiscountPercent / 100f);
                            }

                            // Get category and apply promo if eligible
                            string category = productData.Child("category").Value.ToString();
                            float promoDiscountAmount = 0f;

                            if (appliesTo == "all" || appliesTo == category)
                            {
                                promoDiscountAmount = discountedPrice * (promoPercentage / 100f);
                                hasValidProduct = true;
                            }

                            float finalUnitPrice = discountedPrice - promoDiscountAmount;
                            float finalTotal = finalUnitPrice * quantity;
                            float originalSubtotal = originalPrice * quantity;

                            PromotionalManager.ProductDiscounts[productId] = new DiscountInfo
                            {
                                originalPrice = originalSubtotal,
                                discountPercentage = promoDiscountAmount > 0 ? promoPercentage : 0f,
                                discountAmount = promoDiscountAmount * quantity,
                                finalPrice = finalTotal
                            };

                            discountedTotal += finalTotal;
                        }

                        checkedCount++;
                        if (checkedCount == totalItems)
                        {
                            if (hasValidProduct)
                            {
                                ApplyDiscount(discountedTotal, enteredCode, promoPercentage, appliesTo);
                            }
                            else
                            {
                                ShowMessage("This code is not valid for the products in your cart.", false);
                            }
                        }
                    });
                }
            }
            else
            {
                ShowMessage("No items in cart to apply promo code.", false);
            }
        });
    }

    void ApplyDiscount(float discountedTotal, string enteredCode, float discountPercentage, string appliesTo)
    {
        PromotionalManager.UsedPromoCode = enteredCode;
        PromotionalManager.DiscountPercentage = discountPercentage;
        PromotionalManager.DiscountedTotal = discountedTotal;
        isApplied = true;

        string message = appliesTo == "all"
            ? "Promo code applied successfully to all items!"
            : $"Promo code applied successfully to {appliesTo} items!";

        ShowMessage(message, true);
    }

    void ShowMessage(string message, bool success)
    {
        Debug.Log("ShowMessage: " + message);
        pendingMessage = message;
        isSuccessMessage = success;
        hasNewMessage = true;
    }
}
