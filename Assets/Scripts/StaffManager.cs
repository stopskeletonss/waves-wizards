using UnityEngine;
using System;

public class StaffManager : MonoBehaviour
{
    [Header("Staff Settings")]
    public GameObject starterStaffPrefab;
    public Transform weaponHoldPoint; // Assign this in inspector
    public GameObject[] staffSlots = new GameObject[2];
    private int currentSlot = 0;

    // Event to notify when a staff is equipped
    public event Action<GameObject> onStaffEquipped;

    void Start()
    {
        // Equip the starter staff in slot 0
        GameObject starter = Instantiate(starterStaffPrefab, weaponHoldPoint);
        starter.transform.localPosition = Vector3.zero;
        starter.transform.localRotation = Quaternion.identity;

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
        if (newSlot < 0) newSlot = staffSlots.Length - 1;
        if (newSlot >= staffSlots.Length) newSlot = 0;

        EquipStaff(newSlot);
    }

    void EquipStaff(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= staffSlots.Length) return;
        if (staffSlots[slotIndex] == null) return;

        for (int i = 0; i < staffSlots.Length; i++)
        {
            if (staffSlots[i] != null)
                staffSlots[i].SetActive(i == slotIndex);
        }

        currentSlot = slotIndex;

        // Fire event to notify listeners about the newly equipped staff
        onStaffEquipped?.Invoke(staffSlots[slotIndex]);
    }

    public StaffController GetCurrentStaff()
    {
        if (staffSlots[currentSlot] == null) return null;
        return staffSlots[currentSlot].GetComponent<StaffController>();
    }

    public int GetStaffCount()
    {
        int count = 0;
        foreach (var staff in staffSlots)
        {
            if (staff != null) count++;
        }
        return count;
    }

    public void AddStaffToSlot(int slotIndex, GameObject newStaffPrefab)
    {
        if (slotIndex < 0 || slotIndex >= staffSlots.Length) return;

        if (staffSlots[slotIndex] != null)
        {
            Destroy(staffSlots[slotIndex]);
        }

        GameObject newStaff = Instantiate(newStaffPrefab, weaponHoldPoint);
        newStaff.transform.localPosition = Vector3.zero;
        newStaff.transform.localRotation = Quaternion.identity;
        newStaff.SetActive(false);
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
            if (staff != null && staff.name.Contains(staffPrefab.name))
            {
                StaffController controller = staff.GetComponent<StaffController>();
                if (controller != null)
                {
                    controller.RefillAmmo();
                    Debug.Log("Ammo refilled for staff: " + staff.name);
                }
            }
        }
    }
}