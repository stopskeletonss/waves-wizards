using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Active Player")]
    public Slider activeHealthBar;
    public Image activeCharacterIcon;

    [Header("Hex")]
    public GameObject hexRow;
    public Slider hexHealthBar;
    public PlayerIcon hexIcon;
    public Sprite hexFull, hexMed, hexLow;


    [Header("Cyrus")]
    public GameObject cyrusRow;
    public Slider cyrusHealthBar;
    public PlayerIcon cyrusIcon;
    public Sprite cyrusFull, cyrusMed, cyrusLow;


    [Header("Snickerdoodle")]
    public GameObject snickerdoodleRow;
    public Slider snickerdoodleHealthBar;
    public PlayerIcon snickerdoodleIcon;
    public Sprite snickerdoodleFull, snickerdoodleMed, snickerdoodleLow;


    [Header("Orion")]
    public GameObject orionRow;
    public Slider orionHealthBar;
    public PlayerIcon orionIcon;
    public Sprite orionFull, orionMed, orionLow;

    private float refreshTimer = 0f; 
    private const float refreshInterval = 0.5f;


    private void Update()
    {
        refreshTimer -= Time.deltaTime;

        if (refreshTimer > 0f)
        {
            return;
        }

        refreshTimer = refreshInterval;

        UpdateHUD(); // refresh the active player's HUD and the team health HUD
    }


    private void UpdateHUD()
    {
        // find all PlayerStatus components because they contain the player's health and character
        PlayerStatus[] players =
            FindObjectsByType<PlayerStatus>(FindObjectsSortMode.None);

        PlayerStatus localPlayer = null;

        // find the player controlled by this client
        foreach (PlayerStatus player in players)
        {
            if (!player.IsSpawned)
            {
                continue;
            }

            if (player.IsOwner)
            {
                localPlayer = player;
                break;
            }
        }

        if (localPlayer == null)
        {
            return;
        }

        // update the top-left health bar using the local player's health
        SetHealth(activeHealthBar, localPlayer);

        // initialize all player icons
        InitializePlayerIcons(players);

        // display the icon matching the local player's selected character
        SetActivePlayerIcon(localPlayer.Character.Value);

        // update the bottom-left health bars
        // the local player is excluded from their own team list because
        // their health is already displayed in the top-left HUD
        UpdateTeamRow(
            players,
            CharacterType.Hex,
            localPlayer,
            hexRow,
            hexHealthBar);

        UpdateTeamRow(
            players,
            CharacterType.Cyrus,
            localPlayer,
            cyrusRow,
            cyrusHealthBar);

        UpdateTeamRow(
            players,
            CharacterType.Snickerdoodle,
            localPlayer,
            snickerdoodleRow,
            snickerdoodleHealthBar);

        UpdateTeamRow(
            players,
            CharacterType.Orion,
            localPlayer,
            orionRow,
            orionHealthBar);
    }


    private void SetActivePlayerIcon(CharacterType character)
    {
        // choose the icon that matches the active player's character
        switch (character)
        {
            case CharacterType.Hex:
                CopyIcon(hexIcon);
                break;

            case CharacterType.Cyrus:
                CopyIcon(cyrusIcon);
                break;

            case CharacterType.Snickerdoodle:
                CopyIcon(snickerdoodleIcon);
                break;

            case CharacterType.Orion:
                CopyIcon(orionIcon);
                break;
        }
    }

    // copies icon to the active player's portrait slot
    private void CopyIcon(PlayerIcon sourceIcon)
    {
        if (sourceIcon == null || activeCharacterIcon == null || sourceIcon.CurrentSprite == null)
            return

        activeCharacterIcon.sprite = sourceIcon.CurrentSprite;
        activeCharacterIcon.enabled = true;
    }


    private void UpdateTeamRow(
        PlayerStatus[] players,
        CharacterType character,
        PlayerStatus localPlayer,
        GameObject row,
        Slider healthBar)
    {
        // find the player assigned to this character
        PlayerStatus teamPlayer = FindPlayer(players, character);

        if (row == null)
        {
            return;
        }

        // hide the row if nobody is playing this character
        // also hide it when this character is the local player,
        // since their health is displayed in the top-left instead
        if (teamPlayer == null || teamPlayer == localPlayer)
        {
            row.SetActive(false);
            return;
        }

        row.SetActive(true);

        // update this teammate's health bar with their current health
        SetHealth(healthBar, teamPlayer);
    }


    private PlayerStatus FindPlayer(
        PlayerStatus[] players,
        CharacterType character)
    {
        // search through the spawned players to find the one assigned to the requested character
        foreach (PlayerStatus player in players)
        {
            if (!player.IsSpawned)
            {
                continue;
            }

            if (player.Character.Value == character)
            {
                return player;
            }
        }
        return null;
    }


    private void SetHealth(Slider healthBar, PlayerStatus player)
    {
        if (healthBar == null || player == null)
        {
            return;
        }

        // set the slider's minimum, maximum, and current health values
        healthBar.minValue = 0f;
        healthBar.maxValue = player.maxHP;
        healthBar.value = player.CurrentHP.Value;
    }


    private void InitializePlayerIcons(PlayerStatus[] players)
    {
        // for each player
        foreach (PlayerStatus player in players)
        {
            // only initialize player icon if the player has spawned
            if (player == null || !player.IsSpawned)
                continue;

            switch (player.Character.Value)
            {
                // initialize Hex player icon
                case CharacterType.Hex:
                    if (hexIcon != null)
                        hexIcon.Initialize(player, hexFull, hexMed, hexLow);
                    break;
                
                // initialize Cyrus player icon
                case CharacterType.Cyrus:
                    if (cyrusIcon != null)
                        cyrusIcon.Initialize(player, cyrusFull, cyrusMed, cyrusLow);
                    break;

                // initialize Snickerdoodle player icon
                case CharacterType.Snickerdoodle:
                    if (snickerdoodleIcon != null)
                        snickerdoodleIcon.Initialize(player, snickerdoodleFull, snickerdoodleMed, snickerdoodleLow);
                    break;

                // initialize Orion player icon
                case CharacterType.Orion:
                    if (orionIcon != null)
                        orionIcon.Initialize(player, orionFull, orionMed, orionLow);
                    break;
            }
        }
    }
}