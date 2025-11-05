using UnityEngine;

[DisallowMultipleComponent]
public class player_attack_hit : MonoBehaviour
{
    [Header("攻撃判定（子オブジェクトのColliderを指定）")]
    [SerializeField] private Collider hitbox;

    [Header("攻撃力")]
    [Tooltip("攻撃ごとの基本攻撃力（倍率は攻撃段階側で計算）")]
    public float attackPower = 10f;

    [Header("接続スクリプト")]
    [Tooltip("攻撃元プレイヤー（攻撃段階を参照）")]
    [SerializeField] private player_attack attackController;

    [Header("デバッグ設定")]
    [SerializeField] private bool showDebugLog = true;

    void Reset()
    {
        if (hitbox == null)
            hitbox = GetComponentInChildren<Collider>();

        if (attackController == null)
            attackController = GetComponentInParent<player_attack>();
    }

    void Start()
    {
        // キャッシュを一度だけ行う
        if (hitbox == null)
            hitbox = GetComponentInChildren<Collider>();

        if (attackController == null)
            attackController = GetComponentInParent<player_attack>();

        if (hitbox != null)
        {
            hitbox.enabled = false;
            hitbox.isTrigger = true;
        }
        else
        {
            Debug.LogWarning("[player_attack_hit] ヒットボックスが設定されていません。");
        }
    }
    // ====================================
    // 攻撃判定ON（アニメーションから呼ぶ）
    // ====================================
    public void EnableHitbox()
    {
        if (hitbox == null) return;
        hitbox.enabled = true;
        if (showDebugLog)
            Debug.Log("[player_attack_hit] 攻撃判定 ON");
    }

    // ====================================
    // 攻撃判定OFF
    // ====================================
    public void DisableHitbox()
    {
        if (hitbox == null) return;
        hitbox.enabled = false;
        if (showDebugLog)
            Debug.Log("[player_attack_hit] 攻撃判定 OFF");
    }

    // ====================================
    // 当たり判定処理
    // ====================================
    void OnTriggerEnter(Collider other)
    {
        //if (!isActive) return;
        if (attackController == null)
        {
            Debug.LogWarning("[player_attack_hit] attackController が未設定です。");
            return;
        }

        int attackIndex = attackController.GetCurrentAttackIndex();

        // BossPartsManager に対応
        var partsMgr = other.GetComponent<BossPartsManager>();
        if (partsMgr != null)
        {
            partsMgr.OnHit(attackPower, transform.position, attackIndex);
            DisableHitbox();
            return;
        }

        // BossHealth に直接
        var boss = other.GetComponent<BossHealth>();
        if (boss != null)
        {
            boss.TakeDamage(attackPower);
            DisableHitbox();
        }
    }
}
