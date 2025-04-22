// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
// Firebase
using Firebase.Database;
// C#
using System.Collections;
using Firebase.Extensions;

public class StorePayment : MonoBehaviour
{
    [Header("Panels")]
    public GameObject PaymentPanel;
    public GameObject ShipmentPanel;

    [Header("General")]
    public TextMeshProUGUI AccountBalance;
    public TMP_InputField VoucherCodeInput;

    [Header("Game Objects")]
    public GameObject VoucherSection;
    public GameObject ConfirmOrder;

    [Header("Buttons")]
    public Button UseAccountBalanceButton;
    public Button UseVoucherButtton;
    public Button ApplyVoucherButtton;

    [Header("Display Message")]
    public TextMeshProUGUI errorText1;
    public TextMeshProUGUI errorText2;
    private Coroutine messageCoroutine;

    [Header("Sound")]
    public AudioSource coinsSound;

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
        VoucherSection.SetActive(false);

        float currentBalance = UserManager.Instance.AccountBalance;
        float TotalAmount = OrderSummaryManager.FinalTotal;

        if (currentBalance >= TotalAmount)
            ConfirmOrder.SetActive(true);
        else
            ShowError(errorText1, "Sorry, your balance is not enough for this order.");
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
            ShowError(errorText2, "Please enter a voucher code.");
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
                            ShowError(errorText2, "This Voucher Code is used.");
                            return;
                        }
                        int value = int.Parse(codeEntry.Child("Value (SAR)").Value.ToString());
                        string voucherKey = codeEntry.Key;

                        string userId = UserManager.Instance.UserId;

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

                                        //dbReference.Child("REVIRA").Child("Voucher Code").Child(voucherKey).Child("used").SetValueAsync(true);
                                    }
                                });
                            }
                        });
                        return;
                    }
                }
                if (!foundVoucher)
                    ShowError(errorText2, "Voucher Code is unfound.");
            }
            else
                ShowError(errorText2, "Failed to retrieve Voucher Code data.");
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

        coinsSound?.Play();

        while (current < newBalance)
        {
            current += stepAmount;
            if (current > newBalance) current = newBalance;

            AccountBalance.text = current.ToString("F2");

            yield return new WaitForSeconds(delay);
        }

        AccountBalance.text = newBalance.ToString("F2");
    }

    public void GoToPreviousScene()
    {
        PaymentPanel?.SetActive(false);
        ShipmentPanel?.SetActive(true);

        FindObjectOfType<MenuManagerVR>().HandleUIOpened(ShipmentPanel);
    }

    void ShowError(TextMeshProUGUI errorText, string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
            errorText.gameObject.SetActive(true);

            if (messageCoroutine != null)
                StopCoroutine(messageCoroutine);

            messageCoroutine = StartCoroutine(HideMessageAfterDelay(errorText, 3f));
        }
    }
    IEnumerator HideMessageAfterDelay(TextMeshProUGUI errorText, float delay)
    {
        yield return new WaitForSeconds(delay);
        errorText.gameObject.SetActive(false);
    }
}
