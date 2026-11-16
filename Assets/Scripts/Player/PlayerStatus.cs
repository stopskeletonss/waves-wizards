using Unity.Netcode;
using UnityEngine;

// only allow one PlayerStatus component per GameObject
[DisallowMultipleComponent]

public class PlayerStatus : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] public float maxHP = 100f;

    public NetworkVariable<float> CurrentHP =
        new NetworkVariable<float>(
            100f, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
        );

    [Header("Character")]
    public NetworkVariable<CharacterType> Character =
        new NetworkVariable<CharacterType>(
            CharacterType.Unassigned,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            CurrentHP.Value = maxHP; // initialize health to max
        }

        // server's character manager assigns a character to this client
        if (IsServer && CharacterAssignmentManager.Instance != null)
        {
            CharacterAssignmentManager.Instance.AssignCharacter(OwnerClientId);
        }

        // event listeners for character assignment and health changes
        Character.OnValueChanged += OnCharacterChanged;
        CurrentHP.OnValueChanged += OnHealthChanged;

        // debugging
        Debug.Log("Player spawned.\nCharacter: " + Character.Value + "\nHealth: " + CurrentHP.Value);
    }


    public override void OnNetworkDespawn()
    {
        // remove revent listeners if the player object is removed (player disconnected)
        Character.OnValueChanged -= OnCharacterChanged;
        CurrentHP.OnValueChanged -= OnHealthChanged;

        base.OnNetworkDespawn();

    }


    public void TakeDamage(float damageAmount)
    {
        ApplyDamageServerRpc(damageAmount);
    }

    [ServerRpc(RequireOwnership = false)] // allows other objects (enemies to request damage to this player's object)
    private void ApplyDamageServerRpc(float damageAmount)
    {
        // apply damage and prevent health from dropping below 0
        CurrentHP.Value = Mathf.Max(0f, CurrentHP.Value - damageAmount);

        // debugging
        Debug.Log(Character.Value + " took " + damageAmount + " damage. HP: " + CurrentHP.Value);
    }


    // called when the client's character assignment changes
    // TODO: switch player's 3D model and UI
    private void OnCharacterChanged(CharacterType prevCharacter, CharacterType newCharacter)
    {
        // debugging
        Debug.Log("Character changed from " + prevCharacter + " to " + newCharacter);
    }

    
    // called when the client's health changes
    // TODO: change health bar colour and character portrait to reflect health state
    private void OnHealthChanged(float prevHP, float newHP)
    {
        // debugging
        Debug.Log(Character.Value + " health changed from " + prevHP + " to " + newHP);
    }
}
