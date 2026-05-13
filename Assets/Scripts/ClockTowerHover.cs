using UnityEngine;

public class ClockTowerHover : MonoBehaviour
{
    public float HoverHeight = 0.5f;   // How high it moves up/down
    public float BobSpeed = 2f;      // Speed of the bobbing
    public bool RandomOffset = true;   // Offset so multiple objects don't sync

    private Vector3 startPos;
    private float offset;

    void Start()
    {
        startPos = transform.position;

        if (RandomOffset)
        {
            offset = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    void Update()
    {
        float newY = Mathf.Sin(Time.time * BobSpeed + offset) * HoverHeight;
        transform.position = startPos + new Vector3(0, newY, 0);
    }
}