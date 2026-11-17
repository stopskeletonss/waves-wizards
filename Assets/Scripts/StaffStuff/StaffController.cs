using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class StaffController : MonoBehaviour
{
    [Header("Firing Settings")]
    public List<GameObject> projectilePrefabs = new List<GameObject>();
    public List<GameObject> projectileLCopies = new List<GameObject>();

    public Transform firePoint;
    public float fireForce = 20f;
    public float upwardForce = 0f;
    public int ammo = 30;

    private int maxAmmo;

    [Header("UI")]
    public TMP_Text manaText;

    [Header("Follow Lag Settings")]
    public float positionLag = 12f;
    public float rotationLag = 12f;

    private Transform holdPoint;

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
            Debug.LogWarning($"[{name}] No ProjectileOrigin found.");

        // Cache parent as holdPoint
        if (transform.parent != null)
        {
            holdPoint = transform.parent;
        }
        else
        {
            Debug.LogWarning($"[{name}] Staff has no parent to follow.");
        }

        // Check projectile lists
        if (projectilePrefabs.Count != projectileLCopies.Count)
        {
            Debug.LogWarning($"[{name}] Projectile prefab and local copy lists have different sizes!");
        }

        UpdateManaUI();
    }

    void LateUpdate()
    {
        if (holdPoint == null) return;

        float posT = 1f - Mathf.Exp(-positionLag * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, holdPoint.position, posT);

        float rotT = 1f - Mathf.Exp(-rotationLag * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, holdPoint.rotation, rotT);
    }

    // Returns a random projectile and its matching local copy
    public void GetRandomProjectile(out GameObject projectile, out GameObject localCopy)
    {
        projectile = null;
        localCopy = null;

        if (projectilePrefabs.Count == 0 || projectileLCopies.Count == 0)
        {
            Debug.LogWarning($"[{name}] No projectiles assigned!");
            return;
        }

        int count = Mathf.Min(projectilePrefabs.Count, projectileLCopies.Count);

        int randomIndex = Random.Range(0, count);

        projectile = projectilePrefabs[randomIndex];
        localCopy = projectileLCopies[randomIndex];
    }

    public void RefillAmmo()
    {
        ammo = maxAmmo;
        UpdateManaUI();
    }

    public void SetAmmo(int newAmmo)
    {
        ammo = Mathf.Clamp(newAmmo, 0, maxAmmo);
        UpdateManaUI();
    }

    void UpdateManaUI()
    {
        if (manaText != null)
            manaText.text = $"{ammo} / {maxAmmo}";
    }

    public int GetAmmo() => ammo;
    public int GetMaxAmmo() => maxAmmo;
}
