using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneTracker : MonoBehaviour
{
    public static SceneTracker Instance { get; private set; }

    public string PreviousSceneName { get; private set; }

    public void SetPreviousScene(string SceneName)
    {
        PreviousSceneName = SceneName;
    }
}
