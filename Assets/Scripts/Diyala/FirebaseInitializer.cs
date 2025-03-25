using UnityEngine;
using Firebase;
using Firebase.Auth;

public class FirebaseInitializer : MonoBehaviour
{
    public static FirebaseAuth auth;
    private bool firebaseReady = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject); 

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var status = task.Result;
            if (status == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                firebaseReady = true;
                Debug.Log("Firebase is ready!");
            }
            else
            {
                Debug.LogError("Could not resolve Firebase: " + status);
            }
        });
    }

    public bool IsFirebaseReady()
    {
        return firebaseReady;
    }
}