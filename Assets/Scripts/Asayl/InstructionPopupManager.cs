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
        Vector3 flatForward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        Vector3 targetPos = cam.position + flatForward * 3.0f;
        targetPos.y = cam.position.y + 2.5f; // Fixed height
        instructionPanel.transform.position = targetPos;
        instructionPanel.transform.rotation = Quaternion.LookRotation(flatForward);

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
