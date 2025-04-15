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
    public TextMeshProUGUI subMessageText;   // ÝÞØ ÇáäÕ Çááí ÊÍÊ ÇáÚäæÇä

    private string userId;
    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    private bool firstClick = false;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;

        cancelButton?.onClick.AddListener(CancelDelete);
        confirmDeleteButton?.onClick.AddListener(HandleDeleteClick);
    }

    public void ShowDeletePanel()
    {
        deleteConfirmationPanel.SetActive(true);
        ResetPanel();  // íÑÌÚ ßá ÔíÁ áæÖÚå ÇáØÈíÚí
    }

    void CancelDelete()
    {
        ResetPanel(); // íÑÌÚ ßá ÔíÁ ßÃä ãÇ ÖÛØ ÔíÁ
        deleteConfirmationPanel.SetActive(false);
    }

    void HandleDeleteClick()
    {
        if (!firstClick)
        {
            firstClick = true;
            subMessageText.text = "Click again to delete account";
            subMessageText.color = Color.red;
        }
        else
        {
            DeleteAccountFromFirebase();
        }
    }

    void ResetPanel()
    {
        firstClick = false;
        subMessageText.text = "We're sad to see you go!\nIf you delete your account, all your data will be wiped.\nAre you absolutely sure you want to proceed?";
        subMessageText.color = Color.gray;
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
                        SceneManager.LoadScene("SignUpScene");
                    }
                    else
                    {
                        ShowError("Failed to delete account from Auth.");
                    }
                });
            }
            else
            {
                ShowError("Failed to delete data from Database.");
            }
        });
    }

    void ShowError(string message)
    {
        subMessageText.text = message;
        subMessageText.color = Color.red;
    }
}