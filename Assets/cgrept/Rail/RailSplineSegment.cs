using UnityEngine;

[System.Serializable]
public class RailSplineSegment
{
    public Transform point;

    [Tooltip("���̋�Ԃ̕�ԃ^�C�v")]
    public RailSpline.RailType nextType = RailSpline.RailType.Spline;

    [Tooltip("���̋�ԂŎg�p�����]�i��Ȃ�point�̉�]���g���j")]
    public Quaternion rotation = Quaternion.identity;

    [Header("�|�C���g�ݒ�")]
    public float targetSpeed = 5f;               // ���̃|�C���g�ʉߎ��̃X�s�[�h
    public bool switchCamera = false;            // �J������؂�ւ��邩
    public Camera newCamera;                     // �؂�ւ���̃J����
    public float cameraHoldTime = 3f;            // ���̃J�������g������
}
