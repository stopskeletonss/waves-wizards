using UnityEngine;
using TMPro;

public class StaffController : MonoBehaviour
{
    [Header("Firing Settings")]
    public GameObject projectilePrefab;
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
    private Vector3 velocity;
    private Vector3 lastHoldPosition;

    void Start()
    {
        maxAmmo = ammo;

        // Cache fire point BEFORE unparenting
        if (firePoint == null)
        {
            firePoint = transform.Find("StaffFirePoint");
        }

        if (transform.parent != null)
        {
            holdPoint = transform.parent;
            lastHoldPosition = holdPoint.position;

            transform.SetParent(null); // unparent AFTER caching firePoint
        }

        UpdateManaUI();
    }

    void LateUpdate()
    {
        if (holdPoint == null) return;

        Vector3 holdDelta = holdPoint.position - lastHoldPosition;
        lastHoldPosition = holdPoint.position;

        Vector3 targetPosition = holdPoint.position + (-holdDelta);

        float posT = 1f - Mathf.Exp(-positionLag * Time.deltaTime);

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            posT
        );

        transform.rotation = holdPoint.rotation;
    }

    public void Fire()
    {
        if (ammo <= 0 || firePoint == null || projectilePrefab == null || holdPoint == null) return;

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 forward = holdPoint.forward;
            Vector3 up = holdPoint.up;

            Vector3 shootDirection = (forward * fireForce) + (up * upwardForce);

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