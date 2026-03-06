using UnityEngine;
using UnityEngine.AI;

public class EnemyAITest : MonoBehaviour
{
    public Transform Target;
    public float AttackDistance;
    
    private NavMeshAgent player;
    private float playerDistance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        playerDistance = Vector3.Distance(player.transform.position, Target.position);
        if (playerDistance < AttackDistance)
        {
            //Attack Logic here
            Debug.Log("Attack!");
        }
        else
        {
            player.destination = Target.position;
            
        }
    }
}
