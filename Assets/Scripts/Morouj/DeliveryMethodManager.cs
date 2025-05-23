using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Database;

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
    public TextMeshProUGUI CoinText;

    private bool isSaved = false;

    private Dictionary<string, DeliveryInfo> deliveryOptions = new Dictionary<string, DeliveryInfo>();
    private DatabaseReference dbRef;

    private void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        CoinText.text = UserManager.Instance.AccountBalance.ToString("F2");


        saveButton.onClick.AddListener(SaveDeliveryMethod);
        nextButton.onClick.AddListener(GoToNextStep);
        backButton.onClick.AddListener(GoToPreviousStep);
        closeButton.onClick.AddListener(ReturnToStore);

        // Watch for toggle changes after save
        aramexToggle.onValueChanged.AddListener(delegate { OnDeliveryOptionChanged(); });
        smsaToggle.onValueChanged.AddListener(delegate { OnDeliveryOptionChanged(); });
        redboxToggle.onValueChanged.AddListener(delegate { OnDeliveryOptionChanged(); });

        LoadDeliveryOptions();
    }

    void OnDeliveryOptionChanged()
    {
        if (isSaved)
        {
            isSaved = false;
            messageText.text = "You changed your selection. Please save again.";
            messageText.color = Color.red;
        }
    }

    // get delivery data from Firebase
    void LoadDeliveryOptions()
    {
        string storeID = "storeID_123";

        dbRef.Child("REVIRA").Child("stores").Child(storeID).Child("Deliverymethods").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                foreach (var company in snapshot.Children)
                {
                    string name = company.Key;
                    float price = float.Parse(company.Child("price").Value.ToString());
                    string duration = company.Child("duration").Value.ToString();
                    string website = company.Child("website").Value.ToString();

                    deliveryOptions[name] = new DeliveryInfo
                    {
                        price = price,
                        duration = duration,
                        website = website
                    };
                }
            }
        });
    }

    void SaveDeliveryMethod()
    {
        if (aramexToggle.isOn && deliveryOptions.ContainsKey("Aramex"))
        {
            ApplySelection("Aramex");
        }
        else if (smsaToggle.isOn && deliveryOptions.ContainsKey("SMSA"))
        {
            ApplySelection("SMSA");
        }
        else if (redboxToggle.isOn && deliveryOptions.ContainsKey("RedBox"))
        {
            ApplySelection("RedBox");
        }
        else
        {
            messageText.text = "Please select a delivery method.";
            messageText.color = Color.red;
            return;
        }

        isSaved = true;
        messageText.text = "Delivery method saved successfully!";
        messageText.color = Color.green; // ������� ���� ���� ���� ��� �����
    }

    void ApplySelection(string companyName)
    {
        var info = deliveryOptions[companyName];
        DeliveryManager.DeliveryCompany = companyName;
        DeliveryManager.DeliveryPrice = info.price;
        DeliveryManager.DeliveryDuration = info.duration;
    }

    void GoToNextStep()
    {
        if (!isSaved)
        {
            messageText.text = "Please save your selection before proceeding.";
            messageText.color = Color.red;
            return;
        }

        SceneManager.LoadScene("Payment");  //  Payment page
    }

    void GoToPreviousStep()
    {
        SceneManager.LoadScene("Address"); //  Address page
    }

    void ReturnToStore()
    {
        SceneManager.LoadScene("Store");
    }
}

// Class to store data for each delivery company
public class DeliveryInfo
{
    public float price;
    public string duration;
    public string website;
}