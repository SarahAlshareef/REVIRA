
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuItem : MonoBehaviour
{
  
    [HideInInspector] public RectTransform rectTrans;

    void Awake()
    {
       
        rectTrans = GetComponent<RectTransform>();
    }
}