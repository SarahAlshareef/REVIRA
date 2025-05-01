using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwipInteraction : MonoBehaviour
{
    public Scrollbar scrollbar;
    public float controllerScrollSpeed = 2.5f;

    private float scroll_pos = 0f;
    private float[] pos;
    private int childCount = 0;
    private float distance;

    void Start()
    {
        InitPositions();
    }

    void Update()
    {
        // Check if new children have been added dynamically
        if (transform.childCount != childCount)
        {
            InitPositions();
        }

        scroll_pos = scrollbar.value;

        HandleMouseInput();
        HandleControllerInput();
        SnapToNearest();
        ScaleChildren();
    }

    void InitPositions()
    {
        childCount = transform.childCount;

        if (childCount < 2)
        {
            pos = new float[] { 0f };
            distance = 1f;
            return;
        }

        pos = new float[childCount];
        distance = 1f / (childCount - 1f);

        for (int i = 0; i < childCount; i++)
        {
            pos[i] = distance * i;
        }
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButton(0))
        {
            scroll_pos = scrollbar.value;
        }
    }

    void HandleControllerInput()
    {
        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        if (Mathf.Abs(thumbstick.x) > 0.1f)
        {
            scroll_pos += thumbstick.x * controllerScrollSpeed * Time.deltaTime;
            scroll_pos = Mathf.Clamp01(scroll_pos);
            scrollbar.value = scroll_pos;
        }
    }

    void SnapToNearest()
    {
        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        if (!Input.GetMouseButton(0) && Mathf.Abs(thumbstick.x) < 0.1f)
        {
            for (int i = 0; i < childCount; i++)
            {
                if (scroll_pos < pos[i] + (distance / 2) && scroll_pos > pos[i] - (distance / 2))
                {
                    scrollbar.value = Mathf.Lerp(scrollbar.value, pos[i], 0.1f);
                }
            }
        }
    }

    void ScaleChildren()
    {
        for (int i = 0; i < childCount; i++)
        {
            if (scroll_pos < pos[i] + (distance / 2) && scroll_pos > pos[i] - (distance / 2))
            {
                transform.GetChild(i).localScale = Vector3.Lerp(transform.GetChild(i).localScale, new Vector3(1f, 1f, 1f), 0.1f);

                for (int a = 0; a < childCount; a++)
                {
                    if (a != i)
                    {
                        transform.GetChild(a).localScale = Vector3.Lerp(transform.GetChild(a).localScale, new Vector3(0.8f, 0.8f, 0.8f), 0.1f);
                    }
                }
            }
        }
    }
}
