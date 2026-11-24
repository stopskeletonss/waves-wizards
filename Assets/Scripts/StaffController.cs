using UnityEngine;
using TMPro;
using System.Collections.Generic;

// all possible staff elements
public enum StaffElement
{
    Fire,
    Ice,
    Wind,
    Poison,
    Light,
    Dark
}


public class StaffController : MonoBehaviour
{
    [Header("Firing Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;                // The muzzle / spawn point on the staff
    public float fireForce = 20f;              // Speed applied along aim direction
    public float upwardForce = 0f;             // Optional additional upward component (relative to camera)
    public int ammo = 30;
    private int maxAmmo;

    [Header("Elements")]
    [SerializeField] public List<StaffElement> elements = new List<StaffElement>();

    [Header("UI")]
    public Sprite staffIcon;

    [Header("Follow Lag Settings")]
    public float positionLag = 12f;            // Larger = snappier; used with exponential smoothing
    public float rotationLag = 12f;            // Larger = snappier

    private Transform holdPoint;               // Where the staff should follow (cached original parent)


    void Awake()
    {
        maxAmmo = ammo;
    }


    void Start()
    {
        // Ensure we have a fire point
        if (firePoint == null)
            firePoint = transform.Find("ProjectileOrigin");

        if (firePoint == null)
            Debug.LogWarning($"[{name}] No ProjectileOrigin found. Assign a child named 'StaffFirePoint' or set the ProjectileOrigin in inspector.");

        // Cache parent as holdPoint (WeaponHoldPoint) then unparent so we can smooth-follow it
        if (transform.parent != null)
        {
            holdPoint = transform.parent;
        }
        else
        {
            Debug.LogWarning($"[{name}] Staff has no parent to follow. Make sure the staff prefab is parented to the weapon hold point in the hierarchy before runtime.");
        }
    }


    void LateUpdate()
    {
        if (holdPoint == null) return;

        // Smooth position follow (exponential smoothing)
        float posT = 1f - Mathf.Exp(-positionLag * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, holdPoint.position, posT);

        // Smooth rotation follow (slerp)
        float rotT = 1f - Mathf.Exp(-rotationLag * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, holdPoint.rotation, rotT);
    }



    // AMMO

    public void RefillAmmo()
    {
        ammo = maxAmmo;
    }


    public void SetAmmo(int newAmmo)
    {
        // Ensure ammo can't go below 0 or above max
        ammo = Mathf.Clamp(newAmmo, 0, maxAmmo);
    }


    // Helper getters (optional)
    public int GetAmmo() => ammo;
    public int GetMaxAmmo() => maxAmmo;



    // ELEMENTS

    // add an element if the staff doesn't already have it
    public void AddElement(StaffElement element)
    {
        if (!elements.Contains(element))
        {
            elements.Add(element);
        }
    }


    // remove all elements from the staff
    public void RemoveElements()
    {
        elements.Clear();
    }


    // check whether the staff has a particular element
    public bool HasElement(StaffElement element)
    {
        return elements.Contains(element);
    }
}