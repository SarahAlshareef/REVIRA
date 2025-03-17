using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoreSelectionController : MonoBehaviour, IPointerClickHandler
{
    public Button Enter;
    public GameObject storePopup;

    public Button EnterConfirmation;
    public Button CancelEnter;

    public Button Home;
    public Button Logout;

    public TextMeshProUGUI CoinText;



    void Start()
    {

        // Assign button click events
        Enter.onClick.AddListener(ShowStorePopUp);
        EnterConfirmation.onClick.AddListener(EnterStore);
        CancelEnter.onClick.AddListener(HideStorePopUp);
        Home.onClick.AddListener(BackToHome);
        Logout.onClick.AddListener(LogoutToMainMenu);

        CoinText.text = UserManager.Instance.AccountBalance.ToString("F2");

        storePopup.SetActive(false); // Hide pop-up intially 
    }

    public void ShowStorePopUp()
    {
        storePopup.SetActive(true); // Show pop-up
    }

    public void HideStorePopUp()
    {
        storePopup.SetActive(false); // Hide pop-up
    }

    public void EnterStore()
    {
        SceneManager.LoadScene("Store");
    }

    public void BackToHome()
    {
        SceneManager.LoadScene("HomeScene");
    }

    public void LogoutToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerPress == Enter.gameObject)
        {
            ShowStorePopUp();
        }
        else if (eventData.pointerPress == EnterConfirmation.gameObject)
        {
            EnterStore();
        }
        else if (eventData.pointerPress == CancelEnter.gameObject)
        {
            HideStorePopUp();
        }
        else if (eventData.pointerPress == Home.gameObject)
        {
            BackToHome();
        }
        else if (eventData.pointerPress == Logout.gameObject)
        {
            LogoutToMainMenu();
        }
    }
}
