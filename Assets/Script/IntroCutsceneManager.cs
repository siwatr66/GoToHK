using System.Collections;
using UnityEngine;

public class IntroCutsceneManager : MonoBehaviour
{
    public Camera introCamera;        // ลาก Intro_Camera มาใส่
    public Camera mainGameplayCamera; // ลาก Main Camera มาใส่
    public float cutsceneDuration = 5.0f;

    private void Awake()
    {
        // บังคับให้กล้อง Intro มีความสำคัญสูงสุดทันทีตั้งแต่ก่อนเริ่มเกม
        if (introCamera != null)
        {
            introCamera.gameObject.SetActive(true);
            introCamera.depth = 100; // ค่า Depth สูงสุด บังคับให้ภาพนี้ขึ้นจอแน่นอน
        }

        if (mainGameplayCamera != null)
        {
            mainGameplayCamera.depth = 0;
            mainGameplayCamera.gameObject.SetActive(false);
        }
    }

    private IEnumerator Start()
    {
        // ย้ำอีกรอบในเฟรมแรก เพื่อกันโค้ดอื่นมาแอบเปิดแข่ง
        yield return null;
        if (mainGameplayCamera != null)
        {
            mainGameplayCamera.gameObject.SetActive(false);
        }

        // รอจนแอนิเมชันใต้น้ำเล่นจบครบ 5 วินาที
        yield return new WaitForSeconds(cutsceneDuration);

        // ตัดเข้าเกม: ปิดกล้อง Intro และเปิดกล้องหลักพร้อมเล่น
        if (introCamera != null)
        {
            introCamera.gameObject.SetActive(false);
        }

        if (mainGameplayCamera != null)
        {
            mainGameplayCamera.gameObject.SetActive(true);
            mainGameplayCamera.depth = 100;
        }
    }
}