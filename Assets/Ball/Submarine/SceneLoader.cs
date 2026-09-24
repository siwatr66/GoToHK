using UnityEngine;
using UnityEngine.SceneManagement; // จำเป็นต้องมีบรรทัดนี้เพื่อจัดการ Scene

public class SceneLoader : MonoBehaviour
{
    // ฟังก์ชันสำหรับโหลด Scene โดยใส่ชื่อ Scene ที่ต้องการ
    public void LoadSceneMiniGame(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}