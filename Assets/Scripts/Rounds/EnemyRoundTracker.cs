using Unity.Netcode;
using UnityEngine;

//Put this on the enemy prefab so we can track stuff :)
public class EnemyRoundTracker : NetworkBehaviour
{
    private RoundSystem roundSystem;

    public void Init(RoundSystem system)
    {
        roundSystem = system;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (!IsServer) return;

        if (roundSystem == null) return;

        roundSystem.NotifyEnemyDespawned(this);
    }
}