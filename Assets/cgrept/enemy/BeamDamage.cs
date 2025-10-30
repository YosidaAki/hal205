using UnityEngine;
using System.Collections;

public class BeamDamage : MonoBehaviour
{
    public Transform firePoint;
    public Transform player;
    public LineRenderer lineRenderer;
    public Material beamMaterial;
    public GameObject chargeSpherePrefab; // ← スフィア型プレハブを指定

    [Header("ビーム設定")]
    public float beamLength = 60f;
    public float beamDuration = 3f;
    public float damage = 10f;
    public float beamCooldown = 10f;
    public float chargeTime = 1.5f; // チャージにかける時間
    public float damageInterval = 0.2f;

    private float lastFireTime = -Mathf.Infinity;
    private bool isCharging = false;
    private bool isFiring = false;
    private GameObject chargeSphereInstance;

    [Header("チャージ設定")]
    public float chargeOffsetDistance = 1.5f; // チャージ球を前方に出す距離

    void Start()
    {
        // LineRenderer の初期化
        if (lineRenderer == null)
        {
            lineRenderer = firePoint.gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.3f;
            lineRenderer.endWidth = 0.3f;
            lineRenderer.material = beamMaterial;
            lineRenderer.enabled = false;
        }
    }

    public void FireBeam()
    {
        if (isCharging || isFiring) return;
        if (Time.time - lastFireTime < beamCooldown) return;

        StartCoroutine(FireBeamWithCharge());
    }

    // メンバは不要（ローカルでOK）

    private IEnumerator FireBeamWithCharge()
    {
        isCharging = true;
        lastFireTime = Time.time;

        // ★ 発射方向をチャージ開始時にロック（どちらでもOK：統一して使う）
        // 1) プレイヤー方向で固定したい場合：
        Vector3 lockedDir = (player.position - firePoint.position).normalized;
        // 2) 砲口の向きで固定したい場合はこっち：
        // Vector3 lockedDir = firePoint.forward;

        // ★ チャージ球：発射方向に一定距離オフセット＆向きも固定
        float offset = chargeOffsetDistance; // 例: 1.5f
        Vector3 spawnPos = firePoint.position + lockedDir * offset;
        chargeSphereInstance = Instantiate(chargeSpherePrefab, spawnPos, Quaternion.LookRotation(lockedDir));
        chargeSphereInstance.transform.localScale = Vector3.zero;

        // チャージ演出：位置は "firePoint の現在位置 + lockedDir * offset"（角度は locked）
        float t = 0f;
        while (t < chargeTime)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(0f, 2f, t / chargeTime); // 膨らむ
            chargeSphereInstance.transform.localScale = Vector3.one * s;

            // firePoint が動いても、角度は lockedDir のまま・位置だけ追従
            chargeSphereInstance.transform.position = firePoint.position + lockedDir * offset;
            // 角度は毎フレーム再設定しても同じ（lockedDir）なのでズレない
            chargeSphereInstance.transform.rotation = Quaternion.LookRotation(lockedDir);

            yield return null;
        }

        Destroy(chargeSphereInstance);
        isCharging = false;

        // ★ ビームも lockedDir を使用（チャージ球と完全一致）
        isFiring = true;
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, firePoint.position);

        float beamTimer = 0f;
        float damageTimer = 0f;

        while (beamTimer < beamDuration)
        {
            beamTimer += Time.deltaTime;
            damageTimer += Time.deltaTime;

            Vector3 endPos = firePoint.position + lockedDir * beamLength;
            if (Physics.Raycast(firePoint.position, lockedDir, out RaycastHit hit, beamLength))
            {
                endPos = hit.point;
                if (damageTimer >= damageInterval && hit.collider.CompareTag("Player"))
                {
                    var ph = hit.collider.GetComponent<PlayerHealth>();
                    if (ph != null) ph.TakeDamage(damage);
                    damageTimer = 0f;
                }
            }

            // 発射中、始点は常に現在の firePoint 位置に追従しつつ、方向は lockedDir で固定
            lineRenderer.SetPosition(0, firePoint.position);
            lineRenderer.SetPosition(1, endPos);

            yield return null;
        }

        lineRenderer.enabled = false;
        isFiring = false;
    }
}
