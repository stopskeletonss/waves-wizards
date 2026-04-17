using UnityEngine;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Camera Anchors")]
    public Transform playAnchor;
    public Transform tutorialAnchor;
    public Transform creditsAnchor;
    public Transform settingsAnchor;
    public Transform quitAnchor;
    public Transform mainAnchor;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotateSpeed = 3f;
    public float arrivalThreshold = 0.05f;

    [Header("Position Sway")]
    public float positionAmount = 0f;
    public float positionSpeed = 0f;

    [Header("Rotation Sway")]
    public float rotationAmount = 0.2f;
    public float rotationSpeed = 2.5f;

    [Header("Main Menu Parent")]
    public RectTransform menuParent;
    public Transform onScreenPoint;
    public Transform offScreenPoint;
    public float menuMoveSpeed = 4f;
    public float introDelay = 2f;

    [Header("Submenu Pages")]
    public GameObject settingsPage;
    public GameObject creditsPage;
    public GameObject quitPage;

    [Header("Menu Sounds")]
    public AudioSource audioSource;
    public AudioClip submenuOpenSound;
    public AudioClip backToMainSound;

    [Header("Book Animation")]
    public Animator bookAnimator;

    private Transform currentTarget;
    private bool inSubMenu;

    private Vector3 basePosition;
    private Quaternion baseRotation;

    private Vector2 menuTargetPos;
    private Canvas parentCanvas;

    public Material defaultMaterial;
    public Material joiningMaterial;
    public GameObject changingPage;
    public GameObject lobbyCanvas;
    public GameObject mainbookCanvas;
    public int pagesDeep;

    void Start()
    {
        if (mainAnchor != null)
        {
            basePosition = mainAnchor.position;
            baseRotation = mainAnchor.rotation;
            transform.position = basePosition;
            transform.rotation = baseRotation;
        }

        if (menuParent != null)
        {
            parentCanvas = menuParent.GetComponentInParent<Canvas>();
            SetMenuPosition(offScreenPoint);
        }

        DisableAllSubmenus();
        StartCoroutine(IntroRoutine());
    }

    IEnumerator IntroRoutine()
    {
        yield return new WaitForSeconds(introDelay);
        MoveMenuOnScreen();
    }

    void Update()
    {
        HandleMovement();
        ApplySway();
        HandleMenuMovement();
        HandleEscape();
        Debug.Log(pagesDeep);
    }

    void HandleMovement()
    {
        if (!currentTarget) return;

        basePosition = Vector3.Lerp(
            basePosition,
            currentTarget.position,
            Time.deltaTime * moveSpeed
        );

        baseRotation = Quaternion.Slerp(
            baseRotation,
            currentTarget.rotation,
            Time.deltaTime * rotateSpeed
        );

        if (Vector3.Distance(basePosition, currentTarget.position) < arrivalThreshold)
        {
            currentTarget = null;
        }
    }

    void MoveCamera(Transform target)
    {
        if (target == null) return;
        currentTarget = target;
    }

    void ApplySway()
    {
        Vector3 swayPos = Vector3.zero;
        Quaternion swayRot = Quaternion.identity;

        if (positionAmount > 0f)
        {
            float posX = Mathf.Sin(Time.time * positionSpeed) * positionAmount;
            float posY = Mathf.Cos(Time.time * positionSpeed * 0.8f) * positionAmount;
            swayPos = new Vector3(posX, posY, 0f);
        }

        if (rotationAmount > 0f)
        {
            float rotX = Mathf.Sin(Time.time * rotationSpeed) * rotationAmount;
            float rotY = Mathf.Cos(Time.time * rotationSpeed * 0.7f) * rotationAmount;
            swayRot = Quaternion.Euler(rotX, rotY, 0f);
        }

        transform.position = basePosition + swayPos;
        transform.rotation = baseRotation * swayRot;
    }

    void HandleMenuMovement()
    {
        if (!menuParent) return;

        menuParent.anchoredPosition = Vector2.Lerp(
            menuParent.anchoredPosition,
            menuTargetPos,
            Time.deltaTime * menuMoveSpeed
        );
    }

    void MoveMenuOnScreen()
    {
        SetMenuTarget(onScreenPoint);
    }

    void MoveMenuOffScreen()
    {
        SetMenuTarget(offScreenPoint);
    }

    void SetMenuTarget(Transform worldPoint)
    {
        if (!worldPoint || !menuParent || !parentCanvas) return;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            Camera.main,
            worldPoint.position
        );

        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
            out menuTargetPos
        );
    }

    void SetMenuPosition(Transform worldPoint)
    {
        SetMenuTarget(worldPoint);
        menuParent.anchoredPosition = menuTargetPos;
    }

    // =========================
    // Button Functions
    // =========================

    public void PlayGame()
    {
        EnterSubMenu(playAnchor, null);
        mainbookCanvas.SetActive(true);
        //if (bookAnimator != null)
        //{
        //    bookAnimator.ResetTrigger("Closing");
        //    bookAnimator.Play("Opening", 0, 0f);
        //}
    }

    public void Tutorial()
    {
        EnterSubMenu(tutorialAnchor, null);

        if (bookAnimator != null)
        {
            bookAnimator.ResetTrigger("Closing");
            bookAnimator.Play("Opening", 0, 0f);
        }
    }

    public void Credits() => EnterSubMenu(creditsAnchor, creditsPage);
    public void Settings() => EnterSubMenu(settingsAnchor, settingsPage);
    public void QuitGame() => EnterSubMenu(quitAnchor, quitPage);

    public void BackToMain()
    {
        inSubMenu = false;
        MoveCamera(mainAnchor);
        MoveMenuOnScreen();
        DisableAllSubmenus();

        if (bookAnimator != null)
        {
            bookAnimator.ResetTrigger("Opening");
            bookAnimator.SetTrigger("Closing");
        }

        if (audioSource != null && backToMainSound != null)
            audioSource.PlayOneShot(backToMainSound);
    }

    public void HostGame()
    {
        if (bookAnimator != null)
        {
            bookAnimator.ResetTrigger("Closing");
            bookAnimator.Play("Opening", 0, 0f);
        }
        changingPage.GetComponent<Renderer>().material = defaultMaterial;
        pagesDeep = 1;
    }

    public void JoinGame()
    {
        if (bookAnimator != null)
        {
            bookAnimator.ResetTrigger("Closing");
            bookAnimator.Play("Opening", 0, 0f);
        }
        changingPage.GetComponent<Renderer>().material = joiningMaterial;
        pagesDeep = 1;
    }

    public void LocalGame()
    {
        if (bookAnimator != null)
        {
            bookAnimator.ResetTrigger("Closing");
            bookAnimator.Play("Opening", 0, 0f);
        }
        changingPage.GetComponent<Renderer>().material = defaultMaterial;
        pagesDeep = 1;
    }

    public void ReturnToMainBook()
    {
        if (bookAnimator != null)
        {
            bookAnimator.ResetTrigger("Opening");
            bookAnimator.SetTrigger("Closing");
        }
    }

    public void MapSelected()
    {
        lobbyCanvas.SetActive(true);
        pagesDeep = 2;
    }

    public void LobbyFound()
    {
        lobbyCanvas.SetActive(true);
        pagesDeep = 2;

    }

    void EnterSubMenu(Transform anchor, GameObject page)
    {
        inSubMenu = true;
        MoveCamera(anchor);
        MoveMenuOffScreen();

        if (page != null)
            page.SetActive(true);

        if (audioSource != null && submenuOpenSound != null)
            audioSource.PlayOneShot(submenuOpenSound);
    }

    void HandleEscape()
    {
        if (inSubMenu && Input.GetKeyDown(KeyCode.Escape))
        {
            if (pagesDeep == 0)
            {
                BackToMain();
                mainbookCanvas.SetActive(false);
            }
            else if (pagesDeep == 1)
            {
                ReturnToMainBook();
                pagesDeep = 0;
            }
            else if (pagesDeep == 2)
            {
                lobbyCanvas.SetActive(false);
                pagesDeep = 1;
            }
            else
            {
                Debug.Log("Where even are you bro");
            }
        }
    }

    void DisableAllSubmenus()
    {
        if (settingsPage) settingsPage.SetActive(false);
        if (creditsPage) creditsPage.SetActive(false);
        if (quitPage) quitPage.SetActive(false);
    }
}