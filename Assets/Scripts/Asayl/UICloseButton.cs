using UnityEngine;

public class UICloseButton : MonoBehaviour
{
    public GameObject popupToClose;

    public void CloseCurrentPopup()
    {
        if (popupToClose != null)
        {
            popupToClose.SetActive(false);
        }

     
        if (VRProductClickHandler.currentActiveHandler != null)
        {
            VRProductClickHandler.currentActiveHandler.ClosePreview(); 
        }
    }

}