using UnityEngine;

public class StaffFireInput : MonoBehaviour
{
    public StaffManager weaponManager;
    public Transform firePoint;

    void Start()
    {
        if (weaponManager != null)
        {
            weaponManager.onStaffEquipped += HandleStaffEquipped;

            // Assign at start in case one is already equipped
            HandleStaffEquipped(weaponManager.GetCurrentStaff()?.gameObject);
        }
    }

    void OnDestroy()
    {
        if (weaponManager != null)
            weaponManager.onStaffEquipped -= HandleStaffEquipped;
    }

    void HandleStaffEquipped(GameObject equippedStaff)
    {
        if (equippedStaff == null)
        {
            firePoint = null;
            return;
        }

        Transform origin = equippedStaff.transform.Find("ProjectileOrigin");

        if (origin != null)
        {
            firePoint = origin;
        }
        else
        {
            Debug.LogWarning("ProjectileOrigin not found on equipped staff: " + equippedStaff.name);
            firePoint = null;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            FireStaff();
        }
    }

    void FireStaff()
    {
        StaffController currentStaff = weaponManager.GetCurrentStaff();

        if (currentStaff != null && currentStaff.projectilePrefab != null && currentStaff.ammo > 0 && firePoint != null)
        {
            GameObject projectile = Instantiate(currentStaff.projectilePrefab, firePoint.position, firePoint.rotation);
            Rigidbody rb = projectile.GetComponent<Rigidbody>();

            if (rb != null)
            {
                if (currentStaff.isStraightShot)
                {
                    rb.linearVelocity = firePoint.forward * currentStaff.fireForce;
                }
                else if (currentStaff.isLobShot)
                {
                    Vector3 lobDirection = (firePoint.forward + firePoint.up).normalized;
                    rb.linearVelocity = lobDirection * currentStaff.fireForce + Vector3.up * currentStaff.lobArcHeight;
                }
                else
                {
                    Debug.LogWarning("No firing style enabled on current staff: " + currentStaff.gameObject.name);
                }
            }

            currentStaff.ammo--;
        }
    }
}