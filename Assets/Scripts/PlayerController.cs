using UnityEngine;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float sprintSpeed = 10f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;

    [Header("Movement Smoothing")]
    public float acceleration = 12f;
    public float deceleration = 10f;

    [Header("Look Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float verticalLookLimit = 80f;

    [Header("Head Bobbing")]
    public float bobFrequency = 6f;
    public float bobAmplitude = 0.05f;
    public float sprintBobMultiplier = 1.5f;

    [Header("Player Stats")]
    public int maxHealth = 100;
    public int startingPoints = 500;

    [Header("UI References")]
    public TMP_Text healthText;
    public TMP_Text pointsText;

    private int currentHealth;
    private int currentPoints;

    private CharacterController controller;
    private Vector3 velocity;              // vertical velocity
    private Vector3 horizontalVelocity;    // smoothed horizontal movement
    private Vector2 input;
    private float verticalLookRotation = 0f;

    private float bobTimer = 0f;
    private Vector3 cameraInitialPos;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform != null)
            cameraInitialPos = cameraTransform.localPosition;

        currentHealth = maxHealth;
        currentPoints = startingPoints;

        UpdateUI();
    }

    void Update()
    {
        ReadInput();
        HandleMouseLook();
        HandleMovement();
        HandleHeadBobbing();
        UpdateUI();
    }

    // --------------------
    // INPUT
    // --------------------
    void ReadInput()
    {
        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
    }

    // --------------------
    // LOOK
    // --------------------
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -verticalLookLimit, verticalLookLimit);
        cameraTransform.localRotation = Quaternion.Euler(verticalLookRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    // --------------------
    // MOVEMENT
    // --------------------
    void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;

        float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

        Vector3 moveDirection =
            (transform.right * input.x + transform.forward * input.y).normalized;

        Vector3 targetHorizontalVelocity = moveDirection * targetSpeed;

        float rate = moveDirection.magnitude > 0.1f ? acceleration : deceleration;

        horizontalVelocity = Vector3.Lerp(
            horizontalVelocity,
            targetHorizontalVelocity,
            Time.deltaTime * rate
        );

        if (isGrounded)
        {
            if (velocity.y < 0f)
                velocity.y = -2f;

            if (Input.GetButtonDown("Jump"))
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        Vector3 finalMove =
            horizontalVelocity +
            Vector3.up * velocity.y;

        controller.Move(finalMove * Time.deltaTime);
    }

    // --------------------
    // HEAD BOB
    // --------------------
    void HandleHeadBobbing()
    {
        if (cameraTransform == null) return;

        bool isMoving = horizontalVelocity.magnitude > 0.1f && controller.isGrounded;

        if (isMoving)
        {
            float speedMultiplier = Input.GetKey(KeyCode.LeftShift) ? sprintBobMultiplier : 1f;
            bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude * speedMultiplier;

            cameraTransform.localPosition =
                cameraInitialPos + new Vector3(0f, bobOffset, 0f);
        }
        else
        {
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition,
                cameraInitialPos,
                Time.deltaTime * 10f
            );
            bobTimer = 0f;
        }
    }

    // --------------------
    // UI / STATS
    // --------------------
    void UpdateUI()
    {
        if (healthText != null)
            healthText.text = $"HP: {currentHealth}";

        if (pointsText != null)
            pointsText.text = $"Points: {currentPoints}";
    }

    public void AddPoints(int amount)
    {
        currentPoints += amount;
        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        UpdateUI();

        if (currentHealth <= 0)
        {
            Debug.Log("Player has died.");
        }
    }

    public bool SpendPoints(int amount)
    {
        if (currentPoints >= amount)
        {
            currentPoints -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    public int GetCurrentPoints() => currentPoints;
    public int GetCurrentHealth() => currentHealth;
}