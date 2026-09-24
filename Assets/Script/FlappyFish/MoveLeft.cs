using UnityEngine;

public class MoveLeft : MonoBehaviour
{
    public float speed = 3f;
    public float leftLimit = -15f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // 1. ถ้ายังไม่เริ่มเกม ให้หยุดอยู่กับที่ ไม่ต้องขยับ
        if (GameManager.instance != null && !GameManager.instance.isGameStarted)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero; // Unity 6 (เวอร์ชันเก่าใช้ rb.velocity = Vector3.zero)
            }
            return;
        }

        // 2. ถ้าเริ่มเกมแล้ว ให้เสาเคลื่อนที่ไปทางซ้าย
        if (rb != null)
        {
            rb.linearVelocity = Vector3.left * speed;
        }
        else
        {
            transform.position += Vector3.left * speed * Time.deltaTime;
        }

        // ลบเสาทิ้งเมื่อวิ่งเลยขอบจอซ้าย
        if (transform.position.x < leftLimit)
        {
            Destroy(gameObject);
        }
    }
}