
using UnityEngine;
using System.Collections;

public class StaffScale : MonoBehaviour
{
    [Header("Scaling Settings")]
    [SerializeField] private float startScale = 0.05f;
    [SerializeField] private float scaleDuration = 0.25f;

    private Vector3 originalScale;

    void Start()
    {
        // Store the projectile's intended size
        originalScale = transform.localScale;

        // Start very small
        transform.localScale = originalScale * startScale;

        // Begin scaling up
        StartCoroutine(ScaleUp());
    }

    IEnumerator ScaleUp()
    {
        float elapsedTime = 0f;

        Vector3 initialScale = transform.localScale;

        while (elapsedTime < scaleDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / scaleDuration);

            // Smoothly interpolate between starting and original size
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.localScale = Vector3.Lerp(
                initialScale,
                originalScale,
                smoothProgress
            );

            yield return null;
        }

        // Ensure the projectile reaches its exact original size
        transform.localScale = originalScale;
    }
}