using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class RailSpline : MonoBehaviour
{
    public enum RailType { Spline, Linear }

    public bool loop = false;
    public List<RailSplineSegment> segments = new List<RailSplineSegment>();

    // --------------------------
    // ローカル座標上の位置補間
    // --------------------------
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

    // --------------------------
    // ワールド座標を返す便利関数
    // --------------------------
    public Vector3 GetWorldPointOnSpline(float t)
    {
        return transform.TransformPoint(GetPointOnSpline(t));
    }

    // --------------------------
    // 回転補間（ローカル）
    // --------------------------
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

    // --------------------------
    // 補助関数（ローカル）
    // --------------------------
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

    // --------------------------
    // Gizmo描画
    // --------------------------
    void OnDrawGizmos()
    {
        if (segments == null || segments.Count < 2) return;

        const int steps = 80;
        Vector3 prev = GetWorldPointOnSpline(0f);

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 p = GetWorldPointOnSpline(t);
            Gizmos.color = GetColorForSegment(t);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }

        foreach (var seg in segments)
        {
            if (seg?.point == null) continue;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(seg.point.position, 0.05f);

            Gizmos.color = Color.green;
            Vector3 dir = seg.point.forward * 0.3f;
            Gizmos.DrawLine(seg.point.position, seg.point.position + dir);
        }
    }

    Color GetColorForSegment(float t)
    {
        int segCount = loop ? segments.Count : segments.Count - 1;
        int seg = Mathf.FloorToInt(t * segCount);
        seg = Mathf.Clamp(seg, 0, segments.Count - 1);
        var type = segments[seg].nextType;
        return type == RailType.Spline ? Color.cyan : Color.red;
    }
}
