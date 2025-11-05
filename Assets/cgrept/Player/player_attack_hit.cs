using UnityEngine;
using System.Collections;

public class player_attack_hit : MonoBehaviour
{
    [Header("攻撃判定（子オブジェクトのColliderを使用）")]
    [SerializeField] private Collider hitbox;

    [Header("攻撃力（基本値）")]
    public float attackPower = 10f;

    [Header("デバッグ出力")]
    public bool showDebug = true;

    // 攻撃を管理している player_attack を参照
    [HideInInspector] public player_attack attackSource;

    bool isActive = false;

    void Start()
    {
        if (hitbox == null)
            hitbox = GetComponentInChildren<Collider>();

        if (hitbox != null)
        {
            hitbox.enabled = false;
            hitbox.isTrigger = true;
        }

        if (attackSource == null)
            attackSource = GetComponentInParent<player_attack>();
    }

    // ========================
    // 攻撃判定 ON（遅延付き）
    // ========================
    public void EnableHitbox()
    {
        if (hitbox == null) return;
        StartCoroutine(EnableAfterDelay());
    }

    IEnumerator EnableAfterDelay()
    {
        yield return null; // 1フレーム待つ（Animator遷移と競合回避）
        hitbox.enabled = true;
        isActive = true;
        if (showDebug)
            Debug.Log("[player_attack_hit] 攻撃判定 ON");
    }

    // ========================
    // 攻撃判定 OFF
    // ========================
    public void DisableHitbox()
    {
        if (hitbox == null) return;
        hitbox.enabled = false;
        isActive = false;
        if (showDebug)
            Debug.Log("[player_attack_hit] 攻撃判定 OFF");
    }

    // ========================
    // 衝突判定
    // ========================
    void OnTriggerEnter(Collider other)
    {
        if (!hitbox.enabled)
        {
            if (showDebug)
                Debug.Log($"[player_attack_hit] スキップ(hitbox無効) other={other.name}");
            return;
        }

        if (!isActive)
        {
            if (showDebug)
                Debug.Log($"[player_attack_hit] OnTriggerEnter - 無効中 other={other.name}");
            return;
        }

        // 攻撃段階（0,1,2...）
        int attackIndex = attackSource != null ? attackSource.GetCurrentAttackIndex() : 0;
        Vector3 hitPos = other.ClosestPoint(transform.position);

        // 対象のダメージ受け取り側を探す
        var damageable = other.GetComponent<IHitReceiver>();
        if (damageable != null)
        {
            if (showDebug)
                Debug.Log($"[player_attack_hit] Hit! 対象:{other.name} 段階:{attackIndex + 1}");

            damageable.OnHit(attackPower, hitPos, attackIndex);

            // 1ヒット制限
            DisableHitbox();
        }
        else if (showDebug)
        {
            Debug.Log($"[player_attack_hit] {other.name} に IHitReceiver が未実装");
        }
    }
}
