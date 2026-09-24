using System.Collections.Generic;
using UnityEngine;

public class SonarHologramMap : MonoBehaviour
{
    [System.Serializable]
    public struct HologramPoint
    {
        public Vector3 worldPosition;
        public Color color;
        public float spawnTime;
    }

    [Header("Anchor Profiles (แยก 2 มุมมอง)")]
    [Tooltip("จุดวางโฮโลแกรมในมุมมอง TPV (นอกเรือ)")]
    public Transform tpvAnchor;
    public float tpvMapScale = 0.015f;
    public float tpvPointSize = 0.012f;

    [Tooltip("จุดวางโฮโลแกรมในมุมมอง FPV (บนคอนโซลในห้องคนขับ)")]
    public Transform fpvAnchor;
    public float fpvMapScale = 0.005f; // ในห้องคนขับควรย่อให้เล็กลงเพื่อไม่ให้บังกระจก
    public float fpvPointSize = 0.004f;

    [Header("General Settings")]
    public float pointLifetime = 25f;
    public int maxHologramPoints = 8000;
    public Transform submarineRoot;

    private ParticleSystem holoParticleSystem;
    private List<HologramPoint> recordedPoints = new List<HologramPoint>();
    private ParticleSystem.Particle[] particleBuffer;

    private Transform currentAnchor;
    private float currentMapScale;
    private float currentPointSize;

    void Awake()
    {
        SetupHologramParticleSystem();
        particleBuffer = new ParticleSystem.Particle[maxHologramPoints];

        // เริ่มต้นด้วยโหมด TPV
        SetViewMode(false);
    }

    private void SetupHologramParticleSystem()
    {
        holoParticleSystem = GetComponent<ParticleSystem>();
        if (holoParticleSystem == null)
            holoParticleSystem = gameObject.AddComponent<ParticleSystem>();

        var main = holoParticleSystem.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxHologramPoints;
        main.loop = false;
        main.playOnAwake = false;

        var emission = holoParticleSystem.emission;
        emission.enabled = false;

        var shape = holoParticleSystem.shape;
        shape.enabled = false;

        var renderer = holoParticleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("HDRP/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            renderer.material = mat;
        }
    }

    /// <summary>
    /// สลับการแสดงผล Anchor ตามมุมกล้อง (true = FPV, false = TPV)
    /// </summary>
    public void SetViewMode(bool isFirstPerson)
    {
        if (isFirstPerson && fpvAnchor != null)
        {
            currentAnchor = fpvAnchor;
            currentMapScale = fpvMapScale;
            currentPointSize = fpvPointSize;
        }
        else
        {
            currentAnchor = tpvAnchor != null ? tpvAnchor : transform;
            currentMapScale = tpvMapScale;
            currentPointSize = tpvPointSize;
        }
    }

    public void AddPoint(Vector3 worldPos, Color ptColor)
    {
        if (recordedPoints.Count >= maxHologramPoints)
        {
            recordedPoints.RemoveAt(0);
        }

        recordedPoints.Add(new HologramPoint
        {
            worldPosition = worldPos,
            color = ptColor,
            spawnTime = Time.time
        });
    }

    void LateUpdate()
    {
        if (submarineRoot == null || currentAnchor == null || holoParticleSystem == null) return;

        float now = Time.time;
        recordedPoints.RemoveAll(p => now - p.spawnTime > pointLifetime);

        int count = Mathf.Min(recordedPoints.Count, maxHologramPoints);

        for (int i = 0; i < count; i++)
        {
            HologramPoint hp = recordedPoints[i];

            // คำนวณตำแหน่งสัมพัทธ์กับตัวเรือ
            Vector3 relativeToSub = hp.worldPosition - submarineRoot.position;
            Vector3 localScaled = submarineRoot.InverseTransformDirection(relativeToSub) * currentMapScale;

            // วางลงบน Anchor ปัจจุบัน
            Vector3 targetDisplayPos = currentAnchor.TransformPoint(localScaled);

            float age = now - hp.spawnTime;
            float alpha = Mathf.Clamp01(1f - (age / pointLifetime));
            Color c = hp.color;
            c.a *= alpha;

            particleBuffer[i].position = targetDisplayPos;
            particleBuffer[i].startColor = c;
            particleBuffer[i].startSize = currentPointSize;
        }

        holoParticleSystem.SetParticles(particleBuffer, count);
    }

    public void ClearHologram()
    {
        recordedPoints.Clear();
        if (holoParticleSystem != null) holoParticleSystem.Clear();
    }
}