using UnityEngine;

public class TopDownCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("ลาก BoatRoot มาใส่")]
    public Transform target;

    [Header("Position Offset")]
    [Tooltip("ระยะห่างจากเรือ: X, Y (ความสูงเหนือเรือ), Z (ระยะถอยหลัง)")]
    public Vector3 offset = new Vector3(0f, 10f, -16f);

    [Header("Follow Dynamics")]
    [Tooltip("ความเร็วในการตามตำแหน่งแนวนอน (XZ)")]
    public float horizontalMoveSpeed = 5f;
    [Tooltip("ความเร็วในการขยับตามคลื่นแนวดิ่ง (Y)")]
    public float verticalWaveSpeed = 14f;
    [Tooltip("ความเร็วในการหมุนกล้องตามท้ายเรือ")]
    public float rotateSpeed = 3.5f;

    [Header("Look Angle")]
    [Tooltip("มุมก้มของกล้อง")]
    [Range(10f, 90f)] public float pitchAngle = 70f;

    [Header("Cinematic Lean (เอียงรับแรงเหวี่ยงโค้ง)")]
    [Tooltip("องศาการเอียงกล้องสูงสุดเวลาเลี้ยว (แนะนำ 3 - 6 องศา พอดีตา)")]
    [Range(0f, 15f)] public float maxRollLean = 4.5f;
    [Tooltip("ความนุ่มนวลในการเอียงและคืนทรง")]
    public float rollLeanSpeed = 4f;

    private float currentRoll = 0f;
    private float previousTargetYaw;

    void Start()
    {
        if (target != null)
        {
            previousTargetYaw = target.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. คำนวณแกนหันของเรือ (Yaw)
        float targetYaw = target.eulerAngles.y;
        Quaternion flatRotation = Quaternion.Euler(0f, targetYaw, 0f);

        // 2. คำนวณความเร็วในการหมุนเลี้ยว (Angular Velocity รอบแกน Y)
        float yawDelta = Mathf.DeltaAngle(previousTargetYaw, targetYaw);
        previousTargetYaw = targetYaw;

        // คำนวณเป้าหมาย Roll: เลี้ยวซ้ายเอียงซ้าย เลี้ยวขวาเอียงขวา (ติดลบเพื่อให้เอียงรับมุมเลี้ยว)
        float targetRoll = Mathf.Clamp(-yawDelta * 2.5f, -maxRollLean, maxRollLean);
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * rollLeanSpeed);

        // 3. จัดการตำแหน่งกล้อง (แยกแกนแนวดิ่ง Y ให้เด้งตามคลื่น)
        Vector3 targetPos = target.position + flatRotation * offset;
        float newX = Mathf.Lerp(transform.position.x, targetPos.x, Time.deltaTime * horizontalMoveSpeed);
        float newZ = Mathf.Lerp(transform.position.z, targetPos.z, Time.deltaTime * horizontalMoveSpeed);
        float newY = Mathf.Lerp(transform.position.y, targetPos.y, Time.deltaTime * verticalWaveSpeed);

        transform.position = new Vector3(newX, newY, newZ);

        // 4. ประกอบการหมุน: Pitch (มุมก้ม), Yaw (หันตามท้ายเรือ), Roll (เอียงรับโค้ง)
        Quaternion desiredRotation = Quaternion.Euler(pitchAngle, targetYaw, currentRoll);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotateSpeed);
    }
}