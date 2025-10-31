using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class RailEffectEmitter : MonoBehaviour
{
    public Transform player;             // プレイヤー（RailSpline 上を移動する）
    public float spawnHeightOffset = -0.2f; // 足元より少し下
    public float curveForce = 3f;        // 放物線カーブの強さ
    public float trailLifetime = 0.4f;   // 残る時間

    private TrailRenderer trail;
    private Vector3 velocity;

    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        trail.time = trailLifetime;

        // Trail の見た目設定
        trail.startWidth = 0.15f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        trail.material.SetColor("_TintColor", new Color(0f, 1f, 1f, 0.6f)); // シアン発光
    }

    void LateUpdate()
    {
        if (player == null) return;

        // 足元位置からエフェクト発生
        Vector3 basePos = player.position + Vector3.down * spawnHeightOffset;

        // 移動方向に応じて下に曲げる
        velocity += Physics.gravity * curveForce * Time.deltaTime;
        basePos += velocity * Time.deltaTime;

        transform.position = basePos;
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }
}

