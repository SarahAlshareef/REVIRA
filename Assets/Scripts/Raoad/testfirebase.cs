using System.Collections;
using System.Collections.Generic;
using Firebase;
using UnityEngine;
using Firebase.Extensions;
public class testfirebase : MonoBehaviour
{
    
        void Start()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    Debug.Log("Firebase is connected successfully!");
                }
                else
                {
                    Debug.LogError("Firebase connection failed: " + task.Result);
                }
            });
        }
    }

