using UnityEngine;
using UnityEngine.InputSystem;

public class LabWalkTester : MonoBehaviour
{
    public float speed = 3f;
    private Camera vrCamera;

    void Start()
    {
        vrCamera = Camera.main;
    }

    void Update()
    {
        if (vrCamera == null) return;

        float h = 0f;
        float v = 0f;

        // Using the New Input System instead of the old legacy Input
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
        }

        if (h != 0 || v != 0)
        {
            // Move relative to where the camera is looking
            Vector3 forward = vrCamera.transform.forward;
            Vector3 right = vrCamera.transform.right;
            
            // Keep movement flat on the floor
            forward.y = 0; 
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDir = (forward * v + right * h).normalized;
            transform.position += moveDir * speed * Time.deltaTime;
        }
    }
}
