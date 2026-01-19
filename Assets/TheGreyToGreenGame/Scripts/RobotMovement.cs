using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RobotMovement : MonoBehaviour
{
    public float speed = 6f;
    public float verticalSpeed = 6f;
    public float turnSpeed = 120f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        float forward = Input.GetAxis("Vertical");    // W / S
        float turn = Input.GetAxis("Horizontal");     // A / D

        float vertical = 0f;
        if (Input.GetKey(KeyCode.E)) vertical = 1f;   // UP
        if (Input.GetKey(KeyCode.Q)) vertical = -1f;  // DOWN

        Vector3 velocity =
            transform.forward * forward * speed +
            Vector3.up * vertical * verticalSpeed;

        rb.linearVelocity = velocity;

        if (Mathf.Abs(turn) > 0.01f)
        {
            rb.MoveRotation(
                rb.rotation *
                Quaternion.Euler(0f, turn * turnSpeed * Time.fixedDeltaTime, 0f)
            );
        }
    }
}
