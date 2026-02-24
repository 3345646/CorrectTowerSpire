using UnityEngine;

public class CameraRotation : MonoBehaviour
{
    public float RotationSpeed = 1f;

    // For storing current rotation values
    private float _rotationX = 0f;
    private float _rotationY = 0f;

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _rotationY += mouseX * RotationSpeed; // Yaw (left/right)
        _rotationX -= mouseY * RotationSpeed; // Pitch (up/down)

        

        // Apply yaw to the parent object (e.g., player body)
        transform.localRotation = Quaternion.Euler(_rotationX, _rotationY, 0f);

        // Apply pitch to the camera itself (if this script is on the camera)
        // Or to a child object if this script is on the player
        // Example for applying pitch to the camera:
        //Camera.main.transform.localRotation = Quaternion.Euler(_rotationX, 0f, 0f);
    }
}