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
    [Range(0.01f, 10f)]
    public float targetSpeed = 5f;               // このポイント通過時のスピード
    public bool switchCamera = false;
    public Camera targetCamera;
    public float cameraHoldTime = 3f;
}
