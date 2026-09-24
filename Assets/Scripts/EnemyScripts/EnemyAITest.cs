using UnityEngine;
using Unity.Netcode;
using System.Collections;

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
    public bool applyDamage = false;

    Animator animator;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Skeleton = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponent<Animator>();

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
            //below this might be a cleaner code option not sure yet
            //agent.SetDestination(destination)
            Skeleton.destination = Skeleton.transform.position;
            //Attack Logic here when close enough
            Debug.Log("Attack!");
            StartAttack();
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
    void StartAttack()
    {
        Debug.Log("Starting Attack");
        //storing victim's playerstatus to access when adjusting damage (playerStatus tracks HP, Mana/Ammo, etc.)
        PlayerStatus player = Target.GetComponent<PlayerStatus>();
        AttackAction(player);
    }
    void AttackAction(PlayerStatus playerStatus)
    {
        //ensures the animation doesn't interrupt itself
        if(!animator.GetCurrentAnimatorStateInfo(0).IsName("EnemyAttack"))
        {
            //Debug.Log("Winding Up!");
            animator.SetTrigger("Attack");  
        }

        if(inRange == true && applyDamage == true)
        {
            applyDamage = false;
            //Debug.Log("In Range!");
            playerStatus.takeDamage(AttackDamage);
        }
    }

    private void Die()
    {
        GetComponent<NetworkObject>().Despawn();
    }
}
