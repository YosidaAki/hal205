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
    public float targetSpeed = 5f;               // このポイント通過時のスピード
    public bool switchCamera = false;            // カメラを切り替えるか
    public Camera newCamera;                     // 切り替え先のカメラ
    public float cameraHoldTime = 3f;            // このカメラを使う時間
}
