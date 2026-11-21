
using UnityEngine;
using UnityEngine.UI;

public class CustomRadialFill : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;

    [Header("Arc Settings")]
    [SerializeField] private float startAngle = -35f;
    [SerializeField] private float endAngle = 355f;

    [Header("Segment Settings")]
    [SerializeField] private float segmentGap = 2f; // width of the gaps between ammo segments, in degrees
    [SerializeField] private Color segmentColor = Color.black;

    private Material mat;

    // shader property IDs
    private static readonly int StartAngleID = Shader.PropertyToID("_StartAngle");
    private static readonly int EndAngleID = Shader.PropertyToID("_EndAngle");
    private static readonly int ProgressID = Shader.PropertyToID("_Progress");
    private static readonly int SegmentCountID = Shader.PropertyToID("_SegmentCount");
    private static readonly int SegmentGapID = Shader.PropertyToID("_SegmentGap");
    private static readonly int SegmentColorID = Shader.PropertyToID("_SegmentColor");


    private void Awake()
    {
        // create a separate material instance for this Image
        mat = new Material(fillImage.material);
        fillImage.material = mat;

        mat.SetFloat(StartAngleID, startAngle);
        mat.SetFloat(EndAngleID, endAngle);
        mat.SetFloat(SegmentGapID, segmentGap);
        mat.SetColor(SegmentColorID, segmentColor);

        SetProgress(1f); // initially set arc progress to display entire arc
    }


    // updates how much of the arc is visible
    public void SetProgress(float normalizedProgress)
    {
        // clamp between 0 and 1
        mat.SetFloat(ProgressID, Mathf.Clamp01(normalizedProgress));
    }


    // sets the number of segments based on max ammo
    public void SetSegmentCount(int maxAmmo)
    {
        mat.SetFloat(SegmentCountID, Mathf.Max(1, maxAmmo));
    }


    private void OnDestroy()
    {
        if (mat != null)
            Destroy(mat);
    }
}
