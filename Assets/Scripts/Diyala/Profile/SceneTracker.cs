// Unity
using UnityEngine;

public class SceneTracker : MonoBehaviour
{
    public static SceneTracker Instance { get; private set; }
    void Awake()
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

    public string PreviousSceneName { get; private set; }

    public void SetPreviousScene(string SceneName)
    {
        PreviousSceneName = SceneName;
    }
}