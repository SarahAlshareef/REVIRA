using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using UnityEngine.SceneManagement;

public class PromotionalCodeManager : MonoBehaviour
{
    public TMP_InputField promoCodeInput;
    public Button applyButton;
    public Button nextButton;
    public TextMeshProUGUI errorMessageText;

    private DatabaseReference dbRef;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        applyButton.onClick.AddListener(ValidatePromoCode);
        nextButton.onClick.AddListener(SkipPromoCode);
    }

    void SkipPromoCode()
    {
        if (string.IsNullOrEmpty(promoCodeInput.text))
        {
            SceneManager.LoadScene("Address");
        }
    }

    void ValidatePromoCode()
    {
        string enteredCode = promoCodeInput.text.ToUpper().Trim();
        if (string.IsNullOrEmpty(enteredCode))
        {
            return;
        }

        string storeID = "storeID_123";
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
                        ValidateCart(appliesTo, discount, enteredCode);
                    }
                    else
                    {
                        ShowError("This promotional code is expired or inactive.");
                    }
                }
                else
                {
                    ShowError("This code is not valid for the products in your cart");
                }
            }
        });
    }

    void ValidateCart(string appliesTo, float discount, string promoCode)
    {
        string userId = UserManager.Instance.UserId;
        string storeID = "storeID_123";

        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("cart").GetValueAsync().ContinueWith(cartTask =>
        {
            if (cartTask.IsCompleted)
            {
                float total = 0f;
                int validItems = 0;
                int totalItems = 0;

                foreach (DataSnapshot item in cartTask.Result.Children)
                {
                    string productId = item.Key;
                    float price = float.Parse(item.Child("price").Value.ToString());

                    int quantity = 0;
                    foreach (var size in item.Child("sizes").Children)
                        quantity += int.Parse(size.Value.ToString());

                    total += price * quantity;
                    totalItems++;

                    dbRef.Child("REVIRA").Child("stores").Child(storeID).Child("products").Child(productId).Child("category").GetValueAsync().ContinueWith(catTask =>
                    {
                        if (catTask.IsCompleted)
                        {
                            string category = catTask.Result.Value.ToString();

                            if (appliesTo == "all" || appliesTo == category)
                            {
                                validItems++;
                            }

                            if (validItems == totalItems)
                            {
                                float discountedTotal = total - (total * (discount / 100f));

                                CheckoutManager.UsedPromoCode = promoCode;
                                CheckoutManager.DiscountPercentage = discount;
                                CheckoutManager.DiscountedTotal = discountedTotal;

                                errorMessageText.text = "";
                                SceneManager.LoadScene("Address");
                            }
                            else if (validItems < totalItems && validItems + 1 == totalItems)
                            {
                                ShowError("This code is not valid for the products in your cart");
                            }
                        }
                    });
                }
            }
        });
    }

    void ShowError(string msg)
    {
        errorMessageText.text = msg;
    }
}
