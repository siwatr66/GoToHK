using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("ใส่ Prefab โน้ตสั้น 4 ทิศ (0=ซ้าย, 1=ขึ้น, 2=ลง, 3=ขวา)")]
    public GameObject[] notePrefabs = new GameObject[4];

    [Header("ใส่จุดเกิด Spawn 4 เลน")]
    public Transform[] spawnPoints = new Transform[4];

    [Header("ตั้งค่าความเร็วการเสก")]
    public float spawnInterval = 1f;
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnRandomNote();
            timer = 0f;
        }
    }

    void SpawnRandomNote()
    {
        if (notePrefabs.Length == 0 || spawnPoints.Length == 0) return;

        // สุ่มเลนที่จะเกิด (0 ถึง 3)
        int lane = Random.Range(0, spawnPoints.Length);

        // ดึง Prefab โน้ตตามเลนที่สุ่มได้ เพื่อให้ทิศทางตรงกับเลนเสมอ
        GameObject prefabToSpawn = notePrefabs[lane];

        // เสกตัวโน้ตออกมา
        GameObject spawnedNote = Instantiate(prefabToSpawn, spawnPoints[lane].position, Quaternion.identity);

        // ระบบหมุนลูกศรให้หันตรงกับเลนด้านบน
        if (lane == 0) spawnedNote.transform.rotation = Quaternion.Euler(0, 0, 0);
        else if (lane == 1) spawnedNote.transform.rotation = Quaternion.Euler(0, 0, 90);
        else if (lane == 2) spawnedNote.transform.rotation = Quaternion.Euler(0, 0, -90);
        else if (lane == 3) spawnedNote.transform.rotation = Quaternion.Euler(0, 0, 180);
    }

    // ฟังก์ชันนี้จะโดนเรียกจาก DanceGameManager และรับตัวเลขเลเวลมาด้วย
    public void IncreaseDifficulty(int level)
    {
        if (level == 2)
        {
            // ปรับให้เวลาเกิดโน้ตสั้นลง 30%
            spawnInterval = spawnInterval * 0.7f;
            Debug.Log("เข้าสู่เลเวล 2! โน้ตเกิดถี่ขึ้นเป็นทุกๆ " + spawnInterval + " วินาที");
        }
        else if (level == 3)
        {
            // ปรับให้เวลาเกิดโน้ตสั้นลงไปอีก 30% จากความเร็วของเลเวล 2
            spawnInterval = spawnInterval * 0.7f;
            Debug.Log("เข้าสู่เลเวล 3! โน้ตเกิดเร็วสุดๆ เป็นทุกๆ " + spawnInterval + " วินาที");
        }
    }
}