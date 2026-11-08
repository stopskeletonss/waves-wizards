using UnityEngine;
using UnityEngine.UI;

public class PlayerIcon : MonoBehaviour
{
    [Header("PlayerIcons")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Sprite fullIcon;
    [SerializeField] private Sprite medIcon;
    [SerializeField] private Sprite lowIcon;

    [SerializeField] private Image deathOverlay;

    private PlayerStatus playerStatus;

    // allows PlayerHealthUI to access the currently displayed icon
    public Sprite CurrentSprite => portraitImage != null
        ? portraitImage.sprite
        : null;

    // allows PlayerHealthUI to know whether or not the active player is dead
    public bool IsDead => playerStatus != null &&
        playerStatus.CurrentHP.Value <= 0f;

    public void Initialize(PlayerStatus status, Sprite full, Sprite med, Sprite low)
    {
        if (status == null)
        {
            Debug.LogWarning("PlayerIcon: PlayerStatus is null!");
            return;
        }

        if (portraitImage == null)
        {
            Debug.LogWarning("PlayerIcon: Portrait Image is not assigned!");
            return;
        }

        if (full == null || med == null || low == null)
        {
            Debug.LogWarning("PlayerIcon: One or more health sprites are missing!");
            return;
        }

        if (playerStatus == status &&
            fullIcon == full &&
            medIcon == med &&
            lowIcon == low)
            return;

        if (playerStatus != null)
            playerStatus.CurrentHP.OnValueChanged -= UpdatePlayerIcon;

        playerStatus = status;
        fullIcon = full;
        medIcon = med;
        lowIcon = low;

        playerStatus.CurrentHP.OnValueChanged += UpdatePlayerIcon;

        Debug.Log("Initialized icon for " + status.Character.Value);

        UpdatePlayerIcon(
            playerStatus.CurrentHP.Value,
            playerStatus.CurrentHP.Value
        );
    }


    // update the player icon based on their HP
    private void UpdatePlayerIcon(float oldHP, float newHP)
    {
        if (portraitImage == null)
            return;

        // enable red death overlay when player reaches 0 HP
        if (deathOverlay != null)
            deathOverlay.enabled = newHP <= 0f;

        if (newHP > 66f)
            portraitImage.sprite = fullIcon;
        else if (newHP > 33f)
            portraitImage.sprite = medIcon;
        else
            portraitImage.sprite = lowIcon;
    }


    private void OnDestroy()
    {
        if (playerStatus != null)
        {
            playerStatus.CurrentHP.OnValueChanged -= UpdatePlayerIcon;
        }
    }
}
