using UnityEngine;
using Oculus.Interaction;

public class BallInteraction : MonoBehaviour
{
    private Rigidbody rb;
    private Grabbable grab;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<Grabbable>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        DisableInteraction();
    }

    public void EnableInteraction()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        grab.enabled = true;
    }

    public void DisableInteraction()
    {
        rb.isKinematic = true;
        rb.useGravity = false;
        grab.enabled = false;

        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }
}
