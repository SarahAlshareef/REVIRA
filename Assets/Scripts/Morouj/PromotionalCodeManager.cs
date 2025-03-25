using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using UnityEngine.SceneManagement;
using System;

public class PromotionalCodeManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField promoCodeInput;
    public Button applyButton;
    public Button nextButton;
    public Button backToStoreButton;
    public Button exitButton;
    public TextMeshProUGUI errorMessageText;

    private DatabaseReference dbRef;
    private string storeID = "storeID_123";

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        applyButton.onClick.AddListener(ValidatePromoCode);
        nextButton.onClick.AddListener(SkipPromoCode);
        backToStoreButton.onClick.AddListener(() => SceneManager.LoadScene("Store"));
        exitButton.onClick.AddListener(() => SceneManager.LoadScene("Store"));
    }

    void SkipPromoCode()
    {
        if (string.IsNullOrEmpty(promoCodeInput.text))
        {
            GoToNextStep();
        }
    }

    void ValidatePromoCode()
    {
        string enteredCode = promoCodeInput.text.ToUpper().Trim();

        if (string.IsNullOrEmpty(enteredCode))
        {
            return;
        }

        dbRef.Child("REVIRA").Child("stores").Child(storeID).Child("PromotionalCodes").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

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
                        ValidateCartCompatibility(appliesTo, discount, enteredCode);
                    }
                    else
                    {
                        ShowMessage("This promotional code is expired or inactive.");
                    }
                }
                else
                {
                    ShowMessage("This code is not valid for the products in your cart");
                }
            }
        });
    }

    void ValidateCartCompatibility(string appliesTo, float discount, string enteredCode)
    {
        string userId = UserManager.Instance.UserId;
        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").GetValueAsync().ContinueWith(cartTask =>
        {
            if (cartTask.IsCompleted)
            {
                float total = 0f;
                bool isValid = false;
                int checkedCount = 0;
                int totalItems = (int)cartTask.Result.ChildrenCount;

                foreach (DataSnapshot item in cartTask.Result.Children)
                {
                    string productId = item.Key;
                    int quantity = 0;
                    float price = float.Parse(item.Child("price").Value.ToString());

                    foreach (var size in item.Child("sizes").Children)
                    {
                        quantity += int.Parse(size.Value.ToString());
                    }

                    float subtotal = quantity * price;
                    total += subtotal;

                    dbRef.Child("REVIRA").Child("stores").Child(storeID).Child("products").Child(productId).Child("category").GetValueAsync().ContinueWith(categoryTask =>
                    {
                        if (categoryTask.IsCompleted)
                        {
                            string category = categoryTask.Result.Value.ToString();

                            if (appliesTo == "all" || appliesTo == category)
                            {
                                isValid = true;
                            }

                            checkedCount++;

                            if (checkedCount == totalItems)
                            {
                                if (isValid)
                                {
                                    ApplyDiscount(discount, total, enteredCode);
                                }
                                else
                                {
                                    ShowMessage("This code is not valid for the products in your cart");
                                }
                            }
                        }
                    });
                }
            }
        });
    }

    void ApplyDiscount(float discount, float total, string enteredCode)
    {
        CheckoutManager.UsedPromoCode = enteredCode;
        CheckoutManager.DiscountPercentage = discount;
        CheckoutManager.DiscountedTotal = total - (total * (discount / 100f));

        ShowMessage("Promo code applied successfully!");
        GoToNextStep();
    }

    void ShowMessage(string message)
    {
        errorMessageText.text = message;
        errorMessageText.color = Color.red; // Ì„ﬂ‰ﬂ  €ÌÌ—Â Õ”» —€» ﬂ
    }

    void GoToNextStep()
    {
        SceneManager.LoadScene("Morouj Method 1");
    }
}
