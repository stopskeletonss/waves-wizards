using UnityEngine;
using TMPro;

public class StaffController : MonoBehaviour
{
    [Header("Firing Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireForce = 20f;  // How fast the projectile moves forward
    public float upwardForce = 0f;    // Optional upward boost for lobbed shots
    public int ammo = 30;

    private int maxAmmo;

    [Header("UI")]
    public TMP_Text manaText; // Assign in inspector

    void Start()
    {
        maxAmmo = ammo;

        // Look for a sibling named "StaffHoldPoint" if firePoint not assigned
        if (firePoint == null && transform.parent != null)
        {
            Transform sibling = transform.parent.Find("StaffHoldPoint");
            if (sibling != null)
            {
                firePoint = sibling;
            }
            else
            {
                Debug.LogWarning("StaffHoldPoint not found as a sibling of " + gameObject.name);
            }
        }

        UpdateManaUI();
    }

    public void Fire()
    {
        if (ammo <= 0 || firePoint == null || projectilePrefab == null) return;

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Always use camera forward for direction
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraUp = Camera.main.transform.up;

            // Combine forward and upward force
            Vector3 shootDirection = (cameraForward * fireForce) + (cameraUp * upwardForce);

            // Apply as linear velocity
            rb.linearVelocity = shootDirection;
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
}