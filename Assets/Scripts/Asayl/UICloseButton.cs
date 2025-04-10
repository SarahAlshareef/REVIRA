using UnityEngine;

public class UIClosetButton : MonoBehaviour
{
    // This method will be called when the close button is selected (clicked)
    public void CloseCurrentPopup()
    {
        if (VRProductClickHandler.currentActiveHandler != null)
        {
            VRProductClickHandler handler = VRProductClickHandler.currentActiveHandler;
            handler.ClosePreview();
        }
    }
}