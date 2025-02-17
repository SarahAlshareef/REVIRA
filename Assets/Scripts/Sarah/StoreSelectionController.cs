using System.Collections;
using System.Collections.Generic;
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



    void Start()
    {

        // Assign button click events
        Enter.onClick.AddListener(ShowStorePopUp);
        EnterConfirmation.onClick.AddListener(EnterStore);
        CancelEnter.onClick.AddListener(HideStorePopUp);
        Home.onClick.AddListener(BackToHome);
        Logout.onClick.AddListener(LogoutToMainMenu);

        storePopup.SetActive(false); // Hide pop-up
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
}
