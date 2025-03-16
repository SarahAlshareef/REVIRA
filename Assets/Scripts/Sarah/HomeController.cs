using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeController : MonoBehaviour, IPointerClickHandler
{
    public Button StoreSelect;
    public TextMeshProUGUI CoinText;



    void Start()
    {

        // Assign button click events
        StoreSelect.onClick.AddListener(OpenStoreSelection);
        CoinText.text = UserManager.Instance.AccountBalance.ToString("F2");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenStoreSelection();
    }

    public void OpenStoreSelection()
    {
        SceneManager.LoadScene("StoreSelection"); 
    }
}
