using UnityEngine;
using System.Collections;

public class MenuMusicFade : MonoBehaviour
{
    [Header("Fade Settings")]
    public float startDelay = 0f;
    public float fadeTime = 2f;
    public float targetVolume = 1f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0f;

        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        // Wait before starting the music
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        audioSource.Play(); // start song from the beginning

        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, timer / fadeTime);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}