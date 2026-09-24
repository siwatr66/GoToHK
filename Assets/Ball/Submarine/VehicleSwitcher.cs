using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleSwitcher : MonoBehaviour
{
    [Header("Boat Settings")]
    public Camera boatCamera;                   // กล้องเรือผิวน้ำ (Main Camera)
    public TopDownCameraFollow boatCamFollow;   // สคริปต์ตามกล้องเรือผิวน้ำ
    public MonoBehaviour boatController;        // สคริปต์ขับเรือผิวน้ำ

    [Header("Submarine Cameras")]
    public Camera subThirdPersonCamera;         // กล้องนอกเรือดำน้ำ (Free Cam TPV)
    public Camera subFirstPersonCamera;          // กล้องในห้องคนขับ (FPV)
    public Key subCamToggleKey = Key.V;          // ปุ่มสลับ FPV <-> TPV (กด V)

    [Header("Hologram Map Reference")]
    public SonarHologramMap sonarHologramMap;

    [Header("Submarine UI")]
    [Tooltip("ลากสคริปต์ SubmarineDepthHUD หรือ Object ที่แปะสคริปต์นี้มาใส่")]
    public SubmarineDepthHUD depthHUD;

    [Header("Submarine TPV Free Cam Settings")]
    public float distance = 7.0f;               // ระยะห่างกล้อง TPV
    public float height = 2.0f;                 // ความสูงจุดหมุนเหนือเรือ
    public float mouseSensitivity = 2.5f;       // ความไวเมาส์
    public float smoothSpeed = 10f;
    public float minPitch = -30f;
    public float maxPitch = 70f;
    public float minDistance = 3f;
    public float maxDistance = 15f;
    public float zoomSpeed = 2f;

    [Header("Submarine Vehicle")]
    public SubmarineController submarineController; // สคริปต์ขับเรือดำน้ำ
    public GameObject submarineRoot;            // Submarine_Root
    public Transform deployPoint;               // จุดปล่อยเรือดำน้ำใต้ท้องเรือ

    [Header("HDRP Underwater Lighting & Fog")]
    [Tooltip("ลาก GameObject Underwater_Volume จาก Hierarchy มาใส่ที่นี่")]
    public GameObject underwaterVolumeObj;

    [Header("Fade UI")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.4f;

    [Header("Controls")]
    public Key toggleVehicleKey = Key.G;        // ปุ่มสลับเรือหลัก <-> เรือดำน้ำ (กด G)

    private bool isControllingSubmarine = false;
    private bool isTransitioning = false;
    private bool isSubFirstPerson = false;
    private float currentYaw = 0f;
    private float currentPitch = 15f;

    void Start()
    {
        // ผูกกล้องเข้ากับ SubmarineController อัตโนมัติหากยังไม่ได้ใส่
        if (submarineController != null)
        {
            if (submarineController.subThirdPersonCamera == null)
                submarineController.subThirdPersonCamera = subThirdPersonCamera;

            if (submarineController.subFirstPersonCamera == null)
                submarineController.subFirstPersonCamera = subFirstPersonCamera;
        }

        isControllingSubmarine = false;
        ApplyControlState(false);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null) return;

        // 1. กด G เพื่อสลับยานพาหนะ
        if (keyboard[toggleVehicleKey].wasPressedThisFrame && !isTransitioning)
        {
            StartCoroutine(SwitchVehicleRoutine());
        }

        // 2. ถ้ากำลังคุมเรือดำน้ำอยู่
        if (isControllingSubmarine)
        {
            if (keyboard[subCamToggleKey].wasPressedThisFrame)
            {
                isSubFirstPerson = !isSubFirstPerson;
                UpdateSubmarineCameras();
            }

            if (!isSubFirstPerson && mouse != null && subThirdPersonCamera != null)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue() * (mouseSensitivity * 0.1f);
                currentYaw += mouseDelta.x;
                currentPitch -= mouseDelta.y;
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    distance -= (scroll * 0.01f) * zoomSpeed;
                    distance = Mathf.Clamp(distance, minDistance, maxDistance);
                }
            }
        }
    }

    void LateUpdate()
    {
        if (isControllingSubmarine && !isSubFirstPerson && subThirdPersonCamera != null && submarineRoot != null)
        {
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 targetPivot = submarineRoot.transform.position + Vector3.up * height;
            Vector3 desiredPosition = targetPivot - (rotation * Vector3.forward * distance);

            subThirdPersonCamera.transform.position = Vector3.Lerp(subThirdPersonCamera.transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            subThirdPersonCamera.transform.LookAt(targetPivot);
        }
    }

    private IEnumerator SwitchVehicleRoutine()
    {
        isTransitioning = true;

        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(1f));
        }

        isControllingSubmarine = !isControllingSubmarine;

        if (isControllingSubmarine)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (deployPoint != null && submarineRoot != null)
            {
                submarineRoot.transform.position = deployPoint.position;
                submarineRoot.transform.rotation = deployPoint.rotation;

                Rigidbody subRb = submarineRoot.GetComponent<Rigidbody>();
                if (subRb != null)
                {
                    subRb.linearVelocity = Vector3.zero;
                    subRb.angularVelocity = Vector3.zero;
                }

                currentYaw = submarineRoot.transform.eulerAngles.y;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // คืนค่า FOV ของกล้องเรือดำน้ำกลับเป็นค่าปกติ
            if (submarineController != null)
            {
                if (subThirdPersonCamera != null) subThirdPersonCamera.fieldOfView = submarineController.normalFov;
                if (subFirstPersonCamera != null) subFirstPersonCamera.fieldOfView = submarineController.normalFov;
            }
        }

        ApplyControlState(isControllingSubmarine);

        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(0f));
        }

        isTransitioning = false;
    }

    private void ApplyControlState(bool controllingSub)
    {
        // สั่งเปิด-ปิด UI วัดระดับความลึก
        if (depthHUD != null)
        {
            depthHUD.SetSubmarineMode(controllingSub);
        }

        // ควบคุมการเปิด-ปิด Volume ใต้น้ำสำหรับ HDRP (Fog, Exposure, Color Adjustments)
        if (underwaterVolumeObj != null)
        {
            underwaterVolumeObj.SetActive(controllingSub);
        }

        // เรือผิวน้ำ
        if (boatCamera != null)
        {
            boatCamera.gameObject.SetActive(!controllingSub);
            AudioListener bl = boatCamera.GetComponent<AudioListener>();
            if (bl != null) bl.enabled = !controllingSub;
        }
        if (boatCamFollow != null) boatCamFollow.enabled = !controllingSub;
        if (boatController != null) boatController.enabled = !controllingSub;

        // เรือดำน้ำ
        if (submarineRoot != null) submarineRoot.SetActive(controllingSub);
        if (submarineController != null) submarineController.enabled = controllingSub;

        if (controllingSub)
        {
            UpdateSubmarineCameras();
        }
        else
        {
            SetCamActive(subThirdPersonCamera, false);
            SetCamActive(subFirstPersonCamera, false);
        }
        
    }

  private void UpdateSubmarineCameras()
    {
        SetCamActive(subFirstPersonCamera, isSubFirstPerson);
        SetCamActive(subThirdPersonCamera, !isSubFirstPerson);

        // อัปเดตกล้องให้กับ Canvas UI แบบอัตโนมัติ
        if (depthHUD != null)
        {
            Camera activeCam = isSubFirstPerson ? subFirstPersonCamera : subThirdPersonCamera;
            depthHUD.UpdateRenderCamera(activeCam);
        }
        // สลับตำแหน่งและขนาดของ Hologram Map ตามมุมกล้องทันที
        if (sonarHologramMap != null)
        {
            sonarHologramMap.SetViewMode(isSubFirstPerson);
        }
    }

    private void SetCamActive(Camera cam, bool state)
    {
        if (cam != null)
        {
            cam.gameObject.SetActive(state);
            AudioListener listener = cam.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = state;
        }
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}