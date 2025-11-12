using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RailSpline : MonoBehaviour
{
    public enum RailType { Spline, Linear }

    [Header("レール設定")]
    public bool loop = false;

    [Header("アニメーション設定")]
    [Range(0f, 1f)] public float visibleLength = 0f;
    public bool animateGrow = false;
    public float growSpeed = 0.3f;

    [Header("レールポイント")]
    public List<RailSplineSegment> segments = new List<RailSplineSegment>();

    [Header("チューブ描画設定")]
    [Range(0.01f, 2f)] public float radius = 0.3f;
    [Range(3, 64)] public int radialSegments = 24;
    [Range(10, 400)] public int lengthSegments = 120;
    public Color railColor = new Color(0f, 1f, 1f, 1f);

    [Header("エフェクト設定")]
    public bool animateTexture = true;
    public Vector2 textureScrollSpeed = new Vector2(0f, 1f);
    public bool glowEffect = true;
    [ColorUsage(true, true)] public Color emissionColor = new Color(0.2f, 0.8f, 1f, 1f);

    [Header("マーカー設定")]
    [Range(1.0f, 10.0f)] public float siz = 1.5f;
    public bool markerGlow = true;

    [Header("イベント設定")]
    public UnityEvent onRailFullyAppeared;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh railMesh;
    private Material railMaterial;
    private Vector2 textureOffset;
    private bool isAppearing = false;

    void Awake()
    {
        SetupComponents();
        GenerateRailMesh();
        GenerateEndpointMarkers();
    }

    void OnValidate()
    {
        SetupComponents();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                GenerateRailMesh();
                GenerateEndpointMarkers();
            }
        };
#else
        GenerateRailMesh();
        GenerateEndpointMarkers();
