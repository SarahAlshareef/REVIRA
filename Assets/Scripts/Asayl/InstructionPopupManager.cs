using UnityEngine;
using UnityEngine.UI;

public class InstructionPopupManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject instructionPanel;
    public Toggle confirmToggle;
    public Button closeButton;

    [Header("Other References")]
    public PlayerControlManager controlManager;     
    public PlayerRotation rotationScript;

    public void Start()
    {
        instructionPanel.SetActive(true);
       
        Transform cam = Camera.main.transform;
        instructionPanel.transform.rotation = Quaternion.LookRotation(cam.forward);
        instructionPanel.transform.position = cam.position + cam.forward * 2f + cam.up * 3.0f;

        controlManager.LockControls();

        closeButton.interactable = false;
        closeButton.onClick.AddListener(CloseInstructions);
        confirmToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    public void OnToggleChanged(bool isOn)
    {
        closeButton.interactable = isOn;
    }

    public void CloseInstructions()
    {
        instructionPanel.SetActive(false);
        controlManager.UnlockControls(); 
        rotationScript.ShowRotationPopup();  
    }
}
