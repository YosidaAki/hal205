using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class RailEffectEmitter : MonoBehaviour
{
    public Transform player;             // �v���C���[�iRailSpline ����ړ�����j
    public float spawnHeightOffset = -0.2f; // ������菭����
    public float curveForce = 3f;        // �������J�[�u�̋���
    public float trailLifetime = 0.4f;   // �c�鎞��

    private TrailRenderer trail;
    private Vector3 velocity;

    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        trail.time = trailLifetime;

        // Trail �̌����ڐݒ�
        trail.startWidth = 0.15f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        trail.material.SetColor("_TintColor", new Color(0f, 1f, 1f, 0.6f)); // �V�A������
    }

    void LateUpdate()
    {
        if (player == null) return;

        // �����ʒu����G�t�F�N�g����
        Vector3 basePos = player.position + Vector3.down * spawnHeightOffset;

        // �ړ������ɉ����ĉ��ɋȂ���
        velocity += Physics.gravity * curveForce * Time.deltaTime;
        basePos += velocity * Time.deltaTime;

        transform.position = basePos;
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }
}