#endif
    }

    void Update()
    {
        if (meshFilter == null || meshRenderer == null)
        {
            SetupComponents();
            return;
        }

        // --- レール出現アニメーション ---
        if (animateGrow && isAppearing)
        {
            visibleLength = Mathf.Clamp01(visibleLength + growSpeed * Time.deltaTime);
            if (visibleLength >= 1f)
            {
                isAppearing = false;
                onRailFullyAppeared?.Invoke();
            }
        }

        GenerateRailMesh();

        // --- テクスチャスクロール ---
        if (animateTexture && railMaterial != null)
        {
            textureOffset += textureScrollSpeed * Time.deltaTime;
            railMaterial.mainTextureOffset = textureOffset;
        }

        // --- 発光設定 ---
        if (glowEffect && railMaterial != null)
        {
            railMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            railMaterial.EnableKeyword("_EMISSION");
            railMaterial.SetColor("_EmissionColor", emissionColor * 0.8f);
        }

        if (railMaterial != null)
            railMaterial.color = railColor;

        // --- マーカー追従＆サイズ変化 ---
        Transform startMarker = transform.Find("StartMarker");
        Transform endMarker = transform.Find("EndMarker");

        float scaleFactor = Mathf.Clamp01(visibleLength);
        float currentScale = radius * siz * scaleFactor;

        if (startMarker != null)
        {
            startMarker.position = GetWorldPointOnSpline(0f);
            startMarker.localScale = Vector3.one * currentScale;

            if (markerGlow)
            {
                var rend = startMarker.GetComponent<Renderer>();
                if (rend && railMaterial != null)
                    rend.sharedMaterial.SetColor("_EmissionColor", emissionColor * (0.5f + 0.5f * scaleFactor));
            }
        }

        if (endMarker != null)
        {
            endMarker.position = GetWorldPointOnSpline(visibleLength);
            endMarker.localScale = Vector3.one * currentScale;

            if (markerGlow)
            {
                var rend = endMarker.GetComponent<Renderer>();
                if (rend && railMaterial != null)
                    rend.sharedMaterial.SetColor("_EmissionColor", emissionColor * (0.5f + 0.5f * scaleFactor));
            }
        }
    }

    void SetupComponents()
    {
        if (!meshFilter)
            meshFilter = GetComponent<MeshFilter>();
        if (!meshRenderer)
            meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer.sharedMaterial == null)
        {
            railMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            railMaterial.SetColor("_BaseColor", railColor);
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

        if (visibleLength <= 0f)
        {
            if (meshFilter.sharedMesh) meshFilter.sharedMesh.Clear();
            return;
        }

        int visibleSegs = Mathf.Max(1, Mathf.RoundToInt(lengthSegments * visibleLength));
        List<Vector3> centers = new List<Vector3>();
        for (int i = 0; i <= visibleSegs; i++)
        {
            float t = (float)i / lengthSegments;
            centers.Add(GetWorldPointOnSpline(t));
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        Vector3 prevForward = (centers[1] - centers[0]).normalized;
        Vector3 prevUp = Vector3.up;
        if (Vector3.Dot(prevForward, prevUp) > 0.9f) prevUp = Vector3.forward;

        for (int i = 0; i < centers.Count; i++)
        {
            Vector3 forward;
            if (i < centers.Count - 1)
                forward = (centers[i + 1] - centers[i]).normalized;
            else if (loop)
                forward = (centers[0] - centers[i]).normalized;
            else
                forward = prevForward;

            Vector3 rotationAxis = Vector3.Cross(prevForward, forward);
            float angle = Mathf.Asin(rotationAxis.magnitude);
            if (rotationAxis.sqrMagnitude > 1e-6f)
            {
                rotationAxis.Normalize();
                Quaternion deltaRot = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, rotationAxis);
                prevUp = deltaRot * prevUp;
            }

            Vector3 right = Vector3.Cross(prevUp, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right);

            for (int j = 0; j <= radialSegments; j++)
            {
                float angleRad = (float)j / radialSegments * Mathf.PI * 2f;
                Vector3 offset = right * Mathf.Cos(angleRad) * radius + up * Mathf.Sin(angleRad) * radius;
                vertices.Add(transform.InverseTransformPoint(centers[i] + offset));
                uvs.Add(new Vector2((float)j / radialSegments, (float)i / lengthSegments));
            }

            prevForward = forward;
        }

        int vertsPerRing = radialSegments + 1;
        for (int i = 0; i < centers.Count - 1; i++)
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
            }
        }

        if (railMesh == null)
            railMesh = new Mesh() { name = "RailTubeMesh" };
        else
            railMesh.Clear();

        railMesh.SetVertices(vertices);
        railMesh.SetTriangles(triangles, 0);
        railMesh.SetUVs(0, uvs);
        railMesh.RecalculateNormals();
        railMesh.RecalculateTangents();
        railMesh.RecalculateBounds();

        meshFilter.sharedMesh = railMesh;
    }

    void GenerateEndpointMarkers()
    {
        if (transform.Find("StartMarker") == null)
        {
            GameObject startMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            startMarker.name = "StartMarker";
            startMarker.transform.SetParent(transform);
            startMarker.transform.localScale = Vector3.one * radius * siz;
            if (railMaterial != null)
                startMarker.GetComponent<Renderer>().sharedMaterial = railMaterial;
        }

        if (transform.Find("EndMarker") == null)
        {
            GameObject endMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            endMarker.name = "EndMarker";
            endMarker.transform.SetParent(transform);
            endMarker.transform.localScale = Vector3.one * radius * siz;
            if (railMaterial != null)
                endMarker.GetComponent<Renderer>().sharedMaterial = railMaterial;
        }
    }

    public void TriggerRailAppearance()
    {
        if (isAppearing) return;
        visibleLength = 0f;
        isAppearing = true;
        animateGrow = true;
    }

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
        return (type == RailType.Linear)
            ? GetLinearPosition(seg, localT)
            : GetCatmullRomPosition(seg, localT);
    }

    public Vector3 GetWorldPointOnSpline(float t)
    {
        return transform.TransformPoint(GetPointOnSpline(t));
    }

    Vector3 GetLinearPosition(int segment, float t)
    {
        int count = segments.Count;
        int p1 = segment;
        int p2 = (segment + 1) % count;
        if (!loop) p2 = Mathf.Clamp(p2, 0, count - 1);
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

        return 0.5f * ((2f * P1)
            + (-P0 + P2) * t
            + (2f * P0 - 5f * P1 + 4f * P2 - P3) * t2
            + (-P0 + 3f * P1 - 3f * P2 + P3) * t3);
    }
}
