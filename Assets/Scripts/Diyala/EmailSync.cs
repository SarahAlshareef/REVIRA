// Unity
using UnityEngine;
// Firebase
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
// C#
using System.Collections;

public class EmailSync : MonoBehaviour
{
    public static EmailSync Instance { get; private set; }

    private bool isSynced = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartSync()
    {
        if (!isSynced)
        {
            StartCoroutine(EmailWatcher());
        }
    }

    IEnumerator EmailWatcher()
    {
        while (!isSynced)
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null)
            {
                yield return null;
                continue;
            }

            bool waiting = true;

            user.ReloadAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    string authEmail = user.Email;
                    string userId = user.UserId;

                    DatabaseReference userRef = FirebaseDatabase.DefaultInstance
                        .RootReference.Child("REVIRA").Child("Consumers").Child(userId).Child("email");

                    userRef.GetValueAsync().ContinueWithOnMainThread(dbTask =>
                    {
                        if (dbTask.IsCompleted && dbTask.Result.Exists)
                        {
                            string dbEmail = dbTask.Result.Value.ToString();

                            if (authEmail != dbEmail)
                            {
                                userRef.SetValueAsync(authEmail).ContinueWithOnMainThread(setTask =>
                                {
                                    if (setTask.IsCompleted)
                                    {
                                        Debug.Log("Email synced successfully.");
                                        UserManager.Instance.UpdateEmail(authEmail);
                                        isSynced = true;
                                    }
                                });
                            }
                            else
                            {
                                Debug.Log("Emails already match. No sync needed.");
                                UserManager.Instance.UpdateEmail(authEmail);
                                isSynced = true;
                            }
                        }

                        waiting = false;
                    });
                }
                else
                {
                    waiting = false;
                }
            });

            while (waiting)
            {
                yield return null;
            }
        }
    }
}