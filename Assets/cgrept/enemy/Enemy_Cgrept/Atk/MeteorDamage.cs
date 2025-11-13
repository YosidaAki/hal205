using UnityEngine;

/// <summary>
/// メテオの攻撃判定処理。
/// プレイヤーに衝突したら HP を減らして消滅。
/// </summary>
[RequireComponent(typeof(Collider))]
public class MeteorDamage : MonoBehaviour
{
    [Header("=== メテオ攻撃設定 ===")]
    public float damage = 20f;           // プレイヤーに与えるダメージ
    public float destroyDelay = 0.2f;    // 衝突後に消えるまでの時間
    public GameObject hitEffectPrefab;   // 衝突時のエフェクト（任意）

    [Header("=== 落下中の自動消滅設定 ===")]
    public float lifeTime = 10f;         // 一定時間後に自動削除

    private void Start()
    {
        // 落下中に何も当たらなかった場合でも自動削除
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // プレイヤーに衝突した？
        if (collision.collider.CompareTag("Player"))
        {
            // hpbar 経由でダメージ
            if (hpbar.Instance != null)
            {
                hpbar.Instance.TakeDamageFromExternal(damage);
                Debug.Log($"[MeteorDamage] Player hit! -{damage} HP");
            }
            else
            {
                Debug.LogWarning("[MeteorDamage] hpbar.Instance が存在しません！");
            }
        }

        // エフェクトがある場合は生成
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // メテオ削除
        Destroy(gameObject, destroyDelay);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Trigger Collider 用（Rigidbodyなしメテオにも対応）
        if (other.CompareTag("Player"))
        {
            if (hpbar.Instance != null)
            {
                hpbar.Instance.TakeDamageFromExternal(damage);
                Debug.Log($"[MeteorDamage] Player hit (Trigger)! -{damage} HP");
            }

            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject, destroyDelay);
        }
    }
}
