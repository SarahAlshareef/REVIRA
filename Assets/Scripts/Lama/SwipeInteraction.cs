using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwipInteraction : MonoBehaviour
{
    // 
    public Scrollbar scrollbar;
    float scroll_pos = 0;
    float[] pos;

    void Update()
    {
        int childCount = transform.childCount;
        pos = new float[childCount];
        float distance = 1f / (childCount - 1f);

        //
        for (int i = 0; i < childCount; i++)
        {
            pos[i] = distance * i;
        }

        // Always update scroll_pos from the scrollbar's current value.
        scroll_pos = scrollbar.value;

      
        if (!Input.GetMouseButton(0))
        {
            for (int i = 0; i < childCount; i++)
            {

                //pos0=0 . dis=0 
                if (scroll_pos < pos[i] + (distance / 2) && scroll_pos > pos[i] - (distance / 2))
                {
                    scrollbar.value = Mathf.Lerp(scrollbar.value, pos[i], 0.1f);
                }
            }
        }

        // 
        for (int i = 0; i < childCount; i++)
        {
            if (scroll_pos < pos[i] + (distance / 2) && scroll_pos > pos[i] - (distance / 2))
            {
                // 
                transform.GetChild(i).localScale = Vector3.Lerp(transform.GetChild(i).localScale, new Vector3(1f, 1f, 1f), 0.1f);

                // Scale down all other children.
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
