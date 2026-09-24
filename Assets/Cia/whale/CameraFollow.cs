using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // ลาก PlayerCube มาใส่
    public Vector3 offset = new Vector3(0f, 5f, -7f); // ระยะห่างและความสูงของกล้อง
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target != null)
        {
            // คำนวณตำแหน่งเป้าหมายของกล้อง
            Vector3 desiredPosition = target.position + offset;
            // เคลื่อนที่กล้องตามแบบนุ่มนวล (Smooth)
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;

            // ให้กล้องหันหน้ามองตามเรือเสมอ
            transform.LookAt(target.position);
        }
    }
}