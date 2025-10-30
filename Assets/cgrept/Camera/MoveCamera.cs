using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("�Ǐ]�Ώہi�v���C���[��Transform���h���b�O�j")]
    public Transform target;

    [Header("�^�[�Q�b�g����̃I�t�Z�b�g�i���[���h��j")]
    public Vector3 offset = new Vector3(0f, 1.8f, -5f); // ���ŏ��Ɏ����l

    [Header("�ǂ������邩�i���t�߂����鍂���j")]
    public float lookHeight = 1.5f;

    void LateUpdate()
    {
        if (!target) return;

        // �ʒu�F�v���C���[�ʒu + �I�t�Z�b�g
        transform.position = target.position + offset;

        // �����F�v���C���[�̓��t��
        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        transform.LookAt(lookPoint);
    }
}