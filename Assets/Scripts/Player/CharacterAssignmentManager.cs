using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// custom data type to distinguish between the playable characters (unassigned is the default value)
public enum CharacterType
{
    Unassigned,
    Hex,
    Cyrus,
    Snickerdoodle,
    Orion
}

[DisallowMultipleComponent] // prevents more than one CharacterAssignmentManager from being attached to the same GameObject

public class CharacterAssignmentManager : NetworkBehaviour
{
    public static CharacterAssignmentManager Instance { get; private set; }


    // stores the character assigned to each client's ID
    private readonly Dictionary<ulong, CharacterType> assignments =
        new Dictionary<ulong, CharacterType>();


    // list of characters available to be assigned
    private static readonly CharacterType[] availableCharacters =
    {
        CharacterType.Hex,
        CharacterType.Cyrus,
        CharacterType.Snickerdoodle,
        CharacterType.Orion
    };


    public override void OnNetworkSpawn()
    {
        Instance = this;

        // only the server can assign characters at this point
        // TODO: players should be able to assign their own characters in the future
        if (!IsServer)
            return;

        // listens for clients connecting and disconnecting
        NetworkManager.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

        // assign a character to each client
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            AssignCharacter(clientId);
        }
    }


    public override void OnNetworkDespawn()
    {
        // stop listening for connects/diconnects when the server manager is removed
        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (Instance == this)
        {
            Instance = null;
        }

        base.OnNetworkDespawn();
    }


    // assigns an available character to a specific client (via clientId)
    public void AssignCharacter(ulong clientId)
    {
        // again, only the server can assign characters at this point
        // TODO: players should be able to assign their own characters in the future
        if (!IsServer)
            return;

        // do not assign a character to this client if it already has been assigned a character
        if (assignments.ContainsKey(clientId))
            return;

        // stop if the client is no longer connected to the server
        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
            return;

        // stop if connected client doesn't have a spawned player object yet
        if (client.PlayerObject == null)
            return;

        // get the PlayerStatus attached to this player's GameObject
        PlayerStatus playerStatus = client.PlayerObject.GetComponent<PlayerStatus>();

        // go through the list of available characters in order
        foreach (CharacterType character in availableCharacters)
        {
            if (!assignments.ContainsValue(character)) // check whether another player is already assigned this character
            {
                // if not, assign this client the character
                assignments.Add(clientId, character);
                playerStatus.Character.Value = character;

                // debugging
                // TODO: remove later
                Debug.Log("Client " + clientId + " was assigned " + character);

                return;
            }
        }
        // debug log for if there are more than 4 connected clients
        Debug.LogWarning("No characters available for client " + clientId);
    }


    //called automatically when a client connects to the session
    private void OnClientConnected(ulong clientId)
    {
        AssignCharacter(clientId);
    }


    // called automatically when a client disconnects from the session
    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer)
            return;

        // check whether the disconnected client had a character assigned
        if (assignments.TryGetValue(clientId, out CharacterType character))
        {
            // remove client from the dictionary to make the character available again
            assignments.Remove(clientId);
            Debug.Log(character + " is now available for another client.");
        }
    }
}