using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogoutGlobalPopup : MonoBehaviour
{

    public static LogoutGlobalPopup Instance; // Singleton instance to access the popup from any scene
    public GameObject logoutPopupPanel; // Prefab reference for the logout confirmation popup
    private GameObject currentPopup; // Stores the current active popup instance

    private void Awake()
    {
        // Ensure only one instance exists across all scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Prevent destruction when changing scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    public void ShowLogoutPopup()
    {
        // Prevent multiple popups from being instantiated
        if (currentPopup != null)
        {
            return;
        }

        // Load the popup prefab from Resources and instantiate it inside the current scene's Canvas
        GameObject popupPrefab = Resources.Load<GameObject>("LogoutPopup");

        if (popupPrefab != null)
        {
            currentPopup = Instantiate(popupPrefab, FindObjectOfType<Canvas>().transform);
        }
        else
        {
            Debug.LogError("Logout popup prefab not found in Resources folder!");
        }
    }

    public void DestroyPopup()
    {
        // Destroy the popup if it exists
        if (currentPopup != null)
        {
            Destroy(currentPopup);
            currentPopup = null;
        }
    }
}