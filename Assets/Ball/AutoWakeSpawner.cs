using UnityEngine;
using UnityEngine.InputSystem;

public class AutoWakeSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("ลาก Prefab เอฟเฟกต์ฟองน้ำจากหน้าต่าง Project มาใส่ตรงนี้")]
    public GameObject wakePrefab;

    [Header("Spawn Position")]
    [Tooltip("จุดที่อยากให้ปล่อยฟอง (ถ้าไม่ใส่ จะปล่อยตรงท้ายเรือให้อัตโนมัติ)")]
    public Transform spawnPoint;

    [Header("Settings")]
    [Tooltip("ระยะห่างระหว่างการปล่อยแต่ละก้อน (เมตร)")]
    public float distanceBetweenSpawns = 0.8f;

    [Tooltip("ระยะเวลาให้ฟองน้ำคงอยู่ก่อนทำลายทิ้ง (วินาที)")]
    public float lifeTime = 2f;

    [Tooltip("สเกลขนาดของฟองน้ำ (ปรับตามขนาดเรือได้เลย)")]
    public float effectScale = 1f;

    private Vector3 lastSpawnPosition;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastSpawnPosition = GetCurrentSpawnPos();
    }

    void Update()
    {
        if (wakePrefab == null) return;

        // เช็กว่าเรือกำลังเคลื่อนที่ หรือมีการกดปุ่มขับเรือ
        bool isMoving = false;
        if (rb != null)
        {
            isMoving = rb.linearVelocity.magnitude > 0.1f;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.sKey.isPressed)
            {
                isMoving = true;
            }
        }

        if (!isMoving) return;

        Vector3 currentPos = GetCurrentSpawnPos();

        // เมื่อเรือแล่นไปข้างหน้าจนได้ระยะห่างที่กำหนด ให้ปล่อยเอฟเฟกต์ 1 ครั้ง
        if (Vector3.Distance(currentPos, lastSpawnPosition) >= distanceBetweenSpawns)
        {
            SpawnWake(currentPos);
            lastSpawnPosition = currentPos;
        }
    }

    private Vector3 GetCurrentSpawnPos()
    {
        if (spawnPoint != null) return spawnPoint.position;
        // ถ้าไม่มีการกำหนดจุด ให้ใช้ท้ายเรือระดับผิวน้ำ
        return transform.position - transform.forward * 2f;
    }

    private void SpawnWake(Vector3 position)
    {
        // สร้างเอฟเฟกต์ทิ้งไว้บนโลก ไม่เป็นลูกของเรือ (จึงไม่วิ่งตามเรือ)
        GameObject wake = Instantiate(wakePrefab, position, Quaternion.identity);
        wake.transform.localScale = Vector3.one * effectScale;

        // ทำลายทิ้งอัตโนมัติหลังจากหมดเวลา
        Destroy(wake, lifeTime);
    }
}