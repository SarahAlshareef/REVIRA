using UnityEngine;
using UnityEngine.UI;

public class ThumbstickScrollVR : MonoBehaviour
{
    [Header("References")]
    public ScrollRect scrollRect;

    [Header("Settings")]
    public float scrollSpeed = 1.0f;
    public bool useRightHand = true;
    public bool useLeftHand = true;

    private string rightAxis = "Oculus_CrossPlatform_SecondaryThumbstickVertical";
    private string leftAxis = "Oculus_CrossPlatform_PrimaryThumbstickVertical";

    void Update()
    {
        float input = 0f;

        if (useRightHand)
            input += Input.GetAxis(rightAxis);

        if (useLeftHand)
            input += Input.GetAxis(leftAxis);

        if (Mathf.Abs(input) > 0.1f)
        {
            float newPos = scrollRect.verticalNormalizedPosition + input * scrollSpeed * Time.deltaTime;
            newPos = Mathf.Clamp01(newPos);
            scrollRect.verticalNormalizedPosition = newPos;
        }
    }
}
