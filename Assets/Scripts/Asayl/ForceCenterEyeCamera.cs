using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceCenterEyeCamera : MonoBehaviour
{
    public Camera centerEyeCamera; 

    void Start()
    {
        if (centerEyeCamera != null) 
        {
            centerEyeCamera.gameObject.SetActive(true); 
            centerEyeCamera.tag = "MainCamera"; 
        }
        else
        {
            Debug.LogError("Center Eye Camera Not inserted."); 
        }
    }
}