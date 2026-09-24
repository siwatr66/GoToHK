using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipCenterOfMass : MonoBehaviour
{
    // กำหนดตำแหน่งจุดศูนย์ถ่วงเอง (ยิ่งติดลบแกน Y น้ำหนักจะยิ่งถ่วงลงล่าง)
    public Vector3 centerOfMassOffset = new Vector3(0f, -1.5f, 0f);
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMassOffset;
    }

    void OnDrawGizmosSelected()
    {
        // วาดจุดสีแดงแสดงตำแหน่งจุดศูนย์ถ่วงใน Scene view
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.TransformPoint(centerOfMassOffset), 0.3f);
    }
}