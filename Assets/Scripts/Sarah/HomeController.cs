using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeController : MonoBehaviour
{
    public Button StoreSelect;
    public TextMeshProUGUI WelcomeText;
    public TextMeshProUGUI CoinText;



    void Start()
    {

        // Assign button click events
        StoreSelect.onClick.AddListener(OpenStoreSelection);
        WelcomeText.text = $"Welcome \ndear {UserManager.Instance.FirstName} in \nREVIRA";
        CoinText.text = UserManager.Instance.AccountBalance.ToString("F2");
    }

   

    public void OpenStoreSelection()
    {
        SceneManager.LoadScene("StoreSelection");
        ///SceneManager.LoadScene("Promotional");
        //SceneManager.LoadScene("CartTest");
        //SceneManager.LoadScene("Raoad");
    }
}
