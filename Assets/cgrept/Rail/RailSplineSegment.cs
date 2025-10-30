using UnityEngine;

[System.Serializable]
public class RailSplineSegment
{
    public Transform point;

    [Tooltip("この区間の補間タイプ")]
    public RailSpline.RailType nextType = RailSpline.RailType.Spline;

    [Tooltip("この区間で使用する回転（空ならpointの回転を使う）")]
    public Quaternion rotation = Quaternion.identity;
}
