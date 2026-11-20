
using UnityEngine;
using UnityEngine.UI;

public class CustomRadialFill : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;

    [Header("Arc Settings")]
    [SerializeField] private float startAngle = -35f;
    [SerializeField] private float endAngle = 355f;

    private Material mat;

    // shader property IDs
    private static readonly int StartAngleID = hader.PropertyToID("_StartAngle");
    private static readonly int EndAngleID = Shader.PropertyToID("_EndAngle");
    private static readonly int ProgressID = Shader.PropertyToID("_Progress");


    private void Awake()
    {
        // create a separate material instance for this Image
        mat = new Material(fillImage.material);
        fillImage.material = mat;

        mat.SetFloat(StartAngleID, startAngle);
        mat.SetFloat(EndAngleID, endAngle);

        SetProgress(1f); // initially set arc progress to display entire arc
    }


    // updates how much of the arc is visible
    public void SetProgress(float normalizedProgress)
    {
        // clamp between 0 and 1
        mat.SetFloat(ProgressID, Mathf.Clamp01(normalizedProgress));
    }


    private void OnDestroy()
    {
        if (mat != null)
            Destroy(mat);
    }
}
