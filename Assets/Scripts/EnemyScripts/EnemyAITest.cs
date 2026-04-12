using UnityEngine;
using UnityEngine.AI;

public class EnemyAITest : MonoBehaviour
{
    //Player Transform
    public Transform Target;
    //Distance to damage a Player
    public float AttackDistance;
    
    //NavAgent for this object
    private NavMeshAgent Skeleton;
    //Distance to player
    private float playerDistance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Skeleton = GetComponent<NavMeshAgent>();
        if (Target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
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
            //Stops the skeleton when in range
            Skeleton.destination = Skeleton.transform.position;
            //Attack Logic here when close enough
            Debug.Log("Attack!");
        }
        else
        {
            //otherwise moves towards player
            Skeleton.destination = Target.position;
            
        }
    }
}
