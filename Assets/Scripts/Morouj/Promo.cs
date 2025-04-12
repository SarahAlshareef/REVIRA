using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;

public class Promo : MonoBehaviour
{
    public TMP_InputField promoCodeInput;
    public Button applyButton;
    public Button nextButton;
    public Button backToStoreButton;
    public Button exitButton;
    public TextMeshProUGUI messageText;

    private DatabaseReference dbRef;
    private string storeID = "storeID_123";
    private string lastAppliedCode = "";
    private bool isApplied = false;

    private string pendingMessage = "";
    private bool hasNewMessage = false;
    private bool isSuccessMessage = false;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

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

        if (string.IsNullOrEmpty(enteredCode))
        {
            ShowMessage("Please enter a promo code.", false);
            return;
        }

        if (enteredCode == lastAppliedCode)
        {
            ShowMessage("Promo code already applied.", true);
            return;
        }

        dbRef.Child("REVIRA").Child("stores").Child(storeID).Child("PromotionalCodes").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
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
                    ShowMessage("This code is not valid for the products in your cart.", false);
                }
            }
        });
    }

    void CheckCart(string enteredCode, string appliesTo, float discount)
    {
        string userId = UserManager.Instance.UserId;

        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").Child("cartItems").GetValueAsync().ContinueWith(cartTask =>
        {
            if (cartTask.IsCompleted && cartTask.Result.Exists)
            {
                float total = 0f;
                bool isValid = false;
                int checkedCount = 0;
                int totalItems = (int)cartTask.Result.ChildrenCount;

                foreach (var item in cartTask.Result.Children)
                {
                    string productId = item.Key;
                    int quantity = 0;
                    float basePrice = float.Parse(item.Child("price").Value.ToString());

                    foreach (var size in item.Child("sizes").Children)
                    {
                        quantity += int.Parse(size.Value.ToString());
                    }

                    dbRef.Child("REVIRA").Child("stores").Child(storeID).Child("products").Child(productId).GetValueAsync().ContinueWith(productTask =>
                    {
                        if (productTask.IsCompleted && productTask.Result.Exists)
                        {
                            float finalPrice = basePrice;
                            var discountNode = productTask.Result.Child("discount");
                            bool hasDiscount = Convert.ToBoolean(discountNode.Child("exists").Value);

                            if (hasDiscount)
                            {
                                float percent = float.Parse(discountNode.Child("percentage").Value.ToString());
                                finalPrice -= basePrice * (percent / 100f);
                            }

                            float productTotal = finalPrice * quantity;
                            total += productTotal;

                            string category = productTask.Result.Child("category").Value.ToString();
                            if (appliesTo == "all" || appliesTo == category)
                            {
                                isValid = true;
                            }

                            checkedCount++;
                            if (checkedCount == totalItems)
                            {
                                if (isValid)
                                {
                                    ApplyDiscount(discount, total, enteredCode, appliesTo);
                                }
                                else
                                {
                                    ShowMessage("This code is not valid for the products in your cart.", false);
                                }
                            }
                        }
                    });
                }
            }
        });
    }

    void ApplyDiscount(float discount, float total, string enteredCode, string appliesTo)
    {
        PromotionalManager.UsedPromoCode = enteredCode;
        PromotionalManager.DiscountPercentage = discount;
        PromotionalManager.DiscountedTotal = total - (total * discount / 100f);
        lastAppliedCode = enteredCode;
        isApplied = true;

        string message = appliesTo == "all"
            ? "Promo code applied successfully to all items!"
            : $"Promo code applied successfully to {appliesTo} items!";

        ShowMessage(message, true);
    }

    void ShowMessage(string message, bool success)
    {
        pendingMessage = message;
        isSuccessMessage = success;
        hasNewMessage = true;
    }
}
