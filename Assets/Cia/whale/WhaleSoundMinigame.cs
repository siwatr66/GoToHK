using System.Collections;
using UnityEngine;

public class WhaleSoundMinigame : MonoBehaviour
{
    [Header("Game Settings")]
    public float timeLimit = 60f; // เวลาทั้งหมดของอีเวนต์
    private float timer;
    private bool isGameEnded = false;

    [Header("Win Condition")]
    public int targetScoreToWin = 5; // ต้องชนจุดเรืองแสงกี่ตัวถึงจะชนะ
    private int currentScore = 0;

    [Header("Whale Audio Settings")]
    public AudioSource whaleAudioSource;
    public AudioClip whaleCallClip;
    public float minWhaleInterval = 6f;
    public float maxWhaleInterval = 10f;

    [Header("Target & Movement (WASD)")]
    public Transform playerSubmarine;
    public Transform currentTargetBeacon;
    public float moveSpeed = 6f;
    public float rotationSpeed = 120f;
    public float reachDistance = 1.5f;

    [Header("Random Area Bounds")]
    public float minX = -8f;
    public float maxX = 8f;
    public float minZ = -5f;
    public float maxZ = 5f;

    void Start()
    {
        timer = timeLimit;
        currentScore = 0;
        MoveBeaconToRandomPosition();
        StartCoroutine(WhaleSoundRoutine());
    }

    void Update()
    {
        if (isGameEnded) return;

        // นับถอยหลังเวลา
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            EndGame(false); // หมดเวลา = แพ้ (พลังงานเรือลดฮวบ / ภารกิจล้มเหลว)
            return;
        }

        HandleWASDMovement();
        CheckReachTarget();
    }

    void HandleWASDMovement()
    {
        if (playerSubmarine == null) return;

        float moveDir = 0f;
        float rotateDir = 0f;

        // W = เดินหน้า, S = ถอยหลัง
        if (Input.GetKey(KeyCode.W)) moveDir = 1f;
        else if (Input.GetKey(KeyCode.S)) moveDir = -1f;

        // A = เลี้ยวซ้าย, D = เลี้ยวขวา
        if (Input.GetKey(KeyCode.A)) rotateDir = -1f;
        else if (Input.GetKey(KeyCode.D)) rotateDir = 1f;

        // สั่งหมุนและเคลื่อนที่
        playerSubmarine.Rotate(Vector3.up * rotateDir * rotationSpeed * Time.deltaTime);
        playerSubmarine.Translate(Vector3.forward * moveDir * moveSpeed * Time.deltaTime);
    }

    void CheckReachTarget()
    {
        if (playerSubmarine != null && currentTargetBeacon != null)
        {
            float distance = Vector3.Distance(playerSubmarine.position, currentTargetBeacon.position);
            if (distance <= reachDistance)
            {
                currentScore++;
                Debug.Log($"🎯 เก็บจุดเรืองแสงสำเร็จ! ({currentScore}/{targetScoreToWin})");

                if (currentScore >= targetScoreToWin)
                {
                    EndGame(true); // เก็บครบ = ชนะ!
                }
                else
                {
                    MoveBeaconToRandomPosition(); // สุ่มจุดใหม่ให้ไปเก็บต่อ
                }
            }
        }
    }

    void MoveBeaconToRandomPosition()
    {
        if (currentTargetBeacon != null)
        {
            float randomX = Random.Range(minX, maxX);
            float randomZ = Random.Range(minZ, maxZ);
            currentTargetBeacon.position = new Vector3(randomX, currentTargetBeacon.position.y, randomZ);
            Debug.Log("✨ จุดเรืองแสงย้ายตำแหน่งใหม่แล้ว!");
        }
    }

    IEnumerator WhaleSoundRoutine()
    {
        while (!isGameEnded)
        {
            float waitTime = Random.Range(minWhaleInterval, maxWhaleInterval);
            yield return new WaitForSeconds(waitTime);

            if (isGameEnded) break;

            PlayWhaleSoundEffect();
        }
    }

    void PlayWhaleSoundEffect()
    {
        if (whaleAudioSource != null && whaleCallClip != null)
        {
            whaleAudioSource.PlayOneShot(whaleCallClip);
            Debug.Log("🐳 [เสียงวาฬร้อง!] เสียงนำทางดังขึ้นในความมืด...");
        }
        else
        {
            Debug.Log("🐳 [เสียงวาฬร้อง!]");
        }
    }

    void EndGame(bool success)
    {
        isGameEnded = true;
        if (success)
        {
            Debug.Log("🎉 ชนะภารกิจ! ช่วยเหลือฝูงวาฬและนำทางผ่านพ้นเขตน่านน้ำอันตรายได้สำเร็จ!");
            // TODO: ใส่โค้ดโหลดกลับซีนหลัก หรือให้โบนัสคะแนน
        }
        else
        {
            Debug.Log("💥 ภารกิจล้มเหลว (หมดเวลา)! พลังงานเรือดำน้ำหลักลดลงอย่างรวดเร็วเนื่องจากหลงทางในเขตมลพิษ!");
            // TODO: ใส่โค้ดหักพลังงานเรือในแมพหลัก หรือโหลดกลับซีนหลัก
        }
    }
}