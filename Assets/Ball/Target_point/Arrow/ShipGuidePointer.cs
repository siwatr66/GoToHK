using System.Collections.Generic;
using UnityEngine;

public class ShipGuidePointer : MonoBehaviour
{
    public enum NavigationMode
    {
        TargetListInOrder,  // ชี้ตามลำดับใน List (จุดที่ 1, 2, 3...)
        NearestPoint        // ชี้หาจุดที่ใกล้เรือที่สุดอัตโนมัติ
    }

    [Header("Mode Selection")]
    public NavigationMode mode = NavigationMode.TargetListInOrder;

    [Header("Targets")]
    [Tooltip("ลาก Transform ของจุดสำรวจมาใส่ในนี้ (ใส่จุดเดียวหรือหลายจุดก็ได้)")]
    public List<Transform> targetPoints = new List<Transform>();
    public int currentTargetIndex = 0;

    [Header("2D Sprite Settings")]
    [Tooltip("ลาก GameObject ของลูกศร 2D จาก Hierarchy มาใส่ช่องนี้")]
    public Transform spriteArrowTransform;

    [Tooltip("ติ๊กถูกถ้าต้องการให้ Sprite หันหน้าตั้งฉากกับกล้องเสมอ (Billboard)")]
    public bool faceCameraBillboard = true;

    [Tooltip("ชดเชยองศาของภาพ (หากรูปชี้ขึ้นบนให้ใส่ 0, หากชี้ขวาให้ใส่ -90 หรือ 90)")]
    public float spriteAngleOffset = 0f;

    [Tooltip("ความเร็วในการหมุนของลูกศร")]
    public float rotationSpeed = 8f;

    private Transform currentTarget;
    private Camera activeCamera;

   void Update()
{
    UpdateCurrentTarget();

    // ถ้าไม่มีเป้าหมาย ไม่ต้องทำอะไร ปล่อยให้สคริปต์ ArrowDistanceFade คุมการจางหายเอง
    if (currentTarget == null) return;

    RotateSpriteArrow();
}

[Header("Rotator Setup")]
[Tooltip("ลาก ArrowRotator มาใส่ช่องนี้")]
public Transform arrowRotator;

void RotateSpriteArrow()
{
    if (arrowRotator == null || currentTarget == null) return;

    // 1. หาตำแหน่งเป้าหมาย แต่ล็อกความสูง Y ให้อยู่ระดับเดียวกับเรือ (ไม่เงยขึ้นฟ้า/ไม่กดลงดิน)
    Vector3 targetPosition = new Vector3(
        currentTarget.position.x, 
        arrowRotator.position.y, 
        currentTarget.position.z
    );

    // 2. คำนวณทิศทาง
    Vector3 direction = targetPosition - arrowRotator.position;

    if (direction.sqrMagnitude < 0.001f) return;

    // 3. หมุนแกนสีน้ำเงินของ ArrowRotator เข้าหาเป้าหมายตรงๆ
    Quaternion targetRotation = Quaternion.LookRotation(direction);

    arrowRotator.rotation = Quaternion.Slerp(
        arrowRotator.rotation, 
        targetRotation, 
        Time.deltaTime * rotationSpeed
    );
}

    void UpdateCurrentTarget()
    {
        if (targetPoints == null || targetPoints.Count == 0)
        {
            currentTarget = null;
            return;
        }

        if (mode == NavigationMode.TargetListInOrder)
        {
            if (currentTargetIndex >= 0 && currentTargetIndex < targetPoints.Count)
            {
                currentTarget = targetPoints[currentTargetIndex];
            }
            else
            {
                currentTarget = null;
            }
        }
        else if (mode == NavigationMode.NearestPoint)
        {
            Transform closest = null;
            float minDistance = Mathf.Infinity;

            foreach (var point in targetPoints)
            {
                if (point == null || !point.gameObject.activeInHierarchy) continue;

                float dist = Vector3.Distance(transform.position, point.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = point;
                }
            }
            currentTarget = closest;
        }
    }

    // ฟังก์ชันสาธารณะ: เรียกใช้จากสคริปต์ Interact ภายนอกเมื่อต้องการเปลี่ยนจุดถัดไป
    public void NextTarget()
    {
        currentTargetIndex++;
    }

    // ฟังก์ชันสาธารณะ: กำหนดเป้าหมายใหม่แบบเจาะจง
    public void SetTarget(Transform newTarget)
    {
        currentTarget = newTarget;
    }
}