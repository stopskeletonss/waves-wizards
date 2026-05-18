using UnityEngine;
using System;

public class StaffManager : MonoBehaviour
{
    [Header("Staff Settings")]
    public GameObject starterStaffPrefab;
    public Transform weaponHoldPoint;

    public GameObject[] staffSlots = new GameObject[2];

    private int currentSlot = 0;

    // Event to notify when a staff is equipped
    public event Action<GameObject> onStaffEquipped;

    void Start()
    {
        // Create catalyst object
        GameObject catalyst = new GameObject("StaffAnimationCatalyst");

        catalyst.transform.position = weaponHoldPoint.position;
        catalyst.transform.rotation = weaponHoldPoint.rotation;

        // Spawn staff as child of catalyst
        GameObject starter = Instantiate(
            starterStaffPrefab,
            catalyst.transform
        );

        starter.transform.localPosition = Vector3.zero;
        starter.transform.localRotation = Quaternion.identity;
        starter.transform.localScale = Vector3.one;

        // Assign references to controller
        StaffController controller =
            starter.GetComponent<StaffController>();

        if (controller != null)
        {
            controller.holdPoint = weaponHoldPoint;
            controller.animationCatalyst = catalyst.transform;
        }

        staffSlots[0] = starter;

        EquipStaff(0);
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        // Fire current staff
        if (Input.GetMouseButtonDown(0))
        {
            StaffController staff = GetCurrentStaff();

            if (staff != null)
                staff.Fire();
        }

        // Switch staff using 1 and 2
        if (Input.GetKeyDown(KeyCode.Alpha1))
            EquipStaff(0);

        else if (Input.GetKeyDown(KeyCode.Alpha2))
            EquipStaff(1);

        // Scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
            ScrollStaff(-1);

        else if (scroll < 0f)
            ScrollStaff(1);
    }

    void ScrollStaff(int direction)
    {
        int newSlot = currentSlot + direction;

        if (newSlot < 0)
            newSlot = staffSlots.Length - 1;

        if (newSlot >= staffSlots.Length)
            newSlot = 0;

        EquipStaff(newSlot);
    }

    void EquipStaff(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= staffSlots.Length)
            return;

        if (staffSlots[slotIndex] == null)
            return;

        for (int i = 0; i < staffSlots.Length; i++)
        {
            if (staffSlots[i] != null)
            {
                bool active = i == slotIndex;

                // Toggle catalyst instead of only staff
                staffSlots[i].transform.parent.gameObject.SetActive(active);
            }
        }

        currentSlot = slotIndex;

        onStaffEquipped?.Invoke(staffSlots[slotIndex]);
    }

    public StaffController GetCurrentStaff()
    {
        if (staffSlots[currentSlot] == null)
            return null;

        return staffSlots[currentSlot]
            .GetComponent<StaffController>();
    }

    public int GetStaffCount()
    {
        int count = 0;

        foreach (var staff in staffSlots)
        {
            if (staff != null)
                count++;
        }

        return count;
    }

    public void AddStaffToSlot(
        int slotIndex,
        GameObject newStaffPrefab
    )
    {
        if (slotIndex < 0 || slotIndex >= staffSlots.Length)
            return;

        // Destroy old catalyst if one exists
        if (staffSlots[slotIndex] != null)
        {
            Destroy(
                staffSlots[slotIndex]
                .transform.parent.gameObject
            );
        }

        // Create catalyst
        GameObject catalyst =
            new GameObject("StaffAnimationCatalyst");

        catalyst.transform.position = weaponHoldPoint.position;
        catalyst.transform.rotation = weaponHoldPoint.rotation;

        // Create new staff
        GameObject newStaff = Instantiate(
            newStaffPrefab,
            catalyst.transform
        );

        newStaff.transform.localPosition = Vector3.zero;
        newStaff.transform.localRotation = Quaternion.identity;
        newStaff.transform.localScale = Vector3.one;

        // Assign references
        StaffController controller =
            newStaff.GetComponent<StaffController>();

        if (controller != null)
        {
            controller.holdPoint = weaponHoldPoint;
            controller.animationCatalyst = catalyst.transform;
        }

        catalyst.SetActive(false);

        staffSlots[slotIndex] = newStaff;
    }

    public void ReplaceActiveStaff(GameObject newStaffPrefab)
    {
        AddStaffToSlot(currentSlot, newStaffPrefab);

        EquipStaff(currentSlot);
    }

    public void RefillAmmoForStaff(GameObject staffPrefab)
    {
        foreach (GameObject staff in staffSlots)
        {
            if (staff != null &&
                staff.name.Contains(staffPrefab.name))
            {
                StaffController controller =
                    staff.GetComponent<StaffController>();

                if (controller != null)
                {
                    controller.RefillAmmo();

                    Debug.Log(
                        "Ammo refilled for staff: " +
                        staff.name
                    );
                }
            }
        }
    }
}