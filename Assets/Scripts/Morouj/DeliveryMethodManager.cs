using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class DeliveryMethodManager : MonoBehaviour
{
    [Header("Delivery Options")]
    public Toggle aramexToggle;
    public Toggle smsaToggle;
    public Toggle redboxToggle;

    [Header("Buttons")]
    public Button saveButton;
    public Button nextButton;
    public Button closeButton;

    [Header("UI")]
    public TextMeshProUGUI messageText;

    private bool isSaved = false;

    void Start()
    {
        saveButton.onClick.AddListener(SaveSelection);
        nextButton.onClick.AddListener(GoToNextScene);
        closeButton.onClick.AddListener(BackToStore);
    }

    void SaveSelection()
    {
        if (aramexToggle.isOn)
        {
            CheckoutManager.DeliveryCompany = "Aramex";
            CheckoutManager.DeliveryPrice = 21f;
            CheckoutManager.DeliveryDuration = "3 to 6 days";
        }
        else if (smsaToggle.isOn)
        {
            CheckoutManager.DeliveryCompany = "SMSA";
            CheckoutManager.DeliveryPrice = 29.5f;
            CheckoutManager.DeliveryDuration = "3 to 6 days";
        }
        else if (redboxToggle.isOn)
        {
            CheckoutManager.DeliveryCompany = "RedBox";
            CheckoutManager.DeliveryPrice = 15f;
            CheckoutManager.DeliveryDuration = "2 to 5 days";
        }
        else
        {
            messageText.text = "Please select a delivery method before saving.";
            messageText.color = Color.red;
            return;
        }

        isSaved = true;
        messageText.text = "Delivery method saved successfully!";
        messageText.color = Color.green;
    }

    void GoToNextScene()
    {
        if (!isSaved)
        {
            messageText.text = "Please select and save a delivery method first.";
            messageText.color = Color.red;
            return;
        }

        SceneManager.LoadScene("Payment");
    }

    void BackToStore()
    {
        SceneManager.LoadScene("StoreSelection");
    }
}
