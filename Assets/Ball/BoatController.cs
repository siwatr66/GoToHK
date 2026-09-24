using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Engine & Speed")]
    public float motorForce = 4500f;
    public float reverseForce = 2000f;
    public float maxSpeed = 12f;

    [Header("Steering (ความลื่นไหลในการเลี้ยว)")]
    [Tooltip("ความเร็วในการหันหัวเรือ (องศา/วินาที)")]
    public float turnSpeed = 80f;
    [Tooltip("ช่วยให้เรือค่อยๆ คืนพวงมาลัย ไม่หักเลี้ยวกระชาก")]
    public float turnSmoothness = 6f;

    [Header("Boat Slide & Drift (ระบบการไถล)")]
    [Tooltip("ค่ายิ่งน้อยเรือยิ่งลื่น/ไถลออกข้างมาก (0 = ลื่นเหมือนน้ำแข็ง, 1 = เกาะผิวน้ำเลี้ยวคมทันที)")]
    [Range(0.01f, 100f)]
    public float lateralGrip = 0.15f;
    [Tooltip("แรงต้านน้ำชะลอตัวขณะปล่อยคันเร่ง")]
    public float waterDrag = 0.5f;

    [Header("Tilt / Waves Adaptation")]
    public float maxTiltAngle = 16f;
    public float uprightForce = 6f;

    private Rigidbody rb;
    private Vector2 inputVector;
    private float currentYaw;
    private float steerVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentYaw = transform.eulerAngles.y;
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
    }

    private void Update()
    {
        ReadInputs();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ApplyBoatDrift();
        ApplySmoothSteeringAndTilt();
    }

    private void ReadInputs()
    {
        if (moveAction != null)
        {
            inputVector = moveAction.action.ReadValue<Vector2>();
            return;
        }

        Vector2 directInput = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) directInput.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) directInput.y -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) directInput.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) directInput.x += 1f;
        }
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.04f) directInput = stick;
        }
        inputVector = directInput;
    }

    private void ApplyMovement()
    {
        float forwardInput = inputVector.y;

        Vector3 forwardDir = transform.forward;
        forwardDir.y = 0f;
        forwardDir.Normalize();

        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (forwardInput > 0 && horizontalVel.magnitude < maxSpeed)
        {
            rb.AddForce(forwardDir * forwardInput * motorForce, ForceMode.Force);
        }
        else if (forwardInput < 0 && horizontalVel.magnitude < maxSpeed * 0.5f)
        {
            rb.AddForce(forwardDir * forwardInput * reverseForce, ForceMode.Force);
        }
    }

    private void ApplyBoatDrift()
    {
        // 1. แยกความเร็วตามแนวตั้งและแนวนอน (ไม่ยุ่งกับแกน Y เพื่อให้ฟิสิกส์คลื่น/ลอยน้ำทำงานตามปกติ)
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        // 2. หาความเร็วเดินหน้า/ถอยหลัง และความเร็วที่ไถลออกด้านข้าง
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, forward);
        float lateralSpeed = Vector3.Dot(rb.linearVelocity, right);

        // 3. ลดแรงไถลออกด้านข้างตามค่า lateralGrip เพื่อจำลองแรงต้านข้างลำเรือ
        float gripFactor = Mathf.Clamp01(lateralGrip * 10f * Time.fixedDeltaTime);
        lateralSpeed = Mathf.Lerp(lateralSpeed, 0f, gripFactor);

        // 4. ชะลอความเร็วตามแรงต้านน้ำเวลาปล่อยคันเร่ง
        if (Mathf.Approximately(inputVector.y, 0f))
        {
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, waterDrag * Time.fixedDeltaTime * 10f);
        }

        // 5. ปรับค่า linearVelocity โดยรักษาค่าแรงตก/ลอยในแนวแกน Y ไว้ตามเดิม
        rb.linearVelocity = (forward * forwardSpeed) + (right * lateralSpeed) + new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    private void ApplySmoothSteeringAndTilt()
    {
        float steerInput = inputVector.x;

        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 1.5f);
        float effectiveTurnSpeed = turnSpeed * (0.35f + 0.65f * speedFactor);

        currentYaw += steerInput * effectiveTurnSpeed * Time.fixedDeltaTime;

        Vector3 currentEuler = transform.eulerAngles;
        float pitch = NormalizeAngle(currentEuler.x);
        float roll = NormalizeAngle(currentEuler.z);

        float clampedPitch = Mathf.Clamp(pitch, -maxTiltAngle, maxTiltAngle);
        float clampedRoll = Mathf.Clamp(roll, -maxTiltAngle, maxTiltAngle);

        float smoothPitch = Mathf.Lerp(pitch, clampedPitch, Time.fixedDeltaTime * uprightForce);
        float smoothRoll = Mathf.Lerp(roll, clampedRoll, Time.fixedDeltaTime * uprightForce);

        rb.MoveRotation(Quaternion.Euler(smoothPitch, currentYaw, smoothRoll));
        rb.angularVelocity = Vector3.zero;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}