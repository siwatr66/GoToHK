using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[Serializable]
public class SubmarineMode
{
    public string modeName = "Mode Name";
    public Key hotkey = Key.Digit1;
    [Tooltip("สิ่งที่ต้องการเปิดเมื่ออยู่ในโหมดนี้ (GameObject)")]
    public GameObject targetObject;
    public Sprite modeIcon;
}

public class SubmarineModeManager : MonoBehaviour
{
    public static SubmarineModeManager Instance { get; private set; }

    [Header("--- Mode Configurations ---")]
    public List<SubmarineMode> modes = new List<SubmarineMode>();
    public int currentModeIndex = 0;

    [Header("--- Submarine Hardware ---")]
    public DenseSonarScanner sonarScanner;
    public List<GameObject> spotLights = new List<GameObject>();

    [Header("--- Action Key Bindings ---")]
    [Tooltip("ปุ่มที่ใช้ยิงสแกน Sonar (เช่น Spacebar)")]
    public Key sonarTriggerKey = Key.Space;
    [Tooltip("ปุ่มเปิดไฟ (เช่น F)")]
    public Key lightTriggerKey = Key.F;

    [Header("--- Warning Notification UI ---")]
    public CanvasGroup warningCanvasGroup;
    public Text warningText;
    public float warningDuration = 2.2f;

    [Header("--- 3D Hologram Setup ---")]
    public Transform submarineTransform;
    public CanvasGroup selectorCanvasGroup;
    public Transform cardsContainer; 
    public GameObject modeCardPrefab; 
    public Vector3 offsetFromSub = new Vector3(-3.8f, 1.2f, 1.5f);

    [Header("--- Display & Animation Settings ---")]
    public float showDuration = 2.0f;
    public float fadeSpeed = 4.0f;

    [Header("--- Visual Style: Card Colors ---")]
    public Color cardActiveColor = new Color(0f, 1f, 0.9f, 0.95f);
    public Color cardInactiveColor = new Color(0.12f, 0.18f, 0.24f, 0.6f);

    [Header("--- Visual Style: Icon BG Colors ---")]
    public Color iconBgActiveColor = new Color(0f, 0.8f, 0.7f, 1f);
    public Color iconBgInactiveColor = new Color(0.05f, 0.08f, 0.12f, 0.85f);

    // โครงสร้างช่วยเก็บ References ภายใน Card แต่ละใบให้เป็นระเบียบ
    private class ModeCardUI
    {
        public GameObject root;
        public Image cardBg;
        public Image iconBg;
        public Image icon;
        public Text label;
    }

    private List<ModeCardUI> spawnedCards = new List<ModeCardUI>();
    private Coroutine displayRoutine;
    private Coroutine warningRoutine;
    private Camera activeCam;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitComponents();
        BuildModeCards();
        ApplyCurrentMode(false);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 1. ตรวจจับการกดเปลี่ยนโหมด
        HandleModeSwitchInput(keyboard);

