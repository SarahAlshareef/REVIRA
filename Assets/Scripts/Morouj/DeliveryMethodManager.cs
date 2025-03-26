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

    private bool isSaved = false;

    private Dictionary<string, DeliveryInfo> deliveryOptions = new Dictionary<string, DeliveryInfo>();
    private DatabaseReference dbRef;

    private void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        saveButton.onClick.AddListener(SaveDeliveryMethod);
        nextButton.onClick.AddListener(GoToNextStep);
        backButton.onClick.AddListener(GoToPreviousStep);
        closeButton.onClick.AddListener(ReturnToStore);

        LoadDeliveryOptions();
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
            return;
        }

        isSaved = true;
        messageText.text = "Delivery method saved successfully!";
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
            return;
        }

        SceneManager.LoadScene("final test");  //  Payment pag
    }

    void GoToPreviousStep()
    {
        SceneManager.LoadScene("Morouj Promotional 1"); //  Address pag
    }

    void ReturnToStore()
    {
        SceneManager.LoadScene("StoreSelection");
    }
}

// Class to store data for each delivery company
public class DeliveryInfo
{
    public float price;
    public string duration;
    public string website;
}
