using UnityEngine;
using Unity.Netcode;

public class PlayerModelAssignment : NetworkBehaviour
{
    [Header("Character Models")]
    public GameObject hexModel;
    public GameObject cyrusModel;
    public GameObject snickerdoodleModel;
    public GameObject orionModel;

    private PlayerStatus playerStatus;


    public override void OnNetworkSpawn()
    {
        playerStatus = GetComponent<PlayerStatus>(); // get the player's networked status

        if (playerStatus == null)
            return;

        playerStatus.Character.OnValueChanged += OnCharacterChanged; // listen for changes to the player's character type
        UpdateCharacterModel(playerStatus.Character.Value); // apply the current character in case it was assigned before this script spawned
    }

    public override void OnNetworkDespawn()
    {
        // stop listening for character changes
        if (playerStatus != null)
            playerStatus.Character.OnValueChanged -= OnCharacterChanged;

        base.OnNetworkDespawn();
    }


    // called whenever the networked character type changes
    private void OnCharacterChanged(CharacterType previousCharacter, CharacterType newCharacter)
    {
        UpdateCharacterModel(newCharacter);
    }


    // activate the model that matches the player's character type
    private void UpdateCharacterModel(CharacterType character)
    {
        // disable all character models first
        if (hexModel != null)
            hexModel.SetActive(false);

        if (cyrusModel != null)
            cyrusModel.SetActive(false);

        if (snickerdoodleModel != null)
            snickerdoodleModel.SetActive(false);

        if (orionModel != null)
            orionModel.SetActive(false);

        // activate the model matching the assigned character
        switch (character)
        {
            case CharacterType.Hex:
                if (hexModel != null)
                    hexModel.SetActive(true);
                break;

            case CharacterType.Cyrus:
                if (cyrusModel != null)
                    cyrusModel.SetActive(true);
                break;

            case CharacterType.Snickerdoodle:
                if (snickerdoodleModel != null)
                    snickerdoodleModel.SetActive(true);
                break;

            case CharacterType.Orion:
                if (orionModel != null)
                    orionModel.SetActive(true);
                break;

            case CharacterType.Unassigned:
                break;
        }
    }
}