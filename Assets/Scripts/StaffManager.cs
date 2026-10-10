using UnityEngine;
using System;
using Unity.Netcode;

public class StaffManager : NetworkBehaviour
{
    [Header("Staff Settings")]
    public GameObject starterStaffPrefab;
    public Transform weaponHoldPoint;
    public GameObject[] staffSlots = new GameObject[2];
    private int currentSlot = 0;

    // notifies StaffFireInput when the equipped staff changes
    public event Action<GameObject> onStaffEquipped;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;


        // equip the starter (base) staff in slot 0 if there is not staff already in slot 0
        if (staffSlots[0] == null && starterStaffPrefab != null)
        {
            GameObject starter = Instantiate(starterStaffPrefab, weaponHoldPoint);
            starter.transform.localPosition = Vector3.zero;
            starter.transform.localRotation = Quaternion.identity;

            staffSlots[0] = starter;
        }
        EquipStaff(0);
    }


    void Update()
    {
        if (!IsSpawned || !IsOwner)
            return;

        HandleInput();
    }


    void HandleInput()
    {
        // reload currently equipped staff with 'R'
        if (Input.GetKeyDown(KeyCode.R))
        {
            StaffController staff = GetCurrentStaff();
            if (staff != null)
            {
                staff.RefillAmmo();
                Debug.Log("Reloaded staff: " + staff.gameObject.name);
            }
        }

        // switch between staffs using '1' and '2'
        if (Input.GetKeyDown(KeyCode.Alpha1))
            EquipStaff(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            EquipStaff(1);

        // or switch using the scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
            ScrollStaff(-1);
        else if (scroll < 0f)
            ScrollStaff(1);
    }


    // handles scrolling to switch staffs
    void ScrollStaff(int direction)
    {
        int newSlot = currentSlot + direction;
        if (newSlot < 0) newSlot = staffSlots.Length - 1; // wrap around when you go past the start or end of the staff inventory
        if (newSlot >= staffSlots.Length) newSlot = 0;

        EquipStaff(newSlot);
    }


    // handles switching between staffs
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

        // fire event to notify listeners about the newly equipped staff
        // so it can update its firing point
        onStaffEquipped?.Invoke(staffSlots[slotIndex]);
    }


    // getter to get the currently equipped staff
    public StaffController GetCurrentStaff()
    {
        if (staffSlots[currentSlot] == null) return null;
        return staffSlots[currentSlot].GetComponent<StaffController>();
    }


    // getter to count the inventory slots that contain a staff
    public int GetStaffCount()
    {
        int count = 0;
        foreach (var staff in staffSlots)
        {
            if (staff != null) count++;
        }
        return count;
    }


    // handles adding a staff to the player's inventory
    public void AddStaffToSlot(int slotIndex, GameObject newStaffPrefab)
    {
        if (slotIndex < 0 || slotIndex >= staffSlots.Length) return;

        if (staffSlots[slotIndex] != null)
        {
            Destroy(staffSlots[slotIndex]); // remove the old staff
        }

        // create the new staff as a child of the hold point
        GameObject newStaff = Instantiate(newStaffPrefab, weaponHoldPoint);
        newStaff.transform.localPosition = Vector3.zero;
        newStaff.transform.localRotation = Quaternion.identity;
        newStaff.SetActive(false);
        staffSlots[slotIndex] = newStaff;
    }


    // handles replacing the currently equipped staff
    public void ReplaceActiveStaff(GameObject newStaffPrefab)
    {
        AddStaffToSlot(currentSlot, newStaffPrefab);
        EquipStaff(currentSlot);
    }

    
    // handles refilling ammo
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