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

    private Transform holdPoint;               // Where the staff should follow (cached original parent)

    void Awake()
    {
        maxAmmo = ammo;
    }

    void Start()
    {
        // Ensure we have a fire point
        if (firePoint == null)
            firePoint = transform.Find("ProjectileOrigin");

        if (firePoint == null)
            Debug.LogWarning($"[{name}] No ProjectileOrigin found. Assign a child named 'StaffFirePoint' or set the ProjectileOrigin in inspector.");

        // Cache parent as holdPoint (WeaponHoldPoint) then unparent so we can smooth-follow it
        if (transform.parent != null)
        {
            holdPoint = transform.parent;
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


    public void RefillAmmo()
    {
        ammo = maxAmmo;
        UpdateManaUI();
    }

    public void SetAmmo(int newAmmo)
    {
        // Ensure ammo can't go below 0 or above max
        ammo = Mathf.Clamp(newAmmo, 0, maxAmmo);
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