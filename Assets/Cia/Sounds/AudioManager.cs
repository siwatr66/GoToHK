using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Source References")]
    public AudioSource surfaceAmbientAudioSource; // เสียง Ambient บนผิวน้ำ
    public AudioSource boatAudioSource;          // เสียงเรือบนผิวน้ำ
    public AudioSource submarineAudioSource;     // เสียงเรือดำน้ำใต้น้ำ
    public AudioSource underwaterMusicAudioSource; // เสียงเพลง/Ambient ใต้น้ำ
    public AudioSource uiAudioSource;            // เสียง UI ต่างๆ
    public AudioSource bgmAudioSource;           // 🎵 เพิ่ม Audio Source สำหรับเพลงประกอบหลัก (สไตล์ Dave the Diver)

    [Header("Audio Clips - Surface & Boat")]
    public AudioClip surfaceAmbientClip;
    public AudioClip boatClip;

    [Header("Audio Clips - Underwater & Music")]
    public AudioClip submarineClip;
    public AudioClip underwaterMusicClip;

    [Header("Audio Clips - BGM (Background Music)")]
    public AudioClip backgroundMusicClip;        // 🎵 ไฟล์เพลงประกอบหลักฉาก

    [Header("Audio Clips - UI & Effects")]
    public AudioClip buttonClickClip;
    public AudioClip deploySubmarineClip;        // เสียงตอนกด G ปล่อยเรือดำน้ำ

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // เริ่มต้นเปิดเพลงประกอบหลักและเสียงใต้น้ำ
        PlayBackgroundMusic();
        PlayUnderwaterAudio();
    }

    void Update()
    {
        // กดปุ่ม G เพื่อปล่อยเรือดำน้ำ
        if (Input.GetKeyDown(KeyCode.G))
        {
            PlayDeploySubmarineEffect();
        }
    }

    // 🎵 ฟังก์ชันเล่นเพลงประกอบหลัก (BGM) วนลูปตลอดเวลา
    public void PlayBackgroundMusic()
    {
        if (bgmAudioSource != null && backgroundMusicClip != null)
        {
            bgmAudioSource.clip = backgroundMusicClip;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
            Debug.Log("🎵 เล่นเพลงประกอบหลัก (BGM)");
        }
    }

    // ฟังก์ชันเปิดเสียงเมื่ออยู่โซนใต้น้ำ
    public void PlayUnderwaterAudio()
    {
        if (surfaceAmbientAudioSource != null) surfaceAmbientAudioSource.Stop();
        if (boatAudioSource != null) boatAudioSource.Stop();

        if (submarineAudioSource != null && submarineClip != null)
        {
            submarineAudioSource.clip = submarineClip;
            submarineAudioSource.loop = true;
            submarineAudioSource.Play();
        }

        if (underwaterMusicAudioSource != null && underwaterMusicClip != null)
        {
            underwaterMusicAudioSource.clip = underwaterMusicClip;
            underwaterMusicAudioSource.loop = true;
            underwaterMusicAudioSource.Play();
        }

        Debug.Log("🔊 สลับระบบเสียงเป็น: โซนใต้น้ำ");
    }

    // ฟังก์ชันเปิดเสียงเมื่ออยู่โซนผิวน้ำ
    public void PlaySurfaceAudio()
    {
        if (submarineAudioSource != null) submarineAudioSource.Stop();
        if (underwaterMusicAudioSource != null) underwaterMusicAudioSource.Stop();

        if (surfaceAmbientAudioSource != null && surfaceAmbientClip != null)
        {
            surfaceAmbientAudioSource.clip = surfaceAmbientClip;
            surfaceAmbientAudioSource.loop = true;
            surfaceAmbientAudioSource.Play();
        }

        if (boatAudioSource != null && boatClip != null)
        {
            boatAudioSource.clip = boatClip;
            boatAudioSource.loop = true;
            boatAudioSource.Play();
        }

        Debug.Log("🔊 สลับระบบเสียงเป็น: โซนผิวน้ำ");
    }

    // ฟังก์ชันเล่นเสียง UI
    public void PlayUIClick()
    {
        if (uiAudioSource != null && buttonClickClip != null)
        {
            uiAudioSource.PlayOneShot(buttonClickClip);
        }
    }

    // ฟังก์ชันเล่นเสียงตอนกด G ปล่อยเรือดำน้ำ
    public void PlayDeploySubmarineEffect()
    {
        if (uiAudioSource != null && deploySubmarineClip != null)
        {
            uiAudioSource.PlayOneShot(deploySubmarineClip);
            Debug.Log("🚀 เล่นเสียงปล่อยเรือดำน้ำ (กดปุ่ม G)");
        }
    }
}