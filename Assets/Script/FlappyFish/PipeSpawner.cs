using UnityEngine;

public class PipeSpawner : MonoBehaviour
{
    public GameObject pipePrefab; // ตัวก้อนเสา Prefab
    public float spawnRate = 2f;  // เสกทุกๆ กี่วินาที
    public float heightOffset = 2f; // ระยะสุ่มขึ้น-ลง
    private float timer = 0f;

    void Start()
    {
        SpawnPipe();
    }

    void Update()
    {
        if (timer < spawnRate)
        {
            timer += Time.deltaTime;
        }
        else
        {
            SpawnPipe();
            timer = 0f;
        }
        if (GameManager.instance != null && !GameManager.instance.isGameStarted)
        {
            return;
        }
    }

    void SpawnPipe()
    {
        float lowest = transform.position.y - heightOffset;
        float highest = transform.position.y + heightOffset;
        Instantiate(pipePrefab, new Vector3(transform.position.x, Random.Range(lowest, highest), 0), Quaternion.identity);
    }
}