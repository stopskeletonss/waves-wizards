using UnityEngine;
using TMPro;

public class StaffController : MonoBehaviour
{
    [Header("Firing Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireForce = 20f;
    public int ammo = 30;

    private int maxAmmo;

    [Header("Firing Modes")]
    public bool isStraightShot = true;
    public bool isLobShot = false;
    public float lobArcHeight = 5f;

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
            if (isStraightShot)
            {
                rb.linearVelocity = firePoint.forward * fireForce;
            }
            else if (isLobShot)
            {
                Vector3 lobDirection = (firePoint.forward + firePoint.up).normalized;
                rb.linearVelocity = lobDirection * fireForce + Vector3.up * lobArcHeight;
            }
            else
            {
                Debug.LogWarning("No firing style enabled on " + gameObject.name);
            }
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