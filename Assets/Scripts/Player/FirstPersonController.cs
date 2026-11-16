using Unity.Netcode;
using UnityEngine;

//Command that stops Unity from accidentally assigning multiple controllers to the same Gameobject
//Mostly for bug prevention but could save some headaches once we start to have multiple instances of players in game
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
    [SerializeField] private float maxLookAngle = 85f; //This stops the player from looking directly down. It will help with clipping issues once we have models imported.
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float sprintSpeed = 20f;

    [Header("Movement Smoothing")]
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 10f;

    [Header("Head Bobbing")]
    [SerializeField] private float bobFrequency = 6f;
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float sprintBobMultiplier = 1.5f;
    [SerializeField] private float bobReturnSpeed = 10f;

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

    [Header("Physics")]
    [SerializeField] private float maxSlopeAngle = 60f;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    //Floats for camera movement. Pitch is up and down and Yaw is left and right.
    private float pitch;
    private float yaw;

    //Because I am using moveInput, we should be able to naturally convert controller input into this as well.
    private Vector2 moveInput;
    private bool sprintHeld;
    private bool isMoving;
    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;

    //lastGroundedTime is for Coyote Time, and lastJumpPressedTime prevents jump spam (mostly so we prevent bhopping and our game doesn't play like Quake).
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    //Head bob state
    private float bobTimer;
    private Vector3 cameraInitialLocalPosition;

    // networked animation states
    public Animator animator;

    private NetworkVariable<bool> networkWalking = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> networkSprinting = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> networkGrounded = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // stores last states sent to the server so ServerRpc isn't sent when there are no state changes
    private bool lastWalk;
    private bool lastSprint;
    private bool lastGround;

    public Camera PlayerCamera
    {
        get
        {
            if (playerCamera == null)
                return null;

            return playerCamera.GetComponent<Camera>();
        }
    }


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
            Camera cam = GetComponentInChildren<Camera>(true);
            if (cam != null) playerCamera = cam.transform;
        }

        //Make sure the player has an audio listener.
        if (audioListener == null)
            audioListener = GetComponentInChildren<AudioListener>(true);

        //Assign
        if (playerCamera != null)
        {
            Camera cam = playerCamera.GetComponent<Camera>();
            if (cam != null) cam.enabled = IsOwner;
            cameraInitialLocalPosition = playerCamera.localPosition;
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

        HideOwnModel();

        // listen for animation updates from the server
        networkWalking.OnValueChanged += OnNetworkAnimationChanged;
        networkSprinting.OnValueChanged += OnNetworkAnimationChanged;
        networkGrounded.OnValueChanged += OnNetworkAnimationChanged;

        // find the current character model's Animator
        RefreshAnimator();
        ApplyAnimatorParameters(networkWalking.Value, networkSprinting.Value, networkGrounded.Value);
    }


    public override void OnNetworkDespawn()
    {
        // stop listening for animation updates
        networkWalking.OnValueChanged -= OnNetworkAnimationChanged;
        networkSprinting.OnValueChanged -= OnNetworkAnimationChanged;
        networkGrounded.OnValueChanged -= OnNetworkAnimationChanged;

        base.OnNetworkDespawn();
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
        if (!IsSpawned || !IsOwner) return;

        RefreshAnimator();
 
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
        if (!IsSpawned || !IsOwner) return;
        //This moves the player every frame.
        DoGroundCheck();
        ApplyLook();
        ApplyMovement();
        ApplyGravityAndJump();

        //update the local animations immediately and synchronize with other players
        UpdateAnimationState();
    }


    private void LateUpdate()
    {
        if (!IsSpawned)
            return;

        RefreshAnimator();

        if (!IsOwner)
        {
            ApplyAnimatorParameters(networkWalking.Value, networkSprinting.Value, networkGrounded.Value);
            return;
        }

        HandleHeadBobbing();
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
        Vector3 wishHorizontal = wishDir * speed;
        Vector3 curHorizontal = new Vector3(v.x, 0f, v.z);
        float smoothingRate = inputDir.sqrMagnitude > 0.01f ? acceleration : deceleration;
        Vector3 smoothedHorizontal = Vector3.MoveTowards(curHorizontal, wishHorizontal, smoothingRate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(smoothedHorizontal.x, v.y, smoothedHorizontal.z);
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


    private void HandleHeadBobbing()
    {
        Vector3 v = rb.linearVelocity;
        float horizontalSpeed = new Vector2(v.x, v.z).magnitude;
        isMoving = horizontalSpeed > 0.1f && isGrounded;
        Vector3 targetCameraPosition = cameraInitialLocalPosition;

        // apply head bobbing if the player is mmoving
        if (isMoving)
        {
            float speedMultiplier = sprintHeld ? sprintBobMultiplier : 1f;
            bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;

            float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude * speedMultiplier;
            targetCameraPosition = cameraInitialLocalPosition + new Vector3(0f, bobOffset, 0f);
        }
        else
        {
            bobTimer = 0f;
        }
        // smoothly return the camera to its normal height when the player stops walking/sprinting
        playerCamera.localPosition = Vector3.Lerp(
            playerCamera.localPosition,
            targetCameraPosition,
            bobReturnSpeed * Time.deltaTime
        );
    }


    // finds the Animator on the current character model
    private void RefreshAnimator()
    {
        Animator[] childAnimators = GetComponentsInChildren<Animator>(true);
        Animator activeAnimator = null;

        foreach (Animator childAnimator in childAnimators)
        {
            if (childAnimator != null && childAnimator.enabled && childAnimator.gameObject.activeInHierarchy)
            {
                activeAnimator = childAnimator;
                break;
            }
        }
        animator = activeAnimator;
    }


    // updates the animator state for the local player
    private void UpdateAnimationState()
    {
        Vector3 v = rb.linearVelocity;
        float horizontalSpeed = new Vector2(v.x, v.z).magnitude;
        isMoving = horizontalSpeed > 0.1f && isGrounded;

        ApplyAnimatorParameters(isMoving, sprintHeld, isGrounded);

        // only send an update when an animation state changes
        if (isMoving == lastWalk &&
            sprintHeld == lastSprint &&
            isGrounded == lastGround)
        {
            return;
        }

        lastWalk = isMoving;
        lastSprint = sprintHeld;
        lastGround = isGrounded;

        SetAnimationStateServerRpc(isMoving, sprintHeld, isGrounded); // send state changes to other clients
    }


    [ServerRpc]
    private void SetAnimationStateServerRpc(bool walking, bool sprinting, bool grounded)
    {
        Debug.Log(
        $"[Server] Player {OwnerClientId}: " +
        $"Walking={walking}, " +
        $"Sprinting={sprinting}, " +
        $"Grounded={grounded}"
        );

        networkWalking.Value = walking;
        networkSprinting.Value = sprinting;
        networkGrounded.Value = grounded;
    }


    // called when an animation state changes
    private void OnNetworkAnimationChanged(bool previousValue, bool newValue)
    {
        ApplyAnimatorParameters(networkWalking.Value, networkSprinting.Value, networkGrounded.Value);
    }


    private void ApplyAnimatorParameters(bool walking, bool sprinting, bool grounded)
    {
        if (animator == null || !animator.gameObject.activeInHierarchy)
            return;

        animator.SetBool("Walking", walking);
        animator.SetBool("Sprinting", sprinting);
        animator.SetBool("Grounded", grounded);
    }


    // hides the player's own local character model from only them
    private void HideOwnModel()
    {
        // only hide the model for the player who is this character
        if (!IsOwner)
            return;

        // find all renderers on the player
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false; // hide the character model
        }
    }
}
