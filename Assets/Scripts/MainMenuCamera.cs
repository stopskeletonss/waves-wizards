using UnityEngine;

public class MainMenuCamera : MonoBehaviour
{
    [Header("Position Sway")]
    public float positionAmount = 0.2f;
    public float positionSpeed = 0.3f;

    [Header("Rotation Sway")]
    public float rotationAmount = 2f;
    public float rotationSpeed = 0.2f;

    private Vector3 startPos;
    private Quaternion startRot;

    void Start()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;
    }

    void Update()
    {
        float posX = Mathf.Sin(Time.time * positionSpeed) * positionAmount;
        float posY = Mathf.Cos(Time.time * positionSpeed * 0.8f) * positionAmount;

        transform.localPosition = startPos + new Vector3(posX, posY, 0f);

        float rotX = Mathf.Sin(Time.time * rotationSpeed) * rotationAmount;
        float rotY = Mathf.Cos(Time.time * rotationSpeed * 0.7f) * rotationAmount;

        transform.localRotation = startRot * Quaternion.Euler(rotX, rotY, 0f);
    }
}