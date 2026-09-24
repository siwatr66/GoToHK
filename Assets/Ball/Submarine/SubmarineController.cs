using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SubmarineController : MonoBehaviour
{
    [Header("Thrust Forces (กำลังขับเคลื่อน)")]
    [Tooltip("แรงขับเคลื่อนเดินหน้า-ถอยหลังปกติ")]
    public float forwardThrust = 2800f;
    [Tooltip("แรงดันลอยตัวขึ้นสู่ผิวน้ำ")]
    public float ascendThrust = 3200f;
    [Tooltip("แรงกดดำดิ่งลงใต้น้ำ")]
    public float descendThrust = 2600f;

    [Header("Boost Settings (กด Shift เร่งความเร็ว)")]
    [Tooltip("แรงขับเคลื่อนเดินหน้าสูงสุดขณะกด Shift")]
    public float boostedForwardThrust = 5500f;
    [Tooltip("อัตราเร่งของการไต่ระดับแรงขับ")]
    public float boostAccelerationRate = 3.0f;

    [Header("Cameras Dynamic FOV")]
    [Tooltip("กล้องบุคคลที่ 3 (TPV)")]
    public Camera subThirdPersonCamera;
    [Tooltip("กล้องห้องคนขับ (FPV)")]
    public Camera subFirstPersonCamera;
    public float normalFov = 60f;
    public float boostFov = 75f;
    public float fovTransitionSpeed = 4.0f;

    [Header("Steering & Handling (การบังคับเลี้ยว)")]
    [Tooltip("แรงหมุนเลี้ยวหัวเรือซ้าย-ขวา")]
    public float turnTorque = 1200f;

    [Header("Dynamic Leaning (การเอียงตัวตามฟิสิกส์น้ำ)")]
    [Tooltip("องศาก้ม-เงยหัวเรือเวลาลอยตัวขึ้นหรือดำลง")]
    public float maxPitchAngle = 18f;
    [Tooltip("องศาการเอียงข้างเวลาเลี้ยวโค้ง")]
    public float maxRollAngle = 12f;
    [Tooltip("ความนุ่มนวลในการโคลงตัว")]
    public float tiltSmoothSpeed = 2.5f;

    private Rigidbody rb;
    private float forwardInput;  // W / S
    private float turnInput;     // A / D
    private float verticalInput; // Space / Left Ctrl
    private bool isBoosting;     // Left / Right Shift

    private float currentForwardThrust;
    private float currentPitch = 0f;
    private float currentRoll = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentForwardThrust = forwardThrust;

        // กำหนดค่าเริ่มต้นให้กับกล้อง
        if (subThirdPersonCamera != null) subThirdPersonCamera.fieldOfView = normalFov;
        if (subFirstPersonCamera != null) subFirstPersonCamera.fieldOfView = normalFov;
    }

    void Start()
    {
        // ตั้งค่าฟิสิกส์ให้มีแรงหน่วงใต้น้ำ
        rb.useGravity = false;
        rb.linearDamping = 1.8f;      // แรงต้านน้ำแนวนอน
        rb.angularDamping = 3.0f;     // แรงต้านการหมุน
        rb.constraints = RigidbodyConstraints.None;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 1. เดินหน้า/ถอยหลัง (W/S)
        forwardInput = 0f;
        if (keyboard.wKey.isPressed) forwardInput += 1f;
        if (keyboard.sKey.isPressed) forwardInput -= 1f;

        // 2. เลี้ยวซ้าย/ขวา (A/D)
        turnInput = 0f;
        if (keyboard.dKey.isPressed) turnInput += 1f;
        if (keyboard.aKey.isPressed) turnInput -= 1f;

        // 3. ลอยตัวขึ้น (Spacebar) / ดำดิ่งลง (Left Ctrl)
        verticalInput = 0f;
        if (keyboard.spaceKey.isPressed) verticalInput += 1f;
        if (keyboard.leftCtrlKey.isPressed) verticalInput -= 1f;

        // 4. บูสต์ความเร็ว (กด Shift)
        isBoosting = (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed) && forwardInput > 0f;

        UpdateCamerasFov();
    }

    void FixedUpdate()
    {
        // คำนวณการไต่ระดับแรงขับเคลื่อนให้ค่อยๆ พุ่งอย่างต่อเนื่อง
        float targetThrust = isBoosting ? boostedForwardThrust : forwardThrust;
        currentForwardThrust = Mathf.Lerp(currentForwardThrust, targetThrust, Time.fixedDeltaTime * boostAccelerationRate);

        // --- 1. แรงขับเคลื่อนตามทิศทางหัวเรือ ---
        Vector3 forwardForce = transform.forward * (forwardInput * currentForwardThrust);
        rb.AddForce(forwardForce, ForceMode.Force);

        // --- 2. แรงยกขึ้นและดำลงแนวดิ่ง ---
        float verticalForceMagnitude = verticalInput > 0 ? ascendThrust : descendThrust;
        Vector3 verticalForce = Vector3.up * (verticalInput * verticalForceMagnitude);
        rb.AddForce(verticalForce, ForceMode.Force);

        // --- 3. แรงบิดเลี้ยวหัวเรือ (Yaw) ---
        Vector3 turnTorqueVec = Vector3.up * (turnInput * turnTorque);
        rb.AddTorque(turnTorqueVec, ForceMode.Force);

        // --- 4. ฟิสิกส์การโคลงตัวเรือ (Pitch & Roll) ---
        float targetPitch = -verticalInput * maxPitchAngle;
        float targetRoll = -turnInput * maxRollAngle;

        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.fixedDeltaTime * tiltSmoothSpeed);
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.fixedDeltaTime * tiltSmoothSpeed);

        float currentYaw = transform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, currentRoll);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * tiltSmoothSpeed));
    }

    private void UpdateCamerasFov()
    {
        // คำนวณอัตราส่วนความเร็วเรือปัจจุบัน เพื่อนำไปขยายมุมมองเลนส์
        float speedRatio = Mathf.Clamp01(rb.linearVelocity.magnitude / 12f);
        float targetFov = isBoosting ? Mathf.Lerp(normalFov, boostFov, speedRatio) : normalFov;

        if (subThirdPersonCamera != null && subThirdPersonCamera.gameObject.activeInHierarchy)
        {
            subThirdPersonCamera.fieldOfView = Mathf.Lerp(subThirdPersonCamera.fieldOfView, targetFov, Time.deltaTime * fovTransitionSpeed);
        }

        if (subFirstPersonCamera != null && subFirstPersonCamera.gameObject.activeInHierarchy)
        {
            subFirstPersonCamera.fieldOfView = Mathf.Lerp(subFirstPersonCamera.fieldOfView, targetFov, Time.deltaTime * fovTransitionSpeed);
        }
    }
}