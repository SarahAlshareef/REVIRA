using UnityEngine;

public class LaserPointerController : MonoBehaviour
{
    public LineRenderer laser;
    public Transform controllerTransform;

    void Update()
    {
        if (laser != null && controllerTransform != null)
        {
            laser.SetPosition(0, controllerTransform.position);
            laser.SetPosition(1, controllerTransform.position + controllerTransform.forward * 3);
        }
    }
}