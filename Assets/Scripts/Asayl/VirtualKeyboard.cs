using UnityEngine;
using TMPro;

public class VirtualKeyboard : MonoBehaviour
{
    private TouchScreenKeyboard overlayKeyboard; // VR Keyboard Instance
    public TMP_InputField inputField; // Assign in the Inspector
    public static string inputText = ""; // Stores keyboard text

    void Update()
    {
        // Open VR keyboard when pressing "A" on the right controller
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            OpenKeyboard();
        }

        // Retrieve typed text if keyboard is active
        if (overlayKeyboard != null && overlayKeyboard.active)
        {
            inputText = overlayKeyboard.text;
            inputField.text = inputText; // Update the input field
        }
    }

    public void OpenKeyboard()
    {
        Debug.Log("Opening VR Keyboard...");
        // Open the TouchScreen Keyboard (Default Keyboard)
        overlayKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
    }
}
