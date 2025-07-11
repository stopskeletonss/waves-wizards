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
    private Vector3 velocity;
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
        HandleMouseLook();
        HandleMovement();
        HandleHeadBobbing();
        UpdateUI();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -verticalLookLimit, verticalLookLimit);
        cameraTransform.localRotation = Quaternion.Euler(verticalLookRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

        Vector3 move = transform.right * inputX + transform.forward * inputZ;
        move = move.normalized * currentSpeed;

        if (controller.isGrounded)
        {
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

        controller.Move((move + velocity) * Time.deltaTime);
    }

    void HandleHeadBobbing()
    {
        if (cameraTransform == null) return;

        bool isMoving = controller.velocity.magnitude > 0.1f && controller.isGrounded;

        if (isMoving)
        {
            float speedMultiplier = Input.GetKey(KeyCode.LeftShift) ? sprintBobMultiplier : 1f;
            bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude * speedMultiplier;

            Vector3 newCamPos = cameraInitialPos + new Vector3(0f, bobOffset, 0f);
            cameraTransform.localPosition = newCamPos;
        }
        else
        {
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, cameraInitialPos, Time.deltaTime * 10f);
            bobTimer = 0f;
        }
    }

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
            // Handle player death (expand later)
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

    public int GetCurrentPoints()
    {
        return currentPoints;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}