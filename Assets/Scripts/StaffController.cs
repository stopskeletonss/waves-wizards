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

    public bool singleShot;
    public bool constantShot;
    public bool beamShot;

    [Header("UI")]
    public TMP_Text manaText;

    [Header("Follow Lag Settings")]
    public float positionLag = 12f;
    public float rotationLag = 12f;

    [HideInInspector]
    public Transform holdPoint;

    [HideInInspector]
    public Transform animationCatalyst;

    private Vector3 lastHoldPosition;

    private Animator animator;

    void Start()
    {
        maxAmmo = ammo;

        if (firePoint == null)
        {
            firePoint = transform.Find("StaffFirePoint");
        }

        if (holdPoint != null)
        {
            lastHoldPosition = holdPoint.position;
        }

        // Get animator from parent
        animator = GetComponentInParent<Animator>();

        UpdateManaUI();
    }

    void Update()
    {
        HandleBeamShotAnimation();
    }

    void LateUpdate()
    {
        if (holdPoint == null ||
            animationCatalyst == null)
            return;

        Vector3 holdDelta =
            holdPoint.position - lastHoldPosition;

        lastHoldPosition = holdPoint.position;

        Vector3 targetPosition =
            holdPoint.position + (-holdDelta);

        float posT =
            1f - Mathf.Exp(-positionLag * Time.deltaTime);

        animationCatalyst.position = Vector3.Lerp(
            animationCatalyst.position,
            targetPosition,
            posT
        );

        float rotT =
            1f - Mathf.Exp(-rotationLag * Time.deltaTime);

        animationCatalyst.rotation = Quaternion.Slerp(
            animationCatalyst.rotation,
            holdPoint.rotation,
            rotT
        );
    }

    void HandleBeamShotAnimation()
    {
        if (!beamShot || animator == null)
            return;

        if (Input.GetMouseButton(0))
        {
            animator.SetBool("FireHeld", true);
        }
        else
        {
            animator.SetBool("FireHeld", false);
        }
    }

    public void Fire()
    {
        if (ammo <= 0 ||
            firePoint == null ||
            projectilePrefab == null)
            return;

        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            firePoint.rotation
        );

        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 shootDirection =
                (firePoint.forward * fireForce) +
                (firePoint.up * upwardForce);

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