using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RoundSystem : NetworkBehaviour
{
    [Header("Spawning")]
    [SerializeField] private NetworkObject enemyPrefab;
    //We can assign empty gameobjects around the map for the enemies to spawn at
    [SerializeField] private Transform[] spawnPoints;
    //This is how often we attempt to spawn a new enemy during a round.
    [SerializeField] private float spawnInterval = 1.0f;

    [Header("Rounds")]
    [SerializeField] private int round1EnemyCount = 5;
    [SerializeField] private float exponentialMultiplier = 1.29f;
    //Each round spawns 29% more enemies per round
    [SerializeField] private float nextRoundDelay = 3f;
    //A mini delay between rounds
    [SerializeField] private int maxAliveEnemies = 30;

    [SerializeField] public bool autoStartOnServer = true;
    //Testing

    private int currentRound;
    private int enemiesAlive;

    private int enemiesToSpawnThisRound;
    private int enemiesSpawnedThisRound;
    private int enemiesKilledThisRound;

    private Coroutine spawnCoroutine;

    private readonly List<NetworkObject> spawnedEnemies = new List<NetworkObject>();
    //Enemy list for debugging purposes

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;
        //Ensure the server is the only thing that can spawn enemies
        if (autoStartOnServer)
            StartRound(1);
    }

    private void StartRound(int roundNumber)
    {
        currentRound = roundNumber;

        enemiesToSpawnThisRound = GetEnemyCountForRound(roundNumber);
        enemiesSpawnedThisRound = 0;
        enemiesKilledThisRound = 0;

        spawnedEnemies.Clear();
        enemiesAlive = 0;

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        //Stop old coroutines in case they don't automatically stop

        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    private int GetEnemyCountForRound(int roundNumber)
    {
        float count = round1EnemyCount * Mathf.Pow(exponentialMultiplier, Mathf.Max(0, roundNumber - 1));
        //This keeps spawns exponential

        return Mathf.Max(1, Mathf.RoundToInt(count));
        //Round it upwards to an int to spawn enemies, since we can't spawn 0.7 of an enemy lol
    }

    private IEnumerator SpawnLoop()
    {
        while (enemiesSpawnedThisRound < enemiesToSpawnThisRound)
        //Keep spawning until we have hit the max for the round
        {
            if (enemiesAlive >= maxAliveEnemies)
            {
                yield return null;
                continue;
            }

            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }

        spawnCoroutine = null;
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemyPrefab is not assigned");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("There are no spawnpoints");
            return;
        }

        Vector3 pos = GetSpawnPosition();
        Quaternion rot = Quaternion.identity;

        NetworkObject enemy = Instantiate(enemyPrefab, pos, rot);

        EnemyRoundTracker tracker = enemy.GetComponent<EnemyRoundTracker>();
        if (tracker == null)
            tracker = enemy.gameObject.AddComponent<EnemyRoundTracker>();

        enemy.Spawn();
        tracker.Init(this);

        enemiesSpawnedThisRound++;
        enemiesAlive++;

        spawnedEnemies.Add(enemy);
    }

    private Vector3 GetSpawnPosition()
        //Pick a random spawn point
    {
        Transform t = spawnPoints[Random.Range(0, spawnPoints.Length)];
        return t.position;
    }

    public void NotifyEnemyDespawned(EnemyRoundTracker tracker)
    {
        if (!IsServer) return;

        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        enemiesKilledThisRound++;

        if (enemiesSpawnedThisRound >= enemiesToSpawnThisRound && enemiesAlive == 0)
            //If everything is spawned and everything is dead end the round
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            Invoke(nameof(StartNextRound), nextRoundDelay);
        }
    }

    private void StartNextRound()
    {
        if (enemiesAlive != 0) return;

        StartRound(currentRound + 1);
    }

    //References for UI later
    public int GetCurrentRound() => currentRound;
    public int GetEnemiesAlive() => enemiesAlive;
}