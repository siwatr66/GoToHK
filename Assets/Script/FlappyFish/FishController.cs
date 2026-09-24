using UnityEngine;
using UnityEngine.InputSystem;

public class FishController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float swimForce = 7f;
    public float tiltSmooth = 5f;
    public float maxUpAngle = 25f;
    public float maxDownAngle = -60f;

    private Rigidbody rb;
    private bool hasStarted = false;
    private bool isDead = false;
    private Quaternion initialRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // ล็อกไม่ให้ฟิสิกส์หมุนโมเดลเอง
        rb.freezeRotation = true;
        initialRotation = transform.rotation;

        // ใน 3D ใช้ isKinematic แทน bodyType
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
    }

    void Update()
    {
        if (isDead) return;

        // รับอินพุต
        bool isPressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                         (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                         (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);

        if (isPressed)
        {
            if (!hasStarted)
            {
                hasStarted = true;
                rb.isKinematic = false; // เริ่มให้แรงโน้มถ่วงทำงาน
            }

            // ดีดตัวขึ้นในแกน 3D (Vector3)
            rb.linearVelocity = Vector3.up * swimForce;
        }

        // เอียงหัวตามความเร็ว
        if (hasStarted)
        {
            float targetAngle = Mathf.Clamp(rb.linearVelocity.y * 6f, maxDownAngle, maxUpAngle);
            Quaternion targetRot = initialRotation * Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, tiltSmooth * Time.deltaTime);
        }
    }

    // ชนของแข็ง 3D (เสา / พื้น) -> ตาย
    private void OnCollisionEnter(Collision collision)
    {
        Die("ชนของแข็ง: " + collision.gameObject.name);
    }

    // ชน Trigger 3D (ช่องคะแนน)
    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        // ตรวจถ้าเป็นช่องคะแนน
        if (other.gameObject.name.Contains("Score") || other.GetComponent<ScoreZone>() != null)
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.AddScore();
            }

            // ปิด Collider ของช่องคะแนนทันทีเพื่อไม่ให้แต้มเบิ้ล
            other.enabled = false;
        }
    }

    void Die(string reason)
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Game Over! " + reason);

        if (GameManager.instance != null)
        {
            GameManager.instance.GameOver();
        }
    }
}