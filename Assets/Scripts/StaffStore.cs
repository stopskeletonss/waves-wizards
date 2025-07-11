using UnityEngine;
using TMPro;

public class StaffShop : MonoBehaviour
{
    [Header("Shop Settings")]
    public GameObject staffToSell;
    public int staffPrice = 1000;
    public int manaCost = 200; // Cost to conjure mana (refill ammo)

    private Collider triggerCollider;
    private bool playerInTrigger = false;
    private StaffManager playerStaffManager;
    private GameObject player;
    private TMP_Text buyPromptText;

    private bool isConjureMode = false;

    void Start()
    {
        // Find child named "BuyBoundary" and get its collider
        Transform boundary = transform.Find("BuyBoundary");

        if (boundary != null)
        {
            triggerCollider = boundary.GetComponent<Collider>();

            if (triggerCollider == null || !triggerCollider.isTrigger)
                Debug.LogWarning("BuyBoundary exists but does not have a trigger collider.");
        }
        else
        {
            Debug.LogError("BuyBoundary child not found under StaffShop.");
        }
    }

    void Update()
    {
        if (triggerCollider == null) return;

        // Detect player inside boundary
        if (!playerInTrigger)
        {
            GameObject potentialPlayer = GameObject.FindGameObjectWithTag("Player");

            if (potentialPlayer != null && triggerCollider.bounds.Contains(potentialPlayer.transform.position))
            {
                player = potentialPlayer;
                playerStaffManager = player.GetComponent<StaffManager>();

                // Find TMP child with "BuyTMP" tag
                Transform[] children = player.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child.CompareTag("BuyTMP"))
                    {
                        buyPromptText = child.GetComponent<TMP_Text>();
                        break;
                    }
                }

                if (buyPromptText != null)
                {
                    buyPromptText.gameObject.SetActive(true);

                    // Check if staff already owned
                    if (IsStaffOwned(staffToSell))
                    {
                        isConjureMode = true;
                        buyPromptText.text = $"Conjure Mana: {manaCost}";
                    }
                    else
                    {
                        isConjureMode = false;
                        string staffName = staffToSell != null ? staffToSell.name : "Staff";
                        buyPromptText.text = $"Buy {staffName}: {staffPrice}";
                    }
                }
                else
                {
                    Debug.LogWarning("BuyTMP text not found on player.");
                }

                playerInTrigger = true;
                Debug.Log("Player entered shop area. Press E to buy staff.");
            }
        }
        else
        {
            if (!triggerCollider.bounds.Contains(player.transform.position))
            {
                playerInTrigger = false;
                player = null;
                playerStaffManager = null;

                if (buyPromptText != null)
                    buyPromptText.gameObject.SetActive(false);

                buyPromptText = null;
                isConjureMode = false;

                Debug.Log("Player left shop area.");
            }
        }

        // Handle purchase or mana conjure
        if (playerInTrigger && Input.GetKeyDown(KeyCode.E) && playerStaffManager != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();

            if (playerController == null)
                return;

            if (isConjureMode)
            {
                // Mana conjure logic (refill ammo)
                if (playerController.GetCurrentPoints() >= manaCost)
                {
                    playerController.SpendPoints(manaCost);
                    playerStaffManager.RefillAmmoForStaff(staffToSell);
                    Debug.Log("Mana conjured and staff ammo refilled.");
                }
                else
                {
                    Debug.Log("Not enough points to conjure mana.");
                }
            }
            else
            {
                // Purchase logic
                if (playerController.GetCurrentPoints() >= staffPrice)
                {
                    if (playerStaffManager.GetStaffCount() < 2)
                        playerStaffManager.AddStaffToSlot(1, staffToSell);
                    else
                        playerStaffManager.ReplaceActiveStaff(staffToSell);

                    playerController.SpendPoints(staffPrice);
                    Debug.Log("Staff purchased.");
                }
                else
                {
                    Debug.Log("Not enough points to purchase staff.");
                }
            }
        }
    }

    // Helper method to check if player owns the staff
    private bool IsStaffOwned(GameObject staffPrefab)
    {
        if (playerStaffManager == null || staffPrefab == null) return false;

        foreach (var staff in playerStaffManager.staffSlots)
        {
            if (staff != null && staff.name.Contains(staffPrefab.name))
                return true;
        }

        return false;
    }
}