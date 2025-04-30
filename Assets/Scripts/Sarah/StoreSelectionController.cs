using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoreSelectionController : MonoBehaviour
{
    public Button Enter;
    public GameObject storePopup;

    public Button EnterConfirmation;
    public Button CancelEnter;

    public Button Home;
    public Button Logout;

    public TextMeshProUGUI CoinText;

    // New feature buttons
    public Button featureButton1;
    public Button featureButton2;
    public Button featureButton3;

    // Under Construction popup
    public GameObject underConstructionPopup;
    public Button backFromUnderConstruction;

    void Start()
    {
        Enter.onClick.AddListener(ShowStorePopUp);
        EnterConfirmation.onClick.AddListener(EnterStore);
        CancelEnter.onClick.AddListener(HideStorePopUp);
        Home.onClick.AddListener(BackToHome);

        featureButton1.onClick.AddListener(ShowUnderConstruction);
        featureButton2.onClick.AddListener(ShowUnderConstruction);
        featureButton3.onClick.AddListener(ShowUnderConstruction);
        backFromUnderConstruction.onClick.AddListener(HideUnderConstruction);

        CoinText.text = UserManager.Instance.AccountBalance.ToString("F2");

        storePopup.SetActive(false);
        underConstructionPopup.SetActive(false);
    }

    public void ShowStorePopUp()
    {
        storePopup.SetActive(true);
    }

    public void HideStorePopUp()
    {
        storePopup.SetActive(false);
    }

    public void EnterStore()
    {
        SceneManager.LoadScene("Store");
    }

    public void BackToHome()
    {
        SceneManager.LoadScene("Lolo");
    }

    public void ShowUnderConstruction()
    {
        underConstructionPopup.SetActive(true);
    }

    public void HideUnderConstruction()
    {
        underConstructionPopup.SetActive(false);
    }
}
