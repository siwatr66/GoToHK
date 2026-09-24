using UnityEngine;

public class PropellerVFXController : MonoBehaviour 
{
    [Header("Target Rigidbody")]
    public Rigidbody submarineRigidbody;

    [Header("VFX Prefab & Spawn Points")]
    [Tooltip("ลาก Prefab ของ Particle System จากช่อง Project มาใส่ตรงนี้")]
    public ParticleSystem bubblePrefab;
    [Tooltip("จุดที่ต้องการให้ฟองน้ำพุ่งออกมา (เช่น ปลายใบพัดซ้าย-ขวา)")]
    public Transform[] bubbleSpawnPoints;

    [Header("Settings")]
    [Tooltip("ความเร็วขั้นต่ำที่เริ่มปล่อยฟอง")]
    public float speedThreshold = 0.05f;

    // อาร์เรย์เก็บ Particle System ที่ถูก Spawn ออกมาในฉาก
    private ParticleSystem[] spawnedBubbleEffects;

    void Start()
    {
        if (submarineRigidbody == null)
            submarineRigidbody = GetComponentInParent<Rigidbody>();

        SpawnBubblesFromPrefab();
    }

    void Update()
    {
        if (submarineRigidbody == null || spawnedBubbleEffects == null) return;

        // เช็คความเร็วรวมทุกแกน
        float currentSpeed = submarineRigidbody.linearVelocity.magnitude;
        bool isMoving = currentSpeed > speedThreshold;

        SetVFXState(isMoving);
    }

    private void SpawnBubblesFromPrefab()
    {
        if (bubblePrefab == null || bubbleSpawnPoints == null) return;

        spawnedBubbleEffects = new ParticleSystem[bubbleSpawnPoints.Length];

        for (int i = 0; i < bubbleSpawnPoints.Length; i++)
        {
            if (bubbleSpawnPoints[i] != null)
            {
                // เสก Prefab เข้าไปเป็นลูกของจุด Spawn Point โดยตรง
                // ตำแหน่งและมุมจะขยับตามจุด Spawn Point อัตโนมัติ ไม่ต้องคอยอัปเดตตำแหน่งเอง
                ParticleSystem ps = Instantiate(bubblePrefab, bubbleSpawnPoints[i].position, bubbleSpawnPoints[i].rotation, bubbleSpawnPoints[i]);
                
                // ปิดการปล่อยฟองไว้ก่อนตอนเริ่มเกม
                var emission = ps.emission;
                emission.enabled = false;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                spawnedBubbleEffects[i] = ps;
            }
        }
    }

    private void SetVFXState(bool play)
    {
        foreach (var ps in spawnedBubbleEffects)
        {
            if (ps == null) continue;

            var emission = ps.emission;
            
            // สลับสถานะ Emission เมื่อค่าเปลี่ยน
            if (emission.enabled != play)
            {
                emission.enabled = play;
            }

            // สั่ง Play / Stop
            if (play)
            {
                if (!ps.isPlaying) ps.Play(true);
            }
            else
            {
                if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}