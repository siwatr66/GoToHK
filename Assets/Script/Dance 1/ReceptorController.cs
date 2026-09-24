using System.Collections.Generic;
using UnityEngine;

public class ReceptorController : MonoBehaviour
{
    public KeyCode keyToPress;

    [Header("ใส่ Prefab เอฟเฟกต์ฟองอากาศตรงนี้")]
    public GameObject hitEffectPrefab;

    private List<GameObject> activeNotes = new List<GameObject>();

    // ตัวแปรสำหรับเช็กการกดค้าง (Long Note)
    private bool isHolding = false;
    private GameObject currentLongNote = null;
    private float holdTimer = 0f;

    // ตัวแปรสำหรับทำแอนิเมชันปุ่มยุบ
    private Vector3 originalScale;

    void Start()
    {
        // จดจำขนาดเริ่มต้นของแป้น
        originalScale = transform.localScale;
    }

    void Update()
    {
        // ทำให้แป้นค่อยๆ เด้งคืนขนาดเดิมอย่างนุ่มนวลตลอดเวลา (Visual Feedback)
        transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * 10f);

        // 1. จังหวะเริ่มกดปุ่ม (หัวโน้ต)
        if (Input.GetKeyDown(keyToPress))
        {
            // เมื่อกดปุ่ม ให้แป้นยุบตัวลง 20%
            transform.localScale = originalScale * 0.8f;

            if (activeNotes.Count > 0)
            {
                GameObject noteToHit = activeNotes[0];
                float distance = Mathf.Abs(noteToHit.transform.position.y - transform.position.y);

                if (noteToHit.CompareTag("Note")) // ถ้าเป็นโน้ตสั้น
                {
                    EvaluateHit(distance);
                    activeNotes.Remove(noteToHit);
                    Destroy(noteToHit);
                }
                else if (noteToHit.CompareTag("LongNote")) // ถ้าเป็นโน้ตยาว
                {
                    EvaluateHit(distance);
                    isHolding = true; // เปิดโหมดกดค้าง
                    currentLongNote = noteToHit;
                }
            }
        }

        // 2. ระหว่างที่กดค้างอยู่ (รับคะแนนรัวๆ)
        if (Input.GetKey(keyToPress) && isHolding && currentLongNote != null)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= 0.1f) // แจกคะแนน+5 ทุกๆ 0.1 วินาทีที่กดค้าง
            {
                DanceGameManager.instance.AddScore(5, "Holding...", Color.cyan);
                holdTimer = 0f;
            }
        }

        // 3. จังหวะเผลอปล่อยปุ่มก่อนกำหนด
        if (Input.GetKeyUp(keyToPress))
        {
            if (isHolding && currentLongNote != null)
            {
                // ถ้าปล่อยปุ่มทั้งที่หางโน้ตยังไม่สุด = Miss!
                DanceGameManager.instance.MissNote();
                activeNotes.Remove(currentLongNote);
                Destroy(currentLongNote);
            }
            isHolding = false;
            currentLongNote = null;
        }
    }

    private void EvaluateHit(float distance)
    {
        // ยืดระยะยอมรับความแม่นยำขึ้นเล็กน้อยเผื่อศูนย์กลางลูกศรมันขยับตอนยืดภาพ
        if (distance <= 0.5f)
        {
            DanceGameManager.instance.AddScore(100, "Perfect!", Color.green);
            SpawnHitEffect(); // เรียกใช้ Effect
        }
        else if (distance <= 1.0f)
        {
            DanceGameManager.instance.AddScore(50, "Great!", Color.yellow);
            SpawnHitEffect(); // เรียกใช้ Effect
        }
        else
        {
            DanceGameManager.instance.AddScore(10, "Good!", Color.yellow);
            // ระดับ Good จะไม่โชว์ Effect ฟองอากาศเพื่อให้เห็นความต่าง
        }
    }

    // ฟังก์ชันเสก Effect ฟองอากาศ
    private void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Note") || other.CompareTag("LongNote"))
            activeNotes.Add(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (activeNotes.Contains(other.gameObject))
        {
            // ถ้าโน้ตยาวเลื่อนจนสุดหาง และเรายังกดค้างอยู่จนวินาทีสุดท้าย = สำเร็จ!
            if (other.CompareTag("LongNote") && isHolding && currentLongNote == other.gameObject)
            {
                DanceGameManager.instance.AddScore(200, "Perfect Hold!", Color.green);
                SpawnHitEffect(); // เรียกใช้ Effect ปิดท้ายตอนปล่อยสำเร็จ
                isHolding = false;
                currentLongNote = null;
            }
            else
            {
                // โน้ตธรรมดาหลุดโซน หรือ โน้ตยาวหลุดโดยไม่ได้กดค้างไว้
                DanceGameManager.instance.MissNote();
            }

            activeNotes.Remove(other.gameObject);
            Destroy(other.gameObject);
        }
    }
}