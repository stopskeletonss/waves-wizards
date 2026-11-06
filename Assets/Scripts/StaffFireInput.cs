using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class StaffFireInput : NetworkBehaviour
{
    public StaffManager weaponManager;
    public Transform firePoint; // position and direction from which the projectile is fired (set up in staff prefabs)
    public Camera playerCamera;

    [Header("Projectile Prediction")]
    public GameObject predictedProjectilePrefab;
    public float predictedProjectileLifetime = 5f;
    public float predictionConfirmationTimeout = 2f;
    private ulong nextShotId = 0;
    private Dictionary<ulong, GameObject> predictedProjectiles = new Dictionary<ulong, GameObject>();
    private HashSet<ulong> confirmedShots = new HashSet<ulong>();

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
        if (weaponManager == null || firePoint == null || playerCamera == null)
            return;

        StaffController currentStaff = weaponManager.GetCurrentStaff(); // get the equipped staff

        // do not allow the player to fire if there is no staff, no projectile prefab, or no ammo left
        if (currentStaff == null || currentStaff.projectilePrefab == null || weaponManager.CurrentAmmo.Value <= 0)
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

        // rotate the projectile to face where the player is aiming
        Quaternion spawnRotation = Quaternion.LookRotation(shootDirection);

        // give the projectile a unique shot ID
        ulong shotId = ++nextShotId;

        if (!IsServer && predictedProjectilePrefab != null)
        {
            GameObject predicted = Instantiate(
                predictedProjectilePrefab,
                firePoint.position,
                spawnRotation
            );

            Rigidbody rb = predicted.GetComponent<Rigidbody>(); // get the projectile's Rigidbody to be able to move it

            if (rb != null)
            {
                // calculate the direction the projectile should travel
                rb.linearVelocity =
                    shootDirection * currentStaff.fireForce
                    + Vector3.up * currentStaff.upwardForce;

                rb.angularVelocity = Vector3.zero;
            }
            predictedProjectiles.Add(shotId, predicted);
            StartCoroutine(PredictionTimeout(shotId));
            StartCoroutine(PredictionLifetime(shotId));
        }
        // send a request to the server to create the projectile prefab
        FireStaffServerRpc(firePoint.position, shootDirection, shotId);
    }


    // tells the shooter (client) whether the server successfully created their projectile
    void SendFireResult(ulong shooterClientId, ulong shotId, bool accepted)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { shooterClientId }
            }
        };

        FireResultClientRpc(shotId, accepted, clientRpcParams);
    }


    // runs on the server when the owner (player) requests a projectile
    // because the server is responsible for spawning the projectile
    [ServerRpc]
    void FireStaffServerRpc(Vector3 spawnPosition, Vector3 shootDirection, ulong shotId, ServerRpcParams rpcParams = default)
    {
        ulong shooterClientId = rpcParams.Receive.SenderClientId;

        if (weaponManager == null)
        {
            SendFireResult(shooterClientId, shotId, false);
            return;
        }

        StaffController currentStaff = weaponManager.GetCurrentStaff();

        if (currentStaff == null || currentStaff.projectilePrefab == null || weaponManager.CurrentAmmo.Value <= 0)
        {
            SendFireResult(shooterClientId, shotId, false);
            return;
        }

        // rotate the projectile to face where the player is aiming
        Quaternion spawnRotation = Quaternion.LookRotation(shootDirection);

        // create the projectile
        GameObject projectile = Instantiate(currentStaff.projectilePrefab, spawnPosition, spawnRotation);

        // needs a NetworkObject so all clients can see all projectiles
        NetworkObject networkObject = projectile.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Destroy(projectile);
            SendFireResult(shooterClientId, shotId, false);
            return;
        }

        // if the client is also the host, they juts see the actual server projectile
        bool shooterIsHost = shooterClientId == NetworkManager.ServerClientId;

        // non-host cleints should not see the actial server projectile
        networkObject.SpawnWithObservers = shooterIsHost;

        networkObject.Spawn();

        Rigidbody rb = projectile.GetComponent<Rigidbody>(); // get the projectile's Rigidbody to be able to move it

        if (rb != null)
        {
            // calculate the direction the projectile should travel
            rb.linearVelocity =
                shootDirection.normalized * currentStaff.fireForce
                + Vector3.up * currentStaff.upwardForce;

            rb.angularVelocity = Vector3.zero;
        }

        // show the real projectile to everyone except its shooter
        if (!shooterIsHost)
        {
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (clientId != shooterClientId)
                {
                    networkObject.NetworkShow(clientId);
                }
            }
        }
        weaponManager.CurrentAmmo.Value--;
        SendFireResult(shooterClientId, shotId, true);
    }


    [ClientRpc]
    void FireResultClientRpc(ulong shotId, bool accepted, ClientRpcParams rpcParams = default)
    {
        if (accepted)
        {
            // rhe server accepted this shot
            confirmedShots.Add(shotId);
        }
        else
        {
            // the server rejected this shot
            RemovePrediction(shotId);
        }
    }


    // removes projectiles that have not been confirmed by the server after the timeout length
    IEnumerator PredictionTimeout(ulong shotId)
    {
        yield return new WaitForSeconds(predictionConfirmationTimeout);
        if (!confirmedShots.Contains(shotId))
        {
            RemovePrediction(shotId);
        }
    }


    IEnumerator PredictionLifetime(ulong shotId)
    {
        yield return new WaitForSeconds(predictedProjectileLifetime);

        RemovePrediction(shotId);
    }



    void RemovePrediction(ulong shotId)
    {
        if (predictedProjectiles.TryGetValue(shotId, out GameObject projectile))
        {
            if (projectile != null)
                Destroy(projectile);

            predictedProjectiles.Remove(shotId);
        }

        confirmedShots.Remove(shotId);
    }
}   