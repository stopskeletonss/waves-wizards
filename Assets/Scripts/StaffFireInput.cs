using UnityEngine;
using Unity.Netcode;

public class StaffFireInput : NetworkBehaviour
{
    public StaffManager weaponManager;
    public Transform firePoint; // position and direction from which the projectile is fired (set up in staff prefabs)
    public Camera playerCamera;

    public override void OnNetworkSpawn()
    {
        if (weaponManager != null)
        {
            weaponManager.onStaffEquipped += HandleStaffEquipped; // listen for whenever the player equips a staff to update the projectile's firing position
            HandleStaffEquipped(weaponManager.GetCurrentStaff()?.gameObject); // if there is a staff already equipped, set its projectile origin
        }
    }


    public override void OnNetworkDespawn()
    {
        // stop listening for staff changes
        if (weaponManager != null)
            weaponManager.onStaffEquipped -= HandleStaffEquipped;

        base.OnNetworkDespawn();
    }


    // called whenever the equipped staff changes to find the new projectile origin
    void HandleStaffEquipped(GameObject equippedStaff)
    {
        // if there's no staff equipped, there's nowhere to fire a projectile from
        if (equippedStaff == null)
        {
            firePoint = null;
            return;
        }

        Transform origin = equippedStaff.transform.Find("ProjectileOrigin");

        if (origin != null) {
            firePoint = origin;
        }
        else
        {
            firePoint = null;
            Debug.LogWarning("ProjectileOrigin not found on equipped staff: " + equippedStaff.name);
        }
    }

    void Update()
    {
        if (!IsSpawned)
            return;

        if (!IsOwner)
            return;

        // fire the staff if the player presses the left mouse button
        if (Input.GetMouseButtonDown(0))
        {
            FireStaff();
        }
    }


    // fires a projectile from the player's staff, if able
    void FireStaff()
    {
        if (weaponManager == null || firePoint == null)
            return;

        StaffController currentStaff = weaponManager.GetCurrentStaff(); // get the equipped staff

        // do not allow the player to fire if there is no staff, no projectile prefab, or no ammo left
        if (currentStaff == null || currentStaff.projectilePrefab == null || currentStaff.ammo <= 0)
            return;

        // cast a ray from the centre of the player's screen
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint;

        // aim wherever the player is looking
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            targetPoint = hit.point;
        } else
        {
            targetPoint = ray.GetPoint(1000f);
        }

        // calculate the direction from the staff's target point to where the player is looking
        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

            // send a request to the server to create the projectile prefab
            FireStaffServerRpc(firePoint.position, shootDirection);
    }


    // runs on the server when the owner (player) requests a projectile
    // because the server is responsible for spawning the projectile
    [ServerRpc] void FireStaffServerRpc(Vector3 spawnPosition, Vector3 shootDirection)
    {
        if (weaponManager == null)
            return;

        StaffController currentStaff = weaponManager.GetCurrentStaff();

        if (currentStaff == null || currentStaff.projectilePrefab == null || currentStaff.ammo <= 0)
            return;

        // rotate the projectile to face where the player is aiming
        Quaternion spawnRotation = Quaternion.LookRotation(shootDirection);

        // create the projectile
        GameObject projectile = Instantiate(currentStaff.projectilePrefab, spawnPosition, spawnRotation);

        // needs a NetworkObject so all clients can see all projectiles
        NetworkObject networkObject = projectile.GetComponent<NetworkObject>();

        networkObject.Spawn();

        Rigidbody rb = projectile.GetComponent<Rigidbody>(); // get the projectile's Rigidbody to be able to move it

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // calculate the direction the projectile should travel
            rb.linearVelocity =
                shootDirection * currentStaff.fireForce
                + Vector3.up * currentStaff.upwardForce;
        }
        currentStaff.ammo--;
    }
}   