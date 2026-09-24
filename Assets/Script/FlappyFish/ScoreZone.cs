using UnityEngine;

public class ScoreZone : MonoBehaviour
{
    private bool scored = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (scored) return;

        // ตรวจทั้ง Tag หรือหา FishController บนตัวมันเอง หรือตัวแม่/ลูก
        bool isPlayer = collision.CompareTag("Player") ||
                        collision.GetComponentInParent<FishController>() != null ||
                        collision.GetComponentInChildren<FishController>() != null;

        if (isPlayer)
        {
            scored = true;
            if (GameManager.instance != null)
            {
                GameManager.instance.AddScore();
            }
        }
    }
}