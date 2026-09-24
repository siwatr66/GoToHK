using UnityEngine;

public class ArrowDistanceFade : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ใส่ SpriteRenderer ของลูกศร (ถ้าเว้นว่างไว้ สคริปต์จะค้นหาจากตัวเองหรือลูกให้อัตโนมัติ)")]
    public SpriteRenderer arrowSprite;

    [Tooltip("ดึงเป้าหมายจาก ShipGuidePointer อัตโนมัติ หรือจะลาก Transform ใส่เองก็ได้")]
    public ShipGuidePointer guidePointer;
    public Transform manualTarget;

    [Header("Distance Fade Settings (เมตร)")]
    [Tooltip("ระยะที่ลูกศรจะเริ่มค่อยๆ จางปรากฏขึ้นมา (ไกลกว่านี้ = มองไม่เห็นเลย Alpha 0)")]
    public float maxVisibleDistance = 60f;

    [Tooltip("ระยะที่ลูกศรจะชัดเต็มที่ 100% (Alpha 1)")]
    public float fullVisibleDistance = 20f;

    [Header("Close-up Fade (ออปชัน: จางหายเมื่อถึงเป้าหมาย)")]
    [Tooltip("ต้องการให้ลูกศรจางหายไปเมื่อเรือแล่นเข้ามาประชิดจุดตรวจหรือไม่")]
    public bool fadeOutWhenVeryClose = false;
    [Tooltip("ระยะประชิดที่จะเริ่มจางหาย")]
    public float disappearDistance = 5f;

    [Header("Smoothness")]
    public float fadeSpeed = 5f;

    private Color initialColor;

    void Awake()
    {
        if (arrowSprite == null)
        {
            arrowSprite = GetComponentInChildren<SpriteRenderer>();
        }

        if (guidePointer == null)
        {
            guidePointer = GetComponentInParent<ShipGuidePointer>();
        }

        if (arrowSprite != null)
        {
            initialColor = arrowSprite.color;
        }
    }

    void Update()
    {
        if (arrowSprite == null) return;

        // หาเป้าหมายปัจจุบัน
        Transform target = GetCurrentTarget();
        if (target == null)
        {
            SetAlphaSmooth(0f);
            return;
        }

        // คำนวณระยะห่างบนระนาบผิวน้ำ (แกน X, Z)
        Vector3 boatPos = transform.position;
        Vector3 targetPos = target.position;
        boatPos.y = 0f;
        targetPos.y = 0f;

        float distance = Vector3.Distance(boatPos, targetPos);

        // คำนวณค่า Alpha ตามระยะห่าง
        float targetAlpha = 0f;

        if (distance <= fullVisibleDistance)
        {
            targetAlpha = 1f;

            // หากเปิดให้อยู่ใกล้มากๆ แล้วค่อยๆ จางหาย
            if (fadeOutWhenVeryClose && distance < disappearDistance)
            {
                targetAlpha = Mathf.InverseLerp(0f, disappearDistance, distance);
            }
        }
        else if (distance < maxVisibleDistance)
        {
            // ระยะระหว่าง maxVisibleDistance ถึง fullVisibleDistance ค่อยๆ ชัดขึ้น
            targetAlpha = Mathf.InverseLerp(maxVisibleDistance, fullVisibleDistance, distance);
        }
        else
        {
            // ไกลเกินระยะที่กำหนด = โปร่งใสสมบูรณ์
            targetAlpha = 0f;
        }

        SetAlphaSmooth(targetAlpha);
    }

    private void SetAlphaSmooth(float targetAlpha)
    {
        Color cur = arrowSprite.color;
        float newAlpha = Mathf.MoveTowards(cur.a, targetAlpha, Time.deltaTime * fadeSpeed);
        arrowSprite.color = new Color(initialColor.r, initialColor.g, initialColor.b, newAlpha);
    }

    private Transform GetCurrentTarget()
    {
        if (manualTarget != null) return manualTarget;

        if (guidePointer != null)
        {
            // ดึงเป้าหมายตามลำดับจาก ShipGuidePointer
            if (guidePointer.targetPoints != null && 
                guidePointer.currentTargetIndex >= 0 && 
                guidePointer.currentTargetIndex < guidePointer.targetPoints.Count)
            {
                return guidePointer.targetPoints[guidePointer.currentTargetIndex];
            }
        }

        return null;
    }
}