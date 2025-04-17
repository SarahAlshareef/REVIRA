using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

public class DeleteAccount : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject deleteConfirmationPanel;
    public Button cancelButton;
    public Button confirmDeleteButton;

    [Header("Checkbox Confirmation")]
    public Toggle confirmationToggle;               // ÇáÊÔíß ÈæßÓ
    public TextMeshProUGUI confirmationText;        // ÇáäÕ Çááí ÈÌÇäÈ ÇáÊÔíß ÈæßÓ

    private string userId;
    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;

        cancelButton?.onClick.AddListener(CancelDelete);
        confirmDeleteButton?.onClick.AddListener(HandleDeleteClick);
        confirmationToggle?.onValueChanged.AddListener(OnToggleChanged);
    }
    public void HideDeletePanel()
    {
        if (deleteConfirmationPanel != null)
            deleteConfirmationPanel.SetActive(false);
    }

    public void ShowDeletePanel()
    {
        deleteConfirmationPanel.SetActive(true);
        ResetPanel();
    }

    void CancelDelete()
    {
        ResetPanel();
        deleteConfirmationPanel.SetActive(false);
    }

    void HandleDeleteClick()
    {
        // áÇÒã ÇáãÓÊÎÏã íÍÏÏ ÇáÊÔíß ÈæßÓ Ãæá
        if (confirmationToggle != null && confirmationToggle.isOn)
        {
            confirmationText.color = Color.green;  // ÌÇåÒ ááÍÐÝ
            DeleteAccountFromFirebase();
        }
        else
        {
            confirmationText.color = Color.red; // ÊäÈíå ááãÓÊÎÏã
        }
    }

    void OnToggleChanged(bool isOn)
    {
        confirmationText.color = isOn ? Color.green : Color.gray;
    }

    void ResetPanel()
    {
        confirmationToggle.isOn = false;
        confirmationText.color = Color.gray;
    }

    void DeleteAccountFromFirebase()
    {
        dbReference.Child("REVIRA").Child("Consumers").Child(userId).RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                auth.CurrentUser.DeleteAsync().ContinueWithOnMainThread(deleteTask =>
                {
                    if (deleteTask.IsCompleted)
                    {
                        SceneManager.LoadScene("MainMenu");
                    }
                    else
                    {
                        Debug.LogError("Failed to delete from Auth.");
                    }
                });
            }
            else
            {
                Debug.LogError("Failed to delete from Database.");
            }
        });
    }
}