using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;

public class StoreDelivery : MonoBehaviour
{
    public Toggle aramexToggle;
    public Toggle smsaToggle;
    public Toggle redboxToggle;

    public Button saveButton;
    public Button nextButton;
    public Button backButton;
    

    public TextMeshProUGUI messageText;
    public TextMeshProUGUI CoinText;

    [Header("Panels")]
    public GameObject DeliveryPanel;
    public GameObject PaymentPanel;
    public GameObject AddressPanel; // ? ⁄‘«‰ ‰” Œœ„Â ›Ì GoToPreviousStep

    private bool isSaved = false;

    private Dictionary<string, DeliveryInfoStore> deliveryOptions = new Dictionary<string, DeliveryInfoStore>();
    private DatabaseReference dbRef;

    private void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        CoinText.text = UserManager.Instance.AccountBalance.ToString("F2");

        saveButton.onClick.AddListener(SaveDeliveryMethod);
        nextButton.onClick.AddListener(GoToNextStep);
        backButton.onClick.AddListener(GoToPreviousStep);
        

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

                    deliveryOptions[name] = new DeliveryInfoStore
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
        messageText.color = Color.green;
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

        DeliveryPanel?.SetActive(false);
        PaymentPanel?.SetActive(true);

        // ⁄—÷ »«‰· «·œ›⁄ √„«„ «·ﬂ«„Ì—«
        Transform cam = Camera.main.transform;
        Vector3 targetPos = cam.position + cam.forward * 4.0f;
        PaymentPanel.transform.position = targetPos;
        PaymentPanel.transform.rotation = Quaternion.LookRotation(cam.forward, cam.up);
    }

    void GoToPreviousStep()
    {
        DeliveryPanel?.SetActive(false);
        AddressPanel?.SetActive(true);

        // ⁄—÷ »«‰· «·⁄‰Ê«‰ √„«„ «·ﬂ«„Ì—«
        Transform cam = Camera.main.transform;
        Vector3 targetPos = cam.position + cam.forward * 4.0f;
        AddressPanel.transform.position = targetPos;
        AddressPanel.transform.rotation = Quaternion.LookRotation(cam.forward, cam.up);
    }

    
}

public class DeliveryInfoStore
{
    public float price;
    public string duration;
    public string website;
}