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



    void Start()
    {

        // Assign button click events
        StoreSelect.onClick.AddListener(OpenStoreSelection);
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
