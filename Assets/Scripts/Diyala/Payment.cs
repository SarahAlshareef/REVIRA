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
    public TextMeshProUGUI orderTotalAmount, AccountBalance, errorText1, errorText2;
    public TMP_InputField VoucherCodeInput;
    public Button UseAccountBalanceButton, UseVoucherButtton, ApplyVoucherButtton;
    public GameObject VoucherSection, ConfirmOrder;
    public AudioSource coinsSound;

    public float TotalAmount = 100f;

    private DatabaseReference dbReference;
    void Start()
    {
        VoucherSection.SetActive(false);
        ConfirmOrder.SetActive(false);

        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        AccountBalance.text = UserManager.Instance.AccountBalance.ToString("F2");

        UseVoucherButtton?.onClick.AddListener(ShowVoucherSection);
        UseAccountBalanceButton?.onClick.AddListener(OnUseAccountBalanceClick);
        ApplyVoucherButtton?.onClick.AddListener(OnApplyButtonClick);

    }
    public void OnUseAccountBalanceClick()
    {
        float currentBalance = UserManager.Instance.AccountBalance;

        if (currentBalance >= TotalAmount)
            ConfirmOrder.SetActive(true);
        else
            ShowError1("Sorry, your balance is not enough for this order.");
    }

    public void ShowVoucherSection()
    {
        VoucherSection.SetActive(true);
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
                bool foundVoucher = false;

                foreach (var codeEntry in task.Result.Children)
                {
                    string code = codeEntry.Child("Voucher Code").Value.ToString();

                    if (code == enteredCode)
                    {
                        foundVoucher = true;

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
                                        StartCoroutine(AnimateBalance(UserManager.Instance.AccountBalance, newBalance));
                                        UserManager.Instance.UpdateAccountBalance(newBalance);
                                        VoucherSection.SetActive(false);
                                    }
                                });
                            }
                        });
                        return;
                    }
                }
                if (!foundVoucher)
                    ShowError2("Voucher Code is unfound.");
            }
            else
                ShowError2("Failed to retrieve voucher data.");
        });
    }
    IEnumerator AnimateBalance(float previousBalance, float newBalance)
    {
        float duration = 3f;
        float stepAmount = 10f;

        float difference = newBalance - previousBalance;
        if (difference <= 0)
        {
            AccountBalance.text = newBalance.ToString("F2");
            yield break;
        }

        int steps = Mathf.CeilToInt(difference / stepAmount);
        float delay = duration / steps;

        float current = previousBalance;

        while (current < newBalance)
        {
            current += stepAmount;
            if (current > newBalance) current = newBalance;

            AccountBalance.text = current.ToString("F2");
            
            coinsSound?.Play();

            yield return new WaitForSeconds(delay);
        }

        AccountBalance.text = newBalance.ToString("F2");
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
