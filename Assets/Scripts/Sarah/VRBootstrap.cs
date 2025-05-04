using UnityEngine;

public class VRBootstrap : MonoBehaviour
{
    void Awake()
    {
        // Keep the app running and continue receiving log callbacks
        // even when the Unity window is unfocused (e.g. in VR).
        Application.runInBackground = true;
    }
}
