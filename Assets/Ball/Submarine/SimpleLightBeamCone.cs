using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SimpleLightBeamCone : MonoBehaviour
{
    [Header("Beam Dimensions")]
    [Tooltip("ความยาวของลำแสง")]
    public float beamLength = 15f;
    [Tooltip("รัศมีฐานโคนลำแสง (ความกว้างปลายแสง)")]
    public float endRadius = 3.5f;
    [Tooltip("รัศมีต้นโคน (หน้าหลอดไฟ)")]
    public float startRadius = 0.2f;
    [Tooltip("จำนวนเหลี่ยมของโคน (ยิ่งเยอะยิ่งกลมเนียน)")]
    [Range(12, 64)]
    public int segments = 32;

    [Header("Visual")]
    [Tooltip("สีของลำแสง (ปรับ Alpha เพื่อคุมความจาง)")]
    public Color beamColor = new Color(1f, 0.6f, 0.1f, 0.18f);

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        GenerateBeamCone();
    }

    void OnValidate()
    {
        // อัปเดตขนาดใน Scene ได้ทันทีเมื่อปรับค่าใน Inspector
        GenerateBeamCone();
    }

    public void GenerateBeamCone()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Procedural_LightBeam";
        }
        else
        {
            mesh.Clear();
        }

        int vertCount = (segments + 1) * 2;
        Vector3[] vertices = new Vector3[vertCount];
        Color[] colors = new Color[vertCount];
        int[] triangles = new int[segments * 6];

        float angleStep = 360f / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // จุดยอด (หน้าหลอดไฟ Z = 0)
            vertices[i] = new Vector3(cos * startRadius, sin * startRadius, 0f);
            colors[i] = beamColor;

            // จุดฐาน (ปลายลำแสง Z = beamLength)
            vertices[i + segments + 1] = new Vector3(cos * endRadius, sin * endRadius, beamLength);
            // ปลายลำแสงปรับให้ Alpha เหลือ 0 เพื่อให้แสงค่อยๆ กลืนไปกับน้ำ
            colors[i + segments + 1] = new Color(beamColor.r, beamColor.g, beamColor.b, 0f);
        }

        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int currentTop = i;
            int nextTop = i + 1;
            int currentBottom = i + segments + 1;
            int nextBottom = i + segments + 2;

            triangles[triIndex++] = currentTop;
            triangles[triIndex++] = currentBottom;
            triangles[triIndex++] = nextTop;

            triangles[triIndex++] = nextTop;
            triangles[triIndex++] = currentBottom;
            triangles[triIndex++] = nextBottom;
        }

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;

        // ใส่ Material Additive สำหรับ URP อัตโนมัติ
        if (meshRenderer.sharedMaterial == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s == null) s = Shader.Find("Sprites/Default");

            Material mat = new Material(s);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetInt("_Blend", 1);     // Additive
            mat.SetInt("_Cull", 0);      // Double Sided (Render Face: Both)
            meshRenderer.sharedMaterial = mat;
        }
    }
}