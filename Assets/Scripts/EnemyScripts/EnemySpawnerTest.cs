using UnityEngine;


public class EnemySpawnerTest : MonoBehaviour
{
    private int TotalEnemies; //for testing the spawner tracks how many enemies it spawned
    [SerializeField]
    private int MaxEnemies;   //Enemy Spawn Limit
    [SerializeField]
    private GameObject enemyPrefab;
    [SerializeField]
    private float SpawnDelay; //how long of a delay before spawning a new Enemy
    public float spawnTimer;

    //Couldn't figure out the networking stuff so we will have to refactor this
    //to test the networking properly
   //Start Method for Networking
//    public override void OnNetworkSpawn()
//     {
//         base.OnNetworkSpawn();

//         tbh not sure if this needs anything on start
//     }
    // Update is called once per frame
    private void Update()
    {
        if(TotalEnemies < MaxEnemies)
        {                        
            if(SpawnDelay < spawnTimer)
                {
                    spawnTimer = 0;
                    spawnEnemy(enemyPrefab);
                }
            spawnTimer++;
        }
    }

    private void spawnEnemy(GameObject enemy)
    {
        GameObject newEnemy = Instantiate(enemy, transform.position, transform.rotation);
        TotalEnemies++;
    }
}
