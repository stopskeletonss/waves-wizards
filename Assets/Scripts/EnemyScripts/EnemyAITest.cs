using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using System.Collections;

// make sure the enemy has components needed for networking and navigation
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NavMeshAgent))]

public class EnemyAITest : NetworkBehaviour
{

    [Header("Target")]
    public Transform Target; // transform of the player the enemy is targeting
    [SerializeField] private float targetRefInterval = 0.25f; // refresh interval for enemies to check whether or not to switch targets

    [Header("Attack")]
    public float AttackDamage = 10f;
    public float AttackDistance = 2f;
    public float AttackDelay = 0.5f; // time between starting an attack and applying damage
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Health")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("References")]
    [SerializeField] private TestHealthBar healthBar;
    [SerializeField] private Animator animator;

    private NavMeshAgent skeleton;

    private PlayerStatus targetPlayer;
    private Coroutine attackCoroutine;

    private bool isAttacking;
    private float nextTargetRefTime; // time when the enemy should re-search for a target
    private float nextAttackTime; // time when the enemy is allowed to start its next attack


    // health written by the server and replicated on all clients so everyone sees the same enemy health
    private readonly NetworkVariable<float> networkCurrentHP =
        new NetworkVariable<float>(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        skeleton = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        networkCurrentHP.OnValueChanged += OnHealthChanged; // listen for health changes so each instance can update its health bar

        if (IsServer) // only the server can initialize and update enemy health/attacks
        {
            currentHP = maxHP;
            networkCurrentHP.Value = maxHP;

            // allow the enemy to search for a target and attack 2.5 seconds after spawning
            nextTargetRefTime = Time.time + 2.5f;
            nextAttackTime = Time.time + 2.5f;
        }
        else
        {
            if (skeleton != null)
                skeleton.enabled = false;
        }

        UpdateHealthBar(networkCurrentHP.Value);
    }


    public override void OnNetworkDespawn()
    {
        networkCurrentHP.OnValueChanged -= OnHealthChanged; // stop listening for health changes after the enemy despawns

        // stop an attack coroutine if the enemy despawns during an attack
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        base.OnNetworkDespawn();
    }


    void Update()
    {
        if (!IsSpawned || !IsServer)
            return;

        if (skeleton == null || !skeleton.enabled)
            return;

        // periodically search for the closest living player
        if (Time.time >= nextTargetRefTime)
        {
            FindClosestPlayer();
            nextTargetRefTime = Time.time + targetRefInterval; // schedule the next target search
        }

        // enemy stops moving if there is no valid target (handles dead and disconnected players)
        if (targetPlayer == null || targetPlayer.CurrentHP.Value <= 0f || !targetPlayer.gameObject.activeInHierarchy)
        {
            Target = null;
            skeleton.isStopped = true;
            return;
        }

        Target = targetPlayer.transform;

        // distance between the enemy and its target
        float distance = Vector3.Distance(transform.position, Target.position);

        // check if the enemy is within attacking distance of its target
        if (distance <= AttackDistance)
        {
            skeleton.isStopped = true; // enemy stops moving to attack
            skeleton.ResetPath();

            // start an attack only if one is not already running
            if (!isAttacking && Time.time >= nextAttackTime)
            {
                attackCoroutine = StartCoroutine(AttackTarget());
            }
        }
        else
        {
            // the player is too far away to attack, so enemy chases them
            skeleton.isStopped = false;
            skeleton.SetDestination(Target.position);
        }
    }


    // searches for the closest living player
    private void FindClosestPlayer()
    {
        // get all active PlayerStatus components in the scene
        PlayerStatus[] players = FindObjectsOfType<PlayerStatus>();
        PlayerStatus closestPlayer = null;
        float closestDistance = Mathf.Infinity;

        // check every player to find the closest valid target
        foreach (PlayerStatus player in players)
        {
            if (player == null)
                continue;

            // ignore dead or disconnected players
            if (!player.gameObject.activeInHierarchy || player.CurrentHP.Value <= 0f)
            {
                continue;
            }

            // measure the distance from the enemy to the current player
            float distance = Vector3.Distance(transform.position, player.transform.position);

            // overwrite the closest distance/player if the current player is closer
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }

        targetPlayer = closestPlayer; // store the closest valid player as the target for this enemy
    }


    // handles one attack from start to finish, including the delay before damage is applied
    private IEnumerator AttackTarget()
    {
        isAttacking = true; // set attacking to true to prevent multiple attacks from the same enemy at once

        // play the attack animation
        if (animator != null)
            animator.SetTrigger("Attack");

        yield return new WaitForSeconds(AttackDelay); // wait for the attack's wind-up before checking whether or not it hits

        // re-check the target after the delay because the player could have died/disconnected during the attack
        if (IsSpawned &&
            IsServer &&
            targetPlayer != null &&
            targetPlayer.gameObject.activeInHierarchy &&
            targetPlayer.CurrentHP.Value > 0f)
        {
            // re-calculate the distance when the damage would actually be applied
            float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);

            // only apply the damage to the player if they are still within attacking distance
            if (distance <= AttackDistance)
            {
                targetPlayer.TakeDamage(AttackDamage);

                // debugging
                Debug.Log(
                    $"{name} attacked {targetPlayer.name} " +
                    $"for {AttackDamage} damage."
                );
            }
        }

        nextAttackTime = Time.time + attackCooldown; // set the next allowed attack time
        isAttacking = false;
        attackCoroutine = null;
    }


    // called by the server when the enemy is supposed to take damage
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damageAmount)
    {
        if (!IsServer || !IsSpawned)
            return;

        ApplyDamage(damageAmount);
    }


    // applies damage to the enemy
    public void ApplyDamage(float damageAmount)
    {
        if (!IsServer || !IsSpawned)
            return;

        // subtract damage without letting health go below 0
        currentHP = Mathf.Max(0f, networkCurrentHP.Value - damageAmount);
        networkCurrentHP.Value = currentHP;

        // debugging
        Debug.Log($"Enemy Health: {currentHP}/{maxHP}");

        // despawn the enemy when its health reaches 0
        if (currentHP <= 0f)
            Die();
    }


    // called on each instance whenever the replicated health value changes
    private void OnHealthChanged(float prevHealth, float newHealth)
    {
        // update local copy
        currentHP = newHealth;
        UpdateHealthBar(newHealth);
    }


    // update's the enemy's health bar
    private void UpdateHealthBar(float health)
    {
        if (healthBar != null)
            healthBar.UpdateHealthBar(maxHP, health);
    }


    // removes the enemy from the server when it dies
    private void Die()
    {
        if (!IsServer || !IsSpawned)
            return;

        // stops an attack if one is running from the removed enemy
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        NetworkObject.Despawn(true);
    }
}