using UnityEngine;

public class AutoFollowCamera : MonoBehaviour
{
    public Transform target;      // �v���C���[
    public Vector3 offset = new Vector3(0, 3f, -6f);  // ���Έʒu
    public float followSpeed = 5f;   // �ʒu�̒Ǐ]�X�s�[�h
    public float rotationSpeed = 5f; // ��]�̒Ǐ]�X�s�[�h

    void LateUpdate()
    {
        if (target == null) return;

        // === �v���C���[�́u�����Ă�������v�ɍ��킹�ăJ������z�u ===
        // �v���C���[�̌�������ɃI�t�Z�b�g����]
        Vector3 desiredPosition = target.position + target.rotation * offset;

        // �X���[�Y�Ɉʒu���ړ�
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

        // �v���C���[�̕��������炩�Ɍ���
        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationSpeed);
    }
}

