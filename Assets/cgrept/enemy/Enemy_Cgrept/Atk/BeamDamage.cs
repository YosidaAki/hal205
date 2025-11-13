using UnityEngine;
using System.Collections;

/// <summary>
/// ボスのビーム攻撃を制御するスクリプト。
/// ・チャージしてから発射
/// ・Raycastでプレイヤーにヒット判定
/// ・ヒットしたらCanvas上のhpbarを通してダメージを与える
/// ・当たり位置を黄色／赤のGizmo球で可視化
/// </summary>
public class BeamDamage : MonoBehaviour
{
    [Header("=== 参照設定 ===")]
    public Transform firePoint;           // ビームの起点（ボスの武器など）
    public Transform player;              // プレイヤーのTransform
    public LineRenderer lineRenderer;     // ビーム描画用
    public Material beamMaterial;         // ビームのマテリアル
    public GameObject chargeSpherePrefab; // チャージ演出用プレハブ

    [Header("=== ビーム設定 ===")]
    public float beamLength = 60f;        // 最大到達距離
    public float beamDuration = 3f;       // 維持時間
    public float damage = 10f;            // 1回のダメージ量
    public float beamCooldown = 10f;      // 再発射までの間隔
    public float chargeTime = 1.5f;       // チャージ時間
    public float damageInterval = 0.2f;   // ダメージ間隔
    public float drawSpeed = 80f;         // 伸びる速度

    // 内部管理
    private float lastFireTime = -Mathf.Infinity;
    private bool isCharging = false;
    private bool isFiring = false;
    private GameObject chargeSphereInstance;

    [Header("=== チャージ設定 ===")]
    public float chargeOffsetDistance = 1.5f;

    [Header("=== ビーム見た目設定 ===")]
    public float beamStartWidth = 0.2f;
    public float beamEndWidth = 0.2f;

    [Header("=== デバッグ設定 ===")]
    public bool debugMode = true;
    public Color rayColor = Color.red;
    public bool showHitPointGizmo = true;

    // デバッグ変数
    private Vector3 lastHitPoint = Vector3.zero;
    private bool hitSomething = false;
    private bool hitPlayer = false;

    // ==============================================================
    // 初期化
    // ==============================================================
    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = firePoint.gameObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.material = beamMaterial;

        // 幅をカーブで設定（根元～先端）
        var curve = new AnimationCurve();
        curve.AddKey(0f, beamStartWidth);
        curve.AddKey(1f, beamEndWidth);
        lineRenderer.widthCurve = curve;
        lineRenderer.widthMultiplier = 1f;

        // 見た目補正
        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 4;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.enabled = false;
    }

    // ==============================================================
    // 外部から発射命令を受ける関数（例：ボスAIから呼ばれる）
    // ==============================================================
    public void FireBeam()
    {
        if (isCharging || isFiring) return;
        if (Time.time - lastFireTime < beamCooldown) return;

        StartCoroutine(FireBeamWithCharge());
    }

    // ==============================================================
    // チャージ → 発射 → 判定
    // ==============================================================
    private IEnumerator FireBeamWithCharge()
    {
        // === チャージ開始 ===
        isCharging = true;
        lastFireTime = Time.time;

        Vector3 lockedDir = (player.position - firePoint.position).normalized;

        // チャージ球生成
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

        // チャージ終了
        Destroy(chargeSphereInstance);
        isCharging = false;

        // === ビーム発射 ===
        isFiring = true;
        lineRenderer.enabled = true;

        float beamTimer = 0f;
        float damageTimer = 0f;

        // ループ：ビームが続く間だけ処理
        while (beamTimer < beamDuration)
        {
            if (firePoint == null || player == null) break;

            beamTimer += Time.deltaTime;
            damageTimer += Time.deltaTime;

            // ビームの伸び具合を計算
            float currentLength = Mathf.Min(beamLength, beamTimer * drawSpeed);
            Vector3 endPos = firePoint.position + lockedDir * currentLength;

            hitSomething = false;
            hitPlayer = false;

            // === Raycast判定 ===
            if (Physics.Raycast(firePoint.position, lockedDir, out RaycastHit hit, currentLength))
            {
                hitSomething = true;
                endPos = hit.point;
                lastHitPoint = hit.point;

                // 一定間隔でのみダメージ処理
                if (damageTimer >= damageInterval)
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        hitPlayer = true;

                        // ✅ Canvas上の hpbar に直接アクセス
                        if (hpbar.Instance != null)
                        {
                            hpbar.Instance.TakeDamageFromExternal(damage);
                            if (debugMode)
                                Debug.Log($"[BeamDamage] Player hit! Damage: {damage}, Time: {Time.time:F2}");
                        }
                        else
                        {
                            Debug.LogWarning("[BeamDamage] Player hit but hpbar.Instance not found! Canvasにhpbarが存在しますか？");
                        }
                    }
                    else if (debugMode)
                    {
                        Debug.Log($"[BeamDamage] Hit object: {hit.collider.name}");
                    }

                    damageTimer = 0f;
                }
            }

            // === ビームの太さ補間 ===
            float progress = beamTimer / beamDuration;
            float width = Mathf.Lerp(0.05f, 1.2f, Mathf.Pow(Mathf.Sin(progress * Mathf.PI), 1.2f));
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width * 0.8f;

            // LineRenderer更新
            lineRenderer.SetPosition(0, firePoint.position);
            lineRenderer.SetPosition(1, endPos);

            // デバッグ用Ray
            if (debugMode)
                Debug.DrawRay(firePoint.position, lockedDir * currentLength, rayColor);

            yield return null;
        }

        // === 発射終了 ===
        lineRenderer.enabled = false;
        isFiring = false;
    }

    // ==============================================================
    // Sceneビューでヒット位置を可視化
    // ==============================================================
    private void OnDrawGizmos()
    {
        if (debugMode && showHitPointGizmo && hitSomething)
        {
            Gizmos.color = hitPlayer ? Color.red : Color.yellow;
            Gizmos.DrawSphere(lastHitPoint, 0.25f);
        }
    }
}
