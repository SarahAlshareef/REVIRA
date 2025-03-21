// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// Firebase
using Firebase.Auth;
using Firebase.Database;
// C#
using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;

public class Payment : MonoBehaviour
{
    public TextMeshProUGUI AccountBalance, errorText1, errorText2;
    public TMP_InputField VoucherCodeInput;
    public Button ApplyVoucherButtton;
    public GameObject ConfirmOrder;

    private DatabaseReference dbReference;
    void Start()
    {
        ConfirmOrder.SetActive(false);

        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        AccountBalance.text = UserManager.Instance.AccountBalance.ToString("F2");

        ApplyVoucherButtton?.onClick.AddListener(OnApplyButtonClick);

    }
    public void OnApplyButtonClick()
    {
        string enteredCode = VoucherCodeInput.text.Trim();

        if (string.IsNullOrEmpty(enteredCode))
        {
            ShowError2("Please enter a voucher code.");
            return;
        }
        ApplyVoucher(enteredCode);
    }
    public void ApplyVoucher(string enteredCode)
    {
        dbReference.Child("REVIRA").Child("Voucher Code").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                bool found = false;

                foreach (var codeEntry in task.Result.Children)
                {
                    string code = codeEntry.Child("Voucher Code").Value.ToString();

                    if (code == enteredCode)
                    {
                        found = true;

                        bool used = (bool)codeEntry.Child("used").Value;
                        if (used)
                        {
                            ShowError2("This Code is used.");
                            return;
                        }
                        int value = int.Parse(codeEntry.Child("Value (SAR)").Value.ToString());
                        string voucherKey = codeEntry.Key;

                        string userId = UserManager.Instance.UserId;

                        //dbReference.Child("REVIRA").Child("Voucher Code").Child(voucherKey).Child("used").SetValueAsync(true);

                        var userBalanceReference = dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("accountBalance");

                        userBalanceReference.GetValueAsync().ContinueWithOnMainThread(balanceTask =>
                        {
                            if (balanceTask.IsCompleted)
                            {
                                int currentBalance = 0;
                                if (balanceTask.Result.Exists)
                                {
                                    currentBalance = int.Parse(balanceTask.Result.Value.ToString());
                                }
                                int newBalance = currentBalance + value;
                                userBalanceReference.SetValueAsync(newBalance).ContinueWithOnMainThread(updateTask =>
                                {
                                    if (updateTask.IsCompleted)
                                    {
                                        AccountBalance.text = newBalance.ToString("F2");
                                        UserManager.Instance.UpdateAccountBalance(newBalance);
                                    }
                                });
                            }
                        });
                        return;
                    }
                }
                if (!found)
                    ShowError2("Voucher Code is incorrect.");
            }
            else
                ShowError2("Failed to retrieve voucher data.");
        });
    }

    void ShowError1(string message)
    {
        if (errorText1 != null)
        {
            errorText1.text = message;
            errorText1.color = Color.red;
        }
    }
    void ShowError2(string message)
    {
        if (errorText2 != null)
        {
            errorText2.text = message;
            errorText2.color = Color.red;
        }
    }
}
