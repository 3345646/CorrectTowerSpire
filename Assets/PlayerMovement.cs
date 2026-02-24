using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5.0f;
    public bool freeze;
    public Rigidbody rb;
    public bool ActiveGrapple;

    private Vector3 movementDirection;
    private float horizontalInput;
    private float verticalInput;
    private float jumpInput;

    void Start()
    {
        // Ensure Rigidbody is found
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        Cursor.lockState = CursorLockMode.Locked;
    }

    // This is the ONLY Update method in this file
    void Update()
    {
        // Read input here for responsiveness
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        jumpInput = Input.GetAxis("Jump");
    }

    void FixedUpdate()
    {
        if (freeze)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // Only apply normal movement IF we are NOT grappling
        if (!ActiveGrapple)
        {
            // Calculate the direction based on camera and apply velocity
            CalculateMovementDirection();

            // jump velocity
            float YVel = jumpInput * speed * Time.deltaTime;
            



            // Apply physics-based velocity while preserving existing Y (gravity) velocity
            Vector3 horizontalMovement = movementDirection * speed;
            rb.linearVelocity = new Vector3(horizontalMovement.x, rb.linearVelocity.y, horizontalMovement.z);
        }
    }

    private void CalculateMovementDirection()
    {
        // Get camera direction vectors
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        // Project the camera vectors onto the horizontal plane (Y=0) so moving forward doesn't push into the ground
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate the final movement direction based on player input relative to camera
        movementDirection = cameraForward * verticalInput + cameraRight * horizontalInput;
        movementDirection.Normalize(); // Normalize to ensure consistent speed when moving diagonally
    }
}
