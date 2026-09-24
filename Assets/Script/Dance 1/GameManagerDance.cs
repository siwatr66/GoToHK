using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class DanceGameManager : MonoBehaviour
{
    public static DanceGameManager instance;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI hitText;

    [Header("UI หน้าจอต่างๆ")]
    public Image dimBackground;
    public GameObject startMenuPanel;
    public GameObject gameOverPanel;

    [Header("UI ตอนชนะ")]
    public TextMeshProUGUI winInstructionText;

    [Header("เอฟเฟกต์แสงตอนกดได้ Perfect")]
    public GameObject perfectEffect;

    [Header("ระบบเพลงประกอบ")]
    public AudioSource bgmMusic;
    // ----------------------------------------------------
    [Tooltip("ใส่เวลาเป็นวินาที เช่น อยากให้เริ่มเล่นตอนนาทีที่ 1 ให้ใส่ 60")]
    public float musicStartTime = 0f; // ช่องใหม่สำหรับระบุวินาทีที่จะเริ่มเล่น
    // ----------------------------------------------------

    [Header("ตั้งค่าชื่อหน้าจอหลัก")]
    public string mainSceneName = "MainScaen";

    [Header("ระบบเลือด (HP)")]
    public int maxHP = 10;
    private int currentHP;
    public Slider hpBar;

    public Transform mainCamera;
    private Vector3 cameraOriginalPos;
    private float shakeTimer = 0f;

    private int score = 0;
    private int combo = 0;

    private int currentLevel = 1;
    private bool isGameEnded = false;

    private bool isStageCleared = false;
    private float returnTimer = 0f;
    private bool canClickToReturn = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        currentHP = maxHP;
        if (hpBar != null)
        {
            hpBar.maxValue = maxHP;
            hpBar.value = currentHP;
        }

        UpdateUI();
        if (hitText != null) hitText.text = "";

        if (mainCamera != null)
        {
            cameraOriginalPos = mainCamera.position;
        }

        if (dimBackground != null)
        {
            Color c = dimBackground.color;
            c.a = 0f;
            dimBackground.color = c;
            dimBackground.gameObject.SetActive(false);
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (startMenuPanel != null) startMenuPanel.SetActive(true);

        if (winInstructionText != null) winInstructionText.gameObject.SetActive(false);

        NoteSpawner spawner = FindObjectOfType<NoteSpawner>();
        if (spawner != null) spawner.enabled = false;

        if (perfectEffect != null) perfectEffect.SetActive(false);

        if (bgmMusic != null) bgmMusic.Stop();
    }

    public void StartGame()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(false);

        NoteSpawner spawner = FindObjectOfType<NoteSpawner>();
        if (spawner != null) spawner.enabled = true;

        // ----------------------------------------------------
        // สั่งให้เพลงเริ่มเล่น โดยข้ามไปยังวินาทีที่กำหนดไว้
        // ----------------------------------------------------
        if (bgmMusic != null)
        {
            bgmMusic.time = musicStartTime; // กระโดดไปที่เวลาที่ตั้งไว้
            bgmMusic.Play();
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainScene()
    {
        SceneManager.LoadScene(mainSceneName);
    }

    void Update()
    {
        if (hitText != null)
        {
            Vector3 targetScale = isGameEnded ? Vector3.one * 3f : Vector3.one;
            hitText.transform.localScale = Vector3.Lerp(hitText.transform.localScale, targetScale, Time.deltaTime * 10f);
        }

        if (isGameEnded && dimBackground != null)
        {
            Color c = dimBackground.color;
            c.a = Mathf.Lerp(c.a, 0.8f, Time.deltaTime * 3f);
            dimBackground.color = c;
        }

        if (shakeTimer > 0 && mainCamera != null)
        {
            mainCamera.position = cameraOriginalPos + Random.insideUnitSphere * 0.1f;
            shakeTimer -= Time.deltaTime;
        }
        else if (mainCamera != null)
        {
            mainCamera.position = cameraOriginalPos;
        }

        if (isStageCleared)
        {
            if (!canClickToReturn)
            {
                returnTimer += Time.deltaTime;

                int timeLeft = Mathf.CeilToInt(3f - returnTimer);

                if (timeLeft > 1)
                {
                    winInstructionText.text = "Returning to main menu in " + timeLeft + "...";
                }
                else
                {
                    winInstructionText.text = "- Click anywhere to return -";
                    canClickToReturn = true;
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    GoToMainScene();
                }
            }
        }
    }

    public void AddScore(int amount, string message, Color textColor)
    {
        if (isGameEnded) return;

        score += amount;
        combo++;
        UpdateUI();

        if (hitText != null)
        {
            hitText.text = message;
            hitText.color = textColor;
            hitText.transform.localScale = Vector3.one * 1.5f;
        }

        if (message.ToLower().Contains("perfect") && perfectEffect != null)
        {
            perfectEffect.SetActive(false);
            perfectEffect.SetActive(true);

            ParticleSystem[] particles = perfectEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem p in particles)
            {
                p.Play();
            }
        }

        if (score >= 4000 && !isGameEnded)
        {
            isGameEnded = true;
            isStageCleared = true;

            if (bgmMusic != null) bgmMusic.Stop();

            if (hitText != null)
            {
                hitText.text = "STAGE CLEAR!";
                hitText.color = Color.yellow;
                hitText.transform.localScale = Vector3.one * 4f;

                hitText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                hitText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                hitText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                hitText.rectTransform.sizeDelta = new Vector2(1200f, 400f);
                hitText.rectTransform.anchoredPosition = new Vector2(-540f, 0f);
                hitText.alignment = TextAlignmentOptions.Center;
            }

            if (dimBackground != null) dimBackground.gameObject.SetActive(true);

            if (winInstructionText != null) winInstructionText.gameObject.SetActive(true);

            NoteSpawner spawner = FindObjectOfType<NoteSpawner>();
            if (spawner != null) spawner.enabled = false;

            GameObject[] remainingNotes = GameObject.FindGameObjectsWithTag("Note");
            foreach (GameObject note in remainingNotes)
            {
                Destroy(note);
            }
        }
        else if (score >= 2000 && currentLevel == 2)
        {
            currentLevel = 3;
            if (hitText != null)
            {
                hitText.text = "MAX SPEED!";
                hitText.color = Color.red;
                hitText.transform.localScale = Vector3.one * 2.5f;
            }
            NoteSpawner spawner = FindObjectOfType<NoteSpawner>();
            if (spawner != null) spawner.IncreaseDifficulty(3);
        }
        else if (score >= 1000 && currentLevel == 1)
        {
            currentLevel = 2;
            if (hitText != null)
            {
                hitText.text = "SPEED UP!";
                hitText.color = Color.magenta;
                hitText.transform.localScale = Vector3.one * 2.5f;
            }
            NoteSpawner spawner = FindObjectOfType<NoteSpawner>();
            if (spawner != null) spawner.IncreaseDifficulty(2);
        }
    }

    public void MissNote()
    {
        if (isGameEnded) return;

        combo = 0;
        UpdateUI();

        if (hitText != null)
        {
            hitText.text = "Miss!";
            hitText.color = Color.red;
            hitText.transform.localScale = Vector3.one * 1.5f;
        }

        currentHP--;
        if (hpBar != null) hpBar.value = currentHP;

        if (currentHP <= 0)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        isGameEnded = true;

        if (bgmMusic != null) bgmMusic.Stop();

        if (hitText != null)
        {
            hitText.text = "";
        }

        if (dimBackground != null) dimBackground.gameObject.SetActive(true);

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        NoteSpawner spawner = FindObjectOfType<NoteSpawner>();
        if (spawner != null) spawner.enabled = false;

        GameObject[] remainingNotes = GameObject.FindGameObjectsWithTag("Note");
        foreach (GameObject note in remainingNotes)
        {
            Destroy(note);
        }
    }

    public void ShakeCamera(float duration)
    {
        shakeTimer = duration;
    }

    void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score + "\nCombo: " + combo;
        }
    }
}