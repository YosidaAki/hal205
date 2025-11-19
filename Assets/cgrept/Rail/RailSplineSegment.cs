using UnityEngine;

[System.Serializable]
public class RailSplineSegment
{
    public Transform point;

    [Tooltip("この区間の補間タイプ")]
    public RailSpline.RailType nextType = RailSpline.RailType.Spline;

    [Tooltip("この区間で使用する回転（空ならpointの回転を使う）")]
    public Quaternion rotation = Quaternion.identity;

    [Header("ポイント設定")]
    [Range(0.01f, 1f)]
    public float targetSpeed = 0.5f;               // このポイント通過時のスピード
    public bool switchCamera = false;
    public Camera targetCamera;
    public float cameraHoldTime = 3f;

    // ============================================
    // ★★ ここから追加：変形制御機能 ★★
    // ============================================
    [Header("変形（Scale/Rotation）制御")]
    public bool applyTransform = false;

    public enum TransformMode
    {
        AtPointB,       // B地点に到達した瞬間に変形を適用
        InterpolateAB   // A→B の区間で補間して変形
    }
    public TransformMode transformMode = TransformMode.InterpolateAB;

    [Header("スケール")]
    public Vector3 startScale = Vector3.one;
    public Vector3 endScale = Vector3.one;

    [Header("回転（オイラー角）")]
    public Vector3 startRotationEuler = Vector3.zero;
    public Vector3 endRotationEuler = Vector3.zero;
}
