using UnityEngine;

[System.Serializable]
public class RailSplineSegment
{
    public Transform point;

    [Tooltip("���̋�Ԃ̕�ԃ^�C�v")]
    public RailSpline.RailType nextType = RailSpline.RailType.Spline;

    [Tooltip("���̋�ԂŎg�p�����]�i��Ȃ�point�̉�]���g���j")]
    public Quaternion rotation = Quaternion.identity;
}