        // 2. ดักจับการกดใช้คำสั่งผิดโหมด
        CheckWrongModeInputs(keyboard);
    }

    void LateUpdate()
    {
        if (selectorCanvasGroup == null || selectorCanvasGroup.alpha <= 0.01f) return;

        // ล็อกตำแหน่งกราบซ้ายของลำเรือ
        if (submarineTransform != null)
        {
            transform.position = submarineTransform.TransformPoint(offsetFromSub);
        }

        // หมุนเข้าหากล้องระนาบตรง
        UpdateActiveCam();
        if (activeCam != null)
        {
            transform.rotation = activeCam.transform.rotation;
        }
    }

    // ==========================================
    // INITIALIZATION & SETUP
    // ==========================================

    private void InitComponents()
    {
        if (selectorCanvasGroup == null)
        {
            selectorCanvasGroup = GetComponent<CanvasGroup>();
            if (selectorCanvasGroup == null)
                selectorCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (selectorCanvasGroup != null) selectorCanvasGroup.alpha = 0f;
        if (warningCanvasGroup != null) warningCanvasGroup.alpha = 0f;

        if (sonarScanner == null && submarineTransform != null)
        {
            sonarScanner = submarineTransform.GetComponentInChildren<DenseSonarScanner>();
        }
    }

   private void BuildModeCards()
    {
        if (cardsContainer == null || modeCardPrefab == null) return;

        foreach (var cardUI in spawnedCards)
        {
            if (cardUI != null && cardUI.root != null) Destroy(cardUI.root);
        }
        spawnedCards.Clear();

        for (int i = 0; i < modes.Count; i++)
        {
            GameObject cardObj = Instantiate(modeCardPrefab, cardsContainer);
            cardObj.SetActive(true);

            ModeCardUI cardUI = new ModeCardUI
            {
                root = cardObj,
                cardBg = cardObj.GetComponent<Image>(),
                label = cardObj.GetComponentInChildren<Text>()
            };

            // ค้นหา Icon_BG
            Transform iconBgTransform = cardObj.transform.Find("Icon_BG");
            if (iconBgTransform != null)
            {
                cardUI.iconBg = iconBgTransform.GetComponent<Image>();
            }

            // ค้นหา Image ที่ชื่อ "Icon" ไม่ว่าจะอยู่ตรงไหนใน Card
            Image[] allImages = cardObj.GetComponentsInChildren<Image>(true);
            foreach (var img in allImages)
            {
                if (img.gameObject.name == "Icon")
                {
                    cardUI.icon = img;
                    break;
                }
            }

            // ตั้งค่าชื่อโหมด
            if (cardUI.label != null)
            {
                string keyLabel = (i + 1).ToString();
                cardUI.label.text = $"[{keyLabel}] {modes[i].modeName}";
            }

            // เปลี่ยนรูป Sprite ไอคอนตามโหมด i โดยตรง
            if (cardUI.icon != null)
            {
                if (modes[i].modeIcon != null)
                {
                    cardUI.icon.gameObject.SetActive(true);
                    cardUI.icon.sprite = modes[i].modeIcon; // นำ Sprite ของโหมดนั้นๆ มาใส่
                }
                else
                {
                    cardUI.icon.gameObject.SetActive(false);
                }
            }

            spawnedCards.Add(cardUI);
        }
    }

    // ==========================================
    // LOGIC & MODE SWITCHING
    // ==========================================

    private void HandleModeSwitchInput(Keyboard keyboard)
    {
        for (int i = 0; i < modes.Count; i++)
        {
            if (modes[i].hotkey != Key.None && keyboard[modes[i].hotkey].wasPressedThisFrame)
            {
                if (currentModeIndex != i)
                {
                    SwitchMode(i);
                }
                else
                {
                    ShowSelectorHUD();
                }
                break;
            }
        }
    }

    public void SwitchMode(int newIndex)
    {
        currentModeIndex = newIndex;
        ApplyCurrentMode(true);
    }

    private void ApplyCurrentMode(bool triggerHUD)
    {
        bool isSonarMode = (currentModeIndex == 0);
        bool isLightMode = (currentModeIndex == 1);

        // 1. จัดการฮาร์ดแวร์หลัก
        if (sonarScanner != null)
        {
            sonarScanner.enabled = isSonarMode;
        }

        foreach (var lightObj in spotLights)
        {
            if (lightObj != null)
            {
                lightObj.SetActive(isLightMode);
            }
        }

        // 2. จัดการ Object เสริม & Visuals ของการ์ดแต่ละใบ
        for (int i = 0; i < modes.Count; i++)
        {
            bool isActive = (i == currentModeIndex);

            if (modes[i].targetObject != null)
            {
                modes[i].targetObject.SetActive(isActive);
            }

            if (i < spawnedCards.Count && spawnedCards[i] != null)
            {
                UpdateCardVisuals(spawnedCards[i], isActive);
            }
        }

        if (triggerHUD)
        {
            ShowSelectorHUD();
        }
    }

    private void UpdateCardVisuals(ModeCardUI cardUI, bool isActive)
    {
        // เปลี่ยนสีพื้นหลังการ์ด
        if (cardUI.cardBg != null)
        {
            cardUI.cardBg.color = isActive ? cardActiveColor : cardInactiveColor;
        }

        // เปลี่ยนสีพื้นหลังไอคอน (Icon BG)
        if (cardUI.iconBg != null)
        {
            cardUI.iconBg.color = isActive ? iconBgActiveColor : iconBgInactiveColor;
        }

        // สเกลขยายขนาดกล่องที่เลือก
        cardUI.root.transform.localScale = isActive ? Vector3.one * 1.15f : Vector3.one;
    }

    // ==========================================
    // WARNING & MIS-INPUT LOGIC
    // ==========================================

    private void CheckWrongModeInputs(Keyboard keyboard)
    {
        // ยิงโซนาร์ขณะอยู่โหมดอื่น
        if (sonarTriggerKey != Key.None && keyboard[sonarTriggerKey].wasPressedThisFrame)
        {
            if (currentModeIndex != 0)
            {
                ShowWarning("ระบบโซนาร์ถูกปิดอยู่! กรุณากด [1] เพื่อเปิดโหมดโซนาร์");
            }
        }

        // เปิดไฟขณะอยู่โหมดโซนาร์
        if (lightTriggerKey != Key.None && keyboard[lightTriggerKey].wasPressedThisFrame)
        {
            if (currentModeIndex == 0)
            {
                ShowWarning("ไม่สามารถเปิดไฟขณะใช้โซนาร์ได้! กรุณากด [2] เพื่อเปิดไฟสปอตไลท์");
            }
        }
    }

    public void ShowWarning(string message)
    {
        if (warningText != null)
        {
            warningText.text = message;
        }

        ShowSelectorHUD();

        if (warningRoutine != null) StopCoroutine(warningRoutine);
        warningRoutine = StartCoroutine(WarningFadeRoutine());
    }

    // ==========================================
    // CAM & ANIMATION COROUTINES
    // ==========================================

    private void UpdateActiveCam()
    {
        if (activeCam != null && activeCam.gameObject.activeInHierarchy && activeCam.enabled) return;

        Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera c in cams)
        {
            if (c.gameObject.activeInHierarchy && c.enabled && c.gameObject.name.Contains("Submarine"))
            {
                activeCam = c;
                return;
            }
        }

        if (activeCam == null)
            activeCam = Camera.main;
    }

    private void ShowSelectorHUD()
    {
        if (displayRoutine != null) StopCoroutine(displayRoutine);
        displayRoutine = StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine()
    {
        if (selectorCanvasGroup == null) yield break;

        while (selectorCanvasGroup.alpha < 1f)
        {
            selectorCanvasGroup.alpha = Mathf.MoveTowards(selectorCanvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
            yield return null;
        }

        yield return new WaitForSeconds(showDuration);

        while (selectorCanvasGroup.alpha > 0f)
        {
            selectorCanvasGroup.alpha = Mathf.MoveTowards(selectorCanvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
            yield return null;
        }
    }

    private IEnumerator WarningFadeRoutine()
    {
        if (warningCanvasGroup == null) yield break;

        while (warningCanvasGroup.alpha < 1f)
        {
            warningCanvasGroup.alpha = Mathf.MoveTowards(warningCanvasGroup.alpha, 1f, Time.deltaTime * 6f);
            yield return null;
        }

        yield return new WaitForSeconds(warningDuration);

        while (warningCanvasGroup.alpha > 0f)
        {
            warningCanvasGroup.alpha = Mathf.MoveTowards(warningCanvasGroup.alpha, 0f, Time.deltaTime * 3f);
            yield return null;
        }
    }
}