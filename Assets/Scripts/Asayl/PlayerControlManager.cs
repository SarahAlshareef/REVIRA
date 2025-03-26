using UnityEngine;

public class PlayerControlManager : MonoBehaviour
{
    [Header("References")]
    public OVRPlayerMovement movement;     //  player's movement script
    public PlayerRotation rotation;        //  player's rotation script

    private int lockCount = 0;             // Counter to handle multiple locks from different sources


    /// disable movement and rotation.
    public void LockControls()
    {
        lockCount++;
        UpdateControlState();
    }

  
    ///  re-enable movement and rotation.
    /// Only re-enables when all locks have been released.
    public void UnlockControls()
    {
        lockCount = Mathf.Max(0, lockCount - 1);
        UpdateControlState();
    }

 
    /// update the enabled state of movement and rotation scripts 
    private void UpdateControlState()
    {
        bool shouldDisable = lockCount > 0;

        if (movement != null)
            movement.enabled = !shouldDisable;

        if (rotation != null)
            rotation.enabled = !shouldDisable;
    }
}

