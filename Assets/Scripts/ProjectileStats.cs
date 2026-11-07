using UnityEngine;
using Unity.Netcode;


public class ProjectileStats : MonoBehaviour
{
    public int damage = 10;
    public int pointsOnHit = 10;
    public int pointsOnKill = 50;

    [SerializeField] private float lifetime = 10f; // projectiles despawn after 10 seconds if they don't collide with anything
    private float timer = 0f;

    private NetworkObject networkObject; // reference to this projectile's NetworkObject


    private void Awake()
    {
        networkObject = GetComponent<NetworkObject>();
    }


    private void Update()
    {
        // only the server handles despawning
        if (networkObject == null || !networkObject.IsSpawned || !NetworkManager.Singleton.IsServer)
            return;

        timer += Time.deltaTime; // count how long the projectile has existed

        if (timer >= lifetime) // despawn it after 10 seconds
            DespawnProjectile();
    }


    private void OnTriggerEnter(Collider collision)
    {
        // only the server handles collision
        if (networkObject == null || !networkObject.IsSpawned || !NetworkManager.Singleton.IsServer)
            return;


        // apply damage if the projectile hits an enemy
        if (collision.CompareTag("Enemy"))
        {
            EnemyAITest enemy = collision.GetComponent<EnemyAITest>();

            if (enemy != null)
            {
                enemy.TakeDamageServerRpc(damage);
                DespawnProjectile();
            }
        }
    }


    private void DespawnProjectile()
    {
        if (networkObject != null && networkObject.IsSpawned)
            networkObject.Despawn(true);
    }
}