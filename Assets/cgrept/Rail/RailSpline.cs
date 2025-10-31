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
    public float radius = 0.1f;               // チューブの半径
    [Range(3, 32)]
    public int radialSegments = 12;           // 円の分割数
    [Range(10, 200)]
    public int lengthSegments = 80;           // レールの長さ方向の分割数
    public Color railColor = new Color(0f, 1f, 1f, 0.3f); // 半透明シアン
    public bool doubleSided = false;

    [Header("エフェクト設定")]
    public bool animateTexture = true;                  // テクスチャを動かす
    public Vector2 textureScrollSpeed = new Vector2(0f, 1f); // 縦方向に流す
    public bool glowEffect = true;                      // 光らせる
    [ColorUsage(true, true)]
    public Color emissionColor = new Color(0f, 1f, 1f, 1f); // 発光色

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

        // テクスチャアニメーション
        if (animateTexture && railMaterial != null)
        {
            textureOffset += textureScrollSpeed * Time.deltaTime;
            railMaterial.mainTextureOffset = textureOffset;
        }

        // 発光設定
        if (glowEffect && railMaterial != null)
        {
            railMaterial.EnableKeyword("_EMISSION");
            railMaterial.SetColor("_EmissionColor", emissionColor * 2f); // 光を強調
        }
        else if (railMaterial != null)
        {
            railMaterial.DisableKeyword("_EMISSION");
        }

        // 色更新
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
            railMaterial = new Material(Shader.Find("Standard"));
            railMaterial.color = railColor;

            // 透明設定
            railMaterial.SetFloat("_Mode", 3);
            railMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            railMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            railMaterial.SetInt("_ZWrite", 0);
            railMaterial.DisableKeyword("_ALPHATEST_ON");
            railMaterial.EnableKeyword("_ALPHABLEND_ON");
            railMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            railMaterial.renderQueue = 3000;

            // Emission 有効化
            railMaterial.EnableKeyword("_EMISSION");
            railMaterial.SetColor("_EmissionColor", emissionColor);

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

    // ============================================================
    // 位置・回転補間関数（元のコードと同じ）
    // ============================================================
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

        Vector3 result = 0.5f * ((2f * P1) +
            (-P0 + P2) * t +
            (2f * P0 - 5f * P1 + 4f * P2 - P3) * t2 +
            (-P0 + 3f * P1 - 3f * P2 + P3) * t3);
        return result;
    }

    public Vector3 GetWorldPointOnSegment(int segIndex, float t)
    {
        if (segments == null || segments.Count < 2) return transform.position;
        t = Mathf.Clamp01(t);

        var p1 = segments[segIndex].point.localPosition;
        var p2 = segments[(segIndex + 1) % segments.Count].point.localPosition;
        var pos = Vector3.Lerp(p1, p2, t);
        return transform.TransformPoint(pos);
    }

    public float GetSegmentWorldLength(int segIndex)
    {
        if (segments == null || segments.Count < 2) return 1f;
        Vector3 a = segments[segIndex].point.position;
        Vector3 b = segments[(segIndex + 1) % segments.Count].point.position;
        return Vector3.Distance(a, b);
    }
}

