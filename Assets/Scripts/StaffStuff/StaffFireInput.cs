
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class StaffFireInput : NetworkBehaviour
{
    public StaffManager weaponManager;
    public Transform firePoint;
    public Camera playerCamera;

    [Header("Projectile Prediction")]
    public float predictedProjectileLifetime = 5f;
    public float predictionConfirmationTimeout = 2f;

    private ulong nextShotId = 0;

    private Dictionary<ulong, GameObject> predictedProjectiles =
        new Dictionary<ulong, GameObject>();

    private HashSet<ulong> confirmedShots = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        if (weaponManager != null)
        {
            weaponManager.onStaffEquipped += HandleStaffEquipped;
            HandleStaffEquipped(weaponManager.GetCurrentStaff()?.gameObject);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (weaponManager != null)
            weaponManager.onStaffEquipped -= HandleStaffEquipped;

        base.OnNetworkDespawn();
    }

    // Update firing position when staff changes
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
            firePoint = null;
            Debug.LogWarning("ProjectileOrigin not found on equipped staff: " + equippedStaff.name);
        }
    }

    void Update()
    {
        if (!IsSpawned || !IsOwner)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            FireStaff();
        }
    }

    // =========================
    // FIRING
    // =========================

    void FireStaff()
    {
        if (weaponManager == null || firePoint == null || playerCamera == null)
            return;

        StaffController currentStaff = weaponManager.GetCurrentStaff();

        if (currentStaff == null || weaponManager.CurrentAmmo.Value <= 0)
            return;

        // Make sure projectile lists are properly configured
        if (currentStaff.projectilePrefabs == null ||
            currentStaff.projectileLCopies == null ||
            currentStaff.projectilePrefabs.Count == 0 ||
            currentStaff.projectilePrefabs.Count != currentStaff.projectileLCopies.Count)
        {
            Debug.LogWarning("Invalid projectile lists on equipped staff.");
            return;
        }

        // Select a random projectile index
        int projectileIndex = Random.Range(0, currentStaff.projectilePrefabs.Count);

        if (currentStaff.projectilePrefabs[projectileIndex] == null ||
            currentStaff.projectileLCopies[projectileIndex] == null)
        {
            Debug.LogWarning("Selected projectile or local copy is missing.");
            return;
        }

        // Aim from the centre of the player's screen
        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(1000f);
        }

        Vector3 shootDirection =
            (targetPoint - firePoint.position).normalized;

        Quaternion spawnRotation = Quaternion.LookRotation(shootDirection);

        // Give this shot a unique ID
        ulong shotId = ++nextShotId;

        // CLIENT PREDICTION
        // Use the local copy matching the selected projectile
        if (!IsServer)
        {
            GameObject predictedPrefab =
                currentStaff.projectileLCopies[projectileIndex];

            GameObject predicted = Instantiate(
                predictedPrefab,
                firePoint.position,
                spawnRotation
            );

            Rigidbody rb = predicted.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity =
                    shootDirection * currentStaff.fireForce
                    + Vector3.up * currentStaff.upwardForce;

                rb.angularVelocity = Vector3.zero;
            }

            predictedProjectiles.Add(shotId, predicted);

            StartCoroutine(PredictionTimeout(shotId));
            StartCoroutine(PredictionLifetime(shotId));
        }

        // Tell the server which projectile was selected
        FireStaffServerRpc(
            firePoint.position,
            shootDirection,
            projectileIndex,
            shotId
        );
    }

    // =========================
    // SERVER COMMUNICATION
    // =========================

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

    [ServerRpc]
    void FireStaffServerRpc(
        Vector3 spawnPosition,
        Vector3 shootDirection,
        int projectileIndex,
        ulong shotId,
        ServerRpcParams rpcParams = default)
    {
        ulong shooterClientId = rpcParams.Receive.SenderClientId;

        if (weaponManager == null)
        {
            SendFireResult(shooterClientId, shotId, false);
            return;
        }

        StaffController currentStaff = weaponManager.GetCurrentStaff();

        if (currentStaff == null || weaponManager.CurrentAmmo.Value <= 0)
        {
            SendFireResult(shooterClientId, shotId, false);
            return;
        }

        // Validate the selected projectile index
        if (projectileIndex < 0 ||
            projectileIndex >= currentStaff.projectilePrefabs.Count ||
            projectileIndex >= currentStaff.projectileLCopies.Count)
        {
            SendFireResult(shooterClientId, shotId, false);
            return;
        }

        GameObject selectedPrefab =
            currentStaff.projectilePrefabs[projectileIndex];

        if (selectedPrefab == null)
        {
            SendFireResult(shooterClientId, shotId, false);
            return;
        }

        Quaternion spawnRotation = Quaternion.LookRotation(shootDirection);

        // Create the actual networked projectile
        GameObject projectile = Instantiate(
            selectedPrefab,
            spawnPosition,
            spawnRotation
        );

        NetworkObject networkObject =
            projectile.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Destroy(projectile);
            SendFireResult(shooterClientId, shotId, false);
            return;
        }

        // Host sees the actual projectile.
        // Other clients initially exclude the shooter.
        bool shooterIsHost =
            shooterClientId == NetworkManager.ServerClientId;

        networkObject.SpawnWithObservers = shooterIsHost;

        networkObject.Spawn();

        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity =
                shootDirection.normalized * currentStaff.fireForce
                + Vector3.up * currentStaff.upwardForce;

            rb.angularVelocity = Vector3.zero;
        }

        // Show the actual projectile to everyone except its shooter
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

        // Consume ammo
        weaponManager.CurrentAmmo.Value--;
        currentStaff.SetAmmo(weaponManager.CurrentAmmo.Value);

        SendFireResult(shooterClientId, shotId, true);
    }

    [ClientRpc]
    void FireResultClientRpc(
        ulong shotId,
        bool accepted,
        ClientRpcParams rpcParams = default)
    {
        if (accepted)
        {
            confirmedShots.Add(shotId);
        }
        else
        {
            RemovePrediction(shotId);
        }
    }

    // Remove predictions that aren't confirmed by the server
    IEnumerator PredictionTimeout(ulong shotId)
    {
        yield return new WaitForSeconds(predictionConfirmationTimeout);

        if (!confirmedShots.Contains(shotId))
        {
            RemovePrediction(shotId);
        }
    }

    // Remove predictions after their lifetime
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
