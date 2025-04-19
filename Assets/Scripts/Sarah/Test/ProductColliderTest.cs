using UnityEngine;

public class ProductColliderTest : MonoBehaviour
{
    // This method is called when the object is clicked
    private void OnMouseDown()
    {
        Debug.Log("Collider works! You clicked on: " + gameObject.name);
    }
}
