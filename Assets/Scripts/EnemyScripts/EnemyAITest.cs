using UnityEngine;
using Unity.Netcode;

public class EnemyAITest : NetworkBehaviour
{

    //Player Transform
    public Transform Target;
    //Enemy's Damage
    public float AttackDamage;
    //Distance to damage a Player
    public float AttackDistance;
    //Timer Delay for attacks to register
    public float AttackDelay;
    //Enemy's max HP
    public float maxHP;
    //Enemy's Current HP
    public float currentHP;
    //NavAgent for this object
    private UnityEngine.AI.NavMeshAgent Skeleton;
    //Distance to player
    private float playerDistance;
    //reference to Enemy Health Bar
    [SerializeField]
    private TestHealthBar healthBar;
    private bool inRange = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Skeleton = GetComponent<UnityEngine.AI.NavMeshAgent>();

        currentHP = maxHP;

        healthBar.UpdateHealthBar(maxHP, currentHP);

        if (Target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Target = player.transform;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //gets distance to player
        playerDistance = Vector3.Distance(Skeleton.transform.position, Target.position);
        if (playerDistance < AttackDistance)
        {
            inRange = true;
            //Stops the skeleton when in range
            Skeleton.destination = Skeleton.transform.position;
            //Attack Logic here when close enough
            Debug.Log("Attack!");
            StartAttack(AttackDelay);
        }
        else
        {
            inRange = false;
            //otherwise moves towards player
            Skeleton.destination = Target.position;
            
        }
    }

    [ServerRpc]
    public void TakeDamageServerRpc(int damageAmount)
    {
        Debug.Log("Damage Take RPC called");
        currentHP -= damageAmount;
        Debug.Log("Enemy health: " + currentHP);
        healthBar.UpdateHealthBar(maxHP, currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }
    void StartAttack(float delay)
    {
        PlayerStatus player = Target.parent.GameObject.GetComponent<PlayerStatus>;
        StartCoroutine(AttackAction(player, delay));
    }
    IEnumerator AttackAction(GameObject player,float delay)
    {        
        //waits delay value's seconds before attacking
        yield return new WaitForSeconds(delay);
        if(inRange == true)
        {
            player.takeDamage(AttackDamage);
        }
    }

    private void Die()
    {
        GetComponent<NetworkObject>().Despawn();
    }
}
