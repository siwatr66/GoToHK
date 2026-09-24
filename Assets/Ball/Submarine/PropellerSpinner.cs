using UnityEngine;

public class PropellerSpinner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลากตัวแม่ที่มี Rigidbody (Submarine_Root) มาใส่ หรือเว้นว่างไว้ให้หาอัตโนมัติ")]
    public Rigidbody submarineRigidbody;

    [Header("Rotation Settings")]
    [Tooltip("แกนหมุนของใบพัด (แกน X คือ Vector3.right)")]
    public Vector3 rotationAxis = Vector3.right;

    [Tooltip("ตัวคูณความเร็วรอบ ยิ่งเยอะยิ่งหมุนเร็วตามความเร็วเรือ")]
    public float speedMultiplier = 150f;

    [Header("Custom Behavior")]
    [Tooltip("ติ๊กถูกเพื่อหมุนกลับทิศทาง (สำหรับใบพัดอีกข้าง)")]
    public bool reverseDirection = false;

    [Tooltip("มุมเริ่มต้นเพื่อไม่ให้จังหวะใบพัด 2 ข้างซ้ำกันเป๊ะๆ")]
    [Range(0f, 360f)]
    public float initialAngleOffset = 45f;

    [Tooltip("อัตราความเร็วผันแปรเล็กน้อยเพื่อความเลื่อมจังหวะ")]
    [Range(0.8f, 1.2f)]
    public float speedVariation = 1.0f;

    void Start()
    {
        // ค้นหา Rigidbody จากตัวแม่อัตโนมัติหากไม่ได้ลากใส่
        if (submarineRigidbody == null)
        {
            submarineRigidbody = GetComponentInParent<Rigidbody>();
        }

        // ตั้งมุมเริ่มต้นให้เหลื่อมกัน
        transform.Rotate(rotationAxis * initialAngleOffset, Space.Self);
    }

    void Update()
    {
        if (submarineRigidbody == null) return;

        // คำนวณความเร็วเดินหน้า/ถอยหลังของเรือ (Unity 6 ใช้ linearVelocity)
        float forwardSpeed = Vector3.Dot(submarineRigidbody.linearVelocity, submarineRigidbody.transform.forward);

        // หากเรือจอดนิ่ง (ความเร็วเข้าใกล้ 0) ใบพัดจะหยุดหมุน
        if (Mathf.Abs(forwardSpeed) > 0.05f)
        {
            float direction = reverseDirection ? -1f : 1f;
            float finalSpeed = forwardSpeed * speedMultiplier * speedVariation * direction;

            transform.Rotate(rotationAxis * (finalSpeed * Time.deltaTime), Space.Self);
        }
    }
}