using UnityEngine;
using TMPro;

public class StaffController : MonoBehaviour
{
    [Header("Firing Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;                // The muzzle / spawn point on the staff
    public float fireForce = 20f;              // Speed applied along aim direction
    public float upwardForce = 0f;             // Optional additional upward component (relative to camera)
    public int ammo = 30;

    private int maxAmmo;

    [Header("UI")]
    public TMP_Text manaText;

    [Header("Follow Lag Settings")]
    public float positionLag = 12f;            // Larger = snappier; used with exponential smoothing
    public float rotationLag = 12f;            // Larger = snappier
    public float aimMaxDistance = 1000f;
    public LayerMask aimLayerMask = Physics.DefaultRaycastLayers;

    // Cached transforms / state
    private Transform holdPoint;               // Where the staff should follow (cached original parent)
    private Vector3 lastHoldPosition;
    private Vector3 posVelocity;

    void Start()
    {
        maxAmmo = ammo;

        // Ensure we have a fire point
        if (firePoint == null)
            firePoint = transform.Find("StaffFirePoint");

        if (firePoint == null)
            Debug.LogWarning($"[{name}] No firePoint found. Assign a child named 'StaffFirePoint' or set the firePoint in inspector.");

        // Cache parent as holdPoint (WeaponHoldPoint) then unparent so we can smooth-follow it
        if (transform.parent != null)
        {
            holdPoint = transform.parent;
            lastHoldPosition = holdPoint.position;
            transform.SetParent(null); // keep world transform, but follow programmatically
        }
        else
        {
            Debug.LogWarning($"[{name}] Staff has no parent to follow. Make sure the staff prefab is parented to the weapon hold point in the hierarchy before runtime.");
        }

        UpdateManaUI();
    }

    void LateUpdate()
    {
        if (holdPoint == null) return;

        // Smooth position follow (exponential smoothing)
        float posT = 1f - Mathf.Exp(-positionLag * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, holdPoint.position, posT);

        // Smooth rotation follow (slerp)
        float rotT = 1f - Mathf.Exp(-rotationLag * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, holdPoint.rotation, rotT);
    }

    // Public fire method - use this from StaffManager
    public void Fire()
    {
        if (ammo <= 0)
        {
            // Optionally play empty sound / UI feedback here
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[{name}] No projectilePrefab assigned.");
            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning($"[{name}] No firePoint assigned.");
            return;
        }

        // Compute aim direction using camera ray from screen center so pitch is respected
        Camera cam = Camera.main;
        Vector3 direction;

        if (cam != null)
        {
            Ray centerRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(centerRay, out RaycastHit hit, aimMaxDistance, aimLayerMask))
            {
                Vector3 targetPoint = hit.point;
                direction = (targetPoint - firePoint.position).normalized;
            }
            else
            {
                Vector3 farPoint = centerRay.GetPoint(aimMaxDistance);
                direction = (farPoint - firePoint.position).normalized;
            }
        }
        else if (holdPoint != null)
        {
            // Fallback to holdPoint forward if no camera found
            direction = holdPoint.forward;
        }
        else
        {
            // Last-resort fallback
            direction = transform.forward;
        }

        // Spawn projectile facing the computed direction
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Add velocity along aim direction and optional upward component relative to camera (if available)
            Vector3 upward = Vector3.up;
            if (cam != null)
                upward = cam.transform.up;
            else if (holdPoint != null)
                upward = holdPoint.up;

            rb.linearVelocity = direction * fireForce + upward * upwardForce;
        }
        else
        {
            // If your projectile uses a projectile script instead of Rigidbody, call its initializer here
            var projScript = projectile.GetComponent<MonoBehaviour>();
            // Example: if it had a custom initializer you would call it, otherwise we simply warn.
            Debug.LogWarning($"[{name}] projectilePrefab '{projectile.name}' has no Rigidbody. Make sure it has one or initialize velocity in its script.");
        }

        ammo--;
        UpdateManaUI();
    }

    public void RefillAmmo()
    {
        ammo = maxAmmo;
        UpdateManaUI();
    }

    void UpdateManaUI()
    {
        if (manaText != null)
            manaText.text = $"{ammo} / {maxAmmo}";
    }

    // Helper getters (optional)
    public int GetAmmo() => ammo;
    public int GetMaxAmmo() => maxAmmo;
}