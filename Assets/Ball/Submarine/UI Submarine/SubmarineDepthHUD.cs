using UnityEngine;
using UnityEngine.UI;
using TMPro; // ใช้ TextMeshPro เพื่อความคมชัดสูง

public class SubmarineDepthHUD : MonoBehaviour
{
    [Header("UI Canvas / Object")]
    [SerializeField] private GameObject submarineCanvas; 

    [Header("UI Text (TextMeshPro คมชัด ไม่แตก)")]
    [Tooltip("ลาก TextMeshProUGUI มาใส่")]
    [SerializeField] private TextMeshProUGUI depthTextTMP;

    [Header("Curve Core (เส้นหลักแกนกลาง - คมชัด)")]
    [SerializeField] private Image curveCore;
    [SerializeField] private Color coreBaseColor = new Color(0.6f, 0.9f, 1f, 1f); // สีฟ้าอมขาวสว่าง
    [SerializeField] private float coreMinIntensity = 0.2f;
    [SerializeField] private float coreMaxIntensity = 2.0f;

    [Header("Curve Halo (เส้นออร่าฟุ้งด้านหลัง)")]
    [SerializeField] private Image curveHalo;
    [SerializeField] private Color haloBaseColor = new Color(0f, 0.5f, 1f, 0.4f); // สีฟ้าเข้ม โปร่งแสง
    [SerializeField] private float haloMinIntensity = 0.1f;
    [SerializeField] private float haloMaxIntensity = 1.5f;

    [Header("References")]
    [SerializeField] private Transform submarineTransform;

    [Header("Settings")]
    [SerializeField] private float seaLevelY = 0f;

    [Header("Dynamic Speed-Glow Settings")]
    [SerializeField] private float minSpeedThreshold = 0.2f;
    [SerializeField] private float maxSpeedThreshold = 8.0f;
    [SerializeField] private float glowTransitionSpeed = 4f;

    private bool isDrivingSubmarine = false;
    private Vector3 previousPosition;
    private Rigidbody submarineRigidbody;

    private Canvas targetCanvas;
    private Material matCore;
    private Material matHalo;
    private static readonly int ColorPropID = Shader.PropertyToID("_Color");

    private float currentCoreIntensity;
    private float currentHaloIntensity;

    void Awake()
    {
        SetSubmarineMode(false);

        if (submarineCanvas != null)
        {
            targetCanvas = submarineCanvas.GetComponent<Canvas>();
        }

        if (curveCore != null && curveCore.material != null)
        {
            matCore = new Material(curveCore.material);
            curveCore.material = matCore;
        }

        if (curveHalo != null && curveHalo.material != null)
        {
            matHalo = new Material(curveHalo.material);
            curveHalo.material = matHalo;
        }

        currentCoreIntensity = coreMinIntensity;
        currentHaloIntensity = haloMinIntensity;
    }

    public void UpdateRenderCamera(Camera activeCam)
    {
        if (targetCanvas == null && submarineCanvas != null)
        {
            targetCanvas = submarineCanvas.GetComponent<Canvas>();
        }

        if (targetCanvas != null && activeCam != null)
        {
            targetCanvas.worldCamera = activeCam;
        }
    }

    void Start()
    {
        if (submarineTransform != null)
        {
            previousPosition = submarineTransform.position;
            submarineRigidbody = submarineTransform.GetComponent<Rigidbody>();
        }

        ApplyHDRColor(matCore, curveCore, coreBaseColor, coreMinIntensity);
        ApplyHDRColor(matHalo, curveHalo, haloBaseColor, haloMinIntensity);
    }

    void Update()
    {
        if (!isDrivingSubmarine || submarineTransform == null) return;

        // 1. อัปเดตความลึก
        float currentDepth = seaLevelY - submarineTransform.position.y;
        currentDepth = Mathf.Max(0f, currentDepth);

        if (depthTextTMP != null)
        {
            depthTextTMP.text = $"{Mathf.RoundToInt(currentDepth)}m";
        }

        // 2. คำนวณความเร็ว
        float currentSpeed = 0f;
        if (submarineRigidbody != null && !submarineRigidbody.isKinematic)
        {
            #if UNITY_6000_0_OR_NEWER
            currentSpeed = submarineRigidbody.linearVelocity.magnitude;
            #else
            currentSpeed = submarineRigidbody.velocity.magnitude;
            #endif
        }
        else
        {
            currentSpeed = Vector3.Distance(submarineTransform.position, previousPosition) / Time.deltaTime;
            previousPosition = submarineTransform.position;
        }

        // 3. ปรับ Intensity
        float speedRatio = Mathf.InverseLerp(minSpeedThreshold, maxSpeedThreshold, currentSpeed);

        float targetCoreInt = Mathf.Lerp(coreMinIntensity, coreMaxIntensity, speedRatio);
        float targetHaloInt = Mathf.Lerp(haloMinIntensity, haloMaxIntensity, speedRatio);

        currentCoreIntensity = Mathf.Lerp(currentCoreIntensity, targetCoreInt, Time.deltaTime * glowTransitionSpeed);
        currentHaloIntensity = Mathf.Lerp(currentHaloIntensity, targetHaloInt, Time.deltaTime * glowTransitionSpeed);

        ApplyHDRColor(matCore, curveCore, coreBaseColor, currentCoreIntensity);
        ApplyHDRColor(matHalo, curveHalo, haloBaseColor, currentHaloIntensity);
    }

    private void ApplyHDRColor(Material mat, Image img, Color baseColor, float intensity)
    {
        float factor = Mathf.Pow(2f, intensity);
        Color hdrColor = new Color(baseColor.r * factor, baseColor.g * factor, baseColor.b * factor, baseColor.a);

        if (mat != null && mat.HasProperty(ColorPropID))
        {
            mat.SetColor(ColorPropID, hdrColor);
        }

        if (img != null)
        {
            img.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a);
        }
    }

    public void SetSubmarineMode(bool isActive)
    {
        isDrivingSubmarine = isActive;

        if (submarineCanvas != null)
        {
            submarineCanvas.SetActive(isActive);
        }

        if (isActive && submarineTransform != null)
        {
            previousPosition = submarineTransform.position;
            if (submarineRigidbody == null)
            {
                submarineRigidbody = submarineTransform.GetComponent<Rigidbody>();
            }
        }
    }

    void OnDestroy()
    {
        if (matCore != null) Destroy(matCore);
        if (matHalo != null) Destroy(matHalo);
    }
}