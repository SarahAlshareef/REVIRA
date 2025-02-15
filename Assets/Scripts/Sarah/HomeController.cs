using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeController : MonoBehaviour
{
    public Button StoreSelect;
    


    void Start()
    {

        // Assign button click events
        StoreSelect.onClick.AddListener(OpenStoreSelection);
    }


    public void OpenStoreSelection()
    {
        SceneManager.LoadScene("StoreSelection"); 
    }
}
