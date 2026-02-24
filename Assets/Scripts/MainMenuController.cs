using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Camera Anchors")]
    public Transform playAnchor;
    public Transform tutorialAnchor;
    public Transform creditsAnchor;
    public Transform settingsAnchor;
    public Transform mainAnchor;
    public Transform quitAnchor;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotateSpeed = 3f;
    public float arrivalThreshold = 0.05f;

    [Header("Position Sway")]
    public float positionAmount = 0f;      // default requested
    public float positionSpeed = 0f;       // default requested

    [Header("Rotation Sway")]
    public float rotationAmount = 0.2f;    // default requested
    public float rotationSpeed = 2.5f;     // default requested

    private Transform currentTarget;
    private bool isMoving;

    private Vector3 basePosition;
    private Quaternion baseRotation;

    void Start()
    {
        if (mainAnchor != null)
        {
            basePosition = mainAnchor.position;
            baseRotation = mainAnchor.rotation;
            transform.position = basePosition;
            transform.rotation = baseRotation;
        }
    }

    void Update()
    {
        HandleMovement();
        ApplySway();
    }

    // =========================
    // Camera Movement
    // =========================

    void HandleMovement()
    {
        if (!currentTarget) return;

        basePosition = Vector3.Lerp(
            basePosition,
            currentTarget.position,
            Time.deltaTime * moveSpeed
        );

        baseRotation = Quaternion.Slerp(
            baseRotation,
            currentTarget.rotation,
            Time.deltaTime * rotateSpeed
        );

        if (Vector3.Distance(basePosition, currentTarget.position) < arrivalThreshold)
        {
            currentTarget = null;
            isMoving = false;
        }
    }

    // =========================
    // Sway Logic (Overlay)
    // =========================

    void ApplySway()
    {
        Vector3 swayPos = Vector3.zero;
        Quaternion swayRot = Quaternion.identity;

        if (positionAmount > 0f)
        {
            float posX = Mathf.Sin(Time.time * positionSpeed) * positionAmount;
            float posY = Mathf.Cos(Time.time * positionSpeed * 0.8f) * positionAmount;
            swayPos = new Vector3(posX, posY, 0f);
        }

        if (rotationAmount > 0f)
        {
            float rotX = Mathf.Sin(Time.time * rotationSpeed) * rotationAmount;
            float rotY = Mathf.Cos(Time.time * rotationSpeed * 0.7f) * rotationAmount;
            swayRot = Quaternion.Euler(rotX, rotY, 0f);
        }

        transform.position = basePosition + swayPos;
        transform.rotation = baseRotation * swayRot;
    }

    // =========================
    // Button Functions
    // =========================

    public void PlayGame() => MoveCamera(playAnchor);
    public void Tutorial() => MoveCamera(tutorialAnchor);
    public void Credits() => MoveCamera(creditsAnchor);
    public void Settings() => MoveCamera(settingsAnchor);
    public void BackToMain() => MoveCamera(mainAnchor);
    public void QuitGame() => MoveCamera(quitAnchor);

    void MoveCamera(Transform target)
    {
        if (isMoving || target == null) return;

        currentTarget = target;
        isMoving = true;
    }
}