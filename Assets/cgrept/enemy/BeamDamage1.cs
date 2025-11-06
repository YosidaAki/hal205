using UnityEngine;
using System.Collections;

public class BeamDamage : MonoBehaviour
{
    public Transform firePoint;
    public Transform player;
    public LineRenderer lineRenderer;
    public Material beamMaterial;
    public GameObject chargeSpherePrefab;

    [Header("ビーム設定")]
    public float beamLength = 60f;
    public float beamDuration = 3f;
    public float damage = 10f;
    public float beamCooldown = 10f;
    public float chargeTime = 1.5f;
    public float damageInterval = 0.2f;
    public float drawSpeed = 80f; // ← ビーム伸びる速度

    private float lastFireTime = -Mathf.Infinity;
    private bool isCharging = false;
    private bool isFiring = false;
    private GameObject chargeSphereInstance;

    [Header("チャージ設定")]
    public float chargeOffsetDistance = 1.5f;

    [Header("ビーム見た目設定")]
    public float beamStartWidth = 0.2f;
    public float beamEndWidth = 0.2f;


    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = firePoint.gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.material = beamMaterial;

        // ★ 幅カーブで明確に差をつける（start/end より確実）
        var curve = new AnimationCurve();
        curve.AddKey(0f, beamStartWidth); // 0=根元
        curve.AddKey(1f, beamEndWidth);   // 1=先端
        lineRenderer.widthCurve = curve;
        lineRenderer.widthMultiplier = 1f;

        // 仕上げ（見た目の改善）
        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 4;
        lineRenderer.alignment = LineAlignment.View; // カメラに正面向き
        lineRenderer.enabled = false;
    }

    public void FireBeam()
    {
        if (isCharging || isFiring) return;
        if (Time.time - lastFireTime < beamCooldown) return;

        StartCoroutine(FireBeamWithCharge());
    }

    private IEnumerator FireBeamWithCharge()
    {
        isCharging = true;
        lastFireTime = Time.time;

        // 発射方向を固定
        Vector3 lockedDir = (player.position - firePoint.position).normalized;

        // チャージ球を前方に出す
        Vector3 spawnPos = firePoint.position + lockedDir * chargeOffsetDistance;
        chargeSphereInstance = Instantiate(chargeSpherePrefab, spawnPos, Quaternion.LookRotation(lockedDir));
        chargeSphereInstance.transform.localScale = Vector3.zero;

        // チャージ演出
        float t = 0f;
        while (t < chargeTime)
        {
            t += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 2f, t / chargeTime);
            chargeSphereInstance.transform.localScale = Vector3.one * scale;
            chargeSphereInstance.transform.position = firePoint.position + lockedDir * chargeOffsetDistance;
            yield return null;
        }

        Destroy(chargeSphereInstance);
        isCharging = false;

        // ==== ビーム発射 ====
        isFiring = true;
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, firePoint.position);

        float beamTimer = 0f;
        float damageTimer = 0f;

        while (beamTimer < beamDuration)
            while (beamTimer < beamDuration)
            {
                if (firePoint == null || player == null) break;

                beamTimer += Time.deltaTime;
                damageTimer += Time.deltaTime;

                // ビームの長さ更新（そのまま）
                float currentLength = Mathf.Min(beamLength, beamTimer * drawSpeed);
                Vector3 endPos = firePoint.position + lockedDir * currentLength;

                // ダメージ判定
                if (Physics.Raycast(firePoint.position, lockedDir, out RaycastHit hit, currentLength))
                {
                    endPos = hit.point;

                    if (damageTimer >= damageInterval && hit.collider.CompareTag("Player"))
                    {
                        PlayerHealth ph = hit.collider.GetComponent<PlayerHealth>();
                        if (ph != null) ph.TakeDamage(damage);
                        damageTimer = 0f;
                    }
                }

                // 🎯 太さを時間経過で補間
                float progress = beamTimer / beamDuration;

                // 例：最初細く → 中盤太く → 終盤また細く
                float width = Mathf.Lerp(0.05f, 1.2f, Mathf.Pow(Mathf.Sin(progress * Mathf.PI), 1.2f));
                lineRenderer.startWidth = width;
                lineRenderer.endWidth = width * 0.8f;

                // 描画更新
                lineRenderer.SetPosition(0, firePoint.position);
                lineRenderer.SetPosition(1, endPos);

                yield return null;
            }

        if (lineRenderer != null)
            lineRenderer.enabled = false;

        isFiring = false;
    }
}
