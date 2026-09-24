using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // ใช้สำหรับคุม Depth of Field ของ URP

public class SonarInfoPanel3D : MonoBehaviour
{
    [Header("UI Component Bindings (Standard Text)")]
    public CanvasGroup canvasGroup;
    public Text titleText;
    public Text subtitleText;
    public Text descriptionText;
    public Image previewImage;

    [Header("Display Settings")]
    [Tooltip("ติ๊กออกหากต้องการให้อยู่ค้างตลอดจนกว่าจะกดปุ่มปิดเอง")]
    public bool autoCloseByTimer = false;
    public float displayDuration = 8.0f;
    public float fadeSpeed = 3.0f;

    [Header("Position Offset (ล็อกข้างตัวเรือ)")]
    public Transform submarineTransform;
    [Tooltip("X ติดลบ = กราบซ้าย, Y = ความสูง, Z = เยื้องไปข้างหน้า")]
    public Vector3 offsetFromSubmarine = new Vector3(-4.0f, 1.5f, 2.0f);
    public float followSmoothSpeed = 12f;
    public float panelScale = 0.006f;

    [Header("Blur Control (เฉพาะความเบลอเท่านั้น)")]
    [Tooltip("ลาก Volume Profile ที่มี Depth of Field มาใส่ (เช่น Underwater_Volume หรือ Global Volume)")]
    public Volume targetVolume;

    [Header("Close Key Settings")]
    public Key closeKeyPrimary = Key.E;
    public Key closeKeySecondary = Key.Escape;

    private Camera activeCamera;
    private Coroutine activeRoutine;
    private bool isVisible = false;

    // เจาะจงเฉพาะคอมโพเนนต์ความเบลอ
    private DepthOfField depthOfField;
    private bool originalDofActiveState = false;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        isVisible = false;
        transform.localScale = Vector3.one * panelScale;

        // ดึงเฉพาะ Depth of Field ออกมาจาก Volume
        InitDepthOfField();
    }

    private void InitDepthOfField()
    {
        if (targetVolume != null && targetVolume.profile != null)
        {
            if (targetVolume.profile.TryGet(out depthOfField))
            {
                originalDofActiveState = depthOfField.active;
            }
        }
    }

    public void Init(Transform sub)
    {
        submarineTransform = sub;
        SnapToSubmarine();
    }

    private void SnapToSubmarine()
    {
        if (submarineTransform != null)
        {
            transform.position = submarineTransform.TransformPoint(offsetFromSubmarine);
        }
    }

    void Update()
    {
        if (!isVisible) return;

        bool closeTriggered = false;

        if (Keyboard.current != null)
        {
            if (closeKeyPrimary != Key.None && Keyboard.current[closeKeyPrimary].wasPressedThisFrame)
            {
                closeTriggered = true;
            }
            else if (closeKeySecondary != Key.None && Keyboard.current[closeKeySecondary].wasPressedThisFrame)
            {
                closeTriggered = true;
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            {
                closeTriggered = true;
            }
        }

        if (closeTriggered)
        {
            ClosePanel();
        }
    }

   void LateUpdate()
{
    if (canvasGroup == null || canvasGroup.alpha <= 0.01f) return;

    // 1. ล็อกตำแหน่งแนบข้างเรือทันที ไม่ใช้ Lerp เพื่อป้องกันการกระตุกไล่ตามเฟรม
    if (submarineTransform != null)
    {
        transform.position = submarineTransform.TransformPoint(offsetFromSubmarine);
    }

    // 2. หมุนหันหน้าเข้าหากล้องระนาบตรงหลังจากตำแหน่งนิ่งแล้ว
    UpdateActiveCamera();
    if (activeCamera != null)
    {
        transform.rotation = activeCamera.transform.rotation;
    }
}

    private void UpdateActiveCamera()
    {
        if (activeCamera != null && activeCamera.gameObject.activeInHierarchy && activeCamera.enabled)
            return;

        Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera c in cams)
        {
            if (c.gameObject.activeInHierarchy && c.enabled && c.gameObject.name.Contains("Submarine"))
            {
                activeCamera = c;
                return;
            }
        }

        if (activeCamera == null)
            activeCamera = Camera.main;
    }

    public void DisplayData(ScanData data)
    {
        if (titleText != null) titleText.text = data.title;
        if (subtitleText != null) subtitleText.text = data.subtitle;
        if (descriptionText != null) descriptionText.text = data.description;

        if (previewImage != null)
        {
            if (data.displayImage != null)
            {
                previewImage.gameObject.SetActive(true);
                previewImage.sprite = data.displayImage;
            }
            else
            {
                previewImage.gameObject.SetActive(false);
            }
        }

        SnapToSubmarine();
        isVisible = true;

        // ปิดเฉพาะความเบลอ (Depth of Field) ส่วนหมอกและสีน้ำยังคงเดิม
        SetOnlyBlurActive(false);

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(ShowRoutine());
    }

    public void ClosePanel()
    {
        if (!isVisible) return;

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(FadeOutRoutine());
    }

    // ฟังก์ชันสั่งปิด-เปิดเฉพาะ Depth of Field
    private void SetOnlyBlurActive(bool state)
    {
        if (depthOfField == null)
        {
            InitDepthOfField();
        }

        if (depthOfField != null)
        {
            depthOfField.active = state;
        }
    }

    private IEnumerator ShowRoutine()
    {
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
            yield return null;
        }

        if (autoCloseByTimer)
        {
            yield return new WaitForSeconds(displayDuration);
            yield return StartCoroutine(FadeOutRoutine());
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
            yield return null;
        }

        // คืนค่าความเบลอกลับมาเหมือนเดิม
        SetOnlyBlurActive(originalDofActiveState);
        isVisible = false;
    }
}