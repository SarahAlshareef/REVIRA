using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeController : MonoBehaviour
{
    public Button StoreSelect;
    public Button Logout;



    void Start()
    {

        // Assign button click events
        StoreSelect.onClick.AddListener(OpenStoreSelection);
        Logout.onClick.AddListener(LogoutToMainMenu);
    }


    public void OpenStoreSelection()
    {
        SceneManager.LoadScene("StoreSelection"); 
    }

    public void LogoutToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
