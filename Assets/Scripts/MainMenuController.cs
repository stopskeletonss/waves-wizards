using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Camera Anchors")]
    public Transform playAnchor;
    public Transform tutorialAnchor;
    public Transform creditsAnchor;
    public Transform settingsAnchor;
    public Transform mainAnchor; // optional: default/home view

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotateSpeed = 3f;
    public float arrivalThreshold = 0.05f;

    private Transform currentTarget;
    private bool isMoving;

    void Start()
    {
        // Optional: start at main menu view
        if (mainAnchor != null)
        {
            transform.position = mainAnchor.position;
            transform.rotation = mainAnchor.rotation;
        }
    }

    void Update()
    {
        if (!currentTarget) return;

        // Smooth position
        transform.position = Vector3.Lerp(
            transform.position,
            currentTarget.position,
            Time.deltaTime * moveSpeed
        );

        // Smooth rotation
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            currentTarget.rotation,
            Time.deltaTime * rotateSpeed
        );

        // Stop moving when close enough
        if (Vector3.Distance(transform.position, currentTarget.position) < arrivalThreshold)
        {
            currentTarget = null;
            isMoving = false;
        }
    }

    // =========================
    // Button Functions
    // =========================

    public void PlayGame()
    {
        MoveCamera(playAnchor);
        // Later: load scene, fade, etc.
    }

    public void Tutorial()
    {
        MoveCamera(tutorialAnchor);
    }

    public void Credits()
    {
        MoveCamera(creditsAnchor);
    }

    public void Settings()
    {
        MoveCamera(settingsAnchor);
    }

    public void BackToMain()
    {
        MoveCamera(mainAnchor);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // =========================
    // Internal Logic
    // =========================

    private void MoveCamera(Transform target)
    {
        if (isMoving || target == null) return;

        currentTarget = target;
        isMoving = true;
    }
}