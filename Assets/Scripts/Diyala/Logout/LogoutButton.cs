using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogoutManager : MonoBehaviour
{
    public void ShowLogoutPopup()
    {
        // Check if the GlobalPopupManager exists and call its function to show the popup
        if (LogoutGlobalPopup.Instance != null)
        {
            LogoutGlobalPopup.Instance.ShowLogoutPopup();
        }
        else
        {
            Debug.LogError("GlobalPopupManager not found!");
        }
    }
}