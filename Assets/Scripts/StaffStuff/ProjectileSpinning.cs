
using UnityEngine;

public class ProjectileSpinning : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private bool spinX = false;
    [SerializeField] private bool spinY = true;
    [SerializeField] private bool spinZ = false;

    [Header("Rotation Speeds (Degrees Per Second)")]
    [SerializeField] private float speedX = 180f;
    [SerializeField] private float speedY = 180f;
    [SerializeField] private float speedZ = 180f;

    void Update()
    {
        Vector3 rotation = new Vector3(
            spinX ? speedX : 0f,
            spinY ? speedY : 0f,
            spinZ ? speedZ : 0f
        );

        transform.Rotate(rotation * Time.deltaTime, Space.Self);
    }
}
