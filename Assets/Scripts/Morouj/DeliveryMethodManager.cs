using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class DeliveryMethodManager : MonoBehaviour
{
    public Toggle aramexToggle;
    public Toggle smsaToggle;
    public Toggle redboxToggle;

    public Button saveButton;
    public Button nextButton;
    public Button backButton;
    public Button closeButton;

    public TextMeshProUGUI messageText;

    private bool isSaved = false;

    void Start()
    {
        saveButton.onClick.AddListener(SaveDeliveryMethod);
        nextButton.onClick.AddListener(GoToNextStep);
        backButton.onClick.AddListener(GoToPreviousStep);
        closeButton.onClick.AddListener(ReturnToStore);
    }

    void SaveDeliveryMethod()
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
            messageText.text = "Please select a delivery method.";
            return;
        }

        isSaved = true;
        messageText.text = "Delivery method saved successfully!";
    }

    void GoToNextStep()
    {
        if (!isSaved)
        {
            messageText.text = "Please save your selection before proceeding.";
            return;
        }

        SceneManager.LoadScene("final test");
    }

    void GoToPreviousStep()
    {
        SceneManager.LoadScene("Morouj Promotional 1");
    }

    void ReturnToStore()
    {
        SceneManager.LoadScene("Store");
    }
}
