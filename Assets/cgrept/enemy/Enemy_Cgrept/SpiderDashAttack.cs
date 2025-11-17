using UnityEngine;
using System.Collections;

public class SpiderDashAttack : MonoBehaviour
{
    public Transform player;
    public float dashSpeed = 10f;        // 突進速度
    public float dashDuration = 0.6f;    // 突進時間
    public float cooldown = 2f;          // クールダウン
    public float stopDistance = 1.5f;    // 近すぎると止める
    public float yawOffset = 180f;       // ← モデルの前方向が-Zなら180、Z+なら0
    public BeamDamage beam;

    public float damage = 20f;  // ← これを足せば damage が使えるようになる
    private bool isDashing = false;
    private bool onCooldown = false;
    private BoxCollider boxCol;
    private Vector3 originalSize;
    private Vector3 originalCenter;

    [Header("ノックバック設定")]
    public float knockbackPower = 10f;      // 強さ
    public float knockbackUpRatio = 1.0f; // 上方向の割合（0〜1）
    public ForceMode knockbackMode = ForceMode.VelocityChange;

    // 突進中に使う拡大サイズ
    public float dashSizeMultiplierX = 8.0f; // X方向（左右幅）
    public float dashSizeMultiplierY = 10.0f; // 好きに変更OK
    void Start()
    {
        boxCol = GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            originalSize = boxCol.size;
            originalCenter = boxCol.center;
        }
    }

    void Update()
    {
        if (player == null) return;
        if (beam != null && (beam.isCharging || beam.isFiring)) return;
        if (onCooldown || isDashing) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < 30f && distance > stopDistance)
        {
            StartCoroutine(DashAttack());
        }
    }

    IEnumerator DashAttack()
    {
        isDashing = true;
        onCooldown = true;

        // 隕石クールダウン
        EnemyTurret meteor = GetComponent<EnemyTurret>();
        if (meteor != null)
            meteor.ForceMeteorCooldown();

        // 方向計算
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        dir.Normalize();

        // 向き
        transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0, yawOffset, 0);

        SpiderFollow follow = GetComponent<SpiderFollow>();
        if (follow != null)
            follow.enabled = false;

        // ★ 突進前のY高さを保存
        float fixedY = transform.position.y;

        // ★ timer はここで宣言
        float timer = 0f;

        // 突進ループ（XZのみ動かす）
        while (timer < dashDuration)
        {
            timer += Time.deltaTime;

            Vector3 nextPos = transform.position + dir * dashSpeed * Time.deltaTime;

            // ★ 地面に固定
            nextPos.y = fixedY;

            transform.position = nextPos;

            yield return null;
        }

        // 突進終了
        if (follow != null)
            follow.enabled = true;

        isDashing = false;
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }
    // ダッシュ中かどうか他スクリプトから読めるように
    public bool IsDashing => isDashing;

    // SpiderFollowなど外部から呼び出すためのラッパー
    public IEnumerator StartDashExternal()
    {
        if (isDashing || onCooldown || player == null)
            yield break;

        // 既存の DashAttack() を呼ぶ
        yield return StartCoroutine(DashAttack());
    }
    public void StartDash()
    {
        if (!isDashing && !onCooldown && player != null)
            StartCoroutine(DashAttack());
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!isDashing) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            // ★ダメージ処理
            var ph = collision.gameObject.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.TakeDamage(damage);

            // ★ ノックバック処理（Inspector調整対応）
            Rigidbody prb = collision.gameObject.GetComponent<Rigidbody>();
            if (prb != null)
            {
                // 基本方向（蜘蛛→プレイヤー）
                Vector3 knockDir = (collision.transform.position - transform.position).normalized;

                // 上方向の比率を Inspector で指定する
                knockDir.y = knockbackUpRatio;

                // Inspectorで調整可能な強さ＋ForceMode
                prb.AddForce(knockDir * knockbackPower, knockbackMode);
            }

            // ダッシュ停止
            isDashing = false;
        }
    }

}
