using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DenseSonarScanner : MonoBehaviour
{
    [Header("3D Hologram Map")]
    [Tooltip("ลาก GameObject ที่แปะสคริปต์ SonarHologramMap มาใส่ที่นี่")]
    public SonarHologramMap hologramMap;

    [Header("3D Hologram UI")]
    public SonarInfoPanel3D infoPanel3D;
    private IScannable detectedTarget = null;
    private float detectedTargetDistance = 0f;

    [Header("Sonar Origin")]
    [Tooltip("จุดปล่อยคลื่น (เช่น ปลายหัวเรือ) หากเว้นว่างไว้จะปล่อยจากกล้อง")]
    public Transform customOriginPoint;

    [Header("Input Controls")]
    public KeyCode aimKey = KeyCode.Mouse1;
    public KeyCode fireKey = KeyCode.Mouse0;
    public KeyCode quickScanKey = KeyCode.F;

    [Header("Shockwave Effect")]
    public bool enableShockwave = true;
    public Color shockwaveColor = new Color(0f, 1f, 0.9f, 0.85f);
    public float shockwaveThickness = 0.4f;
    public int shockwaveSegments = 64;
    [Range(1, 8)]
    public int shockwaveRingsCount = 3;
    public float shockwaveInterval = 0.1f;

    [Header("2D Crosshair Reticle")]
    public Camera playerCamera;
    public Sprite crosshairSprite;
    public Vector2 crosshairSize = new Vector2(48f, 48f);
    public Color crosshairColor = new Color(0f, 1f, 0.8f, 0.9f);

    [Header("Sonar 2D Particle Sprite")]
    public Sprite sonarPointSprite;

    [Header("Sonar Settings")]
    public float scanRadius = 120f;
    [Range(15f, 120f)]
    public float scanAngle = 60f;
    public int totalRayCount = 4000;
    public float pingSpeed = 60f;
    public float markerSize = 0.35f;
    public float markerLifetime = 2.0f;

    [Header("Detection & Layers")]
    public LayerMask scanLayers = ~0;
    public string interactableTag = "Interactable";
    public Color normalPointColor = new Color(0f, 0.85f, 1f, 1f);
    public Color interactablePointColor = new Color(1f, 0.15f, 0.15f, 1f);

    private ParticleSystem sonarParticleSystem;
    private bool isAiming = false;
    private GameObject crosshairCanvasObj;
    private Image crosshairImage;
    private Material shockwaveMaterial;
    private Collider[] selfColliders;

    private struct SonarHitData
    {
        public Vector3 position;
        public float distance;
        public Color color;
    }

    void Awake()
    {
        Setup2DCrosshair();
        SetupParticleSystem();
        SetupShockwaveMaterial();

        Transform rootSub = transform.root;
        selfColliders = rootSub.GetComponentsInChildren<Collider>();
    }

    void Update()
    {
        UpdateActiveCamera();
        HandleAimInput();

        if ((isAiming && Input.GetKeyDown(fireKey)) || Input.GetKeyDown(quickScanKey))
        {
            PerformSonarScan();
        }
    }

    private void UpdateActiveCamera()
    {
        // หากล้องที่กำลังเปิดใช้งานจริงในขณะนั้นทันที (รองรับการสลับกล้องแบบเรียลไทม์)
        Camera[] allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in allCams)
        {
            if (cam.gameObject.activeInHierarchy && cam.enabled)
            {
                if (cam.name.Contains("Submarine") || cam.name.Contains("Camera"))
                {
                    playerCamera = cam;
                    return;
                }
            }
        }

        if (playerCamera == null || !playerCamera.gameObject.activeInHierarchy || !playerCamera.enabled)
        {
            playerCamera = Camera.main;
        }
    }

    private void HandleAimInput()
    {
        isAiming = Input.GetKey(aimKey);

        if (crosshairCanvasObj != null)
        {
            crosshairCanvasObj.SetActive(isAiming);
        }
    }

    private void PerformSonarScan()
    {
        UpdateActiveCamera();
        if (playerCamera == null) return;

        // 1. หาจุดที่ผู้เล่นกำลังมองกลางจอ
        Ray aimRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;

        // ยิง Ray หาจุดตกกระทบ ถ้าไม่ชนอะไรให้ล็อกไปที่ระยะ scanRadius ข้างหน้า
        if (Physics.Raycast(aimRay, out RaycastHit hit, scanRadius, scanLayers))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = aimRay.origin + (aimRay.direction * scanRadius);
        }

        // 2. กำหนดจุดปล่อยคลื่น
        Vector3 origin = (customOriginPoint != null) ? customOriginPoint.position : aimRay.origin;

        // 3. ทิศทางพุ่งจากจุดปล่อยไปยังจุดเป้าเล็งอย่างแม่นยำ
        Vector3 finalDirection = (targetPoint - origin).normalized;

        StartCoroutine(PerformTargetedScan(origin, finalDirection));
    }

    private IEnumerator EmitShockwavesRoutine(Vector3 origin, Vector3 direction)
    {
        for (int ring = 0; ring < shockwaveRingsCount; ring++)
        {
            float layerRatio = 1f - (ring / (float)shockwaveRingsCount * 0.3f);
            Color ringColor = shockwaveColor;
            ringColor.a *= layerRatio;
            float ringThickness = shockwaveThickness * layerRatio;

            StartCoroutine(PlayShockwaveRingRoutine(origin, direction, ringColor, ringThickness));
            
            if (shockwaveInterval > 0f)
                yield return new WaitForSeconds(shockwaveInterval);
        }
    }

    private IEnumerator PlayShockwaveRingRoutine(Vector3 origin, Vector3 direction, Color ringColor, float thickness)
    {
        GameObject waveObj = new GameObject("Sonar_Shockwave_Ring");
        LineRenderer lr = waveObj.AddComponent<LineRenderer>();
        lr.material = shockwaveMaterial;
        lr.useWorldSpace = true; // บังคับใช้ World Space เพื่อป้องกันการหมุนบิดตามแกนเรือ
        
        int pointCount = shockwaveSegments + 1;
        lr.positionCount = pointCount;
        lr.startWidth = thickness;
        lr.endWidth = thickness;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        float duration = scanRadius / pingSpeed;
        float elapsed = 0f;
        float halfAngleRad = (scanAngle * 0.5f) * Mathf.Deg2Rad;
        float tanHalfAngle = Mathf.Tan(halfAngleRad);

        // คำนวณแกนระนาบตั้งฉากกับทิศทางการยิง (Right & Up Vector)
        Vector3 forward = direction.normalized;
        Vector3 right = Vector3.Cross(forward, Vector3.up);
        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.Cross(forward, Vector3.right);
        }
        right.Normalize();
        Vector3 up = Vector3.Cross(right, forward).normalized;

        Vector3[] ringPoints = new Vector3[pointCount];

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentDist = Mathf.Min(elapsed * pingSpeed, scanRadius);
            float currentRadius = Mathf.Max(currentDist * tanHalfAngle, 0.8f);

            Vector3 centerPos = origin + (forward * currentDist);

            // คำนวณตำแหน่งวงกลมใน World Space ให้แผ่ออกตั้งฉากกับแนวเล็งเสมอ
            for (int i = 0; i < pointCount; i++)
            {
                float angle = (i / (float)shockwaveSegments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle) * currentRadius;
                float sin = Mathf.Sin(angle) * currentRadius;

                ringPoints[i] = centerPos + (right * cos) + (up * sin);
            }
            lr.SetPositions(ringPoints);

            float progress = elapsed / duration;
            float alpha = Mathf.Lerp(ringColor.a, 0f, Mathf.Pow(progress, 1.4f));
            Color c = new Color(ringColor.r, ringColor.g, ringColor.b, alpha);
            lr.startColor = c;
            lr.endColor = c;

            yield return null;
        }

        Destroy(waveObj);
    }

    private bool IsSelfCollider(Collider col)
    {
        if (selfColliders == null) return false;
        for (int i = 0; i < selfColliders.Length; i++)
        {
            if (selfColliders[i] == col) return true;
        }
        return false;
    }

    private IEnumerator PerformTargetedScan(Vector3 origin, Vector3 mainAimDir)
    {
        detectedTarget = null;
        detectedTargetDistance = float.MaxValue;

        

        if (enableShockwave)
        {
            StartCoroutine(EmitShockwavesRoutine(origin, mainAimDir));
        }

        List<SonarHitData> hits = new List<SonarHitData>();
        float halfAngle = scanAngle * 0.5f;

        for (int i = 0; i < totalRayCount; i++)
        {
            float distanceRatio = Mathf.Sqrt(Random.value);
            float angleOffset = distanceRatio * halfAngle;
            float rollAngle = Random.Range(0f, 360f);

            Quaternion coneRotation = Quaternion.AngleAxis(rollAngle, mainAimDir) * Quaternion.AngleAxis(angleOffset, Vector3.up);
            Vector3 rayDirection = coneRotation * mainAimDir;

            if (Physics.Raycast(origin, rayDirection, out RaycastHit hit, scanRadius, scanLayers))
            {
                if (IsSelfCollider(hit.collider)) continue;

                bool isInteractable = false;

                if (hit.collider.TryGetComponent<IScannable>(out IScannable scannable))
                {
                    isInteractable = true;
                    if (hit.distance < detectedTargetDistance)
                    {
                        detectedTarget = scannable;
                        detectedTargetDistance = hit.distance;
                    }
                }
                else if (hit.collider.CompareTag(interactableTag))
                {
                    isInteractable = true;
                }

                hits.Add(new SonarHitData
                {
                    position = hit.point + (hit.normal * 0.05f),
                    distance = hit.distance,
                    color = isInteractable ? interactablePointColor : normalPointColor
                });
            }
        }

        hits.Sort((a, b) => a.distance.CompareTo(b.distance));

        float currentDist = 0f;
        int hitIndex = 0;
        bool hasTriggeredUI = false;

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();

        while (currentDist < scanRadius && hitIndex < hits.Count)
        {
            currentDist += pingSpeed * Time.deltaTime;

            if (!hasTriggeredUI && detectedTarget != null && currentDist >= detectedTargetDistance)
            {
                hasTriggeredUI = true;
                if (infoPanel3D != null)
                {
                    infoPanel3D.Init(transform);
                    infoPanel3D.DisplayData(detectedTarget.GetScanData());
                }
            }

          while (hitIndex < hits.Count && hits[hitIndex].distance <= currentDist)
            {
                emitParams.position = hits[hitIndex].position;
                emitParams.startColor = hits[hitIndex].color;
                emitParams.startSize = markerSize;
                emitParams.startLifetime = markerLifetime;
                
                sonarParticleSystem.Emit(emitParams, 1);

                // --- เพิ่มบรรทัดนี้: ส่งจุดไปบันทึกบน Hologram Map ทันทีที่คลื่นเดินทางไปถึง ---
                if (hologramMap != null)
                {
                    hologramMap.AddPoint(hits[hitIndex].position, hits[hitIndex].color);
                }
                // -------------------------------------------------------------------------

                hitIndex++;
            }

            yield return null;
        }

        if (!hasTriggeredUI && detectedTarget != null && infoPanel3D != null)
        {
            infoPanel3D.Init(transform);
            infoPanel3D.DisplayData(detectedTarget.GetScanData());
        }
    }

    private void Setup2DCrosshair()
    {
        crosshairCanvasObj = new GameObject("Sonar_2D_Crosshair_Canvas");
        Canvas canvas = crosshairCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        crosshairCanvasObj.AddComponent<CanvasScaler>();

        GameObject imgObj = new GameObject("Crosshair_Icon");
        imgObj.transform.SetParent(crosshairCanvasObj.transform, false);
        crosshairImage = imgObj.AddComponent<Image>();

        if (crosshairSprite != null)
        {
            crosshairImage.sprite = crosshairSprite;
        }

        crosshairImage.color = crosshairColor;

        RectTransform rt = imgObj.GetComponent<RectTransform>();
        rt.sizeDelta = crosshairSize;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        crosshairCanvasObj.SetActive(false);
    }

    private void SetupShockwaveMaterial()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null) s = Shader.Find("HDRP/Unlit");
        if (s == null) s = Shader.Find("Particles/Standard Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");

        shockwaveMaterial = new Material(s);
        shockwaveMaterial.SetFloat("_Surface", 1);
        shockwaveMaterial.SetInt("_Blend", 1);
        shockwaveMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void SetupParticleSystem()
    {
        sonarParticleSystem = GetComponent<ParticleSystem>();
        if (sonarParticleSystem == null)
            sonarParticleSystem = gameObject.AddComponent<ParticleSystem>();

        var main = sonarParticleSystem.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50000;
        main.loop = false;
        main.playOnAwake = false;

        var emission = sonarParticleSystem.emission;
        emission.enabled = false;

        var shape = sonarParticleSystem.shape;
        shape.enabled = false;

        var colorOverLifetime = sonarParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0.0f), 
                new GradientColorKey(Color.white, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(1.0f, 0.3f), 
                new GradientAlphaKey(0.6f, 0.6f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var renderer = sonarParticleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("HDRP/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");

        if (particleShader != null)
        {
            Material spriteMat = new Material(particleShader);
            if (sonarPointSprite != null)
            {
                spriteMat.mainTexture = sonarPointSprite.texture;
            }
            spriteMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            renderer.material = spriteMat;
        }
    }
}