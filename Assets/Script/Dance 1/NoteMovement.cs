using UnityEngine;

public class NoteMovement : MonoBehaviour
{
    // ตัวแปรนี้จะไปโผล่ใน Inspector ให้เราปรับความเร็วได้โดยไม่ต้องกลับมาแก้โค้ด
    public float speed = 5f;

    void Update()
    {
        // ใช้ Vector3.up เพื่อบังคับให้พิกัดขยับขึ้นด้านบนสุดของจอเสมอ
        transform.position += Vector3.up * speed * Time.deltaTime;
    }
}