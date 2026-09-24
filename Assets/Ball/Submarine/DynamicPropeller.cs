using UnityEngine;

public class DynamicPropeller : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก GameObject ตัวแม่ที่มี Rigidbody (Submarine_Root) มาใส่ที่นี่")]
    public Rigidbody submarineRigidbody;

    [Header("Rotation Settings")]
    [Tooltip("แกนหมุนของใบพัด (เช่น Vector3.forward, right หรือ up)")]
    public Vector3 rotationAxis = Vector3.forward;

    [Tooltip("ตัวคูณความเร็วรอบ ยิ่งเยอะใบพัดยิ่งหมุนเร็ว")]
    public float speedMultiplier = 150f;

    void Start()
    {
        // หากลืมลากใส่ในช่อง Inspector ระบบจะค้นหา Rigidbody จากตัวแม่ให้อัตโนมัติ
        if (submarineRigidbody == null)
        {
            submarineRigidbody = GetComponentInParent<Rigidbody>();
        }
    }

    void Update()
    {
        if (submarineRigidbody == null) return;

        // คำนวณความเร็วการเคลื่อนที่ไปข้างหน้า/ถอยหลังตามทิศของตัวเรือ
        float forwardSpeed = Vector3.Dot(submarineRigidbody.linearVelocity, submarineRigidbody.transform.forward);

        // หากความเร็วต่ำมาก (เรือจอดนิ่ง) จะไม่หมุน
        if (Mathf.Abs(forwardSpeed) > 0.01f)
        {
            float rotationAmount = forwardSpeed * speedMultiplier * Time.deltaTime;
            transform.Rotate(rotationAxis * rotationAmount, Space.Self);
        }
    }
}