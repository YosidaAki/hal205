using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RailSpline : MonoBehaviour
{
    public enum RailType { Spline, Linear }

    [Header("レール設定")]
    public bool loop = false;
    public List<RailSplineSegment> segments = new List<RailSplineSegment>();

    [Header("チューブ描画設定")]
    [Range(0.01f, 1f)]
    public float radius = 0.1f;
    [Range(3, 32)]
    public int radialSegments = 12;
    [Range(10, 200)]
    public int lengthSegments = 80;
    public Color railColor = new Color(0f, 1f, 1f, 0.4f); // やや透過したシアン
    public bool doubleSided = false;

    [Header("エフェクト設定")]
    public bool animateTexture = true;
    public Vector2 textureScrollSpeed = new Vector2(0f, 1f);
    public bool glowEffect = true;

    public float nextRailStartT = 0f;


    [ColorUsage(true, true)]
    public Color emissionColor = new Color(0.3f, 0.8f, 1f, 1f); // 淡い水色

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh railMesh;
    private Material railMaterial;
    private Vector2 textureOffset;

    void Awake()
    {
        SetupComponents();
        GenerateRailMesh();
    }

    void OnValidate()
    {
        SetupComponents();
        GenerateRailMesh();
    }

    void Update()
    {
        if (meshFilter == null || meshRenderer == null)
        {
            SetupComponents();
            return;
        }

        GenerateRailMesh();

        // テクスチャを流す（必要なら）
        if (animateTexture && railMaterial != null)
        {
            textureOffset += textureScrollSpeed * Time.deltaTime;
            railMaterial.mainTextureOffset = textureOffset;
        }

        // ✨ 発光処理（見た目だけ光る）
        if (glowEffect && railMaterial != null)
        {
            railMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            railMaterial.EnableKeyword("_EMISSION");

            // 淡く光るように少しだけ発光強度を与える
            Color finalEmission = emissionColor * 0.5f; // 強すぎないように半減
            railMaterial.SetColor("_EmissionColor", finalEmission);
        }
        else if (railMaterial != null)
        {
            railMaterial.DisableKeyword("_EMISSION");
        }

        // ベースカラー更新
        if (railMaterial != null)
            railMaterial.color = railColor;
    }

    void SetupComponents()
    {
        if (!meshFilter)
            meshFilter = GetComponent<MeshFilter>();

        if (!meshRenderer)
            meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer.sharedMaterial == null)
        {
            railMaterial = new Material(Shader.Find("Custom/RailGlow_URP"));
            railMaterial.SetColor("_BaseColor", railColor);
            railMaterial.SetColor("_GlowColor", emissionColor);
            railMaterial.SetFloat("_GlowPower", 3f);
            railMaterial.SetFloat("_GlowIntensity", 3f);
            meshRenderer.sharedMaterial = railMaterial;
        }
        else
        {
            railMaterial = meshRenderer.sharedMaterial;
        }
    }

    public void GenerateRailMesh()
    {
        if (segments == null || segments.Count < 2)
        {
            if (meshFilter.sharedMesh) meshFilter.sharedMesh.Clear();
            return;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i <= lengthSegments; i++)
        {
            float t = (float)i / lengthSegments;
            Vector3 center = GetWorldPointOnSpline(t);
            Quaternion rot = GetRotationOnSpline(t);

            for (int j = 0; j <= radialSegments; j++)
            {
                float angle = (float)j / radialSegments * Mathf.PI * 2f;
                Vector3 localPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
                Vector3 worldPos = center + rot * localPos;
                vertices.Add(transform.InverseTransformPoint(worldPos));
                uvs.Add(new Vector2((float)j / radialSegments, t));
            }
        }

        int vertsPerRing = radialSegments + 1;
        for (int i = 0; i < lengthSegments; i++)
        {
            int ringStart = i * vertsPerRing;
            int nextRingStart = (i + 1) * vertsPerRing;

            for (int j = 0; j < radialSegments; j++)
            {
                int a = ringStart + j;
                int b = ringStart + j + 1;
                int c = nextRingStart + j;
                int d = nextRingStart + j + 1;

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);

                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);

                if (doubleSided)
                {
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(d);
                    triangles.Add(c);
                }
            }
        }

        if (railMesh == null)
        {
            railMesh = new Mesh();
            railMesh.name = "RailTubeMesh";
        }
        else
        {
            railMesh.Clear();
        }

        railMesh.SetVertices(vertices);
        railMesh.SetTriangles(triangles, 0);
        railMesh.SetUVs(0, uvs);
        railMesh.RecalculateNormals();
        railMesh.RecalculateBounds();

        meshFilter.sharedMesh = railMesh;
    }

    // --- 省略：補間関数群（元のままでOK） ---
    public Vector3 GetPointOnSpline(float t)
    {
        if (segments == null || segments.Count == 0) return Vector3.zero;
        if (segments.Count == 1) return segments[0].point.localPosition;

        int segCount = loop ? segments.Count : segments.Count - 1;
        segCount = Mathf.Max(1, segCount);

        float scaled = t * segCount;
        int seg = Mathf.FloorToInt(Mathf.Clamp(scaled, 0, segCount - 1));
        float localT = scaled - seg;

        RailType type = segments[seg].nextType;
        if (type == RailType.Linear)
            return GetLinearPosition(seg, localT);
        else
            return GetCatmullRomPosition(seg, localT);
    }

    public Vector3 GetWorldPointOnSpline(float t)
    {
        return transform.TransformPoint(GetPointOnSpline(t));
    }

    public Quaternion GetRotationOnSpline(float t)
    {
        if (segments == null || segments.Count < 2)
            return transform.rotation;

        int segCount = loop ? segments.Count : segments.Count - 1;
        float scaled = t * segCount;
        int seg = Mathf.FloorToInt(Mathf.Clamp(scaled, 0, segCount - 1));
        float localT = scaled - seg;

        var a = segments[seg].point.localRotation;
        var b = segments[(seg + 1) % segments.Count].point.localRotation;

        RailType type = segments[seg].nextType;

        if (type == RailType.Linear)
            return Quaternion.Lerp(a, b, localT);
        else
            return Quaternion.Slerp(a, b, localT);
    }

    Vector3 GetLinearPosition(int segment, float t)
    {
        int count = segments.Count;
        int p1 = segment;
        int p2 = (segment + 1) % count;
        if (!loop)
            p2 = Mathf.Clamp(p2, 0, count - 1);
        return Vector3.Lerp(segments[p1].point.localPosition, segments[p2].point.localPosition, t);
    }

    Vector3 GetCatmullRomPosition(int segment, float t)
    {
        int count = segments.Count;
        int p1 = segment;
        int p2 = (segment + 1) % count;
        int p0 = (p1 - 1 + count) % count;
        int p3 = (p2 + 1) % count;

        if (!loop)
        {
            p0 = Mathf.Clamp(p1 - 1, 0, count - 1);
            p3 = Mathf.Clamp(p2 + 1, 0, count - 1);
        }

        Vector3 P0 = segments[p0].point.localPosition;
        Vector3 P1 = segments[p1].point.localPosition;
        Vector3 P2 = segments[p2].point.localPosition;
        Vector3 P3 = segments[p3].point.localPosition;

        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * ((2f * P1) +
            (-P0 + P2) * t +
            (2f * P0 - 5f * P1 + 4f * P2 - P3) * t2 +
            (-P0 + 3f * P1 - 3f * P2 + P3) * t3);
    }
}