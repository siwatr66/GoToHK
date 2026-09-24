using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("UI Panels")]
    public GameObject startPanel;     // ลาก StartPanel มาใส่
    public GameObject gameOverPanel;  // ลาก GameOverPanel มาใส่
    public TextMeshProUGUI scoreText; // ลาก ScoreText มาใส่

    [Header("Audio")]
    public AudioClip scoreSound;
    private AudioSource audioSource;

    private int score = 0;
    private bool isGameOver = false;
    public bool isGameStarted = false; // ตัวแปรบอกสถานะว่าเริ่มเกมหรือยัง

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        Time.timeScale = 1f;
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // เริ่มต้นให้เปิดหน้า Start และปิดหน้าอื่นๆ
        isGameStarted = false;
        if (startPanel != null) startPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false); // ซ่อนแต้มไว้ก่อน
            scoreText.text = "0";
        }
    }

    // เรียกฟังก์ชันนี้เมื่อกดปุ่ม START
    public void StartGame()
    {
        isGameStarted = true;

        // ซ่อนหน้า Start แล้วโชว์ตัวเลขนับแต้ม
        if (startPanel != null) startPanel.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(true);
    }

    public void AddScore()
    {
        if (isGameOver || !isGameStarted) return;
        score++;

        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }

        if (audioSource != null && scoreSound != null)
        {
            audioSource.PlayOneShot(scoreSound);
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}