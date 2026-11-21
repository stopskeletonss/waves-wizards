using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerUI : MonoBehaviour
{
    [Header("Active Player")]
    public Slider activeHealthBar;
    public Image activeCharacterIcon;
    public Image activeDeathOverlay;
    [SerializeField] private CustomRadialFill ammoFill;

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


    [Header("Health Bar Colours")]
    public Color fullHealthCol = Color.green;
    public Color twoThirdsHealthCol = Color.yellow;
    public Color halfHealthCol = new Color(1f, 0.5f, 0f); // orange
    public Color oneThirdHealthCol = Color.red;


    [Header("Staff Elements")]
    public Image primaryElementImage;
    public Image[] radialElementImages;
    public Sprite basicStaffIcon;
    public Sprite fireIcon;
    public Sprite iceIcon;
    public Sprite windIcon;
    public Sprite poisonIcon;
    public Sprite lightIcon;
    public Sprite darkIcon;


    private float refreshTimer = 0f;
    private const float refreshInterval = 0.1f;


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
        PlayerStatus[] players = FindObjectsByType<PlayerStatus>(FindObjectsSortMode.None);
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

        // get the local player's staff manager
        StaffManager localWeaponManager = localPlayer.GetComponent<StaffManager>();

        // get the local player's equipped staff
        StaffController currentStaff = localWeaponManager.GetCurrentStaff();

        // update the top-left health bar using the local player's health
        SetHealth(activeHealthBar, localPlayer);

        // update the ammo bar for the local player
        SetAmmo(localPlayer, localWeaponManager);

        // update the elements on the player's equipped staff
        UpdateElements(currentStaff);

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
        // copy character portrait
        if (sourceIcon == null || activeCharacterIcon == null || sourceIcon.CurrentSprite == null)
            return;

        // copy death state
        if (activeDeathOverlay != null)
            activeDeathOverlay.enabled = sourceIcon.IsDead;

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

        float healthPerc = player.CurrentHP.Value / player.maxHP; // calculate the percentage of health left
        Image fillImg = healthBar.fillRect.GetComponent<Image>(); // get the fill image of the health bar

        // change the health bar colour based on the remaining health left
        if (healthPerc > 0.66f)
            fillImg.color = fullHealthCol;
        else if (healthPerc > 0.50f)
            fillImg.color = twoThirdsHealthCol;
        else if (healthPerc > 0.33f)
            fillImg.color = halfHealthCol;
        else
            fillImg.color = oneThirdHealthCol;
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


    private void SetAmmo(PlayerStatus player, StaffManager weaponManager)
    {
        if (ammoFill == null || player == null || weaponManager == null)
        {
            return;
        }

        // set the number of segments to match the maximum ammo
        ammoFill.SetSegmentCount(weaponManager.maxAmmo);

        // calculate the percentage of ammo remaining
        float ammoPerc = (float)weaponManager.CurrentAmmo.Value / weaponManager.maxAmmo;

        // update the radial ammo indicator
        ammoFill.SetProgress(ammoPerc);
    }


    // gets the sprite based on the staff element
    private Sprite GetElementSprite(StaffElement element)
    {
        switch (element)
        {
            case StaffElement.Fire:
                return fireIcon;

            case StaffElement.Ice:
                return iceIcon;

            case StaffElement.Wind:
                return windIcon;

            case StaffElement.Poison:
                return poisonIcon;

            case StaffElement.Light:
                return lightIcon;

            case StaffElement.Dark:
                return darkIcon;

            default:
                return null;
        }
    }


    // updates the element UI when there is a change to the elements on the staff
    private void UpdateElements(StaffController staff)
    {
        List<StaffElement> elements = staff != null
            ? staff.elements
            : null;

        int elementCount = elements != null ? elements.Count : 0;

        // display the basic icon if there are no elements
        if (elementCount == 0)
        {
            primaryElementImage.sprite = basicStaffIcon;
            primaryElementImage.enabled = basicStaffIcon != null;
        }
        else
        {
            // display the first element in the center
            primaryElementImage.sprite = GetElementSprite(elements[0]);
            primaryElementImage.enabled = primaryElementImage.sprite != null;
        }

        // display remaining elements in the predefined radial positions
        for (int i = 0; i < radialElementImages.Length; i++)
        {
            Image icon = radialElementImages[i];

            if (icon == null)
                continue;

            // hide unused slots
            if (i + 1 >= elementCount)
            {
                icon.enabled = false;
                continue;
            }

            // display the next element
            icon.sprite = GetElementSprite(elements[i + 1]);
            icon.enabled = icon.sprite != null;
        }
    }
}