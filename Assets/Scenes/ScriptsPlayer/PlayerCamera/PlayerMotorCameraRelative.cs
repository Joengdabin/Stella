using UnityEngine;

/// <summary>
/// Camera-relative 3rd person movement.
/// - WASD/Joystick moves relative to camera forward/right
/// - Optionally rotate the player to face movement direction
/// - All tunables exposed in Inspector (no hardcoded tuning)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMotorCameraRelative : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Usually your Main Camera transform. If empty, will auto-find Camera.main.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [Tooltip("Move speed in meters/second.")]
    [SerializeField] private float moveSpeed = 4.5f;

    [Tooltip("How fast the character turns to face move direction.")]
    [SerializeField] private float turnSpeed = 12f;

    [Tooltip("If true, character rotates to face move direction.")]
    [SerializeField] private bool faceMoveDirection = true;

    [Tooltip("If true, diagonal input is normalized (prevents faster diagonal movement).")]
    [SerializeField] private bool normalizeInput = true;

    [Header("Gravity / Ground")]
    [Tooltip("Gravity acceleration (negative value recommended, e.g., -25).")]
    [SerializeField] private float gravity = -25f;

    [Tooltip("Small downward force to keep grounded.")]
    [SerializeField] private float groundedStickForce = -2f;

    [Tooltip("Optional: allow jump later (keep off for now).")]
    [SerializeField] private bool enableJump = false;

    [Tooltip("Jump height in meters (used only if enableJump).")]
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("Input")]
    [Tooltip("PC default input axes. For mobile, you can later feed these values from joystick.")]
    [SerializeField] private string horizontalAxis = "Horizontal";
    [SerializeField] private string verticalAxis = "Vertical";
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;

    [Tooltip("Sprint multiplier when run key held.")]
    [SerializeField] private float sprintMultiplier = 1.35f;

    private CharacterController _cc;
    private Vector3 _velocity;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null) cameraTransform = Camera.main.transform;
            else return;
        }

        // 1) Read input
        float x = Input.GetAxisRaw(horizontalAxis);
        float z = Input.GetAxisRaw(verticalAxis);

        Vector2 input = new Vector2(x, z);
        if (normalizeInput && input.sqrMagnitude > 1f) input.Normalize();

        // 2) Convert input to camera-relative move direction
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        // flatten y so movement stays on ground plane
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * input.y + camRight * input.x;

        // 3) Apply movement
        float speed = moveSpeed;
        if (Input.GetKey(runKey)) speed *= sprintMultiplier;

        _cc.Move(moveDir * (speed * Time.deltaTime));

        // 4) Rotate character to face move direction (solves "capsule has no front" confusion)
        if (faceMoveDirection && moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }

        // 5) Gravity
        bool grounded = _cc.isGrounded;
        if (grounded && _velocity.y < 0f)
            _velocity.y = groundedStickForce;

        if (enableJump && grounded && Input.GetKeyDown(KeyCode.Space))
        {
            // v = sqrt(h * -2g)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }
}