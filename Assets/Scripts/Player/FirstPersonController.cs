using Unity.Netcode;
using UnityEngine;

//I learnt this command that stops Unity from accidentally assigning multiple controllers to the same Gameobject. Mostly for bug prevention but could save some headaches once we start to have multiple instances of players in game.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FirstPersonController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private AudioListener audioListener;

    [Header("Movement")]
    [SerializeField] private float mouseSensitivity = 2f;
    //This stops the player from looking directly down. It will help with clipping issues once we have models imported.
    [SerializeField] private float maxLookAngle = 85f;
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float sprintSpeed = 20f;

    [Header("Jump")]
    //This messes with how strong our jumps are. We can make the player jump higher with jumpVelocity, and gravity changes how fast they come back down.
    [SerializeField] private float jumpVelocity = 7f;
    [SerializeField] private float gravity = -5f;
    //I found that 1.05f was the right distance for the capsule at a scale of 1. Our old capsule was scaled to 1.8 so this may need to be changed later on.
    [SerializeField] private float groundCheckDistance = 1.05f;
    [SerializeField] private LayerMask groundMask = ~0;
    //I added this to make jumps feel more consistent. It allows you to jump slightly after falling off of something.
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    //Rigidbody settings.
    private float maxSlopeAngle = 60f;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    //floats for camera movement. Pitch is up and down and Yaw is left and right.
    private float pitch;
    private float yaw;

    //Because I am using moveInput, we should be able to naturally convert controller input into this as well.
    private Vector2 moveInput;
    private bool sprintHeld;

    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;

    //lastGroundedTime is for Coyote Time, and lastJumpPressedTime prevents jump spam (mostly so we prevent bhopping and our game doesn't play like Quake).
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    //This is the Start() method used for networking objects. We are spawning in this gameobject when the network is created.
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.freezeRotation = true;

        //Remove any drag on the floor or air. We can change this later if we want the game to feel less responsive.
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        //Make sure the player has a camera on it.
        if (playerCamera == null)
        {
            var cam = GetComponentInChildren<Camera>(true);
            if (cam != null) playerCamera = cam.transform;
        }

        //Make sure the player has an audio listener.
        if (audioListener == null)
            audioListener = GetComponentInChildren<AudioListener>(true);

        //Assign
        if (playerCamera != null)
        {
            var cam = playerCamera.GetComponent<Camera>();
            if (cam != null) cam.enabled = IsOwner;
        }

        //Assign
        if (audioListener != null)
            audioListener.enabled = IsOwner;

        //Hide that cursor
        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        //Return the cursor if the player is disabled
        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Update()
    {
        //Grabs inputs
        if (!IsOwner) return;

        float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        yaw += mx;
        pitch -= my;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        sprintHeld = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetButtonDown("Jump"))
            lastJumpPressedTime = Time.time;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        //This moves the player every frame.
        DoGroundCheck();
        ApplyLook();
        ApplyMovement();
        ApplyGravityAndJump();
    }

    //This checks if the player is touching the ground, and allows them to jump if able.
    private void DoGroundCheck()
    {
        float radius = Mathf.Max(0.01f, capsule.radius * 0.95f);

        float bottomY = transform.position.y + radius + 0.02f;
        Vector3 origin = new Vector3(transform.position.x, bottomY, transform.position.z);

        float castDistance = Mathf.Max(groundCheckDistance, 0.05f);

        //Spherecast is a more reliable way to detect if a player is grounded. Raycasts may miss if the player is on the edge of something, or if it started in the ground already. This is more reliable for netcode essentially.
        if (Physics.SphereCast(
                origin,
                radius,
                Vector3.down,
                out RaycastHit hit,
                castDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            groundNormal = hit.normal;

            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
            isGrounded = slopeAngle <= maxSlopeAngle;

            if (isGrounded)
                lastGroundedTime = Time.time;
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector3.up;
        }

        //Again, we don't want any drag right now and we want our character to instantly snap and move. We may change this at a later date to change how the player feel is.
        rb.linearDamping = 0f;
    }

    private void ApplyLook()
    {
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));

        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void ApplyMovement()
    {
        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        float speed = sprintHeld ? sprintSpeed : moveSpeed;

        Vector3 wishDir = (transform.right * inputDir.x + transform.forward * inputDir.z);

        if (isGrounded)
            wishDir = Vector3.ProjectOnPlane(wishDir, groundNormal).normalized;

        Vector3 v = rb.linearVelocity;
        Vector3 horizontal = wishDir * speed;

        rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
    }

    private void ApplyGravityAndJump()
    {
        Vector3 v = rb.linearVelocity;

        //Apply gravity
        v.y += gravity * Time.fixedDeltaTime;

        bool canUseCoyote = (Time.time - lastGroundedTime) <= coyoteTime;
        bool hasBufferedJump = (Time.time - lastJumpPressedTime) <= jumpBufferTime;

        //Jump
        if (hasBufferedJump && (isGrounded || canUseCoyote))
        {
            if (v.y < 0f) v.y = 0f;
            v.y += jumpVelocity;

            lastJumpPressedTime = float.NegativeInfinity;
            isGrounded = false;
        }

        rb.linearVelocity = v;
    }
}