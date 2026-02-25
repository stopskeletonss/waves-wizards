using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    public float hoverScale = 1.2f;
    public float scaleSpeed = 10f;

    private TMP_Text buttonText;
    private Transform selectNotif;

    private Vector3 textNormalScale;
    private Vector3 notifNormalScale;

    private Vector3 targetScale;

    void Awake()
    {
        // Find TMP text
        buttonText = GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
            textNormalScale = buttonText.transform.localScale;

        // Find SelectNotif child
        selectNotif = transform.Find("SelectNotif");
        if (selectNotif != null)
        {
            notifNormalScale = selectNotif.localScale;
            selectNotif.gameObject.SetActive(false);
        }

        targetScale = Vector3.one;
    }

    void Update()
    {
        // Smooth text scaling
        if (buttonText != null)
        {
            buttonText.transform.localScale = Vector3.Lerp(
                buttonText.transform.localScale,
                textNormalScale * targetScale.x,
                Time.deltaTime * scaleSpeed
            );
        }

        // Smooth SelectNotif scaling
        if (selectNotif != null)
        {
            selectNotif.localScale = Vector3.Lerp(
                selectNotif.localScale,
                notifNormalScale * targetScale.x,
                Time.deltaTime * scaleSpeed
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = Vector3.one * hoverScale;

        if (selectNotif != null)
            selectNotif.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one;

        if (selectNotif != null)
            selectNotif.gameObject.SetActive(false);
    }
}