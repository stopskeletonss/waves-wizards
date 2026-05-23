using UnityEngine;
using Unity.Netcode;

public class ProjectileStats : MonoBehaviour
{
    public int damage = 10;
    public int pointsOnHit = 10;
    public int pointsOnKill = 50;

    private void OnTriggerEnter(Collider collision)
    {
        Debug.Log("Hit: " + collision.gameObject.name);

        if (collision.CompareTag("Enemy"))
        {
            Debug.Log("Enemy hit");

            EnemyAITest enemy = collision.GetComponent<EnemyAITest>();

            if (enemy != null)
            {
                Debug.Log("Damage RPC is called");
                enemy.TakeDamageServerRpc(damage);

                Invoke("DestroyProjectile", 0.1f);
            }
        }
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}