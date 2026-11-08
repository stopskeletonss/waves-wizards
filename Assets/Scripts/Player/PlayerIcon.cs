using UnityEngine;
using UnityEngine.UI;

public class PlayerIcon : MonoBehaviour
{
    [Header("PlayerIcons")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Sprite fullIcon;
    [SerializeField] private Sprite medIcon;
    [SerializeField] private Sprite lowIcon;

    private PlayerStatus playerStatus;

    // allows PlayerHealthUI to access the currently displayed icon
    public Sprite CurrentSprite => portraitImage != null
        ? portraitImage.sprite
        : null;

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

        Debug.Log("Player HP changed: " + oldHP + " -> " + newHP);

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
